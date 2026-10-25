using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Editor
{
    // The editor's bars, panels and drawing.
    public sealed partial class LevelEditor
    {
        private const int RowHeight = 44;
        private const int LevelsPerPage = 14;
        private const float BackspaceRepeatDelay = 0.4f;
        private const float BackspaceRepeatRate = 0.04f;

        private static readonly string[] SkyColors = { "CornflowerBlue", "#4A0D12", "MidnightBlue", "DarkSlateGray", "SkyBlue", "#3B2F4A" };
        private static readonly string[] TintColors = { "White", "Salmon", "LightSteelBlue", "PaleGreen", "Khaki", "Plum" };
        private static readonly PowerUpType[] PowerUpTypes = { PowerUpType.Health, PowerUpType.DoubleJump, PowerUpType.HighJump };

        private readonly List<Widget> _barWidgets = new List<Widget>();
        private readonly List<Widget> _panelWidgets = new List<Widget>();
        private readonly Rectangle _settingsPanel = UiDrawing.CenteredPanel(1100, 700);
        private readonly Rectangle _listPanel = UiDrawing.CenteredPanel(1200, 840);

        /// <summary>The text field being typed in, if any.</summary>
        private string _focus;

        private float _backspaceTimer;
        private int _npcLine;
        private List<int> _levelNumbers = new List<int>();
        private List<string> _levelTitles = new List<string>();
        private int _listPage;

        // ---------- typing ----------

        private Widget FocusedField => _barWidgets.Concat(_panelWidgets).FirstOrDefault(w => w.Kind == WidgetKind.TextField && w.FieldId == _focus);

        private void UpdateTyping()
        {
            Widget field = FocusedField;
            if (field == null || Input.Back || Input.WasPressed(Keys.Enter) || Input.WasPressed(Keys.Tab))
            {
                _focus = null;
                return;
            }

            string text = field.GetText() ?? "";
            if (Input.TypedText.Length > 0)
                text += Input.TypedText;

            // Backspace deletes a letter, and keeps deleting while held.
            if (Input.WasPressed(Keys.Back))
            {
                text = text.Length > 0 ? text.Substring(0, text.Length - 1) : text;
                _backspaceTimer = BackspaceRepeatDelay;
            }
            else if (Input.IsDown(Keys.Back))
            {
                _backspaceTimer -= 1f / 60f;
                if (_backspaceTimer <= 0f && text.Length > 0)
                {
                    text = text.Substring(0, text.Length - 1);
                    _backspaceTimer = BackspaceRepeatRate;
                }
            }

            if (text != (field.GetText() ?? ""))
            {
                field.SetText(text);
                Changed();
            }
        }

        // ---------- widgets ----------

        /// <summary>Lays out the bars (and the settings panel when open) and handles clicks on them. True when a click was used.</summary>
        private bool UpdateWidgets(out EditorResult result)
        {
            result = EditorResult.None;
            BuildWidgets();

            Vector2 barMouse = Input.ViewMousePosition;
            Vector2 panelMouse = Input.MousePosition;
            if (!Input.WasClicked)
                return _settingsOpen || !MouseOverLevel;

            // A click anywhere ends typing; a click on a text field starts it there.
            string focusBefore = _focus;
            _focus = null;
            foreach ((List<Widget> widgets, Vector2 mouse) in new[] { (_panelWidgets, panelMouse), (_barWidgets, barMouse) })
            {
                Widget hit = widgets.FirstOrDefault(w => w.Enabled && w.Kind != WidgetKind.Label && w.Bounds.Contains(mouse));
                if (hit == null)
                    continue;

                if (hit.Kind == WidgetKind.TextField)
                {
                    if (focusBefore != hit.FieldId)
                        _doc.RememberForUndo();
                    _focus = hit.FieldId;
                }
                else
                {
                    hit.Click?.Invoke();
                    if (_pendingResult != EditorResult.None)
                    {
                        result = _pendingResult;
                        _pendingResult = EditorResult.None;
                    }
                }
                return true;
            }

            // Clicking outside the open settings panel closes it.
            if (_settingsOpen && !_settingsPanel.Contains(panelMouse))
                _settingsOpen = false;
            return _settingsOpen || !MouseOverLevel;
        }

        /// <summary>Set by buttons that leave the editor or start a test.</summary>
        private EditorResult _pendingResult;

        private void BuildWidgets()
        {
            _barWidgets.Clear();
            _panelWidgets.Clear();
            if (_doc == null)
                return;

            // Top row: the tools.
            int width = (int)VirtualScreen.ViewWidth;
            int toolWidth = Math.Min(150, (width - 16 - 8 * (ToolNames.Length - 1)) / ToolNames.Length);
            var tools = new WidgetRow(_barWidgets, 8, 6, RowHeight);
            for (int i = 0; i < ToolNames.Length; i++)
            {
                var tool = (Tool)i;
                tools.Button(ToolNames[i], () => ChooseTool(tool), on: _tool == tool, width: toolWidth);
            }

            // Second row: the areas on the left, saving and testing on the right.
            var areas = new WidgetRow(_barWidgets, 8, 6 + RowHeight + 6, RowHeight);
            for (int i = 0; i < _doc.File.Areas.Count; i++)
            {
                int index = i;
                areas.Button(_doc.File.Areas[i].Name, () => SwitchArea(index), on: i == _area);
            }
            areas.Button("+ Area", AddArea);
            areas.Space(20);
            areas.Label(_doc.FileName + (_doc.HasChanges ? "  (not saved)" : ""));

            string[] actions = { "Level settings", "Undo", "Redo", "Test (F5)", "Save", "Back" };
            int actionsWidth = actions.Sum(a => (int)Assets.SmallFont.MeasureString(a).X + 28 + 8);
            var right = new WidgetRow(_barWidgets, width - 8 - actionsWidth, 6 + RowHeight + 6, RowHeight);
            right.Button("Level settings", () => _settingsOpen = !_settingsOpen, on: _settingsOpen);
            right.Button("Undo", Undo, enabled: _doc.CanUndo);
            right.Button("Redo", Redo, enabled: _doc.CanRedo);
            right.Button("Test (F5)", () => _pendingResult = StartTest());
            right.Button("Save", () => Save());
            right.Button("Back", () => _pendingResult = Leave());

            BuildPropertyBar(width);
            if (_settingsOpen)
                BuildSettingsPanel();
        }

        private void SwitchArea(int index)
        {
            _area = index;
            _selected = Thing.Nothing;
            _cameraTop = RestingCameraTop;
            Changed();
        }

        private void AddArea()
        {
            _doc.RememberForUndo();
            int n = _doc.File.Areas.Count + 1;
            string name = "area" + n;
            while (_doc.File.Areas.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = "area" + ++n;
            _doc.File.Areas.Add(LevelDocument.NewArea(name, "MidnightBlue"));
            SwitchArea(_doc.File.Areas.Count - 1);
            ShowMessage("New area. Add a portal here and one in another area to connect them.");
        }

        /// <summary>The bottom bar: what can be changed about the selected thing.</summary>
        private void BuildPropertyBar(int width)
        {
            int y = (int)VirtualScreen.ViewHeight - BottomBarHeight + (BottomBarHeight - RowHeight) / 2;
            var row = new WidgetRow(_barWidgets, 12, y, RowHeight);
            AreaJson area = CurrentArea;
            int i = _selected.Index;

            void Edit(Action change)
            {
                _doc.RememberForUndo();
                change();
                Changed();
            }

            switch (_selected.Kind)
            {
                case Kind.None:
                    string name = _tool == Tool.Npc ? "Pip" : "a " + ToolNames[(int)_tool].ToLowerInvariant();
                    row.Label(_tool switch
                    {
                        Tool.Select => "Click something to change it, or drag it to move it. Right-click deletes. Ctrl+Z undoes, Ctrl+D copies.",
                        Tool.Platform => "Drag in the level to draw a platform, or click for the usual size.",
                        Tool.Spikes => "Drag in the level to draw a row of spikes, or click for the usual size.",
                        Tool.Gem or Tool.Ghost => $"Click in the level to add {name}. It floats where you click.",
                        Tool.Start => "Click in the level to move where the player starts.",
                        _ => $"Click in the level to add {name}. It stands on the platform under the mouse.",
                    });
                    return;

                case Kind.Platform:
                case Kind.PathEnd:
                    PlatformJson platform = area.Platforms[i];
                    row.Label("Platform", UiDrawing.Highlight);
                    row.Space(10);
                    row.Stepper("Length", ((int)platform.Length).ToString(CultureInfo.InvariantCulture),
                        () => Edit(() => platform.Length = Math.Max(40, platform.Length - 20)), () => Edit(() => platform.Length += 20));
                    row.Toggle("Frog", platform.Monster, () => Edit(() =>
                    {
                        platform.Monster = !platform.Monster;
                        if (platform.Monster)
                            platform.MoveTo = null; // A frog can't ride a moving platform.
                    }));
                    row.Toggle("Crumbles", platform.Crumbling, () => Edit(() => platform.Crumbling = !platform.Crumbling));
                    row.Toggle("Moves", platform.MoveTo != null, () => Edit(() =>
                    {
                        platform.MoveTo = platform.MoveTo == null ? new PointJson { X = platform.X + 300, Y = platform.Y } : null;
                        if (platform.MoveTo != null)
                            platform.Monster = false;
                    }));
                    if (platform.MoveTo != null)
                    {
                        row.Stepper("Seconds", platform.MoveTime.ToString("0.#", CultureInfo.InvariantCulture),
                            () => Edit(() => platform.MoveTime = Math.Max(0.5f, platform.MoveTime - 0.5f)), () => Edit(() => platform.MoveTime += 0.5f));
                        row.Label("(drag the blue outline to change where it goes)");
                    }
                    break;

                case Kind.Spikes:
                    SpikesJson spikes = area.Spikes[i];
                    row.Label("Spikes", UiDrawing.Highlight);
                    row.Stepper("Length", ((int)spikes.Length).ToString(CultureInfo.InvariantCulture),
                        () => Edit(() => spikes.Length = Math.Max(Spikes.SpikeWidth, spikes.Length - Spikes.SpikeWidth)),
                        () => Edit(() => spikes.Length += Spikes.SpikeWidth));
                    break;

                case Kind.Door:
                    DoorJson door = area.Doors[i];
                    row.Label("Exit door", UiDrawing.Highlight);
                    row.Toggle("Locked (needs a key)", door.Locked, () => Edit(() => door.Locked = !door.Locked));
                    break;

                case Kind.Portal:
                    PortalJson portal = area.Portals[i];
                    row.Label("Portal", UiDrawing.Highlight);
                    row.Toggle("Locked", portal.Locked, () => Edit(() => portal.Locked = !portal.Locked));
                    row.Text("Name", $"portal-name-{_area}-{i}", () => portal.Name, value => RenamePortal(portal, value), 220);
                    List<string> targets = AllPortals().Where(p => p != portal).Select(p => p.Name).ToList();
                    targets.Insert(0, "");
                    int current = Math.Max(0, targets.FindIndex(t => string.Equals(t, portal.LeadsTo, StringComparison.OrdinalIgnoreCase)));
                    row.Choice("Leads to", portal.LeadsTo is { Length: > 0 } ? portal.LeadsTo : "(nowhere: works as an exit)",
                        () => Edit(() => portal.LeadsTo = targets[(current + targets.Count - 1) % targets.Count]),
                        () => Edit(() => portal.LeadsTo = targets[(current + 1) % targets.Count]));
                    break;

                case Kind.Ghost:
                    GhostJson ghost = area.Ghosts[i];
                    row.Label("Ghost", UiDrawing.Highlight);
                    row.Stepper("Chases within", ((int)ghost.Radius).ToString(CultureInfo.InvariantCulture) + " px",
                        () => Edit(() => ghost.Radius = Math.Max(40, ghost.Radius - 20)), () => Edit(() => ghost.Radius += 20));
                    break;

                case Kind.PowerUp:
                    PowerUpJson powerUp = area.PowerUps[i];
                    int type = Array.IndexOf(PowerUpTypes, powerUp.Type);
                    string[] typeNames = { "Heart", "Double jump", "High jump" };
                    row.Label("Power-up", UiDrawing.Highlight);
                    row.Choice("Type", typeNames[type],
                        () => Edit(() => powerUp.Type = PowerUpTypes[(type + PowerUpTypes.Length - 1) % PowerUpTypes.Length]),
                        () => Edit(() => powerUp.Type = PowerUpTypes[(type + 1) % PowerUpTypes.Length]));
                    break;

                case Kind.Npc:
                    NpcJson npc = area.Npcs[i];
                    if (npc.Lines.Count == 0)
                        npc.Lines.Add("");
                    _npcLine = Math.Clamp(_npcLine, 0, npc.Lines.Count - 1);
                    int line = _npcLine;
                    row.Text("Name", $"npc-name-{_area}-{i}", () => npc.Name, value => npc.Name = value, 140);
                    row.Button("<", () => _npcLine = Math.Max(0, line - 1), width: 34);
                    row.Label($"Line {line + 1} of {npc.Lines.Count}");
                    row.Button(">", () => _npcLine = Math.Min(npc.Lines.Count - 1, line + 1), width: 34);
                    int lineWidth = Math.Max(200, width - row.X - 270);
                    row.Text(null, $"npc-line-{_area}-{i}-{line}", () => npc.Lines[line], value => npc.Lines[line] = value, lineWidth);
                    row.Button("+ Line", () => Edit(() =>
                    {
                        npc.Lines.Insert(line + 1, "");
                        _npcLine = line + 1;
                    }));
                    row.Button("- Line", () => Edit(() =>
                    {
                        if (npc.Lines.Count > 1)
                            npc.Lines.RemoveAt(line);
                    }), enabled: npc.Lines.Count > 1);
                    return;

                default:
                    string[] names = { "", "", "", "", "", "", "Key", "Gem", "", "", "Flag (checkpoint)", "Spring (bounce pad)", "", "Start" };
                    row.Label(names[(int)_selected.Kind], UiDrawing.Highlight);
                    row.Label(_selected.Kind == Kind.Start ? "Where the player starts. Drag it to move it." : "Drag it to move it.");
                    break;
            }

            if (_selected.Kind != Kind.Start)
                row.Button("Delete", () => Remove(_selected));
        }

        /// <summary>Renaming a portal also updates the portals that lead to it.</summary>
        private void RenamePortal(PortalJson portal, string name)
        {
            foreach (PortalJson other in AllPortals())
            {
                if (other != portal && string.Equals(other.LeadsTo, portal.Name, StringComparison.OrdinalIgnoreCase))
                    other.LeadsTo = name;
            }
            portal.Name = name;
        }

        /// <summary>The Level settings panel: the level's name, description and medal times, and this area's look.</summary>
        private void BuildSettingsPanel()
        {
            LevelFile file = _doc.File;
            AreaJson area = CurrentArea;
            int left = _settingsPanel.X + 50;
            int y = _settingsPanel.Y + 110;
            int fieldWidth = _settingsPanel.Width - 100 - 160;

            void Edit(Action change)
            {
                _doc.RememberForUndo();
                change();
                Changed();
            }

            WidgetRow Row()
            {
                var row = new WidgetRow(_panelWidgets, left, y, RowHeight);
                y += RowHeight + 14;
                return row;
            }

            var name = Row();
            name.Label("Name");
            name.Space(160 - (int)Assets.SmallFont.MeasureString("Name").X - 8);
            name.Text(null, "level-name", () => file.Name, value => file.Name = value, fieldWidth);

            var description = Row();
            description.Label("Description");
            description.Space(160 - (int)Assets.SmallFont.MeasureString("Description").X - 8);
            description.Text(null, "level-description", () => file.Description, value => file.Description = value, fieldWidth);

            var medals = Row();
            medals.Toggle("Medal times", file.Medals != null, () => Edit(() =>
                file.Medals = file.Medals == null ? new MedalsJson { Gold = 30, Silver = 50, Bronze = 75 } : null));
            if (file.Medals is MedalsJson m)
            {
                medals.Space(20);
                medals.Stepper("Gold", $"{m.Gold:0}s", () => Edit(() => m.Gold = Math.Max(5, m.Gold - 5)), () => Edit(() => m.Gold = Math.Min(m.Silver, m.Gold + 5)));
                medals.Stepper("Silver", $"{m.Silver:0}s", () => Edit(() => m.Silver = Math.Max(m.Gold, m.Silver - 5)), () => Edit(() => m.Silver = Math.Min(m.Bronze, m.Silver + 5)));
                medals.Stepper("Bronze", $"{m.Bronze:0}s", () => Edit(() => m.Bronze = Math.Max(m.Silver, m.Bronze - 5)), () => Edit(() => m.Bronze += 5));
            }

            y += 20;
            var areaName = Row();
            areaName.Label("This area", UiDrawing.Highlight);
            areaName.Space(160 - (int)Assets.SmallFont.MeasureString("This area").X - 8);
            areaName.Text(null, $"area-name-{_area}", () => area.Name, value => RenameArea(area, value), 300);

            var colors = Row();
            int sky = Math.Max(0, Array.FindIndex(SkyColors, c => string.Equals(c, area.Sky ?? "CornflowerBlue", StringComparison.OrdinalIgnoreCase)));
            int tint = Math.Max(0, Array.FindIndex(TintColors, c => string.Equals(c, area.Tint ?? "White", StringComparison.OrdinalIgnoreCase)));
            colors.Choice("Sky", SkyColors[sky],
                () => Edit(() => area.Sky = NullIfDefault(SkyColors[(sky + SkyColors.Length - 1) % SkyColors.Length], "CornflowerBlue")),
                () => Edit(() => area.Sky = NullIfDefault(SkyColors[(sky + 1) % SkyColors.Length], "CornflowerBlue")));
            colors.Space(30);
            colors.Choice("Platforms", TintColors[tint],
                () => Edit(() => area.Tint = NullIfDefault(TintColors[(tint + TintColors.Length - 1) % TintColors.Length], "White")),
                () => Edit(() => area.Tint = NullIfDefault(TintColors[(tint + 1) % TintColors.Length], "White")));

            var start = Row();
            start.Toggle("The player starts in this area", _area == StartAreaIndex, () => Edit(() =>
            {
                file.StartArea = _area == 0 ? null : area.Name;
                file.Spawn = new PointJson { X = 40, Y = LevelData.GroundY - 100 };
            }));
            start.Space(40);
            start.Button("Delete this area", () =>
            {
                if (file.Areas.Count < 2)
                    return;
                Edit(() =>
                {
                    bool wasStart = _area == StartAreaIndex;
                    file.Areas.RemoveAt(_area);
                    if (wasStart || file.StartArea != null && !file.Areas.Any(a => string.Equals(a.Name, file.StartArea, StringComparison.OrdinalIgnoreCase)))
                        file.StartArea = null;
                    _area = Math.Min(_area, file.Areas.Count - 1);
                    _selected = Thing.Nothing;
                });
            }, enabled: file.Areas.Count > 1);

            var close = new WidgetRow(_panelWidgets, _settingsPanel.Center.X - 80, _settingsPanel.Bottom - 40 - RowHeight, RowHeight);
            close.Button("Close", () => _settingsOpen = false, width: 160);
        }

        private static string NullIfDefault(string value, string defaultValue) => string.Equals(value, defaultValue, StringComparison.OrdinalIgnoreCase) ? null : value;

        /// <summary>Renaming an area keeps the start area pointing at it.</summary>
        private void RenameArea(AreaJson area, string name)
        {
            if (_doc.File.StartArea != null && string.Equals(_doc.File.StartArea, area.Name, StringComparison.OrdinalIgnoreCase))
                _doc.File.StartArea = name;
            area.Name = name;
        }

        // ---------- choosing a level ----------

        private void OpenLevelList()
        {
            _levelNumbers = LevelData.FindLevelNumbers();
            _levelTitles = _levelNumbers.Select(n =>
            {
                try
                {
                    return LevelData.Load(n).Title;
                }
                catch (Exception e) when (e is System.IO.InvalidDataException or System.IO.IOException)
                {
                    return $"Level {n} (has a problem)";
                }
            }).ToList();
            _listPage = 0;
        }

        private int PageCount => Math.Max(1, (_levelNumbers.Count + 1 + LevelsPerPage - 1) / LevelsPerPage);

        /// <summary>"New level" and one button per level, two columns, a page at a time.</summary>
        private List<Widget> BuildLevelList()
        {
            var widgets = new List<Widget>();
            int columnWidth = (_listPanel.Width - 150) / 2;
            int top = _listPanel.Y + 130;
            int first = _listPage * LevelsPerPage;
            for (int slot = 0; slot < LevelsPerPage; slot++)
            {
                int entry = first + slot;
                if (entry > _levelNumbers.Count)
                    break;
                int x = _listPanel.X + 50 + slot / (LevelsPerPage / 2) * (columnWidth + 50);
                int y = top + slot % (LevelsPerPage / 2) * (RowHeight + 16);
                var row = new WidgetRow(widgets, x, y, RowHeight);
                if (entry == 0)
                {
                    row.Button("+ New level", () => StartEditing(LevelDocument.CreateNew(_levelNumbers.Count == 0 ? 1 : _levelNumbers.Max() + 1)), on: true, width: columnWidth);
                }
                else
                {
                    int number = _levelNumbers[entry - 1];
                    row.Button(_levelTitles[entry - 1], () => OpenLevel(number), width: columnWidth);
                }
            }

            var bottom = new WidgetRow(widgets, _listPanel.Center.X - 250, _listPanel.Bottom - 40 - RowHeight, RowHeight);
            bottom.Button("<", () => _listPage = Math.Max(0, _listPage - 1), width: 60, enabled: _listPage > 0);
            bottom.Label($"Page {_listPage + 1} of {PageCount}");
            bottom.Button(">", () => _listPage = Math.Min(PageCount - 1, _listPage + 1), width: 60, enabled: _listPage < PageCount - 1);
            bottom.Space(40);
            bottom.Button("Back", () => _pendingResult = EditorResult.Back, width: 160);
            return widgets;
        }

        private void OpenLevel(int number)
        {
            try
            {
                StartEditing(LevelDocument.Load(number));
            }
            catch (Exception e) when (e is System.IO.InvalidDataException or System.IO.IOException)
            {
                ShowMessage(e.Message, error: true);
            }
        }

        private EditorResult UpdateLevelList()
        {
            if (Input.Back)
                return EditorResult.Back;
            if (!Input.WasClicked)
                return EditorResult.None;

            Widget hit = BuildLevelList().FirstOrDefault(w => w.Enabled && w.Kind == WidgetKind.Button && w.Bounds.Contains(Input.MousePosition));
            hit?.Click?.Invoke();
            EditorResult result = _pendingResult;
            _pendingResult = EditorResult.None;
            return result;
        }

        // ---------- drawing ----------

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_choosingLevel)
            {
                DrawLevelList(spriteBatch);
                return;
            }

            AreaJson area = CurrentArea;
            Color sky = View == null ? LevelData.DefaultSky : View.Sky;
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            Backdrop.Draw(spriteBatch, sky, (VirtualScreen.Height - VirtualScreen.ViewHeight) - _cameraTop);
            spriteBatch.End();

            Matrix camera = Matrix.CreateTranslation(0f, -_cameraTop, 0f) * VirtualScreen.Transform;
            var visibleArea = new Rectangle(0, (int)_cameraTop, (int)VirtualScreen.ViewWidth + 1, (int)VirtualScreen.ViewHeight + 1);
            spriteBatch.Begin(transformMatrix: camera);
            DrawGrid(spriteBatch, visibleArea);
            if (View != null)
            {
                View.Draw(spriteBatch, visibleArea);
                if (_area == StartAreaIndex)
                    Player.DrawGhost(spriteBatch, new Vector2(_doc.File.Spawn.X, _doc.File.Spawn.Y), true, Color.White * 0.9f);
                View.DrawGhosts(spriteBatch);
            }
            DrawOverlays(spriteBatch, area);
            spriteBatch.End();

            // Lay the bars out again, so they show what was chosen this frame.
            BuildWidgets();
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            DrawBars(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
            if (_settingsOpen)
                DrawSettingsPanel(spriteBatch);
            _leaveConfirm.Draw(spriteBatch);
            spriteBatch.End();
        }

        /// <summary>Faint lines every 100 pixels, with heights above the ground along the left edge.</summary>
        private static void DrawGrid(SpriteBatch spriteBatch, Rectangle visible)
        {
            Color line = Color.White * 0.07f;
            for (int x = 0; x <= 1920; x += 100)
                spriteBatch.Draw(Assets.Pixel, new Rectangle(x, visible.Top, 1, visible.Height), line);
            int groundY = (int)LevelData.GroundY;
            int firstRow = (int)Math.Floor((groundY - (float)visible.Bottom) / 100f) * 100;
            for (int above = Math.Max(0, firstRow); groundY - above >= visible.Top; above += 100)
            {
                int y = groundY - above;
                spriteBatch.Draw(Assets.Pixel, new Rectangle(0, y, 1920, 1), line);
                if (above > 0)
                    UiDrawing.DrawText(spriteBatch, Assets.SmallFont, above.ToString(CultureInfo.InvariantCulture), new Vector2(6, y - Assets.SmallFont.LineSpacing), Color.White * 0.45f, 0.8f);
            }

            // The level is 1920 pixels wide; a wider window shows more, so mark the edge.
            spriteBatch.Draw(Assets.Pixel, new Rectangle(1920, visible.Top, 3, visible.Height), Color.Red * 0.4f);
        }

        private void DrawOverlays(SpriteBatch spriteBatch, AreaJson area)
        {
            // The far end of every moving platform's path, as an outline that can be dragged.
            for (int i = 0; i < area.Platforms.Count; i++)
            {
                if (area.Platforms[i].MoveTo != null)
                    UiDrawing.DrawOutline(spriteBatch, BoundsOf(new Thing(Kind.PathEnd, i)), Color.Cyan * (_selected.Index == i ? 0.9f : 0.4f), 2);
            }

            // Portal names, so it's clear which leads where.
            foreach (PortalJson portal in area.Portals)
            {
                string label = portal.LeadsTo is { Length: > 0 } ? $"{portal.Name} > {portal.LeadsTo}" : $"{portal.Name} (leads nowhere)";
                var top = new Vector2(portal.X + DoorSize.X / 2f, portal.Y - 18);
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, label, top, Color.White, 0.8f);
            }

            if (_area == StartAreaIndex)
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, "Start", new Vector2(_doc.File.Spawn.X + PlayerSize.X / 2f, _doc.File.Spawn.Y - 16), UiDrawing.Highlight, 0.8f);

            if (_hovered.Kind != Kind.None && _hovered != _selected)
                UiDrawing.DrawOutline(spriteBatch, Grow(BoundsOf(_hovered), 3), Color.White * 0.7f, 2);

            if (_selected.Kind != Kind.None)
            {
                Rectangle bounds = BoundsOf(_selected);
                UiDrawing.DrawOutline(spriteBatch, Grow(bounds, 4), UiDrawing.Highlight, 3);
                if (_selected.Kind is Kind.Platform or Kind.Spikes)
                {
                    var handle = new Rectangle(bounds.Right - HandleSize / 2, bounds.Center.Y - HandleSize / 2, HandleSize, HandleSize);
                    spriteBatch.Draw(Assets.Pixel, handle, UiDrawing.Highlight);
                }
                if (_selected.Kind == Kind.Ghost)
                    DrawCircle(spriteBatch, bounds.Center.ToVector2(), area.Ghosts[_selected.Index].Radius, UiDrawing.Highlight * 0.6f);
            }

            // Where a click with the current tool would put the new thing.
            if (_tool is not (Tool.Select or Tool.Platform or Tool.Spikes) && MouseOverLevel && !_settingsOpen)
            {
                Rectangle place = PlacementFor(_tool, WorldMouse);
                spriteBatch.Draw(Assets.Pixel, place, UiDrawing.Highlight * 0.2f);
                UiDrawing.DrawOutline(spriteBatch, place, UiDrawing.Highlight * 0.8f, 2);
            }

            // What a drag with the platform or spikes tool will make.
            if (_drag == Drag.Draw)
            {
                Vector2 end = SnapPoint(WorldMouse);
                int height = _tool == Tool.Platform ? PlatformHeight : Spikes.Height;
                var preview = new Rectangle((int)Math.Min(_dragStart.X, end.X), (int)_dragStart.Y, (int)Math.Abs(end.X - _dragStart.X), height);
                spriteBatch.Draw(Assets.Pixel, preview, UiDrawing.Highlight * 0.25f);
                UiDrawing.DrawOutline(spriteBatch, preview, UiDrawing.Highlight, 2);
            }
        }

        private static Rectangle Grow(Rectangle bounds, int by)
        {
            bounds.Inflate(by, by);
            return bounds;
        }

        private static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            int dots = Math.Max(24, (int)(radius / 8));
            for (int i = 0; i < dots; i++)
            {
                float angle = MathHelper.TwoPi * i / dots;
                Vector2 dot = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                spriteBatch.Draw(Assets.Pixel, new Rectangle((int)dot.X - 2, (int)dot.Y - 2, 4, 4), color);
            }
        }

        private void DrawBars(SpriteBatch spriteBatch)
        {
            int width = (int)VirtualScreen.ViewWidth;
            int height = (int)VirtualScreen.ViewHeight;
            spriteBatch.Draw(Assets.Pixel, new Rectangle(0, 0, width, TopBarHeight), WidgetDrawing.Bar * 0.92f);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(0, TopBarHeight - 2, width, 2), Color.Black);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(0, height - BottomBarHeight, width, BottomBarHeight), WidgetDrawing.Bar * 0.92f);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(0, height - BottomBarHeight, width, 2), Color.Black);
            WidgetDrawing.Draw(spriteBatch, _barWidgets, Input.ViewMousePosition, _focus, _time);

            // The latest message, just below the top bar.
            if (_messageTimer > 0f && _message != null)
            {
                float opacity = MathHelper.Clamp(_messageTimer / 0.5f, 0f, 1f);
                Vector2 size = Assets.SmallFont.MeasureString(_message);
                var box = new Rectangle((int)(width / 2f - size.X / 2f - 14), TopBarHeight + 8, (int)size.X + 28, (int)size.Y + 12);
                spriteBatch.Draw(Assets.Pixel, box, (_messageIsError ? new Color(120, 20, 20) : Color.Black) * (0.8f * opacity));
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, _message, new Vector2(box.X + 14, box.Y + 6), Color.White * opacity);
            }
        }

        private void DrawSettingsPanel(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * 0.5f);
            UiDrawing.DrawPanel(spriteBatch, _settingsPanel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Level settings", new Vector2(_settingsPanel.Center.X, _settingsPanel.Y + 55), Color.White, 1.2f);
            WidgetDrawing.Draw(spriteBatch, _panelWidgets, Input.MousePosition, _focus, _time);
        }

        private void DrawLevelList(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
            UiDrawing.DrawPanel(spriteBatch, _listPanel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Level editor", new Vector2(_listPanel.Center.X, _listPanel.Y + 55), Color.White, 1.3f);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, "Make a new level, or pick one to change.", new Vector2(_listPanel.Center.X, _listPanel.Y + 100), new Color(210, 200, 185));
            WidgetDrawing.Draw(spriteBatch, BuildLevelList(), Input.MousePosition, null, _time);
            if (_messageTimer > 0f && _message != null)
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, _message, new Vector2(_listPanel.Center.X, _listPanel.Bottom - 100), new Color(255, 110, 90));
            spriteBatch.End();
        }
    }
}
