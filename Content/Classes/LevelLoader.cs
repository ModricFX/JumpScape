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
        public List<(Vector2 Position, int Length, bool HasMonster)> PlatformData { get; private set; }

        public static float GroundY { get; private set; }
        public LevelLoader()
        {
            PlatformData = new List<(Vector2, int, bool)>();
        }

        // Helper method to convert and save a level file with replacements
        public static string ConvertLevelFile(string inputFilePath, int windowHeight, int windowWidth)
        {
            string directory = Path.GetDirectoryName(inputFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            string newFileName = $"{fileNameWithoutExtension}_converted.txt";
            string outputFilePath = Path.Combine(directory, newFileName);

            // Perform conversion (read, process and write)
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
                            // Process platform data replacements
                            ProcessPlatformData(ref values, windowHeight, windowWidth);
                        }

                        // Write the processed line to the output file
                        string modifiedLine = $"{type}: {string.Join(",", values)}";
                        writer.WriteLine(modifiedLine);
                    }
                    else
                    {
                        // Non-platform lines are written as is
                        writer.WriteLine(line);
                    }
                }
            }

            return outputFilePath;  // Return the path of the new converted file
        }

        // Method to process platform lines, handling replacements of "groundY" and "groundLength"
        private static void ProcessPlatformData(ref string[] values, int windowHeight, int windowWidth)
        {
            // Iterate over values and perform necessary replacements
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i].Trim(), "groundY", StringComparison.OrdinalIgnoreCase))
                {
                    values[i] = (windowHeight - windowHeight * 0.21f).ToString();
                }
                else if (string.Equals(values[i].Trim(), "groundLength", StringComparison.OrdinalIgnoreCase))
                {
                    GroundY = (int)(windowWidth + windowWidth * 0.1f);
                    values[i] = GroundY.ToString();
                    Console.WriteLine($"GroundY: {GroundY}");
                }
            }
        }

        // Main method to load level after conversion
        public void LoadLevel(string filePath, int windowHeight, int windowWidth)
        {
            // Convert level file and get the path of the new file
            string convertedFilePath = ConvertLevelFile(filePath, windowHeight, windowWidth);

            // Read and process the converted level file
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

                        // Ensure valid x and y position data
                        if (values.Length >= 2 && float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                        {
                            Vector2 position = new Vector2(x, y);

                            // Handle specific level data types
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
                                    ProcessPlatformLine(values, position, windowHeight);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // Helper method to process the platform line and add to PlatformData
        private void ProcessPlatformLine(string[] values, Vector2 position, int windowHeight)
        {
            // Ensure there are at least 4 values
            if (values.Length < 4) return;

            // Handle "groundY" and "groundLength" replacements
            bool hasMonster = int.TryParse(values[3], out int monsterFlag) && monsterFlag == 1;


            // Parse the length value from the file, with default length if parsing fails
            if (!int.TryParse(values[2], out int length))
            {
                Console.WriteLine($"Failed to parse length for platform at ({position.X},{position.Y}). Using default length.");
                length = 100;  // Default length if parsing fails
            }


            // Add platform data to the list
            PlatformData.Add((position, length, hasMonster));
        }
    }
}
