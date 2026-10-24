using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>A glowing orb the player throws towards the mouse. It defeats the first enemy it touches.</summary>
    public sealed class Projectile
    {
        private const float Speed = 15f; // Pixels per frame.
        private const float Lifetime = 1.5f;
        private const float HitSize = 20f;
        private const float GlowSize = 56f;
        private const int TrailLength = 3;

        private readonly Vector2 _velocity;
        private Vector2 _position;
        private float _age;

        public Projectile(Vector2 start, Vector2 direction)
        {
            _position = start;
            _velocity = Vector2.Normalize(direction) * Speed;
        }

        public Vector2 Position => _position;

        public bool IsFinished { get; private set; }

        public Rectangle Bounds => new Rectangle((int)(_position.X - HitSize / 2f), (int)(_position.Y - HitSize / 2f), (int)HitSize, (int)HitSize);

        public void Update(float deltaTime)
        {
            _position += _velocity;
            _age += deltaTime;

            bool offScreen = _position.X < -GlowSize || _position.X > VirtualScreen.ViewWidth + GlowSize;
            if (_age >= Lifetime || offScreen)
                IsFinished = true;
        }

        public void Finish()
        {
            IsFinished = true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // A few fading copies behind the orb make a short trail.
            for (int i = TrailLength; i >= 1; i--)
            {
                float fade = 1f - i / (TrailLength + 1f);
                DrawGlow(spriteBatch, _position - _velocity * (i * 0.6f), GlowSize * 0.7f * fade, Color.Orange * fade);
            }

            DrawGlow(spriteBatch, _position, GlowSize, Color.DarkOrange);
            // Drawn twice so the middle of the orb is nearly solid.
            for (int i = 0; i < 2; i++)
            {
                DrawGlow(spriteBatch, _position, GlowSize * 0.6f, Color.Gold);
                DrawGlow(spriteBatch, _position, GlowSize * 0.35f, Color.White);
            }
        }

        private static void DrawGlow(SpriteBatch spriteBatch, Vector2 center, float size, Color color)
        {
            float scale = size / Assets.Glow.Width;
            var origin = new Vector2(Assets.Glow.Width / 2f);
            spriteBatch.Draw(Assets.Glow, center, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
