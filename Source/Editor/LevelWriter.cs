using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JumpScape.Gameplay;

namespace JumpScape.Editor
{
    /// <summary>
    /// Writes a level as tidy JSON, the same way the level files are laid out: small objects on one line, heights
    /// as "groundY-N", and settings that are off or empty left out.
    /// </summary>
    internal static class LevelWriter
    {
        /// <summary>A JSON object that keeps its properties in the order they were added.</summary>
        private sealed class JsonObject : List<KeyValuePair<string, object>>
        {
            public void Add(string key, object value) => Add(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>A number or word written exactly as given, like 40 or "groundY-150".</summary>
        private sealed record Raw(string Text);

        public static string Write(LevelFile file)
        {
            var root = new JsonObject { { "$schema", file.Schema ?? "level.schema.json" } };
            if (file.Name != null)
                root.Add("name", file.Name);
            if (file.Description != null)
                root.Add("description", file.Description);
            if (file.Medals is MedalsJson medals)
                root.Add("medals", new JsonObject { { "gold", Number(medals.Gold) }, { "silver", Number(medals.Silver) }, { "bronze", Number(medals.Bronze) } });
            root.Add("spawn", Point(file.Spawn));
            if (file.StartArea != null)
                root.Add("startArea", file.StartArea);
            root.Add("areas", file.Areas.Select(Area).Cast<object>().ToList());

            var text = new StringBuilder();
            WriteValue(text, root, 0);
            text.Append('\n');
            return text.ToString();
        }

        private static JsonObject Area(AreaJson area)
        {
            var json = new JsonObject { { "name", area.Name } };
            if (area.Sky != null)
                json.Add("sky", area.Sky);
            if (area.Tint != null)
                json.Add("tint", area.Tint);
            AddList(json, "platforms", area.Platforms, Platform);
            AddList(json, "portals", area.Portals, portal =>
            {
                var item = Point(portal);
                if (portal.Locked)
                    item.Add("locked", true);
                item.Add("name", portal.Name);
                item.Add("leadsTo", portal.LeadsTo);
                return item;
            });
            AddList(json, "doors", area.Doors, door =>
            {
                var item = Point(door);
                if (door.Locked)
                    item.Add("locked", true);
                return item;
            });
            AddList(json, "keys", area.Keys, Point);
            AddList(json, "ghosts", area.Ghosts, ghost =>
            {
                var item = Point(ghost);
                item.Add("radius", Number(ghost.Radius));
                return item;
            });
            AddList(json, "powerUps", area.PowerUps, powerUp =>
            {
                var item = Point(powerUp);
                item.Add("type", JsonNamingCamel(powerUp.Type.ToString()));
                return item;
            });
            AddList(json, "npcs", area.Npcs, npc =>
            {
                var item = Point(npc);
                item.Add("name", npc.Name);
                item.Add("lines", npc.Lines.Cast<object>().ToList());
                return item;
            });
            AddList(json, "bouncePads", area.BouncePads, Point);
            AddList(json, "checkpoints", area.Checkpoints, Point);
            AddList(json, "spikes", area.Spikes, spikes =>
            {
                var item = Point(spikes);
                item.Add("length", Number(spikes.Length));
                return item;
            });
            AddList(json, "gems", area.Gems, Point);
            return json;
        }

        private static JsonObject Platform(PlatformJson platform)
        {
            var json = Point(platform);
            json.Add("length", Math.Abs(platform.Length - LevelData.GroundLength) < 0.01f ? new Raw("\"groundLength\"") : Number(platform.Length));
            if (platform.Monster)
                json.Add("monster", true);
            if (platform.Crumbling)
                json.Add("crumbling", true);
            if (platform.MoveTo != null)
            {
                json.Add("moveTo", Point(platform.MoveTo));
                json.Add("moveTime", Number(platform.MoveTime));
            }
            return json;
        }

        private static void AddList<T>(JsonObject json, string key, List<T> items, Func<T, JsonObject> write)
        {
            if (items != null && items.Count > 0)
                json.Add(key, items.Select(write).Cast<object>().ToList());
        }

        private static JsonObject Point(PointJson point) => new JsonObject { { "x", Number(point.X) }, { "y", Height(point.Y) } };

        private static Raw Number(float value)
        {
            return new Raw(MathF.Round(value) == value
                ? ((long)value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        /// <summary>A y position as "groundY", "groundY-150" or "groundY+20", or a plain number if it isn't whole.</summary>
        private static Raw Height(float y)
        {
            float above = LevelData.GroundY - y;
            if (MathF.Abs(above - MathF.Round(above)) > 0.001f)
                return Number(y);
            int pixels = (int)MathF.Round(above);
            if (pixels == 0)
                return new Raw("\"groundY\"");
            return new Raw(pixels > 0 ? $"\"groundY-{pixels}\"" : $"\"groundY+{-pixels}\"");
        }

        private static string JsonNamingCamel(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);

        private static void WriteValue(StringBuilder text, object value, int indent)
        {
            switch (value)
            {
                case JsonObject json:
                    WriteObject(text, json, indent);
                    break;
                case List<object> list:
                    WriteList(text, list, indent);
                    break;
                case Raw raw:
                    text.Append(raw.Text);
                    break;
                case bool flag:
                    text.Append(flag ? "true" : "false");
                    break;
                default:
                    WriteString(text, value?.ToString() ?? "");
                    break;
            }
        }

        private static bool IsSimple(object value) => value is not JsonObject && value is not List<object>;

        /// <summary>Objects with only simple values go on one line; others get one property per line.</summary>
        private static void WriteObject(StringBuilder text, JsonObject json, int indent)
        {
            if (json.All(pair => IsSimple(pair.Value)))
            {
                text.Append("{ ");
                for (int i = 0; i < json.Count; i++)
                {
                    if (i > 0)
                        text.Append(", ");
                    WriteString(text, json[i].Key);
                    text.Append(": ");
                    WriteValue(text, json[i].Value, indent);
                }
                text.Append(" }");
                return;
            }

            string pad = new string(' ', indent * 2);
            text.Append("{\n");
            for (int i = 0; i < json.Count; i++)
            {
                text.Append(pad).Append("  ");
                WriteString(text, json[i].Key);
                text.Append(": ");
                WriteValue(text, json[i].Value, indent + 1);
                text.Append(i < json.Count - 1 ? ",\n" : "\n");
            }
            text.Append(pad).Append('}');
        }

        private static void WriteList(StringBuilder text, List<object> list, int indent)
        {
            if (list.Count == 0)
            {
                text.Append("[]");
                return;
            }

            string pad = new string(' ', indent * 2);
            text.Append("[\n");
            for (int i = 0; i < list.Count; i++)
            {
                text.Append(pad).Append("  ");
                WriteValue(text, list[i], indent + 1);
                text.Append(i < list.Count - 1 ? ",\n" : "\n");
            }
            text.Append(pad).Append(']');
        }

        private static void WriteString(StringBuilder text, string value)
        {
            text.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            text.Append(c);
                        break;
                }
            }
            text.Append('"');
        }
    }
}
