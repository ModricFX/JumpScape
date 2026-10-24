using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>A small colored square that flies, falls and fades out.</summary>
    public sealed class Particle
    {
        private readonly float _lifetime;
        private readonly Color _color;
        private readonly float _size;
        private readonly float _gravity;
        private Vector2 _position;
        private Vector2 _velocity;
        private float _age;

        /// <param name="gravity">Downward acceleration in pixels per second squared.</param>
        public Particle(Vector2 position, Vector2 velocity, float lifetime, Color color, float size, float gravity = 300f)
        {
            _position = position;
            _velocity = velocity;
            _lifetime = lifetime;
            _color = color;
            _size = size;
            _gravity = gravity;
        }

        public bool IsDead => _age >= _lifetime;

        public void Update(float deltaTime)
        {
            _age += deltaTime;
            _velocity.Y += _gravity * deltaTime;
            _position += _velocity * deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float alpha = MathHelper.Clamp(1f - _age / _lifetime, 0f, 1f);
            spriteBatch.Draw(Assets.Pixel, _position, null, _color * alpha, 0f, new Vector2(0.5f), _size, SpriteEffects.None, 0f);
        }
    }
}
