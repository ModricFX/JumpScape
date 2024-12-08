using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace JumpScape
{
    public class LevelLoader
    {
        public Vector2 PlayerSpawn { get; private set; }
        public Vector2 KeyPosition { get; private set; }
        public (Vector2 Position, bool IsLocked) DoorData { get; private set; }
        public List<(Vector2 Position, int Length, bool HasMonster, bool IsDisappearing)> PlatformData { get; private set; }

        public static float GroundY { get; private set; }

        public LevelLoader()
        {
            PlatformData = new List<(Vector2, int, bool, bool)>();
        }

        // Helper method to convert and save a level file with replacements
        public static string ConvertLevelFile(string inputFilePath, int windowHeight, int windowWidth)
        {
            string directory = Path.GetDirectoryName(inputFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            string newFileName = $"{fileNameWithoutExtension}_converted.txt";
            string outputFilePath = Path.Combine(directory, newFileName);

            using (StreamReader reader = new StreamReader(inputFilePath))
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string type = parts[0].Trim();
                        string[] values = parts[1].Trim().Split(',');

                        if (type == "Platform" && values.Length >= 4)
                        {
                            ProcessPlatformData(ref values, windowHeight, windowWidth);
                        }

                        string modifiedLine = $"{type}: {string.Join(",", values)}";
                        writer.WriteLine(modifiedLine);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            return outputFilePath;
        }

        // Method to process platform lines, handling replacements of "groundY" and "groundLength"
        private static void ProcessPlatformData(ref string[] values, int windowHeight, int windowWidth)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i].Trim(), "groundY", StringComparison.OrdinalIgnoreCase))
                {
                    values[i] = (windowHeight - windowHeight * 0.21f).ToString();
                }
                else if (string.Equals(values[i].Trim(), "groundLength", StringComparison.OrdinalIgnoreCase))
                {
                    GroundY = windowWidth + windowWidth * 0.1f;
                    values[i] = GroundY.ToString();
                }
            }
        }

        // Main method to load level after conversion
        public void LoadLevel(string filePath, int windowHeight, int windowWidth)
        {
            string convertedFilePath = ConvertLevelFile(filePath, windowHeight, windowWidth);

            using (StreamReader reader = new StreamReader(convertedFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string type = parts[0].Trim();
                        string[] values = parts[1].Trim().Split(',');

                        if (values.Length >= 2 && float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                        {
                            Vector2 position = new Vector2(x, y);

                            switch (type)
                            {
                                case "PlayerSpawn":
                                    PlayerSpawn = position;
                                    break;
                                case "KeyPosition":
                                    KeyPosition = position;
                                    break;
                                case "DoorPosition":
                                    if (values.Length == 3 && int.TryParse(values[2], out int lockState))
                                    {
                                        bool isLocked = lockState == 1;
                                        DoorData = (position, isLocked);
                                    }
                                    break;
                                case "Platform":
                                    ProcessPlatformLine(values, position);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // Helper method to process the platform line and add to PlatformData
        private void ProcessPlatformLine(string[] values, Vector2 position)
        {
            if (values.Length < 5) return;

            bool hasMonster = int.TryParse(values[3], out int monsterFlag) && monsterFlag == 1;
            bool isDisappearing = int.TryParse(values[4], out int disappearingFlag) && disappearingFlag == 1;
            int length = int.TryParse(values[2], out int parsedLength) ? parsedLength : 100;

            PlatformData.Add((position, length, hasMonster, isDisappearing));
        }
    }
}
