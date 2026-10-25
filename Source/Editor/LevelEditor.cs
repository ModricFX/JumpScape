using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Editor
{
    public enum EditorResult
    {
        None,
        Back,

        /// <summary>The player wants to try the level; play <see cref="LevelEditor.TestLevel"/>, then come back.</summary>
        Test,
    }

    /// <summary>
    /// Makes and changes levels with the mouse. Pick a tool along the top and click in the level to add things,
    /// or use Select to pick things up, move them, stretch platforms and change them in the bar at the bottom.
    /// Levels are saved as the same JSON files the game reads.
    /// </summary>
    public sealed partial class LevelEditor
    {
        private const int TopBarHeight = 104;
        private const int BottomBarHeight = 60;
        private const int Grid = 10;
        private const float ScrollSpeed = 14f;
        private const float MessageTime = 4f;
        private const int HandleSize = 16;

        private enum Tool
        {
            Select,
            Platform,
            Spikes,
            Door,
            Portal,
            Key,
            Gem,
            PowerUp,
            Ghost,
            Checkpoint,
            Spring,
            Npc,
            Start,
        }

        private static readonly string[] ToolNames = { "Select", "Platform", "Spikes", "Door", "Portal", "Key", "Gem", "Power-up", "Ghost", "Flag", "Spring", "Pip", "Start" };

        /// <summary>The kinds of things in a level. PathEnd is the far end of a moving platform's path.</summary>
        private enum Kind
        {
            None,
            Platform,
            PathEnd,
            Spikes,
            Door,
            Portal,
            Key,
            Gem,
            PowerUp,
            Ghost,
            Checkpoint,
            Spring,
            Npc,
            Start,
        }

        private readonly record struct Thing(Kind Kind, int Index)
        {
            public static readonly Thing Nothing = new Thing(Kind.None, -1);
        }

        private enum Drag
        {
            None,
            Move,
            Resize,
            Draw,
        }

        private readonly PanelMenu _leaveConfirm = new PanelMenu("Leave without saving?", UiDrawing.Highlight, new[] { "Save and leave", "Leave", "Cancel" });

        private LevelDocument _doc;
        private int _area;
        private float _cameraTop;
        private Tool _tool;
        private Thing _selected = Thing.Nothing;
        private Thing _hovered = Thing.Nothing;
        private Area _view;
        private bool _viewStale = true;

        private Drag _drag;
        private Vector2 _dragStart;
        private Vector2 _grabOffset;
        private bool _dragRemembered;

        private string _message;
        private bool _messageIsError;
        private float _messageTimer;
        private float _time;
        private bool _choosingLevel = true;
        private bool _settingsOpen;
        private bool _savedSomething;

        // Sizes of things, measured from the game's own objects once the textures are loaded.
        private static Point DoorSize => new Door(Vector2.Zero, false).Bounds.Size;
        private static Point GhostSize => new Ghost(Vector2.Zero, 1f).Bounds.Size;
        private static Point KeySize => KeyPickup.Size.ToPoint();
        private static Point PlayerSize => new Point((int)(Assets.PlayerRight.Width * 0.15f), (int)(Assets.PlayerRight.Height * 0.15f));
        private static int PlatformHeight => Assets.Grass.Height;

        /// <summary>The level to play when <see cref="Update"/> returns <see cref="EditorResult.Test"/>.</summary>
        public LevelData TestLevel { get; private set; }

        /// <summary>True once a level was saved, so the game knows to look for new level files.</summary>
        public bool SavedSomething => _savedSomething;

        /// <summary>True while the list of levels is shown, before one is opened.</summary>
        public bool IsChoosingLevel => _choosingLevel;

        private AreaJson CurrentArea => _doc.File.Areas[_area];

        private int StartAreaIndex => Math.Max(0, _doc.File.StartArea == null ? 0 : _doc.File.Areas.FindIndex(a => string.Equals(a.Name, _doc.File.StartArea, StringComparison.OrdinalIgnoreCase)));

        /// <summary>The top of the view when it rests on the ground, with the ground just above the bottom bar.</summary>
        private static float RestingCameraTop => VirtualScreen.Height - VirtualScreen.ViewHeight + BottomBarHeight;

        /// <summary>Shows the list of levels to choose from.</summary>
        public void Open()
        {
            _choosingLevel = true;
            _savedSomething = false;
            OpenLevelList();
        }

        private void StartEditing(LevelDocument doc)
        {
            _doc = doc;
            _area = StartAreaIndex;
            _tool = Tool.Select;
            _selected = Thing.Nothing;
            _drag = Drag.None;
            _focus = null;
            _settingsOpen = false;
            _choosingLevel = false;
            _cameraTop = RestingCameraTop;
            _viewStale = true;
            ShowMessage(doc.HasChanges ? "A new level. Pick a tool above and click in the level to add things." : $"Editing {doc.FileName}.");
        }

        private void ShowMessage(string message, bool error = false)
        {
            _message = message;
            _messageIsError = error;
            _messageTimer = MessageTime;
        }

        /// <summary>The area as the game builds it, for drawing and for the sizes of things.</summary>
        private Area View
        {
            get
            {
                if (_viewStale || _view == null)
                {
                    try
                    {
                        _view?.StopSounds();
                        _view = new Area(LevelData.ToAreaData(CurrentArea, _doc.FileName));
                    }
                    catch (InvalidDataException e)
                    {
                        ShowMessage(e.Message, error: true);
                    }
                    _viewStale = false;
                }
                return _view;
            }
        }

        private void Changed()
        {
            _viewStale = true;
        }

        public EditorResult Update(float deltaTime)
        {
            _time += deltaTime;
            _messageTimer = Math.Max(0f, _messageTimer - deltaTime);

            if (_choosingLevel)
                return UpdateLevelList();

            if (_leaveConfirm.IsOpen)
                return UpdateLeaveConfirm(deltaTime);

            if (_focus != null)
                UpdateTyping();
            else if (_settingsOpen && Input.Back)
                _settingsOpen = false;
            else if (HandleShortcuts() is EditorResult result && result != EditorResult.None)
                return result;

            // A drag in progress keeps going even over the bars.
            if (_drag != Drag.None)
            {
                UpdateLevelMouse();
                return EditorResult.None;
            }

            // The bars and the settings panel come first; clicks on them don't reach the level.
            if (UpdateWidgets(out EditorResult widgetResult))
                return widgetResult;
            if (_settingsOpen)
                return EditorResult.None;

            if (Input.ScrollDelta != 0)
                Scroll(Input.ScrollDelta * 0.6f);
            UpdateLevelMouse();
            return EditorResult.None;
        }

        private EditorResult? HandleShortcuts()
        {
            if (Input.Ctrl && Input.WasPressed(Keys.S))
                Save();
            else if (Input.Ctrl && (Input.WasPressed(Keys.Y) || (Input.Shift && Input.WasPressed(Keys.Z))))
                Redo();
            else if (Input.Ctrl && Input.WasPressed(Keys.Z))
                Undo();
            else if (Input.Ctrl && Input.WasPressed(Keys.D))
                Duplicate();
            else if (Input.WasPressed(Keys.F5))
                return StartTest();
            else if (Input.WasPressed(Keys.Delete) && _selected.Kind != Kind.None)
                Remove(_selected);
            else if (Input.Back)
            {
                if (_selected.Kind != Kind.None || _tool != Tool.Select)
                {
                    _selected = Thing.Nothing;
                    _tool = Tool.Select;
                }
                else
                {
                    return Leave();
                }
            }
            else if (!Input.Ctrl)
            {
                // 1-9 and 0 pick the first ten tools.
                for (int i = 0; i < 10; i++)
                {
                    if (Input.WasPressed(Keys.D1 + i) || (i == 9 && Input.WasPressed(Keys.D0)))
                        ChooseTool((Tool)(i == 9 ? 9 : i));
                }
            }

            // Arrow keys nudge the selected thing (Shift for bigger steps); otherwise they, W/S and Page Up/Down scroll.
            int nudge = Input.Shift ? 10 : 1;
            if (_selected.Kind != Kind.None && !Input.Ctrl)
            {
                var step = new Vector2(
                    (Input.WasPressed(Keys.Right) ? nudge : 0) - (Input.WasPressed(Keys.Left) ? nudge : 0),
                    (Input.WasPressed(Keys.Down) ? nudge : 0) - (Input.WasPressed(Keys.Up) ? nudge : 0));
                if (step != Vector2.Zero)
                {
                    _doc.RememberForUndo();
                    MoveTo(_selected, TopLeftOf(_selected) + step);
                }
            }
            else
            {
                if (Input.IsDown(Keys.Up))
                    Scroll(ScrollSpeed);
                if (Input.IsDown(Keys.Down))
                    Scroll(-ScrollSpeed);
            }
            if (!Input.Ctrl && Input.IsDown(Keys.W))
                Scroll(ScrollSpeed);
            if (!Input.Ctrl && Input.IsDown(Keys.S))
                Scroll(-ScrollSpeed);
            if (Input.WasPressed(Keys.PageUp))
                Scroll(VirtualScreen.ViewHeight * 0.6f);
            if (Input.WasPressed(Keys.PageDown))
                Scroll(-VirtualScreen.ViewHeight * 0.6f);
            if (Input.WasPressed(Keys.Home))
                _cameraTop = RestingCameraTop;
            return EditorResult.None;
        }

        /// <summary>Positive scrolls up, towards the top of the level.</summary>
        private void Scroll(float pixels)
        {
            _cameraTop = MathHelper.Clamp(_cameraTop - pixels, -6000f, RestingCameraTop);
        }

        private void ChooseTool(Tool tool)
        {
            _tool = tool;
            _drag = Drag.None;
            if (tool != Tool.Select)
                _selected = Thing.Nothing;
        }

        private Vector2 WorldMouse => Input.ViewMousePosition + new Vector2(0f, _cameraTop);

        private bool MouseOverLevel => Input.ViewMousePosition.Y > TopBarHeight && Input.ViewMousePosition.Y < VirtualScreen.ViewHeight - BottomBarHeight;

        private void UpdateLevelMouse()
        {
            Vector2 mouse = WorldMouse;
            _hovered = MouseOverLevel ? ThingAt(mouse) : Thing.Nothing;

            if (_drag != Drag.None)
            {
                UpdateDrag(mouse);
                return;
            }
            if (!MouseOverLevel)
                return;

            if (Input.WasRightClicked)
            {
                if (_hovered.Kind != Kind.None)
                    Remove(_hovered);
                return;
            }
            if (!Input.WasClicked)
                return;

            if (_tool == Tool.Select)
            {
                _selected = _hovered;
                if (_selected.Kind == Kind.None)
                    return;
                Rectangle bounds = BoundsOf(_selected);
                bool stretchable = _selected.Kind is Kind.Platform or Kind.Spikes;
                _drag = stretchable && mouse.X > bounds.Right - HandleSize ? Drag.Resize : Drag.Move;
                _dragStart = mouse;
                _grabOffset = TopLeftOf(_selected) - mouse;
                _dragRemembered = false;
            }
            else if (_tool is Tool.Platform or Tool.Spikes)
            {
                _drag = Drag.Draw;
                _dragStart = SnapPoint(mouse);
            }
            else
            {
                Place(_tool, mouse);
            }
        }

        private void UpdateDrag(Vector2 mouse)
        {
            if (!Input.IsMouseDown)
            {
                if (_drag == Drag.Draw)
                    FinishDrawing(mouse);
                _drag = Drag.None;
                return;
            }
            if (_drag == Drag.Draw)
                return;

            // Small wobbles of the mouse while clicking don't move anything.
            if (!_dragRemembered && Vector2.Distance(mouse, _dragStart) < 4f)
                return;
            if (!_dragRemembered)
            {
                _doc.RememberForUndo();
                _dragRemembered = true;
            }

            if (_drag == Drag.Resize)
            {
                Rectangle bounds = BoundsOf(_selected);
                int step = _selected.Kind == Kind.Spikes ? Spikes.SpikeWidth : Grid;
                float length = Math.Max(step, MathF.Round((mouse.X - bounds.X) / step) * step);
                if (_selected.Kind == Kind.Platform)
                    CurrentArea.Platforms[_selected.Index].Length = Math.Max(40, length);
                else
                    CurrentArea.Spikes[_selected.Index].Length = length;
                Changed();
                return;
            }

            MoveTo(_selected, SnapPoint(mouse + _grabOffset));
        }

        /// <summary>Snaps to a 10 pixel grid, measured up from the ground. Shift turns snapping off.</summary>
        private static Vector2 SnapPoint(Vector2 point)
        {
            if (Input.Shift)
                return new Vector2(MathF.Round(point.X), MathF.Round(point.Y));
            float above = LevelData.GroundY - point.Y;
            return new Vector2(MathF.Round(point.X / Grid) * Grid, LevelData.GroundY - MathF.Round(above / Grid) * Grid);
        }

        // ---------- what is where ----------

        private Rectangle BoundsOf(Thing thing)
        {
            AreaJson area = CurrentArea;
            int i = thing.Index;
            return thing.Kind switch
            {
                Kind.Platform => Box(area.Platforms[i], (int)area.Platforms[i].Length, PlatformHeight),
                Kind.PathEnd => Box(area.Platforms[i].MoveTo, (int)area.Platforms[i].Length, PlatformHeight),
                Kind.Spikes => Box(area.Spikes[i], (int)area.Spikes[i].Length, Spikes.Height),
                Kind.Door => Box(area.Doors[i], DoorSize),
                Kind.Portal => Box(area.Portals[i], DoorSize),
                Kind.Key => Box(area.Keys[i], KeySize),
                Kind.Gem => Box(area.Gems[i], new Point(Gem.Size)),
                Kind.PowerUp => Box(area.PowerUps[i], new Point((int)PowerUp.IconSize)),
                Kind.Ghost => Box(area.Ghosts[i], GhostSize),
                Kind.Checkpoint => Box(area.Checkpoints[i], new Point(Checkpoint.Width, Checkpoint.Height)),
                Kind.Spring => Box(area.BouncePads[i], new Point(BouncePad.Width, BouncePad.Height)),
                Kind.Npc => i < View.Npcs.Count ? View.Npcs[i].Bounds : Box(area.Npcs[i], PlayerSize),
                Kind.Start => Box(_doc.File.Spawn, PlayerSize),
                _ => Rectangle.Empty,
            };
        }

        private static Rectangle Box(PointJson point, Point size) => new Rectangle((int)point.X, (int)point.Y, size.X, size.Y);

        private static Rectangle Box(PointJson point, int width, int height) => new Rectangle((int)point.X, (int)point.Y, width, height);

        private PointJson PointOf(Thing thing)
        {
            AreaJson area = CurrentArea;
            int i = thing.Index;
            return thing.Kind switch
            {
                Kind.Platform => area.Platforms[i],
                Kind.PathEnd => area.Platforms[i].MoveTo,
                Kind.Spikes => area.Spikes[i],
                Kind.Door => area.Doors[i],
                Kind.Portal => area.Portals[i],
                Kind.Key => area.Keys[i],
                Kind.Gem => area.Gems[i],
                Kind.PowerUp => area.PowerUps[i],
                Kind.Ghost => area.Ghosts[i],
                Kind.Checkpoint => area.Checkpoints[i],
                Kind.Spring => area.BouncePads[i],
                Kind.Npc => area.Npcs[i],
                Kind.Start => _doc.File.Spawn,
                _ => null,
            };
        }

        private Vector2 TopLeftOf(Thing thing)
        {
            if (thing.Kind == Kind.Npc)
                return BoundsOf(thing).Location.ToVector2();
            PointJson point = PointOf(thing);
            return new Vector2(point.X, point.Y);
        }

        /// <summary>The thing under the mouse, front-most first. Big platforms come last so small things on them win.</summary>
        private Thing ThingAt(Vector2 point)
        {
            AreaJson area = CurrentArea;
            var p = point.ToPoint();
            IEnumerable<Thing> Things(Kind kind, int count) => Enumerable.Range(0, count).Reverse().Select(i => new Thing(kind, i));

            IEnumerable<Thing> candidates = Things(Kind.Npc, area.Npcs.Count)
                .Concat(_area == StartAreaIndex ? new[] { new Thing(Kind.Start, 0) } : Array.Empty<Thing>())
                .Concat(Things(Kind.Gem, area.Gems.Count))
                .Concat(Things(Kind.PowerUp, area.PowerUps.Count))
                .Concat(Things(Kind.Key, area.Keys.Count))
                .Concat(Things(Kind.Ghost, area.Ghosts.Count))
                .Concat(Things(Kind.Portal, area.Portals.Count))
                .Concat(Things(Kind.Door, area.Doors.Count))
                .Concat(Things(Kind.Checkpoint, area.Checkpoints.Count))
                .Concat(Things(Kind.Spring, area.BouncePads.Count))
                .Concat(Things(Kind.Spikes, area.Spikes.Count))
                .Concat(Enumerable.Range(0, area.Platforms.Count).Reverse().Where(i => area.Platforms[i].MoveTo != null).Select(i => new Thing(Kind.PathEnd, i)))
                .Concat(Things(Kind.Platform, area.Platforms.Count));
            return candidates.FirstOrDefault(thing => BoundsOf(thing).Contains(p), Thing.Nothing);
        }

        // ---------- changing things ----------

        private void MoveTo(Thing thing, Vector2 topLeft)
        {
            PointJson point = PointOf(thing);
            if (point == null)
                return;

            // Characters are drawn standing on the platform below where they are placed, so move by the difference.
            Vector2 delta = topLeft - TopLeftOf(thing);
            point.X += delta.X;
            point.Y += delta.Y;

            // A moving platform takes its whole path along.
            if (thing.Kind == Kind.Platform && CurrentArea.Platforms[thing.Index].MoveTo is PointJson end)
            {
                end.X += delta.X;
                end.Y += delta.Y;
            }
            Changed();
        }

        private void Remove(Thing thing)
        {
            if (thing.Kind == Kind.Start)
            {
                ShowMessage("The start can be moved, but every level needs one.");
                return;
            }

            _doc.RememberForUndo();
            AreaJson area = CurrentArea;
            int i = thing.Index;
            switch (thing.Kind)
            {
                case Kind.Platform: area.Platforms.RemoveAt(i); break;
                case Kind.PathEnd: area.Platforms[i].MoveTo = null; break;
                case Kind.Spikes: area.Spikes.RemoveAt(i); break;
                case Kind.Door: area.Doors.RemoveAt(i); break;
                case Kind.Portal: area.Portals.RemoveAt(i); break;
                case Kind.Key: area.Keys.RemoveAt(i); break;
                case Kind.Gem: area.Gems.RemoveAt(i); break;
                case Kind.PowerUp: area.PowerUps.RemoveAt(i); break;
                case Kind.Ghost: area.Ghosts.RemoveAt(i); break;
                case Kind.Checkpoint: area.Checkpoints.RemoveAt(i); break;
                case Kind.Spring: area.BouncePads.RemoveAt(i); break;
                case Kind.Npc: area.Npcs.RemoveAt(i); break;
            }
            _selected = thing.Kind == Kind.PathEnd ? new Thing(Kind.Platform, i) : Thing.Nothing;
            _hovered = Thing.Nothing;
            Changed();
        }

        /// <summary>Ctrl+D: a copy of the selected thing, just to the right of it.</summary>
        private void Duplicate()
        {
            if (_selected.Kind is Kind.None or Kind.Start or Kind.PathEnd)
                return;

            _doc.RememberForUndo();
            AreaJson area = CurrentArea;
            int i = _selected.Index;
            float shift = _selected.Kind is Kind.Platform or Kind.Spikes ? BoundsOf(_selected).Width + 40 : BoundsOf(_selected).Width + 30;
            T Shifted<T>(T copy) where T : PointJson
            {
                copy.X += shift;
                return copy;
            }

            switch (_selected.Kind)
            {
                case Kind.Platform:
                    PlatformJson p = area.Platforms[i];
                    area.Platforms.Add(Shifted(new PlatformJson
                    {
                        X = p.X, Y = p.Y, Length = p.Length, Monster = p.Monster, Crumbling = p.Crumbling, MoveTime = p.MoveTime,
                        MoveTo = p.MoveTo == null ? null : new PointJson { X = p.MoveTo.X + shift, Y = p.MoveTo.Y },
                    }));
                    _selected = new Thing(Kind.Platform, area.Platforms.Count - 1);
                    break;
                case Kind.Spikes:
                    area.Spikes.Add(Shifted(new SpikesJson { X = area.Spikes[i].X, Y = area.Spikes[i].Y, Length = area.Spikes[i].Length }));
                    _selected = new Thing(Kind.Spikes, area.Spikes.Count - 1);
                    break;
                case Kind.Door:
                    area.Doors.Add(Shifted(new DoorJson { X = area.Doors[i].X, Y = area.Doors[i].Y, Locked = area.Doors[i].Locked }));
                    _selected = new Thing(Kind.Door, area.Doors.Count - 1);
                    break;
                case Kind.Ghost:
                    area.Ghosts.Add(Shifted(new GhostJson { X = area.Ghosts[i].X, Y = area.Ghosts[i].Y, Radius = area.Ghosts[i].Radius }));
                    _selected = new Thing(Kind.Ghost, area.Ghosts.Count - 1);
                    break;
                case Kind.PowerUp:
                    area.PowerUps.Add(Shifted(new PowerUpJson { X = area.PowerUps[i].X, Y = area.PowerUps[i].Y, Type = area.PowerUps[i].Type }));
                    _selected = new Thing(Kind.PowerUp, area.PowerUps.Count - 1);
                    break;
                case Kind.Npc:
                    area.Npcs.Add(Shifted(new NpcJson { X = area.Npcs[i].X, Y = area.Npcs[i].Y, Name = area.Npcs[i].Name, Lines = new List<string>(area.Npcs[i].Lines) }));
                    _selected = new Thing(Kind.Npc, area.Npcs.Count - 1);
                    break;
                case Kind.Portal:
                    // A copy of a portal needs its own name.
                    area.Portals.Add(Shifted(new PortalJson { X = area.Portals[i].X, Y = area.Portals[i].Y, Name = NewPortalName(), LeadsTo = "" }));
                    _selected = new Thing(Kind.Portal, area.Portals.Count - 1);
                    break;
                default:
                    List<PointJson> list = PointList(_selected.Kind);
                    list.Add(Shifted(new PointJson { X = list[i].X, Y = list[i].Y }));
                    _selected = new Thing(_selected.Kind, list.Count - 1);
                    break;
            }
            Changed();
        }

        /// <summary>The lists that hold plain positions: keys, gems, flags and springs.</summary>
        private List<PointJson> PointList(Kind kind)
        {
            return kind switch
            {
                Kind.Key => CurrentArea.Keys,
                Kind.Gem => CurrentArea.Gems,
                Kind.Checkpoint => CurrentArea.Checkpoints,
                _ => CurrentArea.BouncePads,
            };
        }

        /// <summary>
        /// The top of the nearest platform just below a point, across the given width, so things placed there stand on it.
        /// </summary>
        private float? PlatformTopBelow(float left, float width, float y)
        {
            float? best = null;
            foreach (PlatformJson platform in CurrentArea.Platforms)
            {
                bool across = platform.X < left + width && platform.X + platform.Length > left;
                if (across && platform.Y >= y - 20 && platform.Y <= y + 160 && (best == null || platform.Y < best))
                    best = platform.Y;
            }
            return best;
        }

        /// <summary>Where a new thing goes for a click at the mouse: on the platform below if there is one, else centered.</summary>
        private Rectangle PlacementFor(Tool tool, Vector2 mouse)
        {
            Point size = tool switch
            {
                Tool.Door or Tool.Portal => DoorSize,
                Tool.Key => KeySize,
                Tool.Gem => new Point(Gem.Size),
                Tool.PowerUp => new Point((int)PowerUp.IconSize),
                Tool.Ghost => GhostSize,
                Tool.Checkpoint => new Point(Checkpoint.Width, Checkpoint.Height),
                Tool.Spring => new Point(BouncePad.Width, BouncePad.Height),
                _ => PlayerSize,
            };

            // How far above the platform's top each thing's top goes, matching the level files.
            float? standHeight = tool switch
            {
                Tool.Door or Tool.Portal => 135,
                Tool.Key or Tool.Checkpoint => 120,
                Tool.PowerUp => 90,
                Tool.Spring => BouncePad.Height,
                Tool.Start => PlayerSize.Y,
                _ => null,
            };

            Vector2 topLeft = SnapPoint(mouse - size.ToVector2() / 2f);
            if (standHeight is float height && PlatformTopBelow(topLeft.X, size.X, mouse.Y) is float top)
                topLeft.Y = top - height;
            return new Rectangle((int)topLeft.X, (int)topLeft.Y, size.X, size.Y);
        }

        /// <summary>Adds a new thing where the mouse is, standing on the platform below when there is one.</summary>
        private void Place(Tool tool, Vector2 mouse)
        {
            Vector2 topLeft = PlacementFor(tool, mouse).Location.ToVector2();

            _doc.RememberForUndo();
            AreaJson area = CurrentArea;
            switch (tool)
            {
                case Tool.Door:
                    area.Doors.Add(new DoorJson { X = topLeft.X, Y = topLeft.Y });
                    _selected = new Thing(Kind.Door, area.Doors.Count - 1);
                    break;
                case Tool.Portal:
                    PlacePortal(topLeft);
                    break;
                case Tool.Key:
                    area.Keys.Add(new PointJson { X = topLeft.X, Y = topLeft.Y });
                    _selected = new Thing(Kind.Key, area.Keys.Count - 1);
                    break;
                case Tool.Gem:
                    area.Gems.Add(new PointJson { X = topLeft.X, Y = topLeft.Y });
                    _selected = new Thing(Kind.Gem, area.Gems.Count - 1);
                    break;
                case Tool.PowerUp:
                    area.PowerUps.Add(new PowerUpJson { X = topLeft.X, Y = topLeft.Y, Type = PowerUpType.Health });
                    _selected = new Thing(Kind.PowerUp, area.PowerUps.Count - 1);
                    break;
                case Tool.Ghost:
                    area.Ghosts.Add(new GhostJson { X = topLeft.X, Y = topLeft.Y, Radius = 220 });
                    _selected = new Thing(Kind.Ghost, area.Ghosts.Count - 1);
                    break;
                case Tool.Checkpoint:
                    area.Checkpoints.Add(new PointJson { X = topLeft.X, Y = topLeft.Y });
                    _selected = new Thing(Kind.Checkpoint, area.Checkpoints.Count - 1);
                    break;
                case Tool.Spring:
                    area.BouncePads.Add(new PointJson { X = topLeft.X, Y = topLeft.Y });
                    _selected = new Thing(Kind.Spring, area.BouncePads.Count - 1);
                    break;
                case Tool.Npc:
                    area.Npcs.Add(new NpcJson { X = topLeft.X, Y = topLeft.Y, Name = "Pip", Lines = new List<string> { "Hello!" } });
                    _selected = new Thing(Kind.Npc, area.Npcs.Count - 1);
                    break;
                case Tool.Start:
                    // There is one start, in the area the player starts in.
                    _doc.File.Spawn = new PointJson { X = topLeft.X, Y = topLeft.Y };
                    _doc.File.StartArea = _area == 0 ? null : CurrentArea.Name;
                    _selected = new Thing(Kind.Start, 0);
                    break;
            }
            Changed();
        }

        /// <summary>A new portal joins up with a portal that doesn't lead anywhere yet, if there is one.</summary>
        private void PlacePortal(Vector2 topLeft)
        {
            var portal = new PortalJson { X = topLeft.X, Y = topLeft.Y, Name = NewPortalName(), LeadsTo = "" };
            PortalJson lonely = AllPortals().FirstOrDefault(other => !AllPortals().Any(p => p != other && string.Equals(p.Name, other.LeadsTo, StringComparison.OrdinalIgnoreCase)));
            if (lonely != null)
            {
                portal.LeadsTo = lonely.Name;
                lonely.LeadsTo = portal.Name;
                ShowMessage($"Joined to the portal \"{lonely.Name}\". Walking through one comes out at the other.");
            }
            else
            {
                ShowMessage("Add a second portal, in this area or another, and the two will lead to each other.");
            }
            CurrentArea.Portals.Add(portal);
            _selected = new Thing(Kind.Portal, CurrentArea.Portals.Count - 1);
        }

        private IEnumerable<PortalJson> AllPortals() => _doc.File.Areas.SelectMany(area => area.Portals);

        private string NewPortalName()
        {
            for (int n = 1; ; n++)
            {
                string name = "portal" + n;
                if (!AllPortals().Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    return name;
            }
        }

        /// <summary>Platforms and spikes are dragged out; a click without dragging makes one of the usual size.</summary>
        private void FinishDrawing(Vector2 mouse)
        {
            Vector2 end = SnapPoint(mouse);
            float left = Math.Min(_dragStart.X, end.X);
            float width = Math.Abs(end.X - _dragStart.X);
            _doc.RememberForUndo();
            if (_tool == Tool.Platform)
            {
                if (width < 40)
                {
                    width = 200;
                    left = _dragStart.X - 100;
                }
                CurrentArea.Platforms.Add(new PlatformJson { X = left, Y = _dragStart.Y, Length = width });
                _selected = new Thing(Kind.Platform, CurrentArea.Platforms.Count - 1);
            }
            else
            {
                float length = Math.Max(Spikes.SpikeWidth, MathF.Round(width / Spikes.SpikeWidth) * Spikes.SpikeWidth);
                if (width < Spikes.SpikeWidth)
                {
                    length = Spikes.SpikeWidth * 3;
                    left = _dragStart.X - length / 2f;
                }
                float y = PlatformTopBelow(left, length, _dragStart.Y) is float top ? top - Spikes.Height : _dragStart.Y;
                CurrentArea.Spikes.Add(new SpikesJson { X = left, Y = y, Length = length });
                _selected = new Thing(Kind.Spikes, CurrentArea.Spikes.Count - 1);
            }
            Changed();
        }

        private void Undo()
        {
            if (_doc.Undo())
            {
                AfterHistoryChange();
                ShowMessage("Undone.");
            }
        }

        private void Redo()
        {
            if (_doc.Redo())
            {
                AfterHistoryChange();
                ShowMessage("Redone.");
            }
        }

        private void AfterHistoryChange()
        {
            _area = Math.Min(_area, _doc.File.Areas.Count - 1);
            _selected = Thing.Nothing;
            _hovered = Thing.Nothing;
            Changed();
        }

        // ---------- saving and testing ----------

        /// <summary>Checks the level the same way the game does. Returns the problem, or null if it is fine.</summary>
        private string Check(out LevelData level)
        {
            level = null;
            try
            {
                level = _doc.Build();
            }
            catch (InvalidDataException e)
            {
                return e.Message;
            }

            if (!_doc.File.Areas.Any(area => area.Doors.Count > 0))
                return "The level has no exit door yet. Add one with the Door tool.";
            return null;
        }

        private bool Save()
        {
            string problem = Check(out _);
            if (problem != null && !problem.StartsWith("The level has no exit", StringComparison.Ordinal))
            {
                ShowMessage("Not saved: " + problem, error: true);
                return false;
            }

            try
            {
                IReadOnlyList<string> folders = _doc.Save();
                _savedSomething = true;
                string where = folders.Count > 1 ? " (next to the game and in the project's Levels folder)" : "";
                ShowMessage($"Saved {_doc.FileName}{where}." + (problem != null ? " " + problem : ""), error: problem != null);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                ShowMessage("Couldn't save: " + e.Message, error: true);
                return false;
            }
        }

        private EditorResult StartTest()
        {
            string problem = Check(out LevelData level);
            if (level == null || problem != null)
            {
                ShowMessage("Can't play yet: " + problem, error: true);
                return EditorResult.None;
            }
            TestLevel = level;
            _drag = Drag.None;
            _focus = null;
            return EditorResult.Test;
        }

        /// <summary>Back to the main menu, asking first if there are unsaved changes.</summary>
        private EditorResult Leave()
        {
            if (_doc.HasChanges)
            {
                _leaveConfirm.Open($"{_doc.FileName} has changes that aren't saved.");
                return EditorResult.None;
            }
            _view?.StopSounds();
            return EditorResult.Back;
        }

        private EditorResult UpdateLeaveConfirm(float deltaTime)
        {
            int choice = Input.Back ? 2 : _leaveConfirm.Update(deltaTime);
            if (choice < 0)
                return EditorResult.None;
            _leaveConfirm.Close();
            if (choice == 2 || (choice == 0 && !Save()))
                return EditorResult.None;
            _view?.StopSounds();
            return EditorResult.Back;
        }
    }
}
