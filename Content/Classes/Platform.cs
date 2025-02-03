using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using System.Collections.Generic;

namespace JumpScape.Classes
{
    public class Platform
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public int Length { get; set; }
        public bool IsDisappearing { get; set; }
        private float CountdownTimer { get; set; }
        private float ReappearTimer { get; set; }
        private bool IsCountingDown { get; set; }
        private bool IsReappearing { get; set; }
        private Vector2 OriginalPosition { get; set; }

        public bool isVisible = true;
        private float RotationAngle { get; set; }

        // Sound effects
        private SoundEffect _platformBreakingSound;
        private SoundEffectInstance _platformBreakingLoop;  // looped "cracking"
        private SoundEffect _platformBreakSound;
        private SoundEffectInstance _platformBreakInstance; // single "break"

        // Reference to GameSettings for Volume
        private GameSettings _settings;

        // ------------------------------
        // Particle Stuff
        // ------------------------------
        private Texture2D _particleTexture;        // The texture for debris
        private List<Particle> _particles;         // Active debris

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Length,
            Texture.Height
        );

        /// <summary>
        /// Particle class representing a piece of debris.
        /// </summary>
        private class Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Lifetime;
            public float MaxLifetime;
            public Color Color;
            public float Scale;

            public Particle(Vector2 position, Vector2 velocity, float lifetime, Color color, float scale)
            {
                Position = position;
                Velocity = velocity;
                Lifetime = lifetime;
                MaxLifetime = lifetime;
                Color = color;
                Scale = scale;
            }

            public bool IsDead => Lifetime <= 0f;

            public void Update(GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                Lifetime -= dt;
                // Simple gravity
                Velocity += new Vector2(0, 9.8f) * dt;
                Position += Velocity * dt;
            }

            public void Draw(SpriteBatch spriteBatch, Texture2D texture)
            {
                float alpha = MathHelper.Clamp(Lifetime / MaxLifetime, 0f, 1f);
                spriteBatch.Draw(
                    texture,
                    Position,
                    null,
                    Color * alpha,
                    0f,
                    Vector2.Zero,
                    Scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        /// <summary>
        /// Constructor for Platform.
        /// Pass in your GameSettings so we can factor in Volume.
        /// We also load a small "particle.png" for debris.
        /// </summary>
        public Platform(Texture2D texture, Vector2 position, int length, bool isDisappearing)
        {
            Texture = texture;
            Position = position;
            OriginalPosition = position;
            Length = length;
            IsDisappearing = isDisappearing;
            CountdownTimer = 3.0f;
            ReappearTimer = 5.0f;
            IsCountingDown = false;
            IsReappearing = false;

            _settings = GameSettings.Load(); // or pass it in from outside if you prefer
            _particles = new List<Particle>();

            // Load sound effects
            _platformBreakingSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlatformBreaking.wav"));
            _platformBreakingLoop = _platformBreakingSound.CreateInstance();
            _platformBreakingLoop.IsLooped = true;

            _platformBreakSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlatformBreak.wav"));
            _platformBreakInstance = _platformBreakSound.CreateInstance();

            // Load the particle texture from file (must be in your Content folder).
            // Suppose you have a 1x1 pixel named "particle.png"
            // If that doesn't exist, you can generate a 1x1 texture in code, see note below.
            var gd = texture.GraphicsDevice;
            _particleTexture = new Texture2D(gd, 1, 1);
            _particleTexture.SetData(new[] { Color.Red });
        }

        public void StartCountdown()
        {
            if (IsDisappearing && !IsCountingDown)
            {
                IsCountingDown = true;
                _platformBreakingLoop.Play(); // Start looped "cracking" sound
            }
        }

        public void Update(GameTime gameTime, Player player)
        {
            // Update existing particles
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i].Update(gameTime);
                if (_particles[i].IsDead)
                {
                    _particles.RemoveAt(i);
                    i--;
                }
            }

            // If this platform is disappearing and the countdown has started
            if (IsCountingDown && isVisible)
            {
                // Slight "shake" effect
                Position = OriginalPosition;
                RotationAngle = (float)Math.Sin(gameTime.TotalGameTime.TotalMilliseconds * 0.03) * 0.02f;

                CountdownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Update the loop's distance-based volume
                UpdatePlatformSoundVolume(player);

                if (CountdownTimer <= 0)
                {
                    CountdownTimer = 3.0f;
                    IsCountingDown = false;
                    isVisible = false;
                    IsReappearing = true;
                    Position = OriginalPosition;
                    RotationAngle = 0;

                    // Stop the looped cracking
                    _platformBreakingLoop.Stop();

                    // Play the single break sound, factoring in distance & game volume
                    PlayBreakSound(player);

                    // Generate some debris
                    GenerateBreakParticles(8);
                    // ^ pick any number of particles you'd like
                }
            }

            // If it's time for the platform to reappear
            if (IsReappearing && !isVisible)
            {
                ReappearTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (ReappearTimer <= 0)
                {
                    ReappearTimer = 5.0f;
                    IsReappearing = false;
                    isVisible = true;
                }
            }
        }

        /// <summary>
        /// Spawn some debris around the platform's position.
        /// </summary>
        private void GenerateBreakParticles(int count)
        {
            Random rand = new Random();

            // We'll pick random points along the top of the platform
            float topY = Position.Y - 4; // slightly above the platform

            for (int i = 0; i < count; i++)
            {
                float x = Position.X + (float)rand.NextDouble() * Length;
                Vector2 pos = new Vector2(x, topY);

                // random velocity
                // e.g. debris flies up a bit, then gravity pulls it down
                float vx = (float)(rand.NextDouble() * 200 - 100); // -100..+100
                float vy = (float)(-100 - rand.NextDouble() * 50); // upward
                Vector2 vel = new Vector2(vx, vy) * 0.5f;

                float lifetime = 1.0f + (float)rand.NextDouble() * 1.5f; // 1-2.5s
                Color color = Color.Brown; // or random color
                float scale = 3f;

                _particles.Add(new Particle(pos, vel, lifetime, color, scale));
            }
        }

        /// <summary>
        /// Adjust the looped "cracking" volume based on distance & GameSettings.Volume.
        /// </summary>
        private void UpdatePlatformSoundVolume(Player player)
        {
            float distance = Vector2.Distance(Position, player.Position);

            float minDistance = 20f;
            float maxDistance = 500f;

            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);
            float volume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));
            volume *= 0.8f; // local max 80%

            if (_settings != null)
            {
                float gameVolumeFactor = _settings.Volume / 100f;
                volume *= gameVolumeFactor;
            }

            float pan = (Position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);

            _platformBreakingLoop.Volume = volume;
            _platformBreakingLoop.Pan = pan;
        }

        /// <summary>
        /// Plays the one-time break sound, factoring distance & settings volume.
        /// </summary>
        private void PlayBreakSound(Player player)
        {
            float distance = Vector2.Distance(Position, player.Position);

            float minDistance = 20f;
            float maxDistance = 500f;
            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);

            float volume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));
            volume *= 0.8f;

            if (_settings != null)
            {
                float gameVolumeFactor = _settings.Volume / 100f;
                volume *= gameVolumeFactor;
            }

            float pan = (Position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);

            _platformBreakInstance.Volume = volume;
            _platformBreakInstance.Pan = pan;
            _platformBreakInstance.Play();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw existing particles
            foreach (var particle in _particles)
            {
                particle.Draw(spriteBatch, _particleTexture);
            }

            if (!isVisible)
                return;

            int textureWidth = Texture.Width;

            // Draw the platform in segments
            for (int i = 0; i < Length / textureWidth; i++)
            {
                Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
                Vector2 position = new Vector2(Position.X + i * textureWidth + origin.X, Position.Y + origin.Y);

                spriteBatch.Draw(
                    Texture,
                    position,
                    null,
                    Color.White,
                    RotationAngle,
                    origin,
                    1.0f,
                    SpriteEffects.None,
                    0f
                );
            }

            // If there's a remainder piece
            int remainder = Length % textureWidth;
            if (remainder > 0)
            {
                Rectangle sourceRectangle = new Rectangle(0, 0, remainder, Texture.Height);
                Vector2 origin = new Vector2(remainder / 2, Texture.Height / 2);
                Vector2 position = new Vector2(Position.X + (Length / textureWidth) * textureWidth + origin.X, Position.Y + origin.Y);

                spriteBatch.Draw(
                    Texture,
                    position,
                    sourceRectangle,
                    Color.White,
                    RotationAngle,
                    origin,
                    1.0f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
