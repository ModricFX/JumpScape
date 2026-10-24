using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    public enum PowerUpType
    {
        /// <summary>Gives back one heart.</summary>
        Health,

        /// <summary>Lets the player jump once more in mid-air, for a while.</summary>
        DoubleJump,

        /// <summary>Makes the player jump higher, for a while.</summary>
        HighJump,
    }

    /// <summary>A floating, glowing power-up the player collects by touching it.</summary>
    public sealed class PowerUp
    {
        public const float IconSize = 44f;

        private readonly Vector2 _origin;
        private float _time;

        public PowerUp(Vector2 position, PowerUpType type)
        {
            _origin = position;
            Type = type;
        }

        public PowerUpType Type { get; }

        public bool IsCollected { get; private set; }

        private Vector2 Position => _origin + new Vector2(0f, MathF.Sin(_time * 3f) * 4f);

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, (int)IconSize, (int)IconSize);

        public static Texture2D IconFor(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.Health => Assets.HeartFull,
                PowerUpType.DoubleJump => Assets.DoubleJumpOrb,
                PowerUpType.HighJump => Assets.HighJumpOrb,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public static Color GlowColorFor(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.Health => Color.HotPink,
                PowerUpType.DoubleJump => Color.LightGreen,
                _ => Color.OrangeRed,
            };
        }

        public void Collect()
        {
            IsCollected = true;
        }

        public void Update(float deltaTime)
        {
            _time += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsCollected)
                return;

            Vector2 center = Position + new Vector2(IconSize / 2f);
            float pulse = 0.8f + 0.2f * MathF.Sin(_time * 4f);
            float glowScale = IconSize * 2.6f * pulse / Assets.Glow.Width;
            spriteBatch.Draw(Assets.Glow, center, null, GlowColorFor(Type), 0f, new Vector2(Assets.Glow.Width / 2f), glowScale, SpriteEffects.None, 0f);

            DrawIcon(spriteBatch, Type, center, IconSize, Color.White);
        }

        /// <summary>Draws a power-up's picture fitted into a square of the given size, centered on a point.</summary>
        public static void DrawIcon(SpriteBatch spriteBatch, PowerUpType type, Vector2 center, float size, Color color)
        {
            Texture2D icon = IconFor(type);
            float scale = size / Math.Max(icon.Width, icon.Height);
            var origin = new Vector2(icon.Width / 2f, icon.Height / 2f);
            spriteBatch.Draw(icon, center, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
