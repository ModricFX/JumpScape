using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    /// <summary>A friendly character the player can talk to, and the lines it says one after another.</summary>
    public sealed record NpcInfo(Vector2 Position, string Name, List<string> Lines);

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
        public List<NpcInfo> Npcs { get; } = new List<NpcInfo>();
    }

    /// <summary>
    /// A level read from Levels/LevelN.json. Coordinates are in the 1920x1080 game world;
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
        private static readonly Regex LevelFileName = new Regex(@"^Level(\d+)\.json$", RegexOptions.IgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new LevelNumberConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private LevelData(int number)
        {
            Number = number;
        }

        public int Number { get; }

        public Vector2 PlayerSpawn { get; private set; }

        /// <summary>Index in <see cref="Areas"/> of the area the player starts in.</summary>
        public int StartArea { get; private set; }

        /// <summary>A fingerprint of the level, so saved best runs are only replayed on the level they were made on.</summary>
        public uint ContentHash { get; private set; }

        public List<AreaData> Areas { get; } = new List<AreaData>();

        /// <summary>The numbers of all LevelN.json files in the Levels folder, sorted.</summary>
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

        /// <summary>Loads Levels/LevelN.json. Throws <see cref="InvalidDataException"/> or <see cref="IOException"/> if it can't.</summary>
        public static LevelData Load(int number)
        {
            return FromJson(number, File.ReadAllText(Path.Combine(LevelsFolder, $"Level{number}.json")));
        }

        /// <summary>
        /// Reads a level from the text of a JSON level file. Mistakes throw an <see cref="InvalidDataException"/>
        /// whose message names the file and line, so they can be shown to whoever is editing the level.
        /// </summary>
        public static LevelData FromJson(int number, string json)
        {
            string fileName = $"Level{number}.json";
            LevelFile file;
            try
            {
                file = JsonSerializer.Deserialize<LevelFile>(json, JsonOptions);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"{fileName}, line {(e.LineNumber ?? 0) + 1}: {Explain(e)}", e);
            }

            if (file == null)
                throw new InvalidDataException($"{fileName} is empty.");
            if (file.Areas.Count == 0)
                throw new InvalidDataException($"{fileName}: a level needs at least one area.");

            var level = new LevelData(number) { PlayerSpawn = new Vector2(file.Spawn.X, file.Spawn.Y) };
            foreach (AreaJson area in file.Areas)
                level.Areas.Add(ToAreaData(area, fileName));

            if (file.StartArea != null)
            {
                level.StartArea = level.Areas.FindIndex(area => string.Equals(area.Name, file.StartArea, StringComparison.OrdinalIgnoreCase));
                if (level.StartArea < 0)
                    throw new InvalidDataException($"{fileName}: \"startArea\" is \"{file.StartArea}\", but there is no area with that name.");
            }

            // The fingerprint ignores spacing, comments and the description, so only real changes reset the best run.
            level.ContentHash = Fnv1a(JsonSerializer.Serialize(new { file.Spawn, file.StartArea, file.Areas }, JsonOptions));
            level.CheckPortals();
            return level;
        }

        private static AreaData ToAreaData(AreaJson json, string fileName)
        {
            Color sky = DefaultSky;
            Color tint = Color.White;
            try
            {
                if (json.Sky != null)
                    sky = ParseColor(json.Sky);
                if (json.Tint != null)
                    tint = ParseColor(json.Tint);
            }
            catch (FormatException e)
            {
                throw new InvalidDataException($"{fileName}, area \"{json.Name}\": {e.Message}");
            }

            var area = new AreaData(json.Name, sky, tint);
            foreach (PlatformJson platform in json.Platforms)
                area.Platforms.Add(new PlatformInfo(new Vector2(platform.X, platform.Y), (int)platform.Length, platform.Monster, platform.Crumbling));
            foreach (DoorJson door in json.Doors)
                area.Doors.Add(new DoorInfo(new Vector2(door.X, door.Y), door.Locked));
            foreach (PortalJson portal in json.Portals)
                area.Doors.Add(new DoorInfo(new Vector2(portal.X, portal.Y), portal.Locked, portal.Name, portal.LeadsTo));
            foreach (PointJson key in json.Keys)
                area.KeyPositions.Add(new Vector2(key.X, key.Y));
            foreach (GhostJson ghost in json.Ghosts)
                area.Ghosts.Add(new GhostInfo(new Vector2(ghost.X, ghost.Y), ghost.Radius));
            foreach (PowerUpJson powerUp in json.PowerUps)
                area.PowerUps.Add(new PowerUpInfo(new Vector2(powerUp.X, powerUp.Y), powerUp.Type));
            foreach (NpcJson npc in json.Npcs)
                area.Npcs.Add(new NpcInfo(new Vector2(npc.X, npc.Y), npc.Name, new List<string>(npc.Lines)));
            return area;
        }

        /// <summary>Turns the JSON reader's error messages into plain advice for whoever is editing the level.</summary>
        private static string Explain(JsonException e)
        {
            string message = e.Message;
            int details = message.IndexOf(" Path:", StringComparison.Ordinal);
            if (details >= 0)
                message = message.Substring(0, details);

            Match unknown = Regex.Match(message, @"property '(.+?)' could not be mapped .*type '[\w.]*?\.(\w+?)(?:Json|File)'");
            if (unknown.Success)
                return $"\"{unknown.Groups[1].Value}\" isn't a setting a {Describe(unknown.Groups[2].Value)} can have. Check the spelling.";

            Match missing = Regex.Match(message, @"type '[\w.]*?\.(\w+?)(?:Json|File)' was missing required properties, including the following: (.+)$");
            if (missing.Success)
                return $"a {Describe(missing.Groups[1].Value)} needs \"{missing.Groups[2].Value.TrimEnd('.')}\".";

            if (message.Contains(nameof(PowerUpType), StringComparison.Ordinal))
                return "unknown power-up type. Use health, doubleJump or highJump.";

            return message;
        }

        private static string Describe(string typeName)
        {
            return typeName switch
            {
                "Level" => "level",
                "Point" => "position",
                "PowerUp" => "power-up",
                "Npc" => "character",
                _ => typeName.ToLowerInvariant(),
            };
        }

        /// <summary>FNV-1a, which (unlike string.GetHashCode) gives the same result every time the game runs.</summary>
        private static uint Fnv1a(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }

        /// <summary>Reports portals that lead nowhere. They behave like exit doors so the level stays playable.</summary>
        private void CheckPortals()
        {
            List<DoorInfo> doors = Areas.SelectMany(area => area.Doors).ToList();
            var portalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DoorInfo door in doors.Where(door => door.Name != null))
            {
                if (!portalNames.Add(door.Name))
                    Debug.WriteLine($"Level{Number}.json: more than one portal is called '{door.Name}'");
            }

            foreach (DoorInfo door in doors.Where(door => door.Target != null))
            {
                if (!portalNames.Contains(door.Target))
                    Debug.WriteLine($"Level{Number}.json: portal '{door.Name}' leads to '{door.Target}', which doesn't exist");
            }
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

            throw new FormatException($"unknown color \"{value}\". Use a hex code like #4A0D12 or a name like DarkRed.");
        }

        /// <summary>Parses a number, "groundY" or "groundLength", optionally followed by +N or -N.</summary>
        public static bool TryParseNumber(string value, out float number)
        {
            number = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
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
            {
                number = baseValue;
                return true;
            }

            if (!float.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return false;

            number = baseValue + parsed;
            return true;
        }
    }
}
