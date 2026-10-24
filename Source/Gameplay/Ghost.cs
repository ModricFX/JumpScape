using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A ghost that hovers in place and chases the player when they come within its radius.
    /// Touching it costs half a heart, after which it floats back home.
    /// </summary>
    public sealed class Ghost
    {
        private const float Scale = 0.1f;
        private const float HitboxFraction = 0.5f;
        private const float ChaseSpeed = 150f;
        private const float ReturnSpeed = 100f;
        private const float Smoothing = 0.2f;

        private readonly Vector2 _home;
        private readonly float _radius;
        private Vector2 _target;
        private Vector2 _position;
        private bool _facingRight = true;
        private float _hoverTime;

        public Ghost(Vector2 position, float radius)
        {
            _home = position;
            _target = position;
            _position = position;
            _radius = radius;
        }

        private static float Size => Assets.GhostRight.Width * Scale;

        private Vector2 DrawPosition => _position + new Vector2(0f, MathF.Sin(_hoverTime * 2f) * 5f);

        public Rectangle Hitbox
        {
            get
            {
                float hitboxSize = Size * HitboxFraction;
                Vector2 topLeft = DrawPosition + new Vector2((Size - hitboxSize) / 2f);
                return new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)hitboxSize, (int)hitboxSize);
            }
        }

        public void Update(float deltaTime, Player player)
        {
            _hoverTime += deltaTime;

            if (player.Bounds.Intersects(Hitbox))
                player.TakeDamage(0.5f, _facingRight ? 1 : -1);

            // Chase the player while they are close, otherwise (or right after a hit) float back home.
            Vector2 offsetToTarget = player.Center - (_target + new Vector2(Size / 2f));
            bool chasing = !player.IsInvincible && !player.IsDead && offsetToTarget.Length() <= _radius;

            if (chasing)
                MoveTowards(player.Center - new Vector2(Size / 2f), ChaseSpeed * deltaTime);
            else
                MoveTowards(_home, ReturnSpeed * deltaTime);

            _position = Vector2.Lerp(_position, _target, Smoothing);
        }

        private void MoveTowards(Vector2 destination, float distance)
        {
            Vector2 offset = destination - _target;
            if (offset.Length() <= Math.Max(distance, 0.1f))
            {
                _target = destination;
                if (destination == _home)
                    _facingRight = true;
                return;
            }

            offset.Normalize();
            _target += offset * distance;
            _facingRight = offset.X >= 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = _facingRight ? Assets.GhostRight : Assets.GhostLeft;
            spriteBatch.Draw(texture, DrawPosition, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}
