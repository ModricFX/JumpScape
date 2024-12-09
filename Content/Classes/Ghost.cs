using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class Ghost
    {
        public Texture2D GhostTextureRight { get; }
        public Texture2D GhostTextureLeft { get; }
        public Vector2 OriginalPosition { get; } // Original location of the ghost
        public float Radius { get; } // Maximum distance ghost can move from its original position
        public float Size { get; } // Size of the ghost

        private Vector2 currentPosition; // Current position of the ghost
        private Vector2 smoothPosition; // Smoothed position to prevent jitter
        private bool movingRight; // Determines the ghost's movement direction
        private bool isAtOriginalPosition; // Tracks if the ghost is at its original position
        private bool isReturning; // Tracks if the ghost is returning to its original position
        private const float ReturnSpeed = 100f; // Speed multiplier for returning to original position
        private float hoverOffset; // Offset for hovering animation
        private float hoverTimer; // Timer for calculating hover effect

        public Rectangle BoundingBox => new Rectangle(
            (int)smoothPosition.X,
            (int)smoothPosition.Y,
            (int)(GhostTextureRight.Width * 0.05f),
            (int)(GhostTextureRight.Height * 0.05f)
        );

        public Ghost(Texture2D ghostTextureRight, Texture2D ghostTextureLeft, Vector2 position, float radius)
        {
            GhostTextureRight = ghostTextureRight;
            GhostTextureLeft = ghostTextureLeft;
            OriginalPosition = position;
            currentPosition = position;
            smoothPosition = position; // Initialize smoothed position
            Radius = radius;
            Size = 0.1f; // Default size
            movingRight = true; // Default direction
            isAtOriginalPosition = true; // Start at the original position
            isReturning = false; // Not returning initially
            hoverOffset = 0f; // Initialize hover offset
            hoverTimer = 0f; // Initialize hover timer
        }

        public void Update(GameTime gameTime, Vector2 playerPosition, Player player)
        {
            // Update hover animation
            hoverTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            hoverOffset = (float)Math.Sin(hoverTimer * 2) * 5; // Oscillates up and down by 5 pixels

            // Check collision with the player
            if (BoundingBox.Intersects(player.BoundingBox) && !player.IsInvincible)
            {
                player.LoseHeart(0.5f, movingRight ? 1 : -1);
                isReturning = true;
            }

            // If the player is invincible, the ghost returns to its original position
            if (player.IsInvincible)
            {
                Vector2 direction = OriginalPosition - currentPosition;

                if (direction.Length() > 0.1f)
                {
                    direction.Normalize();
                    currentPosition += direction * (float)gameTime.ElapsedGameTime.TotalSeconds * ReturnSpeed;
                    movingRight = direction.X >= 0;
                }
                else
                {
                    currentPosition = OriginalPosition;
                    isAtOriginalPosition = true;
                }

                smoothPosition = Vector2.Lerp(smoothPosition, currentPosition, 0.2f);
                return;
            }

            // Check distance between ghost and player
            float distanceToPlayer = Vector2.Distance(currentPosition, playerPosition);

            if (distanceToPlayer <= Radius)
            {
                // Move directly towards the player
                isReturning = false; // Stop returning
                Vector2 directionToPlayer = playerPosition - currentPosition;

                if (directionToPlayer.Length() > 0.1f) // Allow small tolerance to avoid jitter
                {
                    directionToPlayer.Normalize();
                    currentPosition += directionToPlayer * (float)gameTime.ElapsedGameTime.TotalSeconds * 150; // Speed multiplier for chasing
                    movingRight = directionToPlayer.X >= 0; // Update direction based on movement
                    isAtOriginalPosition = false; // Update state
                }
            }
            else
            {
                // Smoothly return to original position if player is outside radius
                Vector2 direction = OriginalPosition - currentPosition;

                if (direction.Length() > 0.1f) // Allow small tolerance to avoid jitter
                {
                    direction.Normalize();
                    currentPosition += direction * (float)gameTime.ElapsedGameTime.TotalSeconds * ReturnSpeed;
                    if (!isReturning)
                    {
                        movingRight = direction.X >= 0; // Set direction only when starting return
                    }
                    isReturning = true; // Mark as returning
                    isAtOriginalPosition = false; // Update state
                }
                else if (!isAtOriginalPosition) // Only update when arriving at original position
                {
                    // Snap to original position
                    currentPosition = OriginalPosition;
                    isAtOriginalPosition = true; // Set state to true
                    isReturning = false; // No longer returning
                }
            }

            // Smooth the ghost's position to avoid jitter
            smoothPosition = Vector2.Lerp(smoothPosition, currentPosition, 0.2f);

            // Ensure the ghost faces right when at the original position
            if (isAtOriginalPosition)
            {
                movingRight = true;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Always use the right-facing texture when at the original position
            Texture2D currentTexture = isAtOriginalPosition ? GhostTextureRight : (movingRight ? GhostTextureRight : GhostTextureLeft);

            // Apply hover effect to the Y position
            Vector2 drawPosition = new Vector2(smoothPosition.X, smoothPosition.Y + hoverOffset);

            // Draw the ghost at its smoothed position with scaling based on Size
            spriteBatch.Draw(currentTexture, drawPosition, null, Color.White, 0f, Vector2.Zero, Size, SpriteEffects.None, 0f);
        }
    }
}
