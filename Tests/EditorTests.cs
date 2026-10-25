using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Editor;
using JumpScape.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Checks for the level editor, driving it with made-up mouse clicks and key presses.
partial class TestGame
{
    void TestLevelWriterRoundTrip()
    {
        foreach (int n in LevelData.FindLevelNumbers())
        {
            string original = File.ReadAllText(Path.Combine(LevelData.LevelsFolder, $"Level{n}.json")).Replace("\r\n", "\n");
            string written = LevelDocument.Load(n).ToJson();
            int differs = Enumerable.Range(0, Math.Min(original.Length, written.Length)).FirstOrDefault(i => original[i] != written[i], -1);
            string where = differs < 0 ? "" : $" (differs at {differs}: ...{written.Substring(Math.Max(0, differs - 40), Math.Min(80, written.Length - Math.Max(0, differs - 40)))}...)";
            Check(original == written, $"level editor: Level{n}.json is written back exactly as it was" + where);
        }
    }

    LevelEditor _editor;
    EditorResult _editorResult;

    void EditorFrame(params Keys[] keys)
    {
        Press(keys);
        _editorResult = _editor.Update(Dt);
    }

    void EditorMouse(Vector2 view, bool down, bool right = false)
    {
        _mousePos = view;
        _mouse = new MouseState((int)view.X, (int)view.Y, 0, down && !right ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, down && right ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    float EditorCamera => Get<float>(_editor, "_cameraTop");

    Vector2 ToView(Vector2 world) => world - new Vector2(0f, EditorCamera);

    void EditorClick(Vector2 view, bool right = false)
    {
        EditorMouse(view, true, right);
        EditorFrame();
        EditorMouse(view, false);
        EditorFrame();
    }

    void EditorDrag(Vector2 fromWorld, Vector2 toWorld)
    {
        EditorMouse(ToView(fromWorld), true);
        EditorFrame();
        for (int i = 1; i <= 10; i++)
        {
            EditorMouse(ToView(Vector2.Lerp(fromWorld, toWorld, i / 10f)), true);
            EditorFrame();
        }
        EditorMouse(ToView(toWorld), false);
        EditorFrame();
    }

    /// <summary>Clicks the button (or toggle) in the editor's bars or panel with this text.</summary>
    bool EditorButton(string text)
    {
        var widgets = Get<List<Widget>>(_editor, "_barWidgets").Concat(Get<List<Widget>>(_editor, "_panelWidgets"));
        Widget widget = widgets.FirstOrDefault(w => w.Text == text && w.Kind != WidgetKind.Label);
        if (widget == null)
            return false;
        EditorClick(widget.Bounds.Center.ToVector2());
        return true;
    }

    void EditorType(string text)
    {
        typeof(Input).GetProperty("TypedText").SetValue(null, text);
        EditorFrame();
        typeof(Input).GetProperty("TypedText").SetValue(null, "");
    }

    LevelDocument EditorDocument => Get<LevelDocument>(_editor, "_doc");

    void TestLevelEditor()
    {
        string saves = Path.Combine(Path.GetTempPath(), "jumpscape-test-editor");
        if (Directory.Exists(saves))
            Directory.Delete(saves, true);
        LevelDocument.SaveFoldersOverride = new[] { saves };
        Check(LevelDocument.SaveFolders().Count >= 1 && LevelDocument.SaveFolders()[0] == LevelData.LevelsFolder,
            "level editor: saves next to the game (and into the project when run from it)");

        _editor = new LevelEditor();
        _editor.Open();
        ShotEditor("180_editor_level_list");

        // "+ New level" is the first button in the list.
        var list = (List<Widget>)typeof(LevelEditor).GetMethod("BuildLevelList", Any).Invoke(_editor, null);
        EditorClick(list.First(w => w.Text == "+ New level").Bounds.Center.ToVector2());
        int number = LevelData.FindLevelNumbers().Max() + 1;
        Check(!_editor.IsChoosingLevel && EditorDocument.Number == number && EditorDocument.File.Areas[0].Doors.Count == 1,
            $"level editor: a new level starts with the ground, a start and an exit door (Level{number}.json)");
        AreaJson area = EditorDocument.File.Areas[0];

        // Draw a platform with the platform tool (key 2).
        EditorFrame(Keys.D2);
        EditorFrame();
        EditorDrag(new Vector2(600, LevelData.GroundY - 300), new Vector2(900, LevelData.GroundY - 300));
        PlatformJson platform = area.Platforms.Last();
        Check(area.Platforms.Count == 2 && platform.X == 600 && platform.Length == 300 && platform.Y == LevelData.GroundY - 300,
            $"level editor: dragging with the platform tool draws a platform ({platform.X}, {LevelData.GroundY - platform.Y} up, {platform.Length} long)");

        // A door clicked above it stands on it.
        EditorFrame(Keys.D4);
        EditorFrame();
        EditorClick(ToView(new Vector2(760, LevelData.GroundY - 360)));
        DoorJson door = area.Doors.Last();
        Check(area.Doors.Count == 2 && door.Y == LevelData.GroundY - 300 - 135, "level editor: a door placed over a platform stands on it");

        // A gem floats where it is clicked.
        EditorFrame(Keys.D7);
        EditorFrame();
        EditorClick(ToView(new Vector2(660, LevelData.GroundY - 520)));
        Check(area.Gems.Count == 1, "level editor: the gem tool adds a gem");

        // Select and drag the platform.
        EditorFrame(Keys.D1);
        EditorFrame();
        EditorDrag(new Vector2(650, LevelData.GroundY - 280), new Vector2(750, LevelData.GroundY - 280));
        Check(platform.X == 700 && door.X != 800, $"level editor: dragging moves the platform ({platform.X})");

        // Stretch it by its right edge.
        EditorDrag(new Vector2(platform.X + platform.Length - 3, LevelData.GroundY - 270), new Vector2(platform.X + 400, LevelData.GroundY - 270));
        Check(platform.Length == 400, $"level editor: dragging the right edge stretches the platform ({platform.Length})");

        // Make it move, then undo and redo that.
        Check(EditorButton("Moves") && platform.MoveTo != null, "level editor: the Moves box makes the selected platform a moving one");
        Frames(1);
        EditorFrame(Keys.LeftControl, Keys.Z);
        EditorFrame();
        platform = EditorDocument.File.Areas[0].Platforms.Last();
        Check(platform.MoveTo == null, "level editor: Ctrl+Z undoes");
        EditorFrame(Keys.LeftControl, Keys.Y);
        EditorFrame();
        area = EditorDocument.File.Areas[0];
        platform = area.Platforms.Last();
        Check(platform.MoveTo != null, "level editor: Ctrl+Y redoes");

        // Right-click deletes.
        PointJson gem = area.Gems[0];
        EditorClick(ToView(new Vector2(gem.X + 18, gem.Y + 18)), right: true);
        Check(area.Gems.Count == 0, "level editor: right-click deletes");

        // Two portals lead to each other.
        EditorFrame(Keys.D5);
        EditorFrame();
        EditorClick(ToView(new Vector2(300, LevelData.GroundY - 70)));
        EditorClick(ToView(new Vector2(1300, LevelData.GroundY - 70)));
        Check(area.Portals.Count == 2 && area.Portals[0].LeadsTo == area.Portals[1].Name && area.Portals[1].LeadsTo == area.Portals[0].Name,
            "level editor: a second portal joins up with the first");

        // Name the level in Level settings.
        EditorFrame(Keys.Escape);
        EditorFrame();
        Check(EditorButton("Level settings") && Get<bool>(_editor, "_settingsOpen"), "level editor: Level settings opens");
        var field = Get<List<Widget>>(_editor, "_panelWidgets").First(w => w.FieldId == "level-name");
        EditorClick(field.Bounds.Center.ToVector2());
        for (int i = 0; i < 12; i++)
        {
            EditorFrame(Keys.Back);
            EditorFrame();
        }
        EditorType("Sky Hop");
        EditorFrame(Keys.Enter);
        EditorFrame();
        Check(EditorDocument.File.Name == "Sky Hop", $"level editor: typing renames the level ({EditorDocument.File.Name})");
        ShotEditor("181_editor_settings");
        EditorFrame(Keys.Escape);
        EditorFrame();

        // Save, and read it back the way the game does.
        EditorFrame(Keys.LeftControl, Keys.S);
        EditorFrame();
        string saved = Path.Combine(saves, $"Level{number}.json");
        LevelData reread = File.Exists(saved) ? LevelData.FromJson(number, File.ReadAllText(saved)) : null;
        Check(reread != null && reread.Title == $"Level {number}: Sky Hop" && reread.Areas[0].Platforms.Count == 2 && reread.Areas[0].Platforms[1].MoveTo != null
            && !EditorDocument.HasChanges, "level editor: Ctrl+S saves a level the game can read");

        // Try it out: the game gets the level, and a try-out saves no best run.
        EditorFrame(Keys.F5);
        Check(_editorResult == EditorResult.Test && _editor.TestLevel?.Areas[0].Doors.Count == 4, "level editor: F5 hands the level over to play");
        var trial = new GameplayScreen(_editor.TestLevel, false, isTest: true);
        Frames(trial, 60);
        StandAt(trial, AreaOf(trial).Doors.First(d => d.Target == null));
        Frame(trial, Keys.E); Frame(trial); Frame(trial, Keys.E);
        Frames(trial, 120);
        string text = string.Join(" ", Get<List<string>>(Get<JumpScape.Menus.PanelMenu>(trial, "_completeMenu"), "_subtitleLines"));
        Check(text.Contains("try-out") && !File.Exists(Path.Combine(SaveFile.Folder, $"best-run-level{number}.bin")), "level editor: a try-out isn't saved as a best run: " + text);
        trial.StopSounds();

        ShotEditor("182_editor");

        // Open an existing level with moving platforms.
        var editor2 = new LevelEditor();
        _editor = editor2;
        _editor.Open();
        list = (List<Widget>)typeof(LevelEditor).GetMethod("BuildLevelList", Any).Invoke(_editor, null);
        EditorClick(list.First(w => w.Text == "Level 10: Sky Ferry").Bounds.Center.ToVector2());
        Check(!_editor.IsChoosingLevel && EditorDocument.Number == 10 && !EditorDocument.HasChanges, "level editor: an existing level opens unchanged");
        EditorClick(ToView(new Vector2(620, LevelData.GroundY - 430)));
        ShotEditor("183_editor_sky_ferry");
        EditorFrame(Keys.Escape);
        EditorFrame();
        Check(!EditorDocument.HasChanges, "level editor: clicking something doesn't count as a change");
        EditorFrame(Keys.Escape);
        Check(_editorResult == EditorResult.Back, "level editor: Esc leaves when nothing has changed");

        LevelDocument.SaveFoldersOverride = null;
        EditorMouse(new Vector2(-1000, -1000), false);
        Press();
    }

    void ShotEditor(string name)
    {
        _bg ??= new JumpScape.Menus.MenuBackground();
        Shot(name, sb =>
        {
            if (_editor.IsChoosingLevel)
            {
                sb.Begin(transformMatrix: VirtualScreen.Transform);
                _bg.Draw(sb);
                sb.End();
            }
            _editor.Draw(sb);
        }, Color.Black);
    }

    void Frames(int n)
    {
        for (int i = 0; i < n; i++) EditorFrame();
    }
}
