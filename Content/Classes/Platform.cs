using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;

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

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X, 
            (int)Position.Y, 
            Length, 
            Texture.Height
        );

        /// <summary>
        /// Constructor for Platform. Pass in your GameSettings so we can factor in Volume.
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

            _settings = GameSettings.Load(); // store the reference to settings

            // Load sound effects
            _platformBreakingSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlatformBreaking.wav"));
            _platformBreakingLoop = _platformBreakingSound.CreateInstance();
            _platformBreakingLoop.IsLooped = true;

            _platformBreakSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlatformBreak.wav"));
            _platformBreakInstance = _platformBreakSound.CreateInstance();
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

            // Incorporate the global Game Volume (0-100 -> 0.0-1.0)
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
