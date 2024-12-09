using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

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
        private const float BoundingBoxScale = 0.06f;  // Adjust the bounding box size relative to the monster's scale

        // Enum for Facing Direction
        public enum FacingDirection
        {
            Left,
            Right
        }

        // The current facing direction of the monster
        public FacingDirection Direction { get; private set; }

        public Monster(GraphicsDevice graphicsDevice, Vector2 position, Rectangle platformBounds)
        {
            _textureLeft = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left.png"));
            _textureRight = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right.png"));

            _textureLeftYellow = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left_yellow.png"));
            _textureRightYellow = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right_yellow.png"));
            
            _currentTexture = _textureRight; // Start facing right
            _position = new Vector2(position.X, position.Y - _textureLeft.Height / 19);;
            _platformBounds = platformBounds;
            _speed = 1.0f;
            _movingLeft = false;

            Direction = FacingDirection.Right; // Initially facing right
        }

        // Adjusted Bounding Box to be smaller than the texture
        public Rectangle BoundingBox => new Rectangle(
            (int)(_position.X + _currentTexture.Width * Scale / 2), // Offset horizontally to center the box
            (int)(_position.Y + _currentTexture.Height * Scale / 2), // Offset vertically to center the box
            (int)(_currentTexture.Width * Scale * BoundingBoxScale),  // Smaller width
            (int)(_currentTexture.Height * Scale * BoundingBoxScale)  // Smaller height
        );

        public void Update(GameTime gameTime, Player player)
        {
            // Update the facing direction based on the player's position
            if (player.Position.X < _position.X)
            {
                Direction = FacingDirection.Left;
            }
            else
            {
                Direction = FacingDirection.Right;
            }

            // Move the monster
            if (_movingLeft)
            {
                _position.X -= _speed;

                // Check if player is on the same platform and to the left
                if (IsPlayerInSight(player.Position) && player.Position.X < _position.X)
                {
                    _currentTexture = _textureLeftYellow;  // Change texture when facing the player
                }
                else
                {
                    _currentTexture = _textureLeft; // Default texture when not facing the player
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
                if (IsPlayerInSight(player.Position) && player.Position.X > _position.X)
                {
                    _currentTexture = _textureRightYellow;  // Change texture when facing the player
                }
                else
                {
                    _currentTexture = _textureRight; // Default texture when not facing the player
                }

                if (_position.X + _currentTexture.Width * Scale >= _platformBounds.Right)
                {
                    _movingLeft = true;
                }
            }

            // Check for collisions between the player and monsters

            if (player.BoundingBox.Intersects(BoundingBox))
            {
                bool isMonsterFacingPlayer = IsFacingPlayer(player.Position);

                if (!player.IsInvincible)
                {
                    int monsterDirection = 0;

                    // Determine the direction the monster is facing
                    if (Direction == Monster.FacingDirection.Right)
                    {
                        monsterDirection = 1;  // Monster is facing right
                    }
                    else if (Direction == Monster.FacingDirection.Left)
                    {
                        monsterDirection = -1;  // Monster is facing left
                    }

                    // Apply damage and knockback
                    if (isMonsterFacingPlayer)
                    {
                        // If the monster is facing the player, apply knockback based on the direction the monster is facing
                        player.LoseHeart(1, monsterDirection);
                    }
                    else
                    {
                        // If the monster is not facing the player, apply lesser damage
                        player.LoseHeart(0.5f, monsterDirection);
                    }
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
