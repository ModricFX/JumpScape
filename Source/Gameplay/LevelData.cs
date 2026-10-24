using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JumpScape.Core;
using Microsoft.Xna.Framework;

namespace JumpScape.Gameplay
{
    public sealed record PlatformInfo(Vector2 Position, int Length, bool HasMonster, bool IsCrumbling);

    public sealed record GhostInfo(Vector2 Position, float Radius);

    public sealed record PowerUpInfo(Vector2 Position, PowerUpType Type);

    /// <summary>
    /// A door. Exit doors finish the level. Portal doors have a <see cref="Name"/> and lead to the portal
    /// called <see cref="Target"/>, which can be in another area.
    /// </summary>
    public sealed record DoorInfo(Vector2 Position, bool IsLocked, string Name = null, string Target = null);

    /// <summary>One part of a level with its own sky, platforms, enemies, items and doors.</summary>
    public sealed class AreaData
    {
        public AreaData(string name, Color sky, Color tint)
        {
            Name = name;
            Sky = sky;
            Tint = tint;
        }

        public string Name { get; }

        /// <summary>Background color.</summary>
        public Color Sky { get; }

        /// <summary>Color the platforms are tinted with (white leaves them unchanged).</summary>
        public Color Tint { get; }

        public List<PlatformInfo> Platforms { get; } = new List<PlatformInfo>();
        public List<GhostInfo> Ghosts { get; } = new List<GhostInfo>();
        public List<PowerUpInfo> PowerUps { get; } = new List<PowerUpInfo>();
        public List<Vector2> KeyPositions { get; } = new List<Vector2>();
        public List<DoorInfo> Doors { get; } = new List<DoorInfo>();
    }

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

        public static readonly Color DefaultSky = Color.CornflowerBlue;

        private static readonly string LevelsFolder = Path.Combine(AppContext.BaseDirectory, "Levels");
        private static readonly Regex LevelFileName = new Regex(@"^Level(\d+)\.txt$", RegexOptions.IgnoreCase);

        private AreaData _currentArea;

        private LevelData(int number)
        {
            Number = number;
        }

        public int Number { get; }

        public Vector2 PlayerSpawn { get; private set; }

        /// <summary>Index in <see cref="Areas"/> of the area the player starts in.</summary>
        public int StartArea { get; private set; }

        /// <summary>
        /// All areas of the level. Everything before the first "Area:" line goes into an area called "main".
        /// </summary>
        public List<AreaData> Areas { get; } = new List<AreaData>();

        private AreaData CurrentArea
        {
            get
            {
                if (_currentArea == null)
                {
                    _currentArea = new AreaData("main", DefaultSky, Color.White);
                    Areas.Add(_currentArea);
                }
                return _currentArea;
            }
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
            return Parse(number, File.ReadLines(Path.Combine(LevelsFolder, $"Level{number}.txt")));
        }

        /// <summary>Reads a level from the lines of a level file. Lines that can't be read are skipped.</summary>
        public static LevelData Parse(int number, IEnumerable<string> lines)
        {
            var level = new LevelData(number);
            int lineNumber = 0;

            foreach (string rawLine in lines)
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
                    level.Warn($"line {lineNumber} skipped: {e.Message}");
                }
            }

            _ = level.CurrentArea; // Every level has at least one area.
            level.CheckPortals();
            return level;
        }

        private void Warn(string message)
        {
            Debug.WriteLine($"Level{Number}.txt: {message}");
        }

        /// <summary>Reports portals that lead nowhere. They behave like exit doors so the level stays playable.</summary>
        private void CheckPortals()
        {
            List<DoorInfo> doors = Areas.SelectMany(area => area.Doors).ToList();
            var portalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DoorInfo door in doors.Where(door => door.Name != null))
            {
                if (!portalNames.Add(door.Name))
                    Warn($"more than one portal is called '{door.Name}'");
            }

            foreach (DoorInfo door in doors.Where(door => door.Target != null))
            {
                if (!portalNames.Contains(door.Target))
                    Warn($"portal '{door.Name}' leads to '{door.Target}', which doesn't exist");
            }
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
                case "Area":
                    if (values[0].Length == 0)
                        throw new FormatException("an area needs a name");
                    _currentArea = new AreaData(
                        values[0],
                        values.Length > 1 ? ParseColor(values[1]) : DefaultSky,
                        values.Length > 2 ? ParseColor(values[2]) : Color.White);
                    Areas.Add(_currentArea);
                    break;

                case "PlayerSpawn":
                    RequireValues(values, 2);
                    PlayerSpawn = ParsePosition(values);
                    StartArea = Areas.IndexOf(CurrentArea);
                    break;

                case "KeyPosition":
                    RequireValues(values, 2);
                    CurrentArea.KeyPositions.Add(ParsePosition(values));
                    break;

                case "DoorPosition":
                    RequireValues(values, 3);
                    CurrentArea.Doors.Add(new DoorInfo(ParsePosition(values), ParseFlag(values[2])));
                    break;

                case "Portal":
                    RequireValues(values, 5);
                    CurrentArea.Doors.Add(new DoorInfo(ParsePosition(values), ParseFlag(values[2]), values[3], values[4]));
                    break;

                case "Platform":
                    RequireValues(values, 5);
                    CurrentArea.Platforms.Add(new PlatformInfo(
                        ParsePosition(values),
                        (int)ParseNumber(values[2]),
                        HasMonster: ParseFlag(values[3]),
                        IsCrumbling: ParseFlag(values[4])));
                    break;

                case "Ghost":
                    RequireValues(values, 3);
                    CurrentArea.Ghosts.Add(new GhostInfo(ParsePosition(values), ParseNumber(values[2])));
                    break;

                case "PowerUp":
                    RequireValues(values, 3);
                    if (!Enum.TryParse(values[2], ignoreCase: true, out PowerUpType powerUpType) || !Enum.IsDefined(powerUpType))
                        throw new FormatException($"unknown power-up '{values[2]}' (use health, doubleJump or highJump)");
                    CurrentArea.PowerUps.Add(new PowerUpInfo(ParsePosition(values), powerUpType));
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

        /// <summary>Parses a color: a hex code like #4A0D12, or a name such as DarkRed (any MonoGame color name works).</summary>
        private static Color ParseColor(string value)
        {
            if (value.Length == 7 && value[0] == '#'
                && int.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            {
                return new Color((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }

            PropertyInfo property = typeof(Color).GetProperty(value, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (property?.PropertyType == typeof(Color))
                return (Color)property.GetValue(null);

            throw new FormatException($"unknown color '{value}' (use a hex code like #4A0D12 or a name like DarkRed)");
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
