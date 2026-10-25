using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Checks for checkpoints, spikes, hidden gems and medals.
partial class TestGame
{
    void TestCheckpoints()
    {
        var level = LevelData.Load(9);
        var gs = new GameplayScreen(level, true);
        Frames(gs, 120);
        var p = PlayerOf(gs);
        var flag = AreaOf(gs).Checkpoints[0];
        Check(!flag.IsActive, "checkpoint: flags start grey");
        Teleport(p, flag.ArrivalPosition(p.Bounds));
        Frames(gs, 5);
        Check(flag.IsActive && Get<Checkpoint>(gs, "_checkpoint") == flag, "checkpoint: touching a flag makes it the checkpoint");
        Frames(gs, 30);
        ShotGame("150_checkpoint", gs);

        // Carry a key away from the flag, then die.
        p.Inventory.TryAdd(ItemType.Key);
        Teleport(p, new Vector2(120, LevelData.GroundY - PH));
        Frames(gs, 5);
        Set(p, "_health", 1);
        p.TakeDamage(1f, 0);
        Frames(gs, 60);
        var menu = Get<PanelMenu>(gs, "_checkpointDeathMenu");
        Check(menu.IsOpen && !Get<PanelMenu>(gs, "_deathMenu").IsOpen, "checkpoint: the death screen offers to go back to the checkpoint");
        ShotGame("151_checkpoint_death_menu", gs);
        Frame(gs, Keys.Enter); Frames(gs, 60);
        Check(PlayerOf(gs) == p && !p.IsDead && Health(p) == 6 && p.Bounds.Intersects(flag.Bounds) && p.Inventory.Contains(ItemType.Key),
            "checkpoint: Back to checkpoint brings the player back there with full health and their key");

        // A run that used a checkpoint doesn't count as a best time.
        File.Delete(Path.Combine(SaveFile.Folder, "best-run-level9.bin"));
        StandAt(gs, AreaOf(gs).Doors.Single());
        Frame(gs, Keys.E); Frame(gs); Frame(gs, Keys.E);
        Frames(gs, 120);
        string text = string.Join(" ", Get<List<string>>(Get<PanelMenu>(gs, "_completeMenu"), "_subtitleLines"));
        Check(text.Contains("checkpoint") && RunRecording.LoadBestTime(9, level.ContentHash) == null && !text.Contains("medal"),
            "checkpoint: the run isn't saved as a best time and earns no medal: " + text);
        gs.StopSounds();

        // Restart level clears the checkpoint.
        var again = new GameplayScreen(level, true);
        Frames(again, 120);
        var q = PlayerOf(again);
        Teleport(q, AreaOf(again).Checkpoints[0].ArrivalPosition(q.Bounds));
        Frames(again, 5);
        Set(q, "_health", 1);
        q.TakeDamage(1f, 0);
        Frames(again, 60);
        Frame(again, Keys.Down); Frame(again); Frame(again, Keys.Enter); Frames(again, 60);
        Check(PlayerOf(again) != q && Get<Checkpoint>(again, "_checkpoint") == null && !AreaOf(again).Checkpoints.Any(c => c.IsActive),
            "checkpoint: Restart level starts again without a checkpoint");
        again.StopSounds();
    }

    void TestSpikes()
    {
        var gs = new GameplayScreen(LevelData.Load(6), true);
        Frames(gs, 120);
        var p = PlayerOf(gs);
        var spikes = AreaOf(gs).Spikes[0];
        Teleport(p, new Vector2(spikes.Bounds.Center.X - PW / 2f, LevelData.GroundY - PH - 80));
        int frames = 0;
        while (Health(p) == 6 && frames < 60) { Frame(gs); frames++; }
        Check(Health(p) == 4 && Rising(p), $"spikes: landing on spikes costs a heart and bounces the player up (health {Health(p)}/6)");
        Frames(gs, 6);
        ShotGame("152_spikes", gs);
        gs.StopSounds();
    }

    void TestGems()
    {
        var level = LevelData.Load(1);
        Check(level.GemCount == 2 && LevelData.FindLevelNumbers().All(n => LevelData.Load(n).GemCount >= 2), "gems: every level hides at least two gems");

        var gs = new GameplayScreen(level, true);
        Frames(gs, 120);
        var p = PlayerOf(gs);
        var gem = AreaOf(gs).Gems[0];
        Teleport(p, new Vector2(gem.Bounds.X - 20, LevelData.GroundY - PH));
        Frames(gs, 10);
        Check(gem.IsCollected && gs.GemsFound.Contains(gem.Index), "gems: touching a gem collects it");
        ShotGame("153_gem_found", gs);
        gs.StopSounds();

        var progress = new GameProgress();
        progress.AddGems(1, gs.GemsFound);
        progress.AddGems(1, new[] { 1, gem.Index });
        Check(progress.GemsFoundIn(1).SequenceEqual(new[] { 0, 1 }), "gems: gems found on different runs add up");
        var again = new GameplayScreen(level, true, progress.GemsFoundIn(1));
        Check(AreaOf(again).Gems.All(g => g.WasFoundBefore && !g.IsCollected), "gems: gems found before are shown see-through and can be collected again");
        again.StopSounds();

        var parsed = LevelData.FromJson(96, @"{
          ""medals"": { ""gold"": 10, ""silver"": 20, ""bronze"": 30 },
          ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
          ""areas"": [ { ""name"": ""main"",
            ""platforms"": [ { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" } ],
            ""checkpoints"": [ { ""x"": 500, ""y"": ""groundY-120"" } ],
            ""spikes"": [ { ""x"": 700, ""y"": ""groundY-30"", ""length"": 90 } ],
            ""gems"": [ { ""x"": 900, ""y"": ""groundY-200"" }, { ""x"": 1000, ""y"": ""groundY-200"" } ] } ]
        }");
        AreaData area = parsed.Areas[0];
        Check(area.Checkpoints.Single() == new Vector2(500, LevelData.GroundY - 120) && area.Spikes.Single().Length == 90 && area.Gems.Count == 2
            && parsed.Medals == new MedalTimes(10, 20, 30), "level files: checkpoints, spikes, gems and medals are read");
        try
        {
            LevelData.FromJson(96, @"{ ""medals"": { ""gold"": 30, ""silver"": 20, ""bronze"": 40 }, ""spawn"": { ""x"": 1, ""y"": 2 }, ""areas"": [ { ""name"": ""main"" } ] }");
            Check(false, "level files: medal times in the wrong order are reported");
        }
        catch (InvalidDataException e)
        {
            Check(e.Message.Contains("fastest to slowest"), "level files: medal times in the wrong order are reported: " + e.Message);
        }
    }

    void TestMedals()
    {
        var times = new MedalTimes(10, 20, 30);
        Check(times.For(9.5f) == Medal.Gold && times.For(20) == Medal.Silver && times.For(25) == Medal.Bronze && times.For(31) == Medal.None
            && times.NextTarget(Medal.Silver) == 10 && times.NextTarget(Medal.Gold) == null, "medals: times give the right medal");
        Check(LevelData.FindLevelNumbers().All(n => LevelData.Load(n).Medals != null), "medals: every level has medal times");

        // A quick finish of level 3 earns gold.
        File.Delete(Path.Combine(SaveFile.Folder, "best-run-level3.bin"));
        var gs = FinishLevel3(hasNextLevel: true, out _);
        Frames(gs, 40);
        string text = string.Join(" ", Get<List<string>>(Get<PanelMenu>(gs, "_completeMenu"), "_subtitleLines"));
        Check(Get<Medal>(gs, "_medal") == Medal.Gold && text.Contains("Gold medal!"), "medals: a fast finish earns gold: " + text);
        ShotGame("154_gold_medal", gs);
        gs.StopSounds();

        // The level picker shows medals, gems and the total of the best times.
        var progress = new GameProgress { HighestCompletedLevel = 3 };
        progress.AddGems(1, new[] { 0, 1 });
        var picker = new LevelSelectMenu(LevelData.FindLevelNumbers(), progress);
        picker.Open();
        string summary = (string)typeof(LevelSelectMenu).GetMethod("Summary", Any).Invoke(picker, null);
        Check(summary.Contains("Gems 2/") && summary.Contains("Best times add up to"), "level picker: " + summary);
        Set(picker, "_selected", 2);
        Set(picker, "_shownLevel", 2);
        ShotMenu("155_level_picker_medals", picker.Draw);
    }
}
