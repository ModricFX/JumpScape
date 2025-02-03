using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace JumpScape.Classes
{
    public class AnimatedItem : Item
    {
        private int _currentFrame;
        private int _totalFrames;
        private float _frameTime;
        private float _timer;
        private int _frameWidth;
        private int _frameHeight;

        // Original position to base our "hover" offsets on
        private Vector2 _originalPosition;

        // A separate timer for the hover effect
        private float _hoverTimer;

        // Rotation angle for the item
        private float _rotation;

        public AnimatedItem(Texture2D texture, Vector2 position, int totalFrames, float frameTime, string name)
            : base(texture, position, name)
        {
            _totalFrames = totalFrames;
            _frameTime = frameTime;
            _currentFrame = 0;
            _timer = 0f;

            // Calculate the frame width and height
            _frameWidth = texture.Width / totalFrames;
            _frameHeight = texture.Height;

            // Store the "starting" position for bobbing
            _originalPosition = position;

            // Initialize the hover timer & rotation
            _hoverTimer = 0f;
            _rotation = 0f;
        }

        // Slight scale for the drawn item
        private const float ITEM_SCALE = 0.105f;

        // The bounding box will move with the item
        public override Rectangle BoundingBox
        {
            get
            {
                // If you want the bounding box to move with the bobbing offset, 
                // compute final position the same way you do in Draw:
                Vector2 finalPos = GetBobbingPosition();

                return new Rectangle(
                    (int)finalPos.X,
                    (int)finalPos.Y,
                    (int)(_frameWidth * ITEM_SCALE),
                    (int)(_frameHeight * ITEM_SCALE)
                );
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Collected) return;

            // Animate frames
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer > _frameTime)
            {
                _currentFrame = (_currentFrame + 1) % _totalFrames;
                _timer = 0f;
            }

            // Update the "hover" timer
            _hoverTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Compute a small rotation (z-axis in 2D)
            _rotation = 0.05f * (float)Math.Sin(_hoverTimer * 1.7f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Collected) return;

            // Where to draw, including bobbing offsets
            Vector2 finalPosition = GetBobbingPosition();

            // Source rectangle for the current animation frame
            Rectangle sourceRectangle = new Rectangle(
                _currentFrame * _frameWidth,
                0,
                _frameWidth,
                _frameHeight
            );

            // Draw with rotation about the texture's center
            Vector2 origin = new Vector2(_frameWidth / 2f, _frameHeight / 2f);

            spriteBatch.Draw(
                _texture,
                finalPosition + origin * ITEM_SCALE, // position plus half-size for correct rotation pivot
                sourceRectangle,
                Color.White,
                _rotation,
                origin,
                ITEM_SCALE,
                SpriteEffects.None,
                0f
            );
        }

        /// <summary>
        /// Calculates the bobbing/breathing offsets in X and Y.
        /// </summary>
        private Vector2 GetBobbingPosition()
        {
            // A small amplitude for subtle movement
            float amplitudeX = 2f;  // how many pixels left-right
            float amplitudeY = 2f;  // how many pixels up-down

            // Frequencies for x and y movement
            float freqX = 2f;  // speed of left-right sway
            float freqY = 3f;  // speed of up-down bounce

            // Sin and cos for variety 
            float offsetX = amplitudeX * (float)Math.Sin(_hoverTimer * freqX);
            float offsetY = amplitudeY * (float)Math.Cos(_hoverTimer * freqY);

            // Return original position plus offsets
            return new Vector2(
                _originalPosition.X + offsetX,
                _originalPosition.Y + offsetY
            );
        }
    }
}
