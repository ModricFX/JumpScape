using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Checks for the levels, the player, enemies, items, doors and the menus around playing a level.
partial class TestGame
{
    void RunAll()
    {
        TestMusicStreams();
        TestLevelFiles();
        TestVirtualScreen();
        TestMenus();
        TestLevelsRenderAndGrounding();
        TestMovementPriority();
        TestJump();
        TestReachability();
        TestKeyFlow();
        TestDeathAtTopAndRetry();
        TestPause();
        TestCrumbling();
        TestMonsterAndGhost();
        TestOtherWindowShapes();
        TestPowerUpTypesAppear();
        TestThrowing();
        TestPowerUps();
        TestDoorsAndKeys();
        TestNetherLevel();
        TestLevelCompleteScreen();
        TestNewLook();
        TestNpcAndDialogue();
        TestRaceGhost();
        TestBrokenLevelLines();
        TestSettingsOptions();
        TestLevelPicker();
        TestPauseMenuExtras();
        TestRaceGhostAfterExit();
        TestChoosingKeys();
        TestCheckpoints();
        TestSpikes();
        TestGems();
        TestMedals();
        TestGamepad();
        TestMovingPlatforms();
        TestBouncePads();
        TestAutomatedPlayerRides();
    }

    /// <summary>Plays Level 3 to the exit and returns the screen with the level complete menu open.</summary>
    GameplayScreen FinishLevel3(bool hasNextLevel, out GameplayResult result)
    {
        var gs = new GameplayScreen(LevelData.Load(3), hasNextLevel);
        Frames(gs, 120);
        var p = PlayerOf(gs);
        p.Inventory.TryAdd(ItemType.Key);
        StandAt(gs, AreaOf(gs).Doors[0]);
        Frame(gs, Keys.E); Frame(gs); Frame(gs, Keys.E); Frame(gs); Frame(gs, Keys.E);
        result = Frames(gs, 120);
        return gs;
    }

    static Rectangle SettingsControl(SettingsMenu settings, string label)
    {
        foreach (object option in (System.Collections.IList)Get<object>(settings, "_options"))
            if ((string)option.GetType().GetField("Label").GetValue(option) == label)
                return (Rectangle)option.GetType().GetField("Control").GetValue(option);
        throw new Exception("no settings option " + label);
    }

    void TestLevelCompleteScreen()
    {
        var gs = FinishLevel3(hasNextLevel: true, out var result);
        Check(result == GameplayResult.LevelCompleted && Get<PanelMenu>(gs, "_completeMenu").IsOpen, "exit door opens the level complete screen");
        Frames(gs, 40);
        ShotGame("95_level_complete", gs);
        var next = Frames(gs, 1, Keys.Enter);
        Check(next == GameplayResult.NextLevel, "level complete: Enter on Next level asks for the next level (" + next + ")");

        var gs2 = FinishLevel3(hasNextLevel: true, out _);
        Frames(gs2, 40);
        Frame(gs2, Keys.Down); Frame(gs2); Frame(gs2, Keys.Enter); Frames(gs2, 60);
        Check(!PlayerOf(gs2).ReachedExit && !Get<PanelMenu>(gs2, "_completeMenu").IsOpen, "level complete: Play again restarts the level");

        var last = FinishLevel3(hasNextLevel: false, out _);
        Frames(last, 40);
        ShotGame("96_last_level_complete", last);
        Frame(last, Keys.Down); Frame(last);
        var menu = Frames(last, 1, Keys.Enter);
        Check(menu == GameplayResult.QuitToMenu, "last level: the second button is Main menu (" + menu + ")");
        gs.StopSounds(); gs2.StopSounds(); last.StopSounds();
    }

    void TestNewLook()
    {
        var gs = new GameplayScreen(LevelData.Load(2), true);
        Frames(gs, 30);
        ShotGame("97_level_start_banner", gs);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(1000, LevelData.GroundY - PH - 300));
        bool landed = false, dust = false;
        for (int i = 0; i < 60 && !landed; i++)
        {
            Frame(gs);
            landed = p.JustLanded;
        }
        dust = Get<Effects>(gs, "_effects") != null;
        Check(landed && dust, "a hard landing is noticed (for the squash and dust)");
        Frames(gs, 3);
        ShotGame("98_landing_dust", gs);

        Frame(gs, Keys.Escape); Frame(gs);
        Frames(gs, 10);
        ShotGame("99_pause_panel", gs);
        Frame(gs, Keys.Escape); Frame(gs);

        Set(p, "_health", 1);
        p.TakeDamage(1f, 0);
        Frames(gs, 60);
        ShotGame("100_death_panel", gs);
        gs.StopSounds();
    }

    void TestNpcAndDialogue()
    {
        var level1 = LevelData.Load(1);
        var info = level1.Areas[0].Npcs.Single();
        Check(info.Name == "Pip" && info.Lines.Count == 6 && info.Lines[0].Contains(", and jump"), "L1: Pip has 6 lines, commas kept");

        var gs = new GameplayScreen(level1, true);
        Frames(gs, 120);
        var npc = AreaOf(gs).Npcs.Single();
        var ground = AreaOf(gs).Platforms[0];
        Check(npc.Bounds.Bottom == ground.Bounds.Top, "Pip stands on the ground below where Pip was placed");
        var p = PlayerOf(gs);
        Check(!npc.CanTalkTo(p) || p.Bounds.Right > npc.Bounds.Left - 80, "Pip is out of reach at the very start, or close by");
        Teleport(p, new Vector2(npc.Bounds.X - 100, LevelData.GroundY - PH));
        Frames(gs, 5);
        Check(npc.CanTalkTo(p), "walking up to Pip lets you talk");
        ShotGame("110_talk_prompt", gs);
        Frame(gs, Keys.E);
        var dialogue = Get<DialogueBox>(gs, "_dialogue");
        Check(dialogue.IsOpen, "E opens the dialogue");
        float x = p.Position.X;
        Frames(gs, 20, Keys.Right);
        Check(p.Position.X == x, "the world waits while talking");
        Frames(gs, 120);
        ShotGame("111_dialogue", gs);
        Frame(gs, Keys.E); Frame(gs);
        Check(Get<int>(dialogue, "_line") == 1, "E moves to the next line");
        Frame(gs, Keys.E); Frame(gs);
        Check(Get<int>(dialogue, "_line") == 1 && Get<float>(dialogue, "_shownCharacters") >= 50, "E while typing shows the whole line first");
        Frame(gs, Keys.Escape); Frame(gs);
        Check(!dialogue.IsOpen && !Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "Esc closes the dialogue without pausing");
        Frames(gs, 10, Keys.Right);
        Check(p.Position.X > x, "the player moves again after talking");
        gs.StopSounds();
    }

    void TestRaceGhost()
    {
        // Earlier tests already finished level 3; start this one without a saved best run.
        File.Delete(Path.Combine(SaveFile.Folder, "best-run-level3.bin"));
        var level = LevelData.Load(3);
        Check(LevelData.Load(3).ContentHash == level.ContentHash && LevelData.Load(2).ContentHash != level.ContentHash, "level fingerprints are stable and differ between levels");
        Check(RunRecording.LoadBestTime(3, level.ContentHash) == null, "no best run saved at first");

        var first = FinishLevel3(hasNextLevel: true, out _);
        Frames(first, 40);
        string firstLine = string.Join(" ", Get<List<string>>(Get<PanelMenu>(first, "_completeMenu"), "_subtitleLines"));
        float? saved = RunRecording.LoadBestTime(3, level.ContentHash);
        Check(saved != null && firstLine.StartsWith("Time: ") && !firstLine.Contains("best"), $"first finish is saved as the best run ({firstLine})");
        Check(RunRecording.LoadBestTime(3, level.ContentHash + 1) == null, "a best run from a changed level is ignored");

        var race = new GameplayScreen(level, true);
        var best = Get<RunRecording>(race, "_bestRun");
        Check(best != null && best.Frames.Count > 100 && Math.Abs(best.Time - saved.Value) < 0.01f, $"the next attempt loads the best run ({best?.Frames.Count} frames)");
        Frames(race, 140);
        ShotGame("112_race_ghost", race);
        race.StopSounds();

        // A slower run doesn't replace the best one.
        var slow = new GameplayScreen(level, true);
        Frames(slow, 400);
        var p = PlayerOf(slow);
        p.Inventory.TryAdd(ItemType.Key);
        StandAt(slow, AreaOf(slow).Doors[0]);
        Frame(slow, Keys.E); Frame(slow); Frame(slow, Keys.E); Frame(slow); Frame(slow, Keys.E);
        Frames(slow, 120);
        string slowLine = string.Join(" ", Get<List<string>>(Get<PanelMenu>(slow, "_completeMenu"), "_subtitleLines"));
        Check(slowLine.Contains("(best ") && RunRecording.LoadBestTime(3, level.ContentHash) == saved, $"a slower run keeps the old best ({slowLine})");
        slow.StopSounds(); first.StopSounds();

        var ls = new LevelSelectMenu(LevelData.FindLevelNumbers(), new GameProgress { HighestCompletedLevel = 3 });
        ls.Open();
        Check(Get<float?[]>(ls, "_bestTimes")[2] != null && Get<float?[]>(ls, "_bestTimes")[0] == null, "the level picker shows the best time for level 3 only");
        ShotMenu("113_level_select_best_time", ls.Draw);

        var settings = new SettingsMenu(() => { }, () => { });
        settings.Open();
        ShotMenu("114_settings_with_ghost_option", settings.Draw);
        var ghostBox = SettingsControl(settings, "Race your best run");
        SetMouse(ghostBox.Center.ToVector2(), true); Press(); settings.Update(Dt);
        SetMouse(new Vector2(-1000, -1000), false); Press();
        Check(!GameSettings.Current.ShowRaceGhost, "the settings checkbox turns the ghost off");
        GameSettings.Current.ShowRaceGhost = true;
    }

    const string TwoLockedDoors = @"{
      ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
      ""areas"": [ {
        ""name"": ""main"",
        ""platforms"": [ { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" } ],
        ""doors"": [ { ""x"": 600, ""y"": ""groundY-135"", ""locked"": true }, { ""x"": 1200, ""y"": ""groundY-135"", ""locked"": true } ]
      } ]
    }";

    void TestDoorsAndKeys()
    {
        var level = LevelData.FromJson(99, TwoLockedDoors);

        var gs = new GameplayScreen(level, true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        var doors = AreaOf(gs).Doors;
        p.Inventory.TryAdd(ItemType.Key);
        StandAt(gs, doors[0]); Frame(gs, Keys.E); Frame(gs);
        Check(doors[0].State == DoorState.Closed && doors[1].State == DoorState.Locked && !p.Inventory.Contains(ItemType.Key), "two doors, one key: unlocking one door uses the key and leaves the other locked");
        StandAt(gs, doors[1]); Frame(gs, Keys.E); Frame(gs);
        Check(doors[1].State == DoorState.Locked, "two doors, one key: the second door can't be unlocked");
        ShotGame("90_second_door_needs_key", gs);

        var gs2 = new GameplayScreen(level, true);
        Frames(gs2, 240);
        var p2 = PlayerOf(gs2);
        var doors2 = AreaOf(gs2).Doors;
        p2.Inventory.TryAdd(ItemType.Key); p2.Inventory.TryAdd(ItemType.Key);
        StandAt(gs2, doors2[0]); Frame(gs2, Keys.E); Frame(gs2);
        Check(doors2[0].State == DoorState.Closed && p2.Inventory.Contains(ItemType.Key), "two doors, two keys: the first unlock uses one key");
        StandAt(gs2, doors2[1]); Frame(gs2, Keys.D2); Frame(gs2); Frame(gs2, Keys.E); Frame(gs2);
        Check(doors2[1].State == DoorState.Closed && !p2.Inventory.Contains(ItemType.Key), "two doors, two keys: the second unlock uses the other key");
        Frame(gs2, Keys.E); Frame(gs2);
        StandAt(gs2, doors2[0]); Frame(gs2, Keys.E); Frame(gs2);
        Check(doors2[0].State == DoorState.Open && doors2[1].State == DoorState.Open, "two doors, two keys: both doors can be opened");
        gs.StopSounds(); gs2.StopSounds();
    }

    void TestNetherLevel()
    {
        var level = LevelData.Load(8); // Into the Nether
        Check(level.Areas.Count == 2 && level.StartArea == 0 && level.Areas[1].Name == "nether"
            && level.Areas[1].Sky == new Color(0x4A, 0x0D, 0x12) && level.Areas[1].Tint == Color.Salmon, "L7: a main area and a dark red nether");

        var gs = new GameplayScreen(level, true);
        Frames(gs, 240);
        ShotGame("91_level4_overworld", gs);
        var p = PlayerOf(gs);
        Area main = AreaOf(gs);
        Door gate = main.Doors.Single(d => d.Name == "netherGate");
        Door exit = main.Doors.Single(d => d.Target == null);

        StandAt(gs, exit); Frame(gs, Keys.E); Frame(gs);
        Check(exit.State == DoorState.Locked, "L7: the exit starts locked");
        StandAt(gs, gate); Frame(gs, Keys.E); Frame(gs);
        Check(gate.State == DoorState.Open, "L7: the portal door opens");
        ShotGame("92_level4_portal_open", gs);
        Frame(gs, Keys.E);
        Frames(gs, 8);
        ShotGame("93_level4_portal_fade", gs);
        Frames(gs, 40);
        Area nether = AreaOf(gs);
        Door back = nether.Doors.Single(d => d.Name == "overworldGate");
        Check(nether.Name == "nether" && back.State == DoorState.Open && p.Bounds.Intersects(back.Bounds) && p.IsOnGround,
            "L7: going through the portal puts the player in front of the open nether door");
        Frames(gs, 20);
        ShotGame("94_level4_nether", gs);

        Platform keyPlatform = nether.Platforms.Last();
        Teleport(p, new Vector2(1760, keyPlatform.Bounds.Top - PH));
        Frames(gs, 20);
        Check(p.Inventory.Contains(ItemType.Key), "L7: the key is picked up in the nether");
        StandAt(gs, back); Frame(gs, Keys.E); Frames(gs, 60);
        Check(AreaOf(gs) == main && p.Bounds.Intersects(gate.Bounds), "L7: the nether door leads back to the portal in the main area");

        StandAt(gs, exit); Frame(gs, Keys.E); Frame(gs);
        Check(exit.State == DoorState.Closed && !p.Inventory.Contains(ItemType.Key), "L7: the nether key unlocks the exit");
        Frame(gs, Keys.E); Frame(gs); Frame(gs, Keys.E);
        var result = Frames(gs, 400);
        Check(result == GameplayResult.LevelCompleted, "L7: the level completes (" + result + ")");

        var gs2 = new GameplayScreen(level, true);
        Frames(gs2, 240);
        StandAt(gs2, AreaOf(gs2).Doors.Single(d => d.Name == "netherGate"));
        Frame(gs2, Keys.E); Frame(gs2); Frame(gs2, Keys.E); Frames(gs2, 60);
        Set(PlayerOf(gs2), "_health", 1);
        PlayerOf(gs2).TakeDamage(1f, 0);
        Frames(gs2, 60);
        Frame(gs2, Keys.Enter); Frames(gs2, 80);
        Check(AreaOf(gs2).Name == "main" && !PlayerOf(gs2).IsDead, "L7: Retry in the nether starts the level again from the beginning");
        gs.StopSounds(); gs2.StopSounds();
    }

    void TestLevelsArePlayable()
    {
        foreach (int n in LevelData.FindLevelNumbers().Where(n => _onlyLevels == null || _onlyLevels.Contains(n)))
        {
            var level = LevelData.Load(n);
            var report = PlayLevelWithBot(level, verbose: true);
            Check(report.Finished, $"{level.Title}: finished={report.Finished} in {report.Seconds:0.0}s, jumps {report.Jumps}, orbs {report.Throws}, hearts lost {report.HeartsLost}, replans {report.Replans}, tightest jumps {string.Join("/", report.Windows.OrderBy(w => w).Take(3))} px" + (report.Problem == null ? "" : " PROBLEM: " + report.Problem));
            Check(report.GemsFound == report.GemCount, $"{level.Title}: all gems collected ({report.GemsFound}/{report.GemCount})" + string.Concat(report.MissedGems.Select(missed => "; " + missed)));

            // Racing straight to the exit, without the gems, sets the pace for the medals. A ghost can still knock the
            // player off a jump, so it's enough that one of the two runs beats the gold time.
            var race = PlayLevelWithBot(level, verbose: false, collectGems: false);
            float fastest = Math.Min(race.Finished ? race.Seconds : float.MaxValue, report.Finished ? report.Seconds : float.MaxValue);
            string medals = level.Medals == null ? "no medal times" : $"gold {level.Medals.Gold}s, silver {level.Medals.Silver}s, bronze {level.Medals.Bronze}s";
            Check(race.Finished && (level.Medals == null || fastest <= level.Medals.Gold),
                $"{level.Title}: straight to the exit in {race.Seconds:0.0}s, replans {race.Replans}, hearts lost {race.HeartsLost} ({medals})");
        }
    }

    void TestBrokenLevelLines()
    {
        string Error(string json)
        {
            try { LevelData.FromJson(98, json); return null; }
            catch (InvalidDataException e) { return e.Message; }
        }

        const string Ground = @"""platforms"": [ { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" } ]";
        string typo = Error(@"{
  ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
  ""areas"": [ { ""name"": ""main"",
    ""platforms"": [ { ""x"": 1, ""y"": 2, ""lenght"": 3 } ] } ]
}");
        Check(typo != null && typo.Contains("line 4") && typo.Contains("\"lenght\"") && typo.Contains("platform"), "a misspelled setting is reported with its line: " + typo);
        string number = Error(@"{ ""spawn"": { ""x"": 100, ""y"": ""groundZ-100"" }, ""areas"": [ { ""name"": ""main"", " + Ground + @" } ] }");
        Check(number != null && number.Contains("groundZ-100"), "a bad number is reported: " + number);
        string spawn = Error(@"{ ""areas"": [ { ""name"": ""main"", " + Ground + @" } ] }");
        Check(spawn != null && spawn.Contains("spawn"), "a missing spawn is reported: " + spawn);
        string color = Error(@"{ ""spawn"": { ""x"": 1, ""y"": 2 }, ""areas"": [ { ""name"": ""main"", ""sky"": ""Purpleish"", " + Ground + @" } ] }");
        Check(color != null && color.Contains("Purpleish"), "an unknown color is reported: " + color);
        string syntax = Error(@"{ ""spawn"": { ""x"": 1 ""y"": 2 }, ""areas"": [] }");
        Check(syntax != null && syntax.StartsWith("Level98.json, line 1"), "broken JSON is reported: " + syntax);
        string powerUp = Error(@"{ ""spawn"": { ""x"": 1, ""y"": 2 }, ""areas"": [ { ""name"": ""main"", ""powerUps"": [ { ""x"": 1, ""y"": 2, ""type"": ""superJump"" } ] } ] }");
        Check(powerUp != null && powerUp.Contains("doubleJump"), "an unknown power-up type is reported: " + powerUp);

        var level = LevelData.FromJson(98, @"{
          // Comments and trailing commas are allowed, to make editing easier.
          ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
          ""areas"": [ { ""name"": ""main"", " + Ground + @",
            ""portals"": [ { ""x"": 600, ""y"": ""groundY-135"", ""name"": ""lonely"", ""leadsTo"": ""nowhere"" }, ],
          } ]
        }");
        var gs = new GameplayScreen(level, true);
        Frames(gs, 240);
        StandAt(gs, AreaOf(gs).Doors[0]);
        Frame(gs, Keys.E); Frame(gs); Frame(gs, Keys.E); Frame(gs);
        Check(PlayerOf(gs).ReachedExit, "a portal that leads nowhere works as an exit door");
        gs.StopSounds();

        var same = LevelData.FromJson(98, @"{""spawn"":{""x"":100,""y"":""groundY-100""},""areas"":[{""name"":""main""," + Ground + "}]}");
        var spaced = LevelData.FromJson(98, @"{
          // a note
          ""description"": ""different notes"",
          ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
          ""areas"": [ { ""name"": ""main"", " + Ground + @" } ]
        }");
        Check(same.ContentHash == spaced.ContentHash, "spacing, comments and the description don't change the level fingerprint");

        var errorMenu = new PanelMenu("This level can't be loaded", new Color(255, 90, 80), new[] { "OK" });
        errorMenu.Open(typo);
        _bg ??= new MenuBackground();
        ShotMenu("120_level_error_panel", errorMenu.Draw);
    }

    void TestPowerUpTypesAppear()
    {
        var levels = LevelData.FindLevelNumbers().Select(LevelData.Load).ToList();
        var types = levels.SelectMany(level => level.Areas).SelectMany(a => a.PowerUps).Select(x => x.Type).ToHashSet();
        Check(types.SetEquals(Enum.GetValues<PowerUpType>()), "every kind of power-up appears in some level: " + string.Join(", ", types));
        Check(levels.All(level => level.Areas.Any(a => a.PowerUps.Count > 0)), "every level has at least one power-up");
    }

    void TestThrowing()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var monsters = AreaOf(gs).Monsters;
        var frog = monsters[0];
        // Stand on the frog's platform; from below, the platform itself would block the shot.
        var frogPlatform = AreaOf(gs).Platforms[4];
        Teleport(PlayerOf(gs), new Vector2(frogPlatform.Bounds.Left, frogPlatform.Bounds.Top - PH));
        Frames(gs, 2);
        Vector2 sky = PlayerOf(gs).Center + new Vector2(0, -400);
        ClickAt(gs, sky);
        Check(Get<List<Projectile>>(gs, "_projectiles").Count == 1, "click throws an orb");
        ClickAt(gs, sky); Frame(gs);
        Check(Get<List<Projectile>>(gs, "_projectiles").Count == 1, "a second click right away is ignored (cooldown)");
        Frames(gs, 30);
        ClickAt(gs, frog.Center);
        Frames(gs, 12);
        ShotGame("80_orb_in_flight", gs);
        int frames = 0;
        while (monsters.Count > 0 && frames < 120) { Frame(gs); frames++; }
        Check(monsters.Count == 0, $"orb thrown at the frog defeats it (after {frames} more frames)");
        Frames(gs, 2);
        ShotGame("81_frog_defeated", gs);

        // Aiming at the ground: the orb stops at the first platform it touches.
        var p = PlayerOf(gs);
        Frames(gs, 30);
        ClickAt(gs, p.Center + new Vector2(0, 300));
        Frames(gs, 15);
        Check(Get<List<Projectile>>(gs, "_projectiles").Count == 0, "orb aimed at the ground stops on it");
        Frames(gs, 30);
        ClickAt(gs, p.Center + new Vector2(600, -250));
        Frames(gs, 8);
        ShotGame("84_orb_flying", gs);

        var gs2 = new GameplayScreen(LevelData.Load(6), true); // Ghost Garden
        Frames(gs2, 240);
        var ghosts = AreaOf(gs2).Ghosts;
        var ghostPlatform = AreaOf(gs2).Platforms[2];
        Teleport(PlayerOf(gs2), new Vector2(ghostPlatform.Bounds.Left + 20, ghostPlatform.Bounds.Top - PH));
        Frames(gs2, 2);
        // The ghost moves, so aim again at where it is now if an orb misses.
        var ghost = ghosts[0];
        int throws = 0;
        frames = 0;
        while (!ghost.IsDefeated && frames < 300)
        {
            if (PlayerOf(gs2).CanThrow) { ClickAt(gs2, ghost.Center); throws++; }
            Frame(gs2); frames++;
        }
        Check(ghost.IsDefeated && !ghosts.Contains(ghost), $"orbs thrown at the ghost defeat it ({throws} throws, {frames} frames)");

        var dead = new GameplayScreen(LevelData.Load(1), true);
        Frames(dead, 60);
        Set(PlayerOf(dead), "_health", 1);
        PlayerOf(dead).TakeDamage(1, 0);
        Frames(dead, 50);
        ClickAt(dead, new Vector2(960, 500));
        Check(Get<List<Projectile>>(dead, "_projectiles").Count == 0, "can't throw while dead");
        gs.StopSounds(); gs2.StopSounds(); dead.StopSounds();
    }

    PowerUp FindPowerUp(GameplayScreen gs, PowerUpType type) => AreaOf(gs).PowerUps.First(x => x.Type == type);

    void StandOn(GameplayScreen gs, PowerUp powerUp)
    {
        var platforms = AreaOf(gs).Platforms;
        var platform = platforms.Where(pl => StandingArea(pl).Intersects(powerUp.Bounds)).OrderBy(pl => pl.Bounds.Top).Last();
        Teleport(PlayerOf(gs), new Vector2(powerUp.Bounds.Center.X - PW / 2, platform.Bounds.Top - PH));
    }

    float JumpHeight(GameplayScreen gs, int secondPressFrame = -1)
    {
        var p = PlayerOf(gs);
        Frames(gs, 10);
        float y0 = p.Position.Y, minY = y0;
        Frame(gs, Keys.Space);
        bool holding = true;
        for (int i = 0; i < 160; i++)
        {
            // Hold jump on the way up. The second press needs the key let go for a frame first.
            bool press = i == secondPressFrame;
            holding = press || (holding && Rising(p) && i != secondPressFrame - 1);
            Frame(gs, holding ? new[] { Keys.Space } : Array.Empty<Keys>());
            minY = Math.Min(minY, p.Position.Y);
        }
        return y0 - minY;
    }

    void TestPowerUps()
    {
        // Health
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        var heart = FindPowerUp(gs, PowerUpType.Health);
        StandOn(gs, heart);
        Frames(gs, 10);
        Check(!heart.IsCollected, "heart is left alone when health is full");
        Set(p, "_health", 3);
        Teleport(p, p.Position + new Vector2(-150, 0)); Frames(gs, 5);
        StandOn(gs, heart);
        Frames(gs, 5);
        Check(heart.IsCollected && Health(p) == 5, $"heart gives back one heart (health {Health(p)}/6)");
        ShotGame("82_heart_collected", gs);

        // High jump
        var hjGs = new GameplayScreen(LevelData.Load(7), true); // Leap of Faith
        Frames(hjGs, 240);
        float normal = JumpHeight(hjGs);
        Frames(hjGs, 30, Keys.Space);
        Frames(hjGs, 60);
        float normalSecondPress = JumpHeight(hjGs, secondPressFrame: 38);
        Check(Math.Abs(normalSecondPress - normal) < 1f, $"without double jump, pressing jump in the air does nothing ({normalSecondPress:0} vs {normal:0})");
        var highJump = FindPowerUp(hjGs, PowerUpType.HighJump);
        StandOn(hjGs, highJump);
        Frames(hjGs, 5);
        var hp = PlayerOf(hjGs);
        Check(highJump.IsCollected && hp.HasHighJump, "high jump orb collected");
        ShotGame("83_high_jump_hud", hjGs);
        Teleport(hp, new Vector2(40, LevelData.GroundY - PH)); // nothing overhead here
        float high = JumpHeight(hjGs);
        Check(high > normal * 1.4f && high < normal * 1.7f, $"high jump goes higher: {high:0}px vs {normal:0}px");
        Frames(hjGs, 15 * 60);
        Check(!hp.HasHighJump, "high jump wears off after 15 s");

        // Double jump
        var djGs = new GameplayScreen(LevelData.Load(6), true); // Ghost Garden
        Frames(djGs, 240);
        var doubleJump = FindPowerUp(djGs, PowerUpType.DoubleJump);
        StandOn(djGs, doubleJump);
        Frames(djGs, 5);
        Check(doubleJump.IsCollected && PlayerOf(djGs).HasDoubleJump, "double jump orb collected");
        Teleport(PlayerOf(djGs), new Vector2(40, LevelData.GroundY - PH)); // nothing overhead here
        float single = JumpHeight(djGs);
        Frames(djGs, 30);
        float twice = JumpHeight(djGs, secondPressFrame: 25);
        Check(twice > single + 250, $"pressing jump again in the air jumps again: {twice:0}px vs {single:0}px");
        Frames(djGs, 30);
        float thrice = JumpHeight(djGs, secondPressFrame: 25);
        Check(thrice < twice + 50, "only one extra jump per jump");
        gs.StopSounds(); hjGs.StopSounds(); djGs.StopSounds();
    }

    void TestOtherWindowShapes()
    {
        var main = new MainMenu();
        ShotMenu("70_menu_maximized_window", main.Draw, 1904, 1009);
        ShotMenu("71_menu_4x3_window", main.Draw, 1280, 960);

        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        Set(p, "_health", 3);
        ShotGame("72_level1_maximized_window", gs, 1904, 1009);
        VirtualScreen.Resize(1280, 960);
        var gs2 = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs2, 240);
        ShotGame("73_level1_4x3_window", gs2, 1280, 960);
        VirtualScreen.Resize(1280, 960);
        Teleport(PlayerOf(gs2), new Vector2(1760, LevelData.GroundY - 600 - PH));
        Frames(gs2, 120);
        Frame(gs2, Keys.Escape); Frame(gs2);
        ShotGame("74_level1_top_paused_4x3", gs2, 1280, 960);
        VirtualScreen.Resize(1920, 1080);
        gs.StopSounds(); gs2.StopSounds();
    }

    void TestMusicStreams()
    {
        Microsoft.Xna.Framework.Media.MediaPlayer.Volume = 0f;
        Microsoft.Xna.Framework.Media.MediaPlayer.Play(Assets.ThemeSong);
        System.Threading.Thread.Sleep(300);
        var state = Microsoft.Xna.Framework.Media.MediaPlayer.State;
        Microsoft.Xna.Framework.Media.MediaPlayer.Stop();
        Check(state == Microsoft.Xna.Framework.Media.MediaState.Playing && Assets.ThemeSong.Duration.TotalSeconds > 10,
            $"theme song streams ({state}, {Assets.ThemeSong.Duration.TotalSeconds:0} s long)");
    }

    void TestLevelFiles()
    {
        var levels = LevelData.FindLevelNumbers();
        Check(levels.SequenceEqual(Enumerable.Range(1, 12)), "found levels " + string.Join(",", levels));
        var l1 = LevelData.Load(1);
        var a1 = l1.Areas.Single();
        Check(l1.Name == "First Steps" && l1.Title == "Level 1: First Steps" && l1.Description.StartsWith("Learn to walk"), "L1 name, title and description: " + l1.Title);
        Check(a1.Platforms.Count == 6 && a1.Platforms[0].Position == new Vector2(-50, LevelData.GroundY) && a1.Platforms[0].Length == 2880, "L1 ground parsed at groundY, length 2880");
        Check(l1.PlayerSpawn == new Vector2(40, LevelData.GroundY - 100) && a1.Doors.Single().Position == new Vector2(1735, LevelData.GroundY - 735) && !a1.Doors[0].IsLocked && a1.Doors[0].Target == null, "L1 spawn/door parsed");
        Check(a1.Platforms[4].HasMonster && !a1.Platforms.Any(pl => pl.IsCrumbling) && LevelData.Load(3).Areas[0].Platforms[2].IsCrumbling, "monster and crumbling flags");
        Check(a1.Name == "main" && a1.Sky == Color.CornflowerBlue && a1.Tint == Color.White, "L1 has one plain 'main' area");
        var l6 = LevelData.Load(6);
        Check(l6.Areas[0].Ghosts.Count == 3 && l6.Areas[0].Ghosts[0].Radius == 220 && l6.Areas[0].Ghosts[0].Position == new Vector2(850, LevelData.GroundY - 450), "L6 ghosts parsed");
        var l4 = LevelData.Load(4);
        Check(l4.Areas[0].KeyPositions.Single() == new Vector2(310, LevelData.GroundY - 1170) && l4.Areas[0].Doors[0].IsLocked && l4.Areas[0].Doors[0].Position == new Vector2(1650, LevelData.GroundY - 135), "L4 key + locked door parsed");
        Check(levels.All(n => LevelData.Load(n).Name != null && LevelData.Load(n).Description != null), "every level has a name and a description");
    }

    void TestVirtualScreen()
    {
        VirtualScreen.Resize(1280, 720);
        Check(VirtualScreen.ToCentered(new Point(640, 360)) == new Vector2(960, 540), "mouse maps from 1280x720 window to menu coords");
        VirtualScreen.Resize(1904, 1009);
        Check(VirtualScreen.ViewHeight == 1080 && VirtualScreen.ViewWidth > 2037 && VirtualScreen.ViewWidth < 2039, $"maximized window shows a wider view, no bars: {VirtualScreen.ViewWidth}x{VirtualScreen.ViewHeight}");
        Check(Vector2.Distance(VirtualScreen.ToCentered(new Point(952, 504)), new Vector2(960, 540)) < 1f, "mouse at window center maps to menu center on a wide window");
        VirtualScreen.Resize(1280, 960);
        Check(VirtualScreen.ViewWidth == 1920 && VirtualScreen.ViewHeight == 1440, $"4:3 window shows a taller view: {VirtualScreen.ViewWidth}x{VirtualScreen.ViewHeight}");
        VirtualScreen.Resize(1920, 1080);
    }

    void TestMenus()
    {
        _bg = new MenuBackground();
        for (int i = 0; i < 240; i++) _bg.Update(Dt);

        var main = new MainMenu();
        ShotMenu("01_main_menu", main.Draw);
        Press(Keys.Down); main.Update(); Press();
        Check(Get<int>(main, "_selected") == 1, "main menu: Down moves selection");
        var buttons = Get<Rectangle[]>(main, "_buttons");
        SetMouse(buttons[2].Center.ToVector2(), true); Press();
        var choice = main.Update();
        SetMouse(new Vector2(-1000, -1000), false); Press();
        Check(choice == MainMenuChoice.Settings, "main menu: clicking Settings returns Settings (" + choice + ")");
        // Mouse resting on a button must not override keyboard navigation.
        SetMouse(buttons[0].Center.ToVector2(), false); Press(); main.Update();
        Press(Keys.Down); main.Update(); Press(); main.Update();
        Check(Get<int>(main, "_selected") == 1, "main menu: keyboard works while mouse rests on a button");
        SetMouse(new Vector2(-1000, -1000), false); Press();

        var ls = new LevelSelectMenu(LevelData.FindLevelNumbers(), new GameProgress { HighestCompletedLevel = 2 });
        ls.Open();
        ShotMenu("02_level_select", ls.Draw);
        var boxes = Get<Rectangle[]>(ls, "_levelBoxes");
        SetMouse(boxes[1].Center.ToVector2(), true); Press();
        var lsChoice = ls.Update(Dt);
        SetMouse(new Vector2(-1000, -1000), false); Press();
        Check(lsChoice == LevelSelectChoice.Level && ls.ChosenLevel == 2, "level select: clicking box 2 picks level 2");

        var settings = new SettingsMenu(() => { }, () => { });
        settings.Open();
        ShotMenu("03_settings", settings.Draw);
        var dropdown = SettingsControl(settings, "Frame rate");
        SetMouse(dropdown.Center.ToVector2(), true); Press(); settings.Update(Dt);
        SetMouse(dropdown.Center.ToVector2(), false); Press(); settings.Update(Dt);
        Check(Get<bool>(settings, "_dropdownOpen"), "settings: dropdown opens on click");
        ShotMenu("04_settings_dropdown", settings.Draw);
        Press(Keys.Escape); settings.Update(Dt); Press();
        Check(!Get<bool>(settings, "_dropdownOpen"), "settings: Escape closes dropdown");
        SetMouse(new Vector2(-1000, -1000), false); Press();
    }

    void TestLevelsRenderAndGrounding()
    {
        foreach (int n in new[] { 1, 2, 3 })
        {
            var gs = new GameplayScreen(LevelData.Load(n), true);
            Frames(gs, 240);
            var p = PlayerOf(gs);
            int grounded = 0;
            for (int i = 0; i < 120; i++) { Frame(gs); if (p.IsOnGround) grounded++; }
            Check(grounded == 120, $"L{n}: standing player is grounded every frame ({grounded}/120), y={p.Position.Y}");
            ShotGame($"1{n}_level{n}_start", gs);
            gs.StopSounds();
        }
    }

    void TestMovementPriority()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 60);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(120, LevelData.GroundY - PH));
        Frames(gs, 5);
        float x = p.Position.X;
        Frames(gs, 10, Keys.Right);
        Check(p.Position.X - x == 30, "holding Right moves right");
        x = p.Position.X;
        Frames(gs, 10, Keys.Right, Keys.Left);
        Check(p.Position.X - x == -30, "Right held, then Left pressed -> moves left");
        x = p.Position.X;
        Frames(gs, 10, Keys.Left);
        Frames(gs, 10, Keys.Left, Keys.Right);
        Check(p.Position.X - x == 0, "Left held, then Right pressed -> moves right (TODO bug)");
        x = p.Position.X;
        Frames(gs, 10, Keys.D);
        Check(p.Position.X - x == 30, "D moves right (WASD)");
        x = p.Position.X;
        Frames(gs, 10, Keys.A);
        Check(p.Position.X - x == -30, "A moves left (WASD)");
    }

    void TestJump()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 60);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(120, LevelData.GroundY - PH));
        Frames(gs, 5);
        float y0 = p.Position.Y, minY = y0;
        Frame(gs, Keys.Space);
        int landedAt = -1;
        for (int i = 0; i < 120; i++)
        {
            Frame(gs, Rising(p) ? new[] { Keys.Space } : Array.Empty<Keys>());
            minY = Math.Min(minY, p.Position.Y);
            if (p.IsOnGround && landedAt < 0) landedAt = i;
        }
        Check(y0 - minY > 370 && y0 - minY < 400, $"jump height {y0 - minY:0.0}px (expected ~380-390)");
        Check(landedAt > 60 && landedAt < 80, $"lands after {landedAt} frames");
        Frames(gs, 5, Keys.W);
        Check(p.Position.Y < y0 - 20, "W also jumps");
        gs.StopSounds();
        TestJumpFeel();
    }

    void TestJumpFeel()
    {
        // Short hop: tapping jump makes a much lower jump than holding it.
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 60);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(120, LevelData.GroundY - PH));
        Frames(gs, 5);
        float y0 = p.Position.Y, minY = y0;
        Frame(gs, Keys.Space);
        for (int i = 0; i < 90; i++) { Frame(gs); minY = Math.Min(minY, p.Position.Y); }
        Check(y0 - minY > 40 && y0 - minY < 150, $"tapping jump makes a short hop ({y0 - minY:0} px)");

        // Coyote time: a jump just after walking off an edge still works, but not much later.
        // A level with one high platform and nothing around it.
        var ledge = LevelData.FromJson(97, @"{
          ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
          ""areas"": [ { ""name"": ""main"", ""platforms"": [
            { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" },
            { ""x"": 600, ""y"": ""groundY-400"", ""length"": 300 } ] } ]
        }");
        bool JumpAfterWalkingOff(int framesLate)
        {
            var screen = new GameplayScreen(ledge, true);
            Frames(screen, 60);
            var player = PlayerOf(screen);
            var high = AreaOf(screen).Platforms[1];
            Teleport(player, new Vector2(high.Bounds.Right - PW - 10, high.Bounds.Top - PH));
            Frames(screen, 3);
            for (int i = 0; i < 60 && player.IsOnGround; i++)
                Frame(screen, Keys.Right);
            Frames(screen, framesLate - 1, Keys.Right);
            Frame(screen, Keys.Right, Keys.Space);
            bool jumped = Rising(player);
            screen.StopSounds();
            return jumped;
        }
        Check(JumpAfterWalkingOff(3), "coyote time: jumping 3 frames after walking off an edge still works");
        Check(!JumpAfterWalkingOff(12), "coyote time: 12 frames after walking off is too late");

        // Jump buffering: a press a few frames before landing jumps as soon as the player lands.
        GameplayScreen Drop(out Player player)
        {
            var screen = new GameplayScreen(ledge, true);
            Frames(screen, 60);
            player = PlayerOf(screen);
            Teleport(player, new Vector2(100, LevelData.GroundY - PH - 200));
            Frame(screen);
            return screen;
        }
        var falling = Drop(out var faller);
        int landing = 1;
        while (!faller.IsOnGround && landing < 120) { Frame(falling); landing++; }
        falling.StopSounds();
        var buffered = Drop(out var bp);
        Frames(buffered, landing - 5);
        Frame(buffered, Keys.Space);
        bool jumpedOnLanding = false;
        for (int i = 0; i < 6 && !jumpedOnLanding; i++) { Frame(buffered); jumpedOnLanding = Rising(bp); }
        Check(landing > 10 && jumpedOnLanding, $"jump buffering: pressing jump 4 frames before landing jumps on landing (lands after {landing} frames)");
        gs.StopSounds(); buffered.StopSounds();
    }

    // Uses the real Player physics and collision code to find what can be reached in each area, starting
    // from the spawn point or from in front of the area's portals.
    void TestReachability()
    {
        foreach (int n in LevelData.FindLevelNumbers())
        {
            var level = LevelData.Load(n);
            var gs = new GameplayScreen(level, true);
            var areas = Get<List<Area>>(gs, "_areas");
            for (int a = 0; a < areas.Count; a++)
            {
                Area area = areas[a];
                var platforms = area.Platforms;
                string label = $"L{n} {area.Name}";
                if (area.BouncePads.Count > 0 || platforms.Any(pl => pl.IsMoving))
                {
                    // This quick check only knows platforms that stand still; the automated player (--levels) plays these.
                    Console.WriteLine($"      {label}: has bounce pads or moving platforms, checked by the automated player instead");
                    continue;
                }

                var entries = new List<Vector2>();
                if (a == level.StartArea)
                    entries.Add(level.PlayerSpawn);
                foreach (Door portal in area.Doors.Where(d => d.Name != null))
                    entries.Add(portal.ArrivalPosition(new Rectangle(0, 0, PW, PH)));

                var reachable = new HashSet<int>();
                foreach (Vector2 entry in entries)
                {
                    int under = SettleOn(entry, platforms);
                    if (under >= 0) reachable.Add(under);
                }
                var queue = new Queue<int>(reachable);
                while (queue.Count > 0)
                {
                    int from = queue.Dequeue();
                    foreach (int to in Destinations(platforms[from], platforms))
                        if (reachable.Add(to)) queue.Enqueue(to);
                }

                bool CanReach(Rectangle target) => reachable.Any(i => StandingArea(platforms[i]).Intersects(target));
                Check(reachable.Count == platforms.Count, $"{label}: reachable platforms {reachable.Count}/{platforms.Count}");
                foreach (Door door in area.Doors)
                    Check(CanReach(door.Bounds), $"{label}: door {door.Name ?? "(exit)"} can be reached");
                foreach (KeyPickup key in area.Keys)
                    Check(CanReach(key.Bounds), $"{label}: key can be reached");
                foreach (PowerUp powerUp in area.PowerUps)
                    Check(CanReach(powerUp.Bounds), $"{label}: {powerUp.Type} power-up can be reached");
            }
        }
    }

    int SettleOn(Vector2 position, List<Platform> platforms)
    {
        var p = new Player(position);
        for (int i = 0; i < 120 && !p.IsOnGround; i++)
        {
            Press(); p.Update(Dt, NoKeys); p.ApplyGravity(); p.ResolveCollisions(platforms);
        }
        p.StopSounds();
        return PlatformUnder(p, platforms);
    }

    static Rectangle StandingArea(Platform pl)
    {
        var b = pl.Bounds;
        int left = Math.Max(0, b.Left - PW + 1), right = Math.Min(1920, b.Right - 1 + PW);
        return new Rectangle(left, b.Top - PH, right - left, PH);
    }

    static int PlatformUnder(Player p, List<Platform> platforms)
    {
        var b = p.Bounds;
        for (int i = 0; i < platforms.Count; i++)
        {
            var pb = platforms[i].Bounds;
            if (pb.Top == b.Bottom && b.Right > pb.Left && b.Left < pb.Right) return i;
        }
        return -1;
    }

    IEnumerable<int> Destinations(Platform from, List<Platform> platforms)
    {
        var found = new HashSet<int>();
        var fb = from.Bounds;
        int xMin = Math.Max(0, fb.Left - PW + 1), xMax = Math.Min(1920 - PW, fb.Right - 1);
        for (int x = xMin; x <= xMax; x += 12)
        foreach (int dir in new[] { -1, 0, 1 })
        foreach (int delay in new[] { 0, 8, 16, 24, 32 })
        foreach (bool jump in new[] { true, false })
        {
            if (!jump && (dir == 0 || delay > 0)) continue;
            var p = new Player(new Vector2(x, fb.Top - PH));
            Press(); p.Update(Dt, NoKeys); p.ApplyGravity(); p.ResolveCollisions(platforms);
            if (!p.IsOnGround) continue;
            for (int f = 0; f < 240; f++)
            {
                var keys = new List<Keys>();
                if (jump && (f == 0 || Rising(p))) keys.Add(Keys.Space);
                if (dir != 0 && f >= delay) keys.Add(dir < 0 ? Keys.Left : Keys.Right);
                Press(keys.ToArray());
                p.Update(Dt, NoKeys); p.ApplyGravity(); p.ResolveCollisions(platforms);
                if (f > 1 && p.IsOnGround)
                {
                    int under = PlatformUnder(p, platforms);
                    if (under >= 0 && platforms[under] != from) { found.Add(under); break; }
                    if (jump) break;
                }
            }
            p.StopSounds();
            FrameworkDispatcher.Update();
        }
        Press();
        return found;
    }

    void TestKeyFlow()
    {
        var gs = new GameplayScreen(LevelData.Load(4), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        var door = AreaOf(gs).Doors[0];
        Teleport(p, new Vector2(290, LevelData.GroundY - 1050 - PH));
        Frames(gs, 30);
        Check(p.Inventory.Contains(ItemType.Key), "L4: key picked up into inventory");
        Check(AreaOf(gs).Keys[0].IsCollected, "L4: key removed from the level");
        // Drop it with Q, then pick it up again.
        Frame(gs, Keys.Q); Frame(gs);
        var keys = AreaOf(gs).Keys;
        Check(!p.Inventory.Contains(ItemType.Key) && keys.Count == 2 && !keys[1].IsCollected && !keys[1].Bounds.Intersects(p.Bounds), "L4: Q drops the selected key in front of the player");
        ShotGame("23_level4_dropped_key", gs);
        Frames(gs, 20, Keys.Right);
        Check(p.Inventory.Contains(ItemType.Key) && keys[1].IsCollected, "L4: walking into the dropped key picks it up again");
        Frames(gs, 20, Keys.Left);
        Teleport(p, new Vector2(1660, LevelData.GroundY - PH));
        Frames(gs, 10);
        Frame(gs, Keys.D2); Frame(gs);
        Frame(gs, Keys.E); Frame(gs);
        Check(door.State == DoorState.Locked, "L4: E with an empty slot selected does not unlock");
        ShotGame("20_level4_select_key_prompt", gs);
        Frame(gs, Keys.D1); Frame(gs);
        ShotGame("21_level4_unlock_prompt", gs);
        Frame(gs, Keys.E); Frame(gs);
        Check(door.State == DoorState.Closed && !p.Inventory.Contains(ItemType.Key), "L4: E with key selected unlocks and uses the key");
        Frame(gs, Keys.E); Frame(gs);
        Check(door.State == DoorState.Open, "L4: E opens the door");
        ShotGame("22_level4_door_open", gs);
        Frame(gs, Keys.E);
        Check(p.ReachedExit, "L4: E enters the open door");
        var r = Frames(gs, 400);
        Check(r == GameplayResult.LevelCompleted, "L4: level completes after fade (" + r + ")");
    }

    void TestDeathAtTopAndRetry()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        Teleport(p, new Vector2(1760, LevelData.GroundY - 600 - PH));
        Frames(gs, 120);
        ShotGame("30_level1_top_door", gs);
        Set(p, "_health", 1);
        p.TakeDamage(1f, 0);
        Check(p.IsDead, "player dies at 0 health");
        Frames(gs, 20, Keys.Enter);
        Check(PlayerOf(gs) == p, "death menu ignores input while fading in");
        Frames(gs, 60);
        Check(Get<float>(p, "_deathRotation") <= -1.5f, "dead player tips over: rotation " + Get<float>(p, "_deathRotation"));
        ShotGame("31_level1_death_menu_at_top", gs);
        Frame(gs, Keys.Enter); Frame(gs);
        Check(PlayerOf(gs) == p && Get<bool>(gs, "_isRetrying"), "Retry first fades out");
        ShotGame("32_retry_fading", gs);
        Frames(gs, 60);
        var p2 = PlayerOf(gs);
        Check(p2 != p && !p2.IsDead, "then restarts the level and fades back in");
        Check(!Get<PanelMenu>(gs, "_deathMenu").IsOpen, "death menu closed after retry");
    }

    void TestPause()
    {
        var gs = new GameplayScreen(LevelData.Load(2), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        Frame(gs, Keys.Escape); Frame(gs);
        Check(Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "Escape opens pause menu");
        float x = p.Position.X;
        Frames(gs, 30, Keys.Right);
        Check(p.Position.X == x, "world is frozen while paused");
        ShotGame("40_pause", gs);
        Frame(gs, Keys.Escape); Frame(gs);
        Check(!Get<PanelMenu>(gs, "_pauseMenu").IsOpen, "Escape again resumes");
        Frames(gs, 10, Keys.Right);
        Check(p.Position.X > x, "player moves again after resuming");
        Frame(gs, Keys.Escape); Frame(gs);
        Frame(gs, Keys.Up); Frame(gs);
        var r = Frame(gs, Keys.Enter);
        Check(r == GameplayResult.QuitToMenu, "pause > Main menu (Up wraps to the last button) returns to menu");
    }

    void TestCrumbling()
    {
        var gs = new GameplayScreen(LevelData.Load(3), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        var platform = AreaOf(gs).Platforms[2];
        Teleport(p, new Vector2(platform.Bounds.Left + 20, platform.Bounds.Top - PH));
        Frames(gs, 120);
        Check(platform.IsSolid && p.IsOnGround, "crumbling platform holds for a while");
        Frames(gs, 75);
        Check(!platform.IsSolid, "crumbling platform breaks after ~3s");
        float y = p.Position.Y;
        Frames(gs, 10);
        Check(p.Position.Y > y, "player falls when it breaks");
        ShotGame("50_crumbled", gs);
        Frames(gs, 320);
        Check(platform.IsSolid, "crumbled platform comes back after ~5s");
    }

    void TestMonsterAndGhost()
    {
        var gs = new GameplayScreen(LevelData.Load(1), true);
        Frames(gs, 240);
        var p = PlayerOf(gs);
        var pl = AreaOf(gs).Platforms[4];
        Teleport(p, new Vector2(pl.Bounds.Center.X, pl.Bounds.Top - PH));
        int frames = 0;
        while (Health(p) == 6 && frames < 600) { Frame(gs); frames++; }
        Check(Health(p) < 6, $"frog hurts the player (health {Health(p)}/6 after {frames} frames)");
        ShotGame("60_frog_hit", gs);
        gs.StopSounds();

        var gs2 = new GameplayScreen(LevelData.Load(6), true); // Ghost Garden
        Frames(gs2, 240);
        var p2 = PlayerOf(gs2);
        var pl2 = AreaOf(gs2).Platforms[3];
        Teleport(p2, new Vector2(pl2.Bounds.Left + 10, pl2.Bounds.Top - PH));
        frames = 0;
        while (Health(p2) == 6 && frames < 600) { Frame(gs2); frames++; }
        Check(Health(p2) == 5, $"ghost hurts the player by half a heart (health {Health(p2)}/6 after {frames} frames)");
        ShotGame("61_ghost_hit", gs2);
        gs2.StopSounds();
    }
}
