using System;
using System.Collections.Generic;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A grass platform. Crumbling platforms start shaking when the player lands on them,
    /// break after a few seconds and come back a few seconds later.
    /// </summary>
    public sealed class Platform
    {
        private const float CrumbleTime = 3f;
        private const float RespawnTime = 5f;
        private const float SoundRange = 500f;
        private const float SoundVolume = 0.8f;
        private const int DebrisCount = 12;

        private static readonly Random Random = new Random();

        private readonly List<Particle> _debris = new List<Particle>();
        private readonly SoundEffectInstance _crackingSound;
        private readonly SoundEffectInstance _breakSound;
        private float _timer;
        private bool _isCrumbling;
        private bool _isBroken;
        private float _shakeAngle;

        public Platform(Vector2 position, int length, bool canCrumble)
        {
            Position = position;
            Length = length;
            CanCrumble = canCrumble;

            if (canCrumble)
            {
                _crackingSound = Assets.PlatformCracking.CreateInstance();
                _crackingSound.IsLooped = true;
                _breakSound = Assets.PlatformBreak.CreateInstance();
            }
        }

        public Vector2 Position { get; }

        public int Length { get; }

        public bool CanCrumble { get; }

        /// <summary>False while a crumbled platform is gone.</summary>
        public bool IsSolid => !_isBroken;

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Length, Assets.Grass.Height);

        private Vector2 Center => Bounds.Center.ToVector2();

        public void OnPlayerLanded()
        {
            if (!CanCrumble || _isCrumbling || _isBroken)
                return;

            _isCrumbling = true;
            _timer = CrumbleTime;
        }

        public void Update(GameTime gameTime, Player player)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                _debris[i].Update(deltaTime);
                if (_debris[i].IsDead)
                    _debris.RemoveAt(i);
            }

            if (_isCrumbling)
            {
                _shakeAngle = MathF.Sin((float)gameTime.TotalGameTime.TotalMilliseconds * 0.03f) * 0.02f;
                PositionalSound.Apply(_crackingSound, Center, player.Center, SoundRange, SoundVolume);
                if (player.IsDead)
                    _crackingSound.Stop();
                else if (_crackingSound.State != SoundState.Playing)
                    _crackingSound.Play();

                _timer -= deltaTime;
                if (_timer <= 0f)
                    Break(player);
            }
            else if (_isBroken)
            {
                _timer -= deltaTime;
                if (_timer <= 0f)
                    _isBroken = false;
            }
        }

        private void Break(Player player)
        {
            _isCrumbling = false;
            _isBroken = true;
            _timer = RespawnTime;
            _shakeAngle = 0f;

            _crackingSound.Stop();
            if (!player.IsDead)
            {
                PositionalSound.Apply(_breakSound, Center, player.Center, SoundRange, SoundVolume);
                _breakSound.Play();
            }

            for (int i = 0; i < DebrisCount; i++)
            {
                var position = new Vector2(Position.X + (float)Random.NextDouble() * Length, Position.Y - 4);
                var velocity = new Vector2((float)Random.NextDouble() * 100f - 50f, -50f - (float)Random.NextDouble() * 25f);
                float lifetime = 1f + (float)Random.NextDouble() * 1.5f;
                _debris.Add(new Particle(position, velocity, lifetime, Color.SaddleBrown, size: 6f));
            }
        }

        public void StopSounds()
        {
            _crackingSound?.Stop();
            _breakSound?.Stop();
        }

        /// <summary>Draws the platform by repeating the grass texture along its length, tinted with the area's color.</summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea, Color tint)
        {
            foreach (Particle particle in _debris)
                particle.Draw(spriteBatch);

            if (_isBroken || !Bounds.Intersects(visibleArea))
                return;

            Texture2D texture = Assets.Grass;
            for (int x = 0; x < Length; x += texture.Width)
            {
                int tileWidth = Math.Min(texture.Width, Length - x);
                var source = new Rectangle(0, 0, tileWidth, texture.Height);
                var origin = new Vector2(tileWidth / 2f, texture.Height / 2f);
                spriteBatch.Draw(texture, Position + new Vector2(x, 0) + origin, source, tint, _shakeAngle, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
