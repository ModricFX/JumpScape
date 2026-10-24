using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>A spinning key floating in the level that the player can pick up.</summary>
    public sealed class KeyPickup
    {
        private const int FrameCount = 16;
        private const float FrameDuration = 0.1f;
        private const float Scale = 0.105f;

        private readonly Vector2 _origin;
        private int _frame;
        private float _frameTimer;
        private float _time;

        public KeyPickup(Vector2 position)
        {
            _origin = position;
        }

        public bool IsCollected { get; private set; }

        private static int FrameWidth => Assets.KeySpriteSheet.Width / FrameCount;

        public static Vector2 Size => new Vector2(FrameWidth, Assets.KeySpriteSheet.Height) * Scale;

        /// <summary>The key gently bobs around its starting position.</summary>
        private Vector2 Position => _origin + new Vector2(2f * MathF.Sin(_time * 2f), 2f * MathF.Cos(_time * 3f));

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        public void Collect()
        {
            IsCollected = true;
        }

        public void Update(float deltaTime)
        {
            if (IsCollected)
                return;

            _time += deltaTime;
            _frameTimer += deltaTime;
            if (_frameTimer > FrameDuration)
            {
                _frame = (_frame + 1) % FrameCount;
                _frameTimer = 0f;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsCollected)
                return;

            var source = new Rectangle(_frame * FrameWidth, 0, FrameWidth, Assets.KeySpriteSheet.Height);
            var origin = new Vector2(source.Width / 2f, source.Height / 2f);
            float rotation = 0.05f * MathF.Sin(_time * 1.7f);

            spriteBatch.Draw(Assets.KeySpriteSheet, Position + origin * Scale, source, Color.White, rotation, origin, Scale, SpriteEffects.None, 0f);
        }
    }
}
