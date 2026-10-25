using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// One part of a level while it is being played: its platforms, enemies, items and doors.
    /// Only the area the player is in is updated and drawn.
    /// </summary>
    public sealed class Area
    {
        /// <param name="firstGemIndex">The number of the area's first gem in the whole level.</param>
        /// <param name="gemsFoundBefore">Numbers of the gems found on earlier finished runs.</param>
        public Area(AreaData data, int firstGemIndex = 0, IReadOnlyCollection<int> gemsFoundBefore = null)
        {
            Name = data.Name;
            Sky = data.Sky;
            Tint = data.Tint;

            foreach (PlatformInfo info in data.Platforms)
            {
                var platform = new Platform(info.Position, info.Length, info.IsCrumbling, info.MoveTo, info.MoveTime);
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

            foreach (Vector2 position in data.Checkpoints)
                Checkpoints.Add(new Checkpoint(position));

            foreach (SpikesInfo info in data.Spikes)
                Spikes.Add(new Spikes(info.Position, info.Length));

            foreach (Vector2 position in data.BouncePads)
                BouncePads.Add(new BouncePad(position));

            for (int i = 0; i < data.Gems.Count; i++)
            {
                int index = firstGemIndex + i;
                Gems.Add(new Gem(data.Gems[i], index, gemsFoundBefore?.Contains(index) == true));
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
        public List<Checkpoint> Checkpoints { get; } = new List<Checkpoint>();
        public List<Spikes> Spikes { get; } = new List<Spikes>();
        public List<Gem> Gems { get; } = new List<Gem>();
        public List<BouncePad> BouncePads { get; } = new List<BouncePad>();

        /// <summary>
        /// Draws everything in the area that stands behind the player: platforms (shadows first), spikes, springs,
        /// flags, doors, frogs, keys, power-ups, gems and characters. Ghosts float in front, see <see cref="DrawGhosts"/>.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea)
        {
            foreach (Platform platform in Platforms)
                platform.DrawShadow(spriteBatch, visibleArea);
            foreach (Platform platform in Platforms)
                platform.Draw(spriteBatch, visibleArea, Tint);
            foreach (Spikes spikes in Spikes)
                spikes.Draw(spriteBatch);
            foreach (BouncePad pad in BouncePads)
                pad.Draw(spriteBatch);
            foreach (Checkpoint checkpoint in Checkpoints)
                checkpoint.Draw(spriteBatch);
            foreach (Door door in Doors)
                door.Draw(spriteBatch);
            foreach (Monster monster in Monsters)
                monster.Draw(spriteBatch);
            foreach (KeyPickup key in Keys)
                key.Draw(spriteBatch);
            foreach (PowerUp powerUp in PowerUps)
                powerUp.Draw(spriteBatch);
            foreach (Gem gem in Gems)
                gem.Draw(spriteBatch);
            foreach (Npc npc in Npcs)
                npc.Draw(spriteBatch);
        }

        public void DrawGhosts(SpriteBatch spriteBatch)
        {
            foreach (Ghost ghost in Ghosts)
                ghost.Draw(spriteBatch);
        }

        public void StopSounds()
        {
            foreach (Monster monster in Monsters)
                monster.StopSounds();
            foreach (Platform platform in Platforms)
                platform.StopSounds();
        }
    }
}
