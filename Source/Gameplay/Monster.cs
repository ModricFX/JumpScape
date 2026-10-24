using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A frog that walks back and forth along its platform. It turns yellow when it sees the player.
    /// Walking into it from the front costs a full heart, from behind half a heart. One thrown orb defeats it.
    /// </summary>
    public sealed class Monster
    {
        private const float Scale = 0.06f;
        private const float Speed = 1f;
        private const float HitboxFraction = 0.5f;
        private const int SightRangeX = 200;
        private const int SightRangeY = 80;
        private const float SoundRange = 400f;
        private const float SoundVolume = 0.5f;

        private readonly Rectangle _platform;
        private readonly SoundEffectInstance _growl;
        private Vector2 _position;
        private bool _movingLeft;
        private bool _seesPlayer;

        public Monster(Rectangle platform)
        {
            _platform = platform;
            // The frog image has empty space at the bottom, so it is drawn slightly into the platform.
            _position = new Vector2(platform.X, platform.Y - Assets.FrogRight.Height / 19);
            _growl = Assets.MonsterGrowl.CreateInstance();
            _growl.IsLooped = true;
        }

        private float Size => Assets.FrogRight.Width * Scale;

        public Vector2 Center => _position + new Vector2(Size / 2f);

        public bool IsDefeated { get; private set; }

        /// <summary>The whole sprite; thrown orbs hit anywhere on it.</summary>
        public Rectangle Bounds => new Rectangle((int)_position.X, (int)_position.Y, (int)Size, (int)Size);

        /// <summary>A box in the middle of the sprite, smaller than the image so near misses don't hurt.</summary>
        public Rectangle Hitbox
        {
            get
            {
                float hitboxSize = Size * HitboxFraction;
                Vector2 topLeft = Center - new Vector2(hitboxSize / 2f);
                return new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)hitboxSize, (int)hitboxSize);
            }
        }

        public void Update(Player player)
        {
            if (_movingLeft)
            {
                _position.X -= Speed;
                if (_position.X <= _platform.Left)
                    _movingLeft = false;
            }
            else
            {
                _position.X += Speed;
                if (_position.X + Size >= _platform.Right)
                    _movingLeft = true;
            }

            _seesPlayer = CanSee(player.Position) && IsMovingTowards(player.Position);

            if (player.Bounds.Intersects(Hitbox))
            {
                int pushDirection = player.Position.X < _position.X ? -1 : 1;
                player.TakeDamage(IsMovingTowards(player.Position) ? 1f : 0.5f, pushDirection);
            }

            UpdateSound(player);
        }

        private bool CanSee(Vector2 playerPosition)
        {
            return playerPosition.Y >= _position.Y - SightRangeY
                && playerPosition.Y <= _position.Y + SightRangeY
                && playerPosition.X >= _platform.Left - SightRangeX
                && playerPosition.X <= _platform.Right + SightRangeX;
        }

        private bool IsMovingTowards(Vector2 playerPosition)
        {
            return _movingLeft ? playerPosition.X < _position.X : playerPosition.X > _position.X;
        }

        private void UpdateSound(Player player)
        {
            if (player.IsDead)
            {
                _growl.Stop();
                return;
            }

            PositionalSound.Apply(_growl, Center, player.Center, SoundRange, SoundVolume);
            if (_growl.State != SoundState.Playing)
                _growl.Play();
        }

        public void StopSounds()
        {
            _growl.Stop();
        }

        public void Defeat()
        {
            IsDefeated = true;
            _growl.Stop();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = _movingLeft
                ? (_seesPlayer ? Assets.FrogLeftAlert : Assets.FrogLeft)
                : (_seesPlayer ? Assets.FrogRightAlert : Assets.FrogRight);

            spriteBatch.Draw(texture, _position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}
