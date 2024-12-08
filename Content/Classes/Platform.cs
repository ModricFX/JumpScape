using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape
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
        }

        public void StartCountdown()
        {
            if (IsDisappearing && !IsCountingDown)
            {
                IsCountingDown = true;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (IsCountingDown && isVisible)
            {
                // Apply a small random offset to simulate continuous shaking
                Position = OriginalPosition;
                RotationAngle = (float)Math.Sin(gameTime.TotalGameTime.TotalMilliseconds * 0.03) * 0.02f; // Small angle in radians


                CountdownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (CountdownTimer <= 0)
                {
                    CountdownTimer = 3.0f; // Reset disappearing timer for future use
                    IsCountingDown = false;
                    isVisible = false;
                    IsReappearing = true; // Start the reappearing countdown
                    Position = OriginalPosition; // Reset position to original
                    RotationAngle = 0; // Reset rotation
                }
            }

            if (IsReappearing && !isVisible)
            {
                ReappearTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (ReappearTimer <= 0)
                {
                    ReappearTimer = 5.0f; // Reset reappearing timer for future use
                    IsReappearing = false;
                    isVisible = true; // Make the platform visible again
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isVisible) return;

            // Texture width
            int textureWidth = Texture.Width;

            // Draw each section of the platform with rotation
            for (int i = 0; i < Length / textureWidth; i++)
            {
                Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2); // Rotation origin
                Vector2 position = new Vector2(Position.X + i * textureWidth + origin.X, Position.Y + origin.Y);

                spriteBatch.Draw(Texture, position, null, Color.White, RotationAngle, origin, 1.0f, SpriteEffects.None, 0f);
            }

            // Draw remaining part if platform length is not divisible by texture width
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
