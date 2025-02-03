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
        public int Length { get; set; } // Platform length
        public bool IsDisappearing { get; set; } // Indicates if the platform disappears
        private float CountdownTimer { get; set; } // Countdown timer for disappearing platforms
        private float ReappearTimer { get; set; } // Timer for reappearing platforms
        private bool IsCountingDown { get; set; } // Indicates if the countdown for disappearing has started
        private bool IsReappearing { get; set; } // Indicates if the countdown for reappearing has started
        private Vector2 OriginalPosition { get; set; } // Stores the original position for shaking effect

        public bool isVisible = true;
        private float RotationAngle { get; set; } // New property for rotation angle

        // --- Sound Effects ---
        private SoundEffect _platformBreakingSound;
        private SoundEffectInstance _platformBreakingLoop;
        private SoundEffect _platformBreakSound;
        private bool _breakingSoundPlayed;

        private SoundEffectInstance _platformBreakInstance;

        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, Length, Texture.Height);

        public Platform(Texture2D texture, Vector2 position, int length, bool isDisappearing)
        {
            Texture = texture;
            Position = position;
            OriginalPosition = position; // Store the original position
            Length = length;
            IsDisappearing = isDisappearing;
            CountdownTimer = 3.0f; // Default countdown time (in seconds)
            ReappearTimer = 5.0f; // Time for the platform to reappear (in seconds)
            IsCountingDown = false;
            IsReappearing = false;
            _breakingSoundPlayed = false;

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
                _platformBreakingLoop.Play(); // Start cracking sound
            }
        }

        private void PlayBreakSound(Player player)
        {
            float distance = Vector2.Distance(Position, player.Position);
            float minDistance = 20f;
            float maxDistance = 500f;
            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);
            float volume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));
            volume *= 0.8f;
            float pan = (Position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);
            _platformBreakInstance.Volume = volume;
            _platformBreakInstance.Pan = pan;
            _platformBreakInstance.Play();
        }

        public void Update(GameTime gameTime, Player player)
        {
            if (IsCountingDown && isVisible)
            {
                Position = OriginalPosition;
                RotationAngle = (float)Math.Sin(gameTime.TotalGameTime.TotalMilliseconds * 0.03) * 0.02f;
                CountdownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                UpdatePlatformSoundVolume(player);

                if (CountdownTimer <= 0)
                {
                    CountdownTimer = 3.0f;
                    IsCountingDown = false;
                    isVisible = false;
                    IsReappearing = true;
                    Position = OriginalPosition;
                    RotationAngle = 0;

                    _platformBreakingLoop.Stop();
                    PlayBreakSound(player);
                }
            }

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

        private void UpdatePlatformSoundVolume(Player player)
        {
            // Calculate distance to player
            float distance = Vector2.Distance(Position, player.Position);

            float minDistance = 20f;  // Full volume when closer than this
            float maxDistance = 500f; // No sound beyond this

            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);
            float volume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));
            volume *= 0.8f; // Cap volume at 80%

            float pan = (Position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);

            _platformBreakingLoop.Volume = volume;
            _platformBreakingLoop.Pan = pan;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isVisible) return;

            int textureWidth = Texture.Width;
            for (int i = 0; i < Length / textureWidth; i++)
            {
                Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
                Vector2 position = new Vector2(Position.X + i * textureWidth + origin.X, Position.Y + origin.Y);
                spriteBatch.Draw(Texture, position, null, Color.White, RotationAngle, origin, 1.0f, SpriteEffects.None, 0f);
            }

            int remainder = Length % textureWidth;
            if (remainder > 0)
            {
                Rectangle sourceRectangle = new Rectangle(0, 0, remainder, Texture.Height);
                Vector2 origin = new Vector2(remainder / 2, Texture.Height / 2);
                Vector2 position = new Vector2(Position.X + (Length / textureWidth) * textureWidth + origin.X, Position.Y + origin.Y);
                spriteBatch.Draw(Texture, position, sourceRectangle, Color.White, RotationAngle, origin, 1.0f, SpriteEffects.None, 0f);
            }
        }
    }
}
