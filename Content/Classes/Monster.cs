using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
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
        private const float Scale = 0.06f;
        private const float BoundingBoxScale = 0.06f;

        public enum FacingDirection
        {
            Left,
            Right
        }
        public FacingDirection Direction { get; private set; }

        // --- Monster Sound ---
        private SoundEffect _monsterSound;
        private SoundEffectInstance _monsterSoundLoop; // Looped instance
        private bool _soundStarted;                    // Ensures we only play once

        public Monster(GraphicsDevice graphicsDevice, Vector2 position, Rectangle platformBounds)
        {
            // Load textures
            _textureLeft = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left.png"));
            _textureRight = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right.png"));
            _textureLeftYellow = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left_yellow.png"));
            _textureRightYellow = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right_yellow.png"));

            // Start facing right
            _currentTexture = _textureRight;
            _position = new Vector2(position.X, position.Y - _textureLeft.Height / 19);
            _platformBounds = platformBounds;
            _speed = 1.0f;
            _movingLeft = false;
            Direction = FacingDirection.Right;

            // Load the monster sound
            _monsterSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "MonsterSound.wav"));
            _monsterSoundLoop = _monsterSound.CreateInstance();
            _monsterSoundLoop.IsLooped = true;
            _soundStarted = false;
        }

        // Adjusted Bounding Box
        public Rectangle BoundingBox => new Rectangle(
            (int)(_position.X + _currentTexture.Width * Scale / 2),
            (int)(_position.Y + _currentTexture.Height * Scale / 2),
            (int)(_currentTexture.Width * Scale * BoundingBoxScale),
            (int)(_currentTexture.Height * Scale * BoundingBoxScale)
        );

        public void Update(GameTime gameTime, Player player)
        {
            // 1) Update monster movement & sprite
            UpdateMonsterMovement(player);

            // 2) Check collision with player
            if (player.BoundingBox.Intersects(BoundingBox))
            {
                bool isMonsterFacingPlayer = IsFacingPlayer(player.Position);

                if (!player.IsInvincible)
                {
                    // Determine monster's facing direction for knockback
                    int monsterDirection = (Direction == FacingDirection.Right) ? 1 : -1;

                    // Apply damage
                    if (isMonsterFacingPlayer)
                    {
                        player.LoseHeart(1, monsterDirection);
                    }
                    else
                    {
                        player.LoseHeart(0.5f, monsterDirection);
                    }
                }
            }

            // 3) Manually set volume & panning based on distance from player
            UpdateMonsterSoundVolume(player);
        }

        private void UpdateMonsterMovement(Player player)
        {
            // Decide facing direction based on player position
            if (player.Position.X < _position.X)
            {
                Direction = FacingDirection.Left;
            }
            else
            {
                Direction = FacingDirection.Right;
            }

            // Move left or right, flipping texture
            if (_movingLeft)
            {
                _position.X -= _speed;

                if (IsPlayerInSight(player.Position) && player.Position.X < _position.X)
                    _currentTexture = _textureLeftYellow;
                else
                    _currentTexture = _textureLeft;

                if (_position.X <= _platformBounds.Left)
                {
                    _movingLeft = false;
                }
            }
            else
            {
                _position.X += _speed;

                if (IsPlayerInSight(player.Position) && player.Position.X > _position.X)
                    _currentTexture = _textureRightYellow;
                else
                    _currentTexture = _textureRight;

                if (_position.X + _currentTexture.Width * Scale >= _platformBounds.Right)
                {
                    _movingLeft = true;
                }
            }
        }

        /// <summary>
        /// Dynamically adjust the monster's sound volume and panning based on how far the player is.
        /// </summary>
        private void UpdateMonsterSoundVolume(Player player)
        {
            // Start the looped monster sound if not started yet
            if (!_soundStarted)
            {
                _monsterSoundLoop.Play();
                _soundStarted = true;
            }

            // Calculate distance to player
            float distance = Vector2.Distance(_position, player.Position);

            // Values you can tweak:
            float minDistance = 20f;   // If closer than this, volume = 1.0
            float maxDistance = 400f;  // If farther than this, volume = 0.0

            // Clamp distance so it doesn't go below minDistance or above maxDistance
            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);

            // Compute a volume from 0.0 to 1.0 (linear fade)
            // distance = minDistance  -> volume = 1
            // distance = maxDistance  -> volume = 0
            float distanceVolume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));

            // --- Apply 80% maximum volume ---
            float finalVolume = distanceVolume * 0.5f;

            // Compute panning from -1 (full left) to 1 (full right)
            // If monster is left of player -> negative pan; if right -> positive pan
            // The divisor (maxDistance * 0.5f) controls how quickly panning moves from center to extremes
            float pan = (_position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);

            // Assign volume and pan to the looped sound
            _monsterSoundLoop.Volume = finalVolume;  // Now capped at 80% of full volume
            _monsterSoundLoop.Pan = pan;
        }

        private bool IsPlayerInSight(Vector2 playerPosition)
        {
            int detectionMargin = 200;
            int verticalMargin = 80;

            bool playerOnSamePlatform =
                playerPosition.Y >= _position.Y - verticalMargin &&
                playerPosition.Y <= _position.Y + verticalMargin;

            bool playerInHorizontalBounds =
                playerPosition.X >= _platformBounds.Left - detectionMargin &&
                playerPosition.X <= _platformBounds.Right + detectionMargin;

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
            spriteBatch.Draw(
                _currentTexture,
                _position,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
