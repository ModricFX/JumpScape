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
        private SoundEffectInstance _monsterSoundLoop;
        private bool _soundStarted;                   
        
        private GameSettings settings;

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

            settings = GameSettings.Load();
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
            // Update monster movement & sprite
            UpdateMonsterMovement(player);

            // Check collision with player
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

            // 3D SOUND
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
            // Start the looped monster sound if not already started
            if (!_soundStarted)
            {
                _monsterSoundLoop.Play();
                _soundStarted = true;
            }

            // distance-based volume
            float distance = Vector2.Distance(_position, player.Position);

            float minDistance = 20f;  
            float maxDistance = 400f;  

            float clampedDist = MathHelper.Clamp(distance, minDistance, maxDistance);

            // Distance-based fade 
            float distanceVolume = 1.0f - ((clampedDist - minDistance) / (maxDistance - minDistance));

            float monsterLocalMax = 0.5f;

            // "Game Volume" from settings (0 - 100)
            float gameVolumeFactor = (settings.Volume / 100f);

            // final volume = distance fade * monster local max * game volume factor
            float finalVolume = distanceVolume * monsterLocalMax * gameVolumeFactor;

            // Compute panning from -1 (full left) to 1 (full right)
            float pan = (_position.X - player.Position.X) / (maxDistance * 0.5f);
            pan = MathHelper.Clamp(pan, -1f, 1f);

            // Apply to the SoundEffectInstance
            _monsterSoundLoop.Volume = finalVolume;
            _monsterSoundLoop.Pan = pan;
        }

        public void stopSound()
        {
            _monsterSoundLoop.Stop();
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
