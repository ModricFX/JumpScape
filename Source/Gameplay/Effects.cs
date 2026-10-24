using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>Short-lived particle effects: bursts when something is hit or collected, and dust under the player's feet.</summary>
    public sealed class Effects
    {
        private static readonly Random Random = new Random();
        private static readonly Color DustColor = new Color(235, 225, 200);

        private readonly List<Particle> _particles = new List<Particle>();

        /// <summary>A small explosion of colored squares.</summary>
        public void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var velocity = new Vector2(RandomBetween(-150f, 150f), RandomBetween(-240f, -60f));
                _particles.Add(new Particle(position, velocity, RandomBetween(0.4f, 0.8f), color, size: 6f));
            }
        }

        /// <summary>A puff of dust spreading sideways from the player's feet, for jumping and landing.</summary>
        public void Dust(Vector2 feet, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float direction = i % 2 == 0 ? -1f : 1f;
                var velocity = new Vector2(direction * RandomBetween(40f, 120f), RandomBetween(-50f, -10f));
                var position = feet + new Vector2(RandomBetween(-20f, 20f), -2f);
                _particles.Add(new Particle(position, velocity, RandomBetween(0.25f, 0.45f), DustColor * 0.8f, RandomBetween(4f, 7f), gravity: 60f));
            }
        }

        public void Update(float deltaTime)
        {
            foreach (Particle particle in _particles)
                particle.Update(deltaTime);
            _particles.RemoveAll(particle => particle.IsDead);
        }

        public void Clear()
        {
            _particles.Clear();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Particle particle in _particles)
                particle.Draw(spriteBatch);
        }

        private static float RandomBetween(float min, float max) => min + (float)Random.NextDouble() * (max - min);
    }
}
