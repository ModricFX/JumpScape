using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Tests for the settings added with the new levels, the level picker and the pause menu.
partial class TestGame
{
    void Key(Keys key, Action update) { Press(key); update(); Press(); update(); }

    void Click(Rectangle target, Action update)
    {
        SetMouse(target.Center.ToVector2(), true); Press(); update();
        SetMouse(new Vector2(-1000, -1000), false); Press(); update();
    }

    void TestSettingsOptions()
    {
        var settings = GameSettings.Current;
        int applied = 0;
        bool reset = false;
        var menu = new SettingsMenu(() => applied++, () => reset = true);
        menu.Open();
        Action update = () => menu.Update(Dt);

        // Keyboard: Down selects the first option, Enter toggles it.
        Key(Keys.Down, update);
        Key(Keys.Down, update);
        Check(Get<int>(menu, "_selected") == 1, "settings: Down moves through the options");
        bool timer = settings.ShowTimer;
        Key(Keys.Enter, update);
        Check(settings.ShowTimer == !timer && applied > 0, "settings: Enter toggles Show timer");
        Key(Keys.Enter, update);

        // Right on a checkbox jumps to the other column; Right on a slider turns it up.
        Key(Keys.Right, update);
        int volumeBefore = settings.Volume;
        Key(Keys.Up, update); // from "Music volume" (level with "Show timer") up to "Game volume"
        Key(Keys.Left, update);
        Check(settings.Volume == Math.Max(0, volumeBefore - 5), $"settings: Left on a slider turns it down ({volumeBefore} -> {settings.Volume})");
        Key(Keys.Right, update);
        Check(settings.Volume == volumeBefore, "settings: Right on a slider turns it up again");
        ShotMenu("130_settings_keyboard", menu.Draw);

        // Extra hearts gives five hearts.
        Click(SettingsControl(menu, "Extra hearts (easier)"), update);
        Check(settings.ExtraHearts, "settings: clicking Extra hearts turns it on");
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 200);
        Check(Health(PlayerOf(gs)) == 10, $"extra hearts: the player starts with five hearts ({Health(PlayerOf(gs))}/10)");
        settings.ShowTimer = false;
        ShotGame("131_five_hearts_no_timer", gs);
        settings.ShowTimer = true;
        gs.StopSounds();
        Click(SettingsControl(menu, "Extra hearts (easier)"), update);
        Check(!settings.ExtraHearts && Health(PlayerOf(new GameplayScreen(LevelData.Load(1), true))) == 6, "extra hearts off: back to three hearts");

        bool vsync = settings.VSync;
        Click(SettingsControl(menu, "VSync"), update);
        Check(settings.VSync == !vsync, "settings: VSync can be switched");
        Click(SettingsControl(menu, "VSync"), update);

        // Controls panel.
        Click(SettingsControl(menu, "Keys and mouse"), update);
        var controls = Get<ControlsPanel>(menu, "_controls");
        Check(controls.IsOpen, "settings: the Controls button opens the controls panel");
        ShotMenu("132_controls_panel", menu.Draw);
        Press(Keys.Escape);
        bool left = menu.Update(Dt);
        Press(); menu.Update(Dt);
        Check(!controls.IsOpen && !left, "settings: Esc closes the controls panel but stays in the settings");

        // Reset progress asks first; Cancel and Esc keep everything.
        Click(SettingsControl(menu, "Your progress"), update);
        var confirm = Get<PanelMenu>(menu, "_resetConfirm");
        Check(confirm.IsOpen, "settings: Reset asks for confirmation");
        ShotMenu("133_reset_confirm", menu.Draw);
        Key(Keys.Escape, update);
        Check(!confirm.IsOpen && !reset, "settings: Esc cancels the reset");
        Click(SettingsControl(menu, "Your progress"), update);
        Key(Keys.Enter, update);
        Check(reset && !confirm.IsOpen, "settings: confirming resets the progress");

        // Resetting deletes the best runs.
        Directory.CreateDirectory(SaveFile.Folder);
        string bestRun = Path.Combine(SaveFile.Folder, "best-run-level7.bin");
        File.WriteAllBytes(bestRun, new byte[20]);
        RunRecording.DeleteAll();
        Check(!File.Exists(bestRun), "resetting deletes the saved best runs");
    }

    void TestLevelPicker()
    {
        var levels = LevelData.FindLevelNumbers();
        var picker = new LevelSelectMenu(levels, new GameProgress { HighestCompletedLevel = 2 });
        picker.Open();
        Action update = () => picker.Update(Dt);
        Check(Get<int>(picker, "_selected") == 2, "level picker: opens on the next unfinished level (3)");
        Check(Get<string[]>(picker, "_titles")[0] == "Level 1: First Steps", "level picker: shows level names");

        var boxes = Get<Rectangle[]>(picker, "_levelBoxes");
        SetMouse(boxes[4].Center.ToVector2(), true); Press();
        var choice = picker.Update(Dt);
        SetMouse(new Vector2(-1000, -1000), false); Press();
        Check(choice == LevelSelectChoice.None && Get<float>(picker, "_lockedMessageTimer") > 0f, "level picker: a locked level can't be started");
        ShotMenu("134_level_picker_locked", picker.Draw);

        Set(picker, "_selected", 2);
        Key(Keys.Down, update);
        Check(Get<int>(picker, "_selected") == 7, "level picker: Down moves a whole row");
        Key(Keys.Down, update);
        Check(Get<int>(picker, "_selected") == levels.Count, "level picker: Down from the bottom row goes to Back");
        Key(Keys.Up, update);
        Check(Get<int>(picker, "_selected") == 7, "level picker: Up from Back returns to the same column");
        Key(Keys.Left, update);
        Check(Get<int>(picker, "_selected") == 6, "level picker: Left moves one level back");

        GameSettings.Current.UnlockAllLevels = true;
        SetMouse(boxes[9].Center.ToVector2(), true); Press();
        choice = picker.Update(Dt);
        SetMouse(new Vector2(-1000, -1000), false); Press();
        Check(choice == LevelSelectChoice.Level && picker.ChosenLevel == 10, "level picker: Unlock all levels lets any level start");
        GameSettings.Current.UnlockAllLevels = false;

        var done = new LevelSelectMenu(levels, new GameProgress { HighestCompletedLevel = 10 });
        done.Open();
        ShotMenu("135_level_picker_all_finished", done.Draw);
        var fresh = new LevelSelectMenu(levels, new GameProgress());
        fresh.Open();
        ShotMenu("136_level_picker_new_player", fresh.Draw);
    }

    void TestRaceGhostAfterExit()
    {
        File.Delete(Path.Combine(SaveFile.Folder, "best-run-level3.bin"));
        var first = FinishLevel3(hasNextLevel: true, out _);
        Frames(first, 40);
        Check(Get<RunRecording>(first, "_raceGhost") == null && Get<RunRecording>(first, "_bestRun") != null,
            "first finish: the run just saved as the best doesn't appear as a ghost at the door");
        ShotGame("139_first_finish_no_ghost", first);
        first.StopSounds();

        var second = FinishLevel3(hasNextLevel: true, out _);
        var ghost = Get<RunRecording>(second, "_raceGhost");
        Frames(second, ghost.Frames.Count + 70);
        Check(Get<int>(second, "_raceFrame") >= ghost.Frames.Count + 60, "after the exit the race ghost keeps going and fades out");
        ShotGame("140_second_finish_ghost_gone", second);
        second.StopSounds();
    }

    void TestChoosingKeys()
    {
        var settings = GameSettings.Current;
        var panel = new ControlsPanel();
        panel.Open();
        Action update = panel.Update;
        Rectangle Box(GameAction action, int slot) => (Rectangle)typeof(ControlsPanel).GetMethod("Box", Any).Invoke(panel, new object[] { (int)action, slot });
        string Message() => Get<string>(panel, "_message");

        // Click the first Jump box and press K: K jumps now, Space doesn't.
        Click(Box(GameAction.Jump, 0), update);
        Check(Get<bool>(panel, "_waitingForKey"), "controls: clicking a key box waits for a key");
        ShotMenu("141_controls_waiting", panel.Draw);
        Key(Keys.D1, update);
        Check(Get<bool>(panel, "_waitingForKey") && Message().Contains("inventory"), "controls: 1-3 can't be chosen: " + Message());
        Key(Keys.K, update);
        Check(!Get<bool>(panel, "_waitingForKey") && Controls.KeysFor(GameAction.Jump).SequenceEqual(new[] { Keys.K, Keys.W, Keys.Up }), "controls: K replaces Space for jumping");

        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 60);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(120, LevelData.GroundY - PH));
        Frames(gs, 3);
        Frame(gs, Keys.Space); Frame(gs);
        bool spaceJumped = Rising(p);
        Frames(gs, 60);
        Frame(gs, Keys.K);
        Check(!spaceJumped && Rising(p), "controls: in the game K jumps and Space doesn't any more");
        gs.StopSounds();

        // Using a key that another action has swaps them, so nothing is left without a key.
        Click(Box(GameAction.MoveLeft, 0), update);
        Key(Keys.W, update);
        Check(Controls.KeysFor(GameAction.MoveLeft)[0] == Keys.W && Controls.KeysFor(GameAction.Jump).Contains(Keys.A), "controls: choosing W for walking left swaps it with A from jumping: " + Message());

        // Backspace clears the selected box, but not an action's last key.
        Set(panel, "_row", (int)GameAction.Use);
        Set(panel, "_column", 0);
        Key(Keys.Back, update);
        Check(Controls.KeysFor(GameAction.Use).SequenceEqual(new[] { Keys.E }) && Message().Contains("at least one"), "controls: the last key of an action can't be cleared");
        Set(panel, "_row", (int)GameAction.Jump);
        Set(panel, "_column", 2);
        Key(Keys.Back, update);
        Check(Controls.KeysFor(GameAction.Jump).Count == 2, "controls: Backspace clears a key");

        // Hints and Pip's lines use the chosen keys.
        Click(Box(GameAction.Use, 0), update);
        Key(Keys.F, update);
        Check(Controls.KeyNameFor(GameAction.Use) == "F" && Controls.FillIn("Press {use} at the door") == "Press F at the door", "controls: hints say the chosen key (F)");
        ShotMenu("142_controls_changed", panel.Draw);

        string saved = File.ReadAllText(Path.Combine(SaveFile.Folder, "settings.json"));
        Check(saved.Contains("\"KeyBindings\"") && saved.Contains("\"F\""), "controls: the keys are saved by name in settings.json");

        // Reset keys puts everything back.
        Click(Get<Rectangle[]>(panel, "_buttons")[0], update);
        Check(Controls.KeysFor(GameAction.Jump).SequenceEqual(new[] { Keys.Space, Keys.W, Keys.Up }) && Controls.KeyNameFor(GameAction.Use) == "E", "controls: Reset keys brings back the defaults");
        Key(Keys.Escape, update);
        Check(!panel.IsOpen, "controls: Esc closes the panel");

        // Broken saved keys are cleaned up.
        var cleaned = Controls.Validate(new Dictionary<GameAction, Keys[]>
        {
            [GameAction.MoveLeft] = new[] { Keys.Escape, Keys.J, Keys.J },
            [GameAction.MoveRight] = new[] { Keys.J },
        });
        Check(cleaned[GameAction.MoveLeft].SequenceEqual(new[] { Keys.J }) && cleaned[GameAction.MoveRight].SequenceEqual(new[] { Keys.D, Keys.Right }) && cleaned[GameAction.Use][0] == Keys.E,
            "controls: saved keys are checked: no fixed keys, no key twice, nothing left empty");
        Check(Controls.KeyName(Keys.LeftShift) == "Left Shift" && Controls.KeyName(Keys.D7) == "7" && Controls.KeyName(Keys.OemComma) == ",", "controls: key names are readable");
    }

    void TestPauseMenuExtras()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var first = PlayerOf(gs);
        Frames(gs, 20, Keys.Right);
        Frame(gs, Keys.Escape); Frame(gs);
        ShotGame("137_pause_menu", gs);
        Frame(gs, Keys.Down); Frame(gs);
        Frame(gs, Keys.Enter); Frame(gs);
        Frames(gs, 60);
        Check(PlayerOf(gs) != first && !Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "pause: Restart level starts the level again");

        Frame(gs, Keys.Escape); Frame(gs);
        Frame(gs, Keys.Down); Frame(gs); Frame(gs, Keys.Down); Frame(gs);
        Frame(gs, Keys.Enter); Frame(gs);
        var controls = Get<ControlsPanel>(gs, "_controls");
        Check(controls.IsOpen, "pause: Controls shows the controls");
        ShotGame("138_pause_controls", gs);
        Frame(gs, Keys.Escape); Frame(gs);
        Check(!controls.IsOpen && Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "pause: Esc closes the controls, back to the pause menu");
        Frame(gs, Keys.Escape); Frame(gs);
        Check(!Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "pause: Esc again resumes");
        gs.StopSounds();
    }
}
