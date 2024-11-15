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
        public List<(Vector2 Position, int Length, bool HasMonster)> PlatformData { get; private set; } // Updated to include HasMonster

        public LevelLoader()
        {
            PlatformData = new List<(Vector2, int, bool)>();
        }

        public void LoadLevel(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string type = parts[0].Trim();
                        string[] values = parts[1].Trim().Split(',');

                        if (values.Length >= 2 &&
                            float.TryParse(values[0], out float x) &&
                            float.TryParse(values[1], out float y))
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
                                    if (values.Length == 4 && 
                                        int.TryParse(values[2], out int length) && 
                                        int.TryParse(values[3], out int monsterFlag))
                                    {
                                        bool hasMonster = monsterFlag == 1;
                                        PlatformData.Add((position, length, hasMonster));
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
