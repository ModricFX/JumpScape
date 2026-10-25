using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Core;

// Runs JumpScape's automated checks:
//   dotnet run --project Tests                    every check, with screenshots
//   dotnet run --project Tests -- --levels        the automated player plays every level to the end
//   dotnet run --project Tests -- --levels 3,7    only some levels
//   dotnet run --project Tests -- --overview      a picture of every area of every level
// The exit code is the number of failed checks.
static class Program
{
    static int Main(string[] args)
    {
        // Saves go to a temporary folder, so the checks never touch your own settings and progress.
        string saves = Path.Combine(Path.GetTempPath(), "jumpscape-test-saves");
        if (Directory.Exists(saves))
            Directory.Delete(saves, true);
        typeof(SaveFile).GetProperty("Folder").SetValue(null, saves);

        TestMode mode = TestMode.All;
        HashSet<int> onlyLevels = null;
        if (args.Length > 0 && args[0] == "--levels")
        {
            mode = TestMode.Levels;
            if (args.Length > 1)
                onlyLevels = args[1].Split(',').Select(int.Parse).ToHashSet();
        }
        else if (args.Length > 0 && args[0] == "--overview")
        {
            mode = TestMode.Overview;
        }
        else if (args.Length > 0)
        {
            Console.WriteLine("Usage: dotnet run --project Tests [-- --levels [1,2,...] | --overview]");
            return 1;
        }

        string screenshots = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(screenshots);

        using var game = new TestGame(screenshots, mode, onlyLevels);
        game.Run();
        if (mode != TestMode.Levels)
            Console.WriteLine("Screenshots are in " + screenshots);
        return game.Failures;
    }
}
