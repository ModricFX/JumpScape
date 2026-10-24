using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// One part of a level while it is being played: its platforms, enemies, items and doors.
    /// Only the area the player is in is updated and drawn.
    /// </summary>
    public sealed class Area
    {
        public Area(AreaData data)
        {
            Name = data.Name;
            Sky = data.Sky;
            Tint = data.Tint;

            foreach (PlatformInfo info in data.Platforms)
            {
                var platform = new Platform(info.Position, info.Length, info.IsCrumbling);
                Platforms.Add(platform);
                if (info.HasMonster)
                    Monsters.Add(new Monster(platform.Bounds));
            }

            foreach (GhostInfo info in data.Ghosts)
                Ghosts.Add(new Ghost(info.Position, info.Radius));

            foreach (PowerUpInfo info in data.PowerUps)
                PowerUps.Add(new PowerUp(info.Position, info.Type));

            foreach (Vector2 position in data.KeyPositions)
                Keys.Add(new KeyPickup(position));

            foreach (DoorInfo info in data.Doors)
                Doors.Add(new Door(info.Position, info.IsLocked, info.Name, info.Target));

            foreach (NpcInfo info in data.Npcs)
            {
                var npc = new Npc(info.Position, info.Name, info.Lines);
                npc.StandOn(Platforms);
                Npcs.Add(npc);
            }
        }

        public string Name { get; }
        public Color Sky { get; }
        public Color Tint { get; }

        public List<Platform> Platforms { get; } = new List<Platform>();
        public List<Monster> Monsters { get; } = new List<Monster>();
        public List<Ghost> Ghosts { get; } = new List<Ghost>();
        public List<PowerUp> PowerUps { get; } = new List<PowerUp>();
        public List<KeyPickup> Keys { get; } = new List<KeyPickup>();
        public List<Door> Doors { get; } = new List<Door>();
        public List<Npc> Npcs { get; } = new List<Npc>();

        public void StopSounds()
        {
            foreach (Monster monster in Monsters)
                monster.StopSounds();
            foreach (Platform platform in Platforms)
                platform.StopSounds();
        }
    }
}
