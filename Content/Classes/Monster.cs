using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class Monster
    {
        private Texture2D _textureLeft;
        private Texture2D _textureRight;
        private Texture2D _textureLeftYellow;
        private Texture2D _textureRightYellow;
        private Texture2D _currentTexture;
        private Vector2 _position;
        private Rectangle _platformBounds;
        private float _speed;
        private bool _movingLeft;

        // Adjust scale to make the monster smaller
        private const float Scale = 0.06f;  // Reduced scale for smaller monster size

        // Bounding Box Adjustments
        private const float BoundingBoxScale = 0.057f;  // Adjust the bounding box size relative to the monster's scale

        public Monster(Texture2D textureLeft, Texture2D textureRight, Texture2D textureLeftYellow, Texture2D textureRightYellow, Vector2 position, Rectangle platformBounds)
        {
            _textureLeft = textureLeft;
            _textureRight = textureRight;
            _textureLeftYellow = textureLeftYellow;
            _textureRightYellow = textureRightYellow;
            _currentTexture = _textureRight; // Start facing right
            _position = position;
            _platformBounds = platformBounds;
            _speed = 1.0f;
            _movingLeft = false;
        }

        // Adjusted Bounding Box to be smaller than the texture
        public Rectangle BoundingBox => new Rectangle(
            (int)_position.X + 10, // Offset horizontally to center the box
            (int)_position.Y + 10, // Offset vertically to center the box
            (int)(_currentTexture.Width * Scale * BoundingBoxScale),  // Smaller width
            (int)(_currentTexture.Height * Scale * BoundingBoxScale)  // Smaller height
        );

        public void Update(GameTime gameTime, Vector2 playerPosition)
        {
            // Move the monster
            if (_movingLeft)
            {
                _position.X -= _speed;

                // Check if player is on the same platform and to the left
                if (IsPlayerInSight(playerPosition) && playerPosition.X < _position.X)
                {
                    _currentTexture = _textureLeftYellow;
                }
                else
                {
                    _currentTexture = _textureLeft;
                }

                if (_position.X <= _platformBounds.Left)
                {
                    _movingLeft = false;
                }
            }
            else
            {
                _position.X += _speed;

                // Check if player is on the same platform and to the right
                if (IsPlayerInSight(playerPosition) && playerPosition.X > _position.X)
                {
                    _currentTexture = _textureRightYellow;
                }
                else
                {
                    _currentTexture = _textureRight;
                }

                if (_position.X + _currentTexture.Width * Scale >= _platformBounds.Right)
                {
                    _movingLeft = true;
                }
            }
        }

        private bool IsPlayerInSight(Vector2 playerPosition)
        {
            int detectionMargin = 200; // Allow detection up to 1000 pixels beyond the monster bounds
            int verticalMargin = 80; // Allow some vertical margin for detection (e.g., for standing or jumping)

            // Check if the player is vertically aligned with the monster
            // This ensures the player is within the vertical range of the monster's position
            bool playerOnSamePlatform = playerPosition.Y >= _position.Y - verticalMargin && playerPosition.Y <= _position.Y + verticalMargin;

            // Check if the player is within the extended horizontal detection margin
            bool playerInHorizontalBounds =
                playerPosition.X >= _platformBounds.Left - detectionMargin &&
                playerPosition.X <= _platformBounds.Right + detectionMargin;

            // The player is considered to be in sight if both conditions are true
            return playerOnSamePlatform && playerInHorizontalBounds;
        }

        public bool IsFacingPlayer(Vector2 playerPosition)
        {
            if (_movingLeft && playerPosition.X < _position.X)
                return true;
            else if (!_movingLeft && playerPosition.X > _position.X)
                return true;
            return false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Apply scale factor to make the texture appear smaller
            spriteBatch.Draw(_currentTexture, _position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }

    }
}
