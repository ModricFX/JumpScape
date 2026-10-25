using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A hidden gem to collect. Gems found on an earlier finished run are drawn see-through, and can be
    /// collected again.
    /// </summary>
    public sealed class Gem
    {
        public const int Size = 36;

        private static readonly Color Body = new Color(80, 230, 255);
        private static readonly Color Light = new Color(200, 250, 255);

        private readonly Vector2 _origin;
        private float _time;

        /// <param name="index">The gem's number in the whole level, used to remember which gems were found.</param>
        public Gem(Vector2 position, int index, bool foundBefore)
        {
            _origin = position;
            Index = index;
            WasFoundBefore = foundBefore;
            _time = index * 0.7f;
        }

        public int Index { get; }

        public bool WasFoundBefore { get; }

        public bool IsCollected { get; private set; }

        /// <summary>The gem bobs gently up and down.</summary>
        private Vector2 Position => _origin + new Vector2(0f, 4f * MathF.Sin(_time * 2.5f));

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Size, Size);

        public Vector2 Center => Position + new Vector2(Size / 2f);

        public void Collect() => IsCollected = true;

        public void Update(float deltaTime)
        {
            _time += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
                DrawIcon(spriteBatch, Center, Size, WasFoundBefore ? 0.35f : 1f, 0.5f + 0.2f * MathF.Sin(_time * 4f));
        }

        /// <summary>Draws a gem centered on <paramref name="center"/>, also used for the counter in the corner.</summary>
        public static void DrawIcon(SpriteBatch spriteBatch, Vector2 center, float size, float opacity = 1f, float glow = 0.5f)
        {
            float glowScale = size * 2.4f / Assets.Glow.Width;
            spriteBatch.Draw(Assets.Glow, center, null, Body * (glow * opacity), 0f, new Vector2(Assets.Glow.Width / 2f), glowScale, SpriteEffects.None, 0f);

            // A square turned 45 degrees is a diamond. A smaller, lighter one on top gives it a shine.
            float side = size / MathF.Sqrt(2f);
            var origin = new Vector2(0.5f);
            spriteBatch.Draw(Assets.Pixel, center, null, Color.Black * (0.5f * opacity), MathHelper.PiOver4, origin, side + 4f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Pixel, center, null, Body * opacity, MathHelper.PiOver4, origin, side, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Pixel, center - new Vector2(size * 0.12f), null, Light * opacity, MathHelper.PiOver4, origin, side * 0.4f, SpriteEffects.None, 0f);
        }
    }
}
