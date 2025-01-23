using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace JumpScape
{
    public class LevelLoader
    {
        private const float BaseWidth = 1920f;
        private const float BaseHeight = 1080f;
        private const float GroundLengthMultiplier = 1.50f;

        public Vector2 PlayerSpawn { get; private set; }
        public Vector2 KeyPosition { get; private set; }
        public (Vector2 Position, bool IsLocked) DoorData { get; private set; }

        public List<(Vector2 Position, int Length, bool HasMonster, bool IsDisappearing)> PlatformData { get; private set; }
        public List<(Vector2 Position, float Radius)> GhostsData { get; private set; }

        public static float GroundY { get; private set; }

        public LevelLoader()
        {
            PlatformData = new List<(Vector2, int, bool, bool)>();
            GhostsData = new List<(Vector2, float)>();
        }

        /// <summary>
        /// Reads lines from a level file and processes them.
        /// Note the parameter order is (windowHeight, windowWidth).
        /// </summary>
        public void LoadLevel(string filePath, int windowHeight, int windowWidth)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length != 2)
                        continue;

                    string type = parts[0].Trim();
                    string[] vals = parts[1].Trim().Split(',');

                    if (vals.Length < 2)
                        continue;

                    // Parse X and Y (these might be numeric or placeholders like groundY)
                    float x = ParseValue(vals[0], windowWidth, windowHeight);
                    float y = ParseValue(vals[1], windowWidth, windowHeight);

                    Vector2 position = new Vector2(x, y);

                    switch (type)
                    {
                        case "PlayerSpawn":
                            PlayerSpawn = ScalePosition(position, windowWidth, windowHeight);
                            break;

                        case "KeyPosition":
                            KeyPosition = ScalePosition(position, windowWidth, windowHeight);
                            break;

                        case "DoorPosition":
                            // Format: DoorPosition: X, Y, lockState(0/1)
                            if (vals.Length == 3 && int.TryParse(vals[2], out int lockState))
                            {
                                bool isLocked = (lockState == 1);
                                Vector2 doorPos = ScalePosition(position, windowWidth, windowHeight);
                                DoorData = (doorPos, isLocked);
                            }
                            break;

                        case "Platform":
                            ProcessPlatformLine(vals, position, windowWidth, windowHeight);
                            break;

                        case "Ghost":
                            // Format: Ghost: X, Y, radius
                            if (vals.Length >= 3 && float.TryParse(vals[2], out float radius))
                            {
                                Vector2 ghostPos = ScalePosition(position, windowWidth, windowHeight);
                                float scaledRadius = radius * (windowWidth / BaseWidth);
                                GhostsData.Add((ghostPos, scaledRadius));
                            }
                            break;
                    }
                }
            }

            // If you need a static "GroundY" to represent the bottom, store it here:
            GroundY = windowHeight;
        }

        /// <summary>
        /// Processes "Platform: X,Y,Length,HasMonster,IsDisappearing" lines.
        /// </summary>
        private void ProcessPlatformLine(string[] values, Vector2 rawPosition, int windowWidth, int windowHeight)
        {
            if (values.Length < 5)
                return;

            float lengthVal = ParseValue(values[2], windowWidth, windowHeight);

            bool hasMonster = (int.TryParse(values[3], out int monsterFlag) && monsterFlag == 1);
            bool isDisappearing = (int.TryParse(values[4], out int disappearingFlag) && disappearingFlag == 1);

            Vector2 scaledPos = ScalePosition(rawPosition, windowWidth, windowHeight);
            float scaledLength = (lengthVal / BaseWidth) * windowWidth;

            PlatformData.Add((scaledPos, (int)scaledLength, hasMonster, isDisappearing));
        }

        /// <summary>
        /// Parses raw value which might be:
        ///   - a numeric ("300")
        ///   - "groundY" or "groundY+10", "groundY-25"
        ///   - "groundLength" or "groundLength-50"
        ///   - returns 0 if unknown
        /// </summary>
        private float ParseValue(string raw, int windowWidth, int windowHeight)
        {
            raw = raw.Trim().ToLowerInvariant();

            if (raw.StartsWith("groundy"))
            {
                // groundY => the bottom of the screen
                float baseVal = windowHeight;
                float offset = 0f;

                string leftover = raw.Substring("groundy".Length).Trim(); // Handle "+10", "-20"
                if (!string.IsNullOrEmpty(leftover) && float.TryParse(leftover, out float parsedOffset))
                    offset = parsedOffset;

                return baseVal + offset;  // Absolute value, not in base resolution
            }
            else if (raw.StartsWith("groundlength"))
            {
                // groundLength => 1.50 * windowWidth
                float baseVal = GroundLengthMultiplier * windowWidth;
                float offset = 0f;

                string leftover = raw.Substring("groundlength".Length).Trim();
                if (!string.IsNullOrEmpty(leftover) && float.TryParse(leftover, out float parsedOffset))
                    offset = parsedOffset;

                return baseVal + offset;  // Screen-relative length
            }
            else
            {
                if (float.TryParse(raw, out float numericVal))
                    return numericVal;  // Base-resolution value
            }

            return 0f;  // Default if parsing fails
        }

        private Vector2 ScalePosition(Vector2 basePos, int windowWidth, int windowHeight)
        {
            float scaledX = basePos.X;
            float scaledY = basePos.Y;

            // Skip scaling for absolute Y-coordinates like groundY
            if (basePos.Y == windowHeight)
            {
                scaledY = basePos.Y-100;  // Ensure it remains at the screen bottom
            }
            else if (basePos.Y >= 0 && basePos.Y <= BaseHeight)
            {
                scaledY = (basePos.Y / BaseHeight) * windowHeight;
            }

            // Scale X-coordinates normally
            if (basePos.X >= 0 && basePos.X <= BaseWidth)
            {
                scaledX = (basePos.X / BaseWidth) * windowWidth;
            }

            return new Vector2(scaledX, scaledY);
        }
    }
}
