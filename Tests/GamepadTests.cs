using System;
using System.Collections.Generic;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Checks for playing with a gamepad, using made-up gamepad states.
partial class TestGame
{
    static GamePadState Pad(params Buttons[] buttons) => new GamePadState(Vector2.Zero, Vector2.Zero, 0f, 0f, buttons);

    static GamePadState Sticks(Vector2 left, Vector2 right, float rightTrigger = 0f) => new GamePadState(left, right, 0f, rightTrigger, Array.Empty<Buttons>());

    /// <summary>Runs frames with a gamepad state held, then lets go of everything.</summary>
    GameplayResult PadFrames(GameplayScreen gs, int frames, GamePadState state)
    {
        _pad = state;
        var result = Frames(gs, frames);
        _pad = default;
        Frame(gs);
        return result;
    }

    static void UseGamepadHints(bool on) => typeof(Input).GetProperty("UsingGamepad").SetValue(null, on);

    void TestGamepad()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 60);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(150, LevelData.GroundY - PH));
        Frames(gs, 5);

        float x = p.Position.X;
        PadFrames(gs, 10, Sticks(new Vector2(1f, 0f), Vector2.Zero));
        Check(p.Position.X - x == 30, $"gamepad: the left stick walks ({p.Position.X - x} px)");
        x = p.Position.X;
        PadFrames(gs, 10, Pad(Buttons.DPadLeft));
        Check(p.Position.X - x == -30, "gamepad: the D-pad walks");

        _pad = Pad(Buttons.A);
        Frame(gs);
        bool jumped = Rising(p);
        for (int i = 0; i < 60 && Rising(p); i++) Frame(gs);
        _pad = default;
        Frames(gs, 60);
        Check(jumped, "gamepad: A jumps");

        // RT throws where the right stick points: here, straight up.
        Frames(gs, 30);
        PadFrames(gs, 1, Sticks(Vector2.Zero, new Vector2(0f, 1f), rightTrigger: 1f));
        var orbs = Get<List<Projectile>>(gs, "_projectiles");
        Check(orbs.Count == 1 && Get<Vector2>(orbs[0], "_velocity").Y < -10f, "gamepad: RT throws an orb where the right stick points");
        Frames(gs, 100); // the first orb is gone by now
        PadFrames(gs, 1, Sticks(Vector2.Zero, Vector2.Zero, rightTrigger: 1f));
        Check(orbs.Count == 1 && Get<Vector2>(orbs[0], "_velocity").X > 10f, "gamepad: without aiming, RT throws straight ahead");

        // LB and RB choose the inventory slot.
        PadFrames(gs, 1, Pad(Buttons.RightShoulder));
        int afterRight = p.Inventory.SelectedSlot;
        PadFrames(gs, 1, Pad(Buttons.LeftShoulder));
        PadFrames(gs, 1, Pad(Buttons.LeftShoulder));
        Check(afterRight == 1 && p.Inventory.SelectedSlot == Inventory.SlotCount - 1, "gamepad: RB and LB move through the inventory slots");

        // X opens the door; the hint shows X while the gamepad is used.
        var door = AreaOf(gs).Doors[0];
        StandAt(gs, door);
        UseGamepadHints(true);
        ShotGame("160_gamepad_door_hint", gs);
        Check(Controls.KeyNameFor(GameAction.Use) == "X" && Controls.FillIn("Jump with {jump}.") == "Jump with A.", "gamepad: hints name the gamepad buttons");
        PadFrames(gs, 1, Pad(Buttons.X));
        Check(door.State == DoorState.Open, "gamepad: X opens a door");
        UseGamepadHints(false);
        Check(Controls.KeyNameFor(GameAction.Use) == "E", "gamepad: back on the keyboard, hints name keys again");

        // Start pauses, the D-pad and A pick from the menu, B resumes.
        PadFrames(gs, 1, Pad(Buttons.Start));
        var pause = Get<PanelMenu>(gs, "_pauseMenu");
        Check(pause.IsOpen, "gamepad: Start pauses");
        PadFrames(gs, 1, Pad(Buttons.DPadDown));
        PadFrames(gs, 1, Pad(Buttons.DPadDown));
        PadFrames(gs, 1, Pad(Buttons.A));
        var controls = Get<ControlsPanel>(gs, "_controls");
        Check(controls.IsOpen, "gamepad: the D-pad and A choose Controls in the pause menu");
        PadFrames(gs, 1, Pad(Buttons.B));
        PadFrames(gs, 1, Pad(Buttons.B));
        Check(!controls.IsOpen && !pause.IsOpen, "gamepad: B closes the controls, then the pause menu");
        gs.StopSounds();

        // Menus: the stick moves the selection, A chooses.
        var main = new MainMenu();
        _pad = Sticks(new Vector2(0f, -1f), Vector2.Zero);
        Press(); main.Update();
        _pad = default;
        Press(); main.Update();
        Check(Get<int>(main, "_selected") == 1, "gamepad: pushing the left stick down moves through the main menu");
        _pad = Pad(Buttons.A);
        Press();
        var choice = main.Update();
        _pad = default;
        Press();
        Check(choice == MainMenuChoice.LevelPicker, "gamepad: A chooses the selected menu item (" + choice + ")");

        var panel = new ControlsPanel();
        panel.Open();
        ShotMenu("161_controls_with_gamepad_help", panel.Draw);
    }
}
