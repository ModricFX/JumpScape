using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JumpScape.Core;
using Microsoft.Xna.Framework;

namespace JumpScape.Gameplay
{
    public sealed record PlatformInfo(Vector2 Position, int Length, bool HasMonster, bool IsCrumbling);

    public sealed record GhostInfo(Vector2 Position, float Radius);

    public sealed record PowerUpInfo(Vector2 Position, PowerUpType Type);

    /// <summary>
    /// A level read from Levels/LevelN.txt. Coordinates are in the 1920x1080 game world;
    /// see Levels/README.md for the file format.
    /// </summary>
    public sealed class LevelData
    {
        /// <summary>
        /// The top of the ground, written as "groundY" in level files. The grass texture is 63 pixels tall,
        /// so this puts the ground right on the bottom edge of the screen.
        /// </summary>
        public const float GroundY = VirtualScreen.Height - 63;

        /// <summary>Written as "groundLength" in level files: wide enough to cover the whole screen.</summary>
        public const float GroundLength = VirtualScreen.Width * 1.5f;

        private static readonly string LevelsFolder = Path.Combine(AppContext.BaseDirectory, "Levels");
        private static readonly Regex LevelFileName = new Regex(@"^Level(\d+)\.txt$", RegexOptions.IgnoreCase);

        public int Number { get; }
        public Vector2 PlayerSpawn { get; private set; }
        public List<Vector2> KeyPositions { get; } = new List<Vector2>();
        public Vector2 DoorPosition { get; private set; }
        public bool IsDoorLocked { get; private set; }
        public List<PlatformInfo> Platforms { get; } = new List<PlatformInfo>();
        public List<GhostInfo> Ghosts { get; } = new List<GhostInfo>();
        public List<PowerUpInfo> PowerUps { get; } = new List<PowerUpInfo>();

        private LevelData(int number)
        {
            Number = number;
        }

        /// <summary>The numbers of all LevelN.txt files in the Levels folder, sorted.</summary>
        public static List<int> FindLevelNumbers()
        {
            if (!Directory.Exists(LevelsFolder))
                return new List<int>();

            return Directory.EnumerateFiles(LevelsFolder)
                .Select(path => LevelFileName.Match(Path.GetFileName(path)))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .OrderBy(number => number)
                .ToList();
        }

        public static LevelData Load(int number)
        {
            string path = Path.Combine(LevelsFolder, $"Level{number}.txt");
            var level = new LevelData(number);
            int lineNumber = 0;

            foreach (string rawLine in File.ReadLines(path))
            {
                lineNumber++;
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                try
                {
                    level.ParseLine(line);
                }
                catch (FormatException e)
                {
                    Debug.WriteLine($"Level{number}.txt line {lineNumber} skipped: {e.Message}");
                }
            }

            return level;
        }

        private void ParseLine(string line)
        {
            int colon = line.IndexOf(':');
            if (colon < 0)
                throw new FormatException("expected 'Type: values'");

            string type = line.Substring(0, colon).Trim();
            string[] values = line.Substring(colon + 1).Split(',', StringSplitOptions.TrimEntries);

            switch (type)
            {
                case "PlayerSpawn":
                    RequireValues(values, 2);
                    PlayerSpawn = ParsePosition(values);
                    break;

                case "KeyPosition":
                    RequireValues(values, 2);
                    KeyPositions.Add(ParsePosition(values));
                    break;

                case "DoorPosition":
                    RequireValues(values, 3);
                    DoorPosition = ParsePosition(values);
                    IsDoorLocked = ParseFlag(values[2]);
                    break;

                case "Platform":
                    RequireValues(values, 5);
                    Platforms.Add(new PlatformInfo(
                        ParsePosition(values),
                        (int)ParseNumber(values[2]),
                        HasMonster: ParseFlag(values[3]),
                        IsCrumbling: ParseFlag(values[4])));
                    break;

                case "Ghost":
                    RequireValues(values, 3);
                    Ghosts.Add(new GhostInfo(ParsePosition(values), ParseNumber(values[2])));
                    break;

                case "PowerUp":
                    RequireValues(values, 3);
                    if (!Enum.TryParse(values[2], ignoreCase: true, out PowerUpType powerUpType) || !Enum.IsDefined(powerUpType))
                        throw new FormatException($"unknown power-up '{values[2]}' (use health, doubleJump or highJump)");
                    PowerUps.Add(new PowerUpInfo(ParsePosition(values), powerUpType));
                    break;

                default:
                    throw new FormatException($"unknown type '{type}'");
            }
        }

        private static void RequireValues(string[] values, int count)
        {
            if (values.Length < count)
                throw new FormatException($"expected {count} values but found {values.Length}");
        }

        private static Vector2 ParsePosition(string[] values) => new Vector2(ParseNumber(values[0]), ParseNumber(values[1]));

        private static bool ParseFlag(string value)
        {
            return value switch
            {
                "1" => true,
                "0" => false,
                _ => throw new FormatException($"expected 0 or 1 but found '{value}'"),
            };
        }

        /// <summary>Parses a number, "groundY" or "groundLength", optionally followed by +N or -N.</summary>
        private static float ParseNumber(string value)
        {
            float baseValue = 0f;
            string offset = value;

            if (value.StartsWith("groundY", StringComparison.OrdinalIgnoreCase))
            {
                baseValue = GroundY;
                offset = value.Substring("groundY".Length);
            }
            else if (value.StartsWith("groundLength", StringComparison.OrdinalIgnoreCase))
            {
                baseValue = GroundLength;
                offset = value.Substring("groundLength".Length);
            }

            offset = offset.Replace(" ", "");
            if (offset.Length == 0)
                return baseValue;

            if (float.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                return baseValue + number;

            throw new FormatException($"'{value}' is not a number");
        }
    }
}
