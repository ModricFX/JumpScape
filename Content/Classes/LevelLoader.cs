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

        private const float GroundLengthMultiplier = 1.10f; // 110% of screen width

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
                    string[] values = parts[1].Trim().Split(',');

                    if (values.Length < 2)
                        continue;

                    float x = ParseValue(values[0], windowWidth, windowHeight);
                    float y = ParseValue(values[1], windowWidth, windowHeight);

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
                            if (values.Length == 3 && int.TryParse(values[2], out int lockState))
                            {
                                bool isLocked = (lockState == 1);
                                DoorData = (ScalePosition(position, windowWidth, windowHeight), isLocked);
                            }
                            break;

                        case "Platform":
                            ProcessPlatformLine(values, position, windowWidth, windowHeight);
                            break;

                        case "Ghost":
                            if (values.Length >= 3 && float.TryParse(values[2], out float radius))
                            {
                                Vector2 ghostPos = ScalePosition(position, windowWidth, windowHeight);
                                float scaledRadius = radius * (windowWidth / BaseWidth);
                                GhostsData.Add((ghostPos, scaledRadius));
                            }
                            break;
                    }
                }
            }
        }

        private void ProcessPlatformLine(string[] values, Vector2 rawPosition, int windowWidth, int windowHeight)
        {
            if (values.Length < 5) return;

            float length = ParseValue(values[2], windowWidth, windowHeight);
            bool hasMonster = int.TryParse(values[3], out int monsterFlag) && monsterFlag == 1;
            bool isDisappearing = int.TryParse(values[4], out int disappearingFlag) && disappearingFlag == 1;

            Vector2 scaledPosition = ScalePosition(rawPosition, windowWidth, windowHeight);

            // Skip scaling `groundLength` if it's already calculated in absolute terms
            if (values[2].Trim().ToLowerInvariant() != "groundlength")
                length = (length / BaseWidth) * windowWidth;

            PlatformData.Add((scaledPosition, (int)length, hasMonster, isDisappearing));

            // Debugging Info
            Console.WriteLine($"Platform Added: Pos={scaledPosition}, Length={length}, Monster={hasMonster}, Disappearing={isDisappearing}");
        }

        private float ParseValue(string raw, int windowWidth, int windowHeight)
        {
            raw = raw.Trim().ToLowerInvariant();

            if (raw == "groundy")
            {
                float groundVal = windowHeight - 50;
                GroundY = groundVal; // Store groundY globally
                return groundVal;
            }
            else if (raw == "groundlength")
            {
                float groundLen = GroundLengthMultiplier * windowWidth;
                return groundLen;
            }
            else if (float.TryParse(raw, out float numericVal))
            {
                return numericVal; // Numeric values are in base-resolution coordinates
            }

            return 0f; // Default for invalid inputs
        }

        private Vector2 ScalePosition(Vector2 basePos, int windowWidth, int windowHeight)
        {
            float scaledX = basePos.X;
            float scaledY = basePos.Y;

            if (basePos.X >= 0 && basePos.X <= BaseWidth)
                scaledX = (basePos.X / BaseWidth) * windowWidth;
            if (basePos.Y >= 0 && basePos.Y <= BaseHeight)
                scaledY = (basePos.Y / BaseHeight) * windowHeight;

            return new Vector2(scaledX, scaledY);
        }
    }
}
