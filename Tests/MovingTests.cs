using System;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// Checks for moving platforms and bounce pads.
partial class TestGame
{
    const string MovingLevel = @"{
      ""spawn"": { ""x"": 100, ""y"": ""groundY-100"" },
      ""areas"": [ { ""name"": ""main"",
        ""platforms"": [
          { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" },
          { ""x"": 400, ""y"": ""groundY-200"", ""length"": 200, ""moveTo"": { ""x"": 1000, ""y"": ""groundY-200"" }, ""moveTime"": 2 },
          { ""x"": 1400, ""y"": ""groundY-150"", ""length"": 200, ""moveTo"": { ""x"": 1400, ""y"": ""groundY-650"" }, ""moveTime"": 2 }
        ],
        ""bouncePads"": [ { ""x"": 200, ""y"": ""groundY-30"" } ] } ]
    }";

    void TestMovingPlatforms()
    {
        var level = LevelData.FromJson(95, MovingLevel);
        var info = level.Areas[0].Platforms[1];
        Check(info.MoveTo == new Vector2(1000, LevelData.GroundY - 200) && info.MoveTime == 2f, "moving platforms: moveTo and moveTime are read");

        var gs = new GameplayScreen(level, true);
        Frames(gs, 30);
        var p = PlayerOf(gs);
        var slider = AreaOf(gs).Platforms[1];
        var lift = AreaOf(gs).Platforms[2];

        // Ride the sliding platform: the player moves with it without walking.
        Teleport(p, slider.Position + new Vector2(50, -PH));
        Frames(gs, 3);
        float offset = p.Position.X - slider.Position.X;
        Vector2 before = slider.Position;
        Frames(gs, 40);
        Check(slider.Position.X > before.X + 50 && p.IsOnGround && p.StandingOn == slider && Math.Abs(p.Position.X - slider.Position.X - offset) < 1.5f,
            $"moving platforms: a sliding platform carries the player ({before.X:0} -> {slider.Position.X:0})");
        ShotGame("170_moving_platform", gs);
        Frames(gs, 2 * 60);
        Check(slider.Position.X < 1000 && slider.Position.X > 400, "moving platforms: it glides back and forth");

        // Ride the lift up.
        Teleport(p, lift.Position + new Vector2(50, -PH));
        Frames(gs, 3);
        float startY = p.Position.Y;
        int frames = 0;
        while (lift.Position.Y > LevelData.GroundY - 640 && frames < 300) { Frame(gs); frames++; }
        Check(p.IsOnGround && p.StandingOn == lift && startY - p.Position.Y > 300, $"moving platforms: a lift carries the player up ({startY - p.Position.Y:0} px)");

        // Jumping off a moving platform works like jumping off any other.
        Frame(gs, Keys.Space);
        Check(Rising(p) && p.StandingOn == null, "moving platforms: the player can jump off");
        gs.StopSounds();

        try
        {
            LevelData.FromJson(95, MovingLevel.Replace(@"""moveTime"": 2 },", @"""moveTime"": 2, ""monster"": true },"));
            Check(false, "level files: a monster on a moving platform is reported");
        }
        catch (InvalidDataException e)
        {
            Check(e.Message.Contains("moving platform"), "level files: a monster on a moving platform is reported: " + e.Message);
        }
    }

    // Only possible with the pad (up to A), the ferry (A to B) and the lift (B up to C, where the exit is).
    const string RideLevel = @"{
      ""spawn"": { ""x"": 40, ""y"": ""groundY-100"" },
      ""areas"": [ { ""name"": ""main"",
        ""platforms"": [
          { ""x"": -50, ""y"": ""groundY"", ""length"": ""groundLength"" },
          { ""x"": 450, ""y"": ""groundY-500"", ""length"": 250 },
          { ""x"": 800, ""y"": ""groundY-500"", ""length"": 150, ""moveTo"": { ""x"": 1200, ""y"": ""groundY-500"" }, ""moveTime"": 2.5 },
          { ""x"": 1450, ""y"": ""groundY-500"", ""length"": 250 },
          { ""x"": 1750, ""y"": ""groundY-500"", ""length"": 150, ""moveTo"": { ""x"": 1750, ""y"": ""groundY-900"" }, ""moveTime"": 2.5 },
          { ""x"": 1450, ""y"": ""groundY-900"", ""length"": 200 }
        ],
        ""bouncePads"": [ { ""x"": 300, ""y"": ""groundY-30"" } ],
        ""doors"": [ { ""x"": 1500, ""y"": ""groundY-1035"" } ] } ]
    }";

    void TestAutomatedPlayerRides()
    {
        var report = PlayLevelWithBot(LevelData.FromJson(94, RideLevel), verbose: true);
        Check(report.Finished, $"automated player: uses a bounce pad, a moving platform and a lift to finish ({report.Seconds:0.0}s, {report.Replans} replans)" + (report.Problem == null ? "" : " PROBLEM: " + report.Problem));
    }

    void TestBouncePads()
    {
        var level = LevelData.FromJson(95, MovingLevel);
        Check(level.Areas[0].BouncePads.Single() == new Vector2(200, LevelData.GroundY - 30), "bounce pads: read from the level file");

        var gs = new GameplayScreen(level, true);
        Frames(gs, 30);
        var p = PlayerOf(gs);
        var pad = AreaOf(gs).BouncePads[0];
        Teleport(p, new Vector2(pad.Bounds.X - PW - 5, LevelData.GroundY - PH));
        Frames(gs, 3);
        float y0 = p.Position.Y, minY = y0;
        bool launched = false;
        for (int i = 0; i < 20 && !launched; i++) { Frame(gs, Keys.Right); launched = Rising(p); }
        Frames(gs, 6);
        ShotGame("171_bounce_pad", gs);
        for (int i = 0; i < 80; i++) { Frame(gs); minY = Math.Min(minY, p.Position.Y); }
        Check(launched && y0 - minY > 550 && y0 - minY < 700, $"bounce pads: walking onto a pad launches the player ({y0 - minY:0} px up)");
        gs.StopSounds();
    }
}
