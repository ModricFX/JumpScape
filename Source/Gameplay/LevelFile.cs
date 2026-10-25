using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JumpScape.Gameplay
{
    // These classes describe the shape of a level's JSON file (Levels/LevelN.json) and nothing else.
    // LevelData turns them into the data the game uses. Levels/level.schema.json describes the same
    // shape for editors like VS Code, so keep the two in step.

    internal sealed class LevelFile
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; set; }

        /// <summary>The level's title, shown in the level picker and when the level starts.</summary>
        public string Name { get; set; }

        /// <summary>A sentence or two about the level, shown in the level picker.</summary>
        public string Description { get; set; }

        /// <summary>Target times in seconds for gold, silver and bronze medals.</summary>
        public MedalsJson Medals { get; set; }

        [JsonRequired]
        public PointJson Spawn { get; set; }

        /// <summary>Name of the area the player starts in; the first area if left out.</summary>
        public string StartArea { get; set; }

        [JsonRequired]
        public List<AreaJson> Areas { get; set; } = new List<AreaJson>();
    }

    internal class PointJson
    {
        [JsonRequired]
        public float X { get; set; }

        [JsonRequired]
        public float Y { get; set; }
    }

    internal sealed class AreaJson
    {
        [JsonRequired]
        public string Name { get; set; }

        public string Sky { get; set; }
        public string Tint { get; set; }
        public List<PlatformJson> Platforms { get; set; } = new List<PlatformJson>();
        public List<DoorJson> Doors { get; set; } = new List<DoorJson>();
        public List<PortalJson> Portals { get; set; } = new List<PortalJson>();
        public List<PointJson> Keys { get; set; } = new List<PointJson>();
        public List<GhostJson> Ghosts { get; set; } = new List<GhostJson>();
        public List<PowerUpJson> PowerUps { get; set; } = new List<PowerUpJson>();
        public List<NpcJson> Npcs { get; set; } = new List<NpcJson>();
        public List<PointJson> Checkpoints { get; set; } = new List<PointJson>();
        public List<SpikesJson> Spikes { get; set; } = new List<SpikesJson>();
        public List<PointJson> Gems { get; set; } = new List<PointJson>();
    }

    internal sealed class MedalsJson
    {
        [JsonRequired]
        public float Gold { get; set; }

        [JsonRequired]
        public float Silver { get; set; }

        [JsonRequired]
        public float Bronze { get; set; }
    }

    internal sealed class SpikesJson : PointJson
    {
        [JsonRequired]
        public float Length { get; set; }
    }

    internal sealed class PlatformJson : PointJson
    {
        [JsonRequired]
        public float Length { get; set; }

        public bool Monster { get; set; }
        public bool Crumbling { get; set; }
    }

    internal sealed class DoorJson : PointJson
    {
        public bool Locked { get; set; }
    }

    internal sealed class PortalJson : PointJson
    {
        public bool Locked { get; set; }

        [JsonRequired]
        public string Name { get; set; }

        [JsonRequired]
        public string LeadsTo { get; set; }
    }

    internal sealed class GhostJson : PointJson
    {
        [JsonRequired]
        public float Radius { get; set; }
    }

    internal sealed class PowerUpJson : PointJson
    {
        [JsonRequired]
        public PowerUpType Type { get; set; }
    }

    internal sealed class NpcJson : PointJson
    {
        [JsonRequired]
        public string Name { get; set; }

        public List<string> Lines { get; set; } = new List<string>();
    }

    /// <summary>Reads a coordinate that is either a plain number or text like "groundY-150" or "groundLength".</summary>
    internal sealed class LevelNumberConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetSingle();

            if (reader.TokenType == JsonTokenType.String && LevelData.TryParseNumber(reader.GetString(), out float value))
                return value;

            string text = reader.TokenType == JsonTokenType.String ? $"\"{reader.GetString()}\"" : "this";
            throw new JsonException($"{text} is not a number. Use a number, or groundY / groundLength with an optional +N or -N.");
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
