using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A flag on a pole. Touching it makes it the place to come back to after dying. The flag is grey until it
    /// is touched, then green and waving.
    /// </summary>
    public sealed class Checkpoint
    {
        public const int Width = 64;
        public const int Height = 120;

        private const int PoleWidth = 6;
        private static readonly Color Pole = new Color(90, 70, 50);
        private static readonly Color Inactive = new Color(150, 150, 150);
        private static readonly Color Active = new Color(90, 220, 110);

        private float _time;

        public Checkpoint(Vector2 position)
        {
            Position = position;
        }

        /// <summary>Top-left corner of the flag and pole.</summary>
        public Vector2 Position { get; }

        public bool IsActive { get; private set; }

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public void Activate() => IsActive = true;

        public void Deactivate() => IsActive = false;

        /// <summary>Where a player of the given size stands when they come back here: at the foot of the pole.</summary>
        public Vector2 ArrivalPosition(Rectangle playerBounds)
        {
            return new Vector2(Position.X + PoleWidth / 2f - playerBounds.Width / 2f, Position.Y + Height - playerBounds.Height);
        }

        public void Update(float deltaTime)
        {
            _time += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var pole = new Rectangle((int)Position.X, (int)Position.Y, PoleWidth, Height);
            spriteBatch.Draw(Assets.Pixel, pole, Pole);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(pole.X, pole.Y, 2, Height), Color.White * 0.25f);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(pole.X - 4, pole.Bottom - 6, PoleWidth + 8, 6), Pole);

            // The flag is a triangle drawn one column at a time, rippling a little once it's active.
            Color color = IsActive ? Active : Inactive;
            const int flagWidth = Width - PoleWidth - 4;
            const int flagHeight = 44;
            for (int x = 0; x < flagWidth; x++)
            {
                float t = (float)x / flagWidth;
                float wave = IsActive ? MathF.Sin(_time * 6f - t * 5f) * 4f * t : 0f;
                int height = (int)(flagHeight * (1f - t));
                if (height <= 0)
                    break;
                int top = (int)(Position.Y + 4 + (flagHeight - height) / 2f + wave);
                Color shade = x % 6 < 3 ? color : color * 0.9f;
                spriteBatch.Draw(Assets.Pixel, new Rectangle(pole.Right + x, top, 1, height), shade);
            }

            if (IsActive)
            {
                float glow = 0.35f + 0.15f * MathF.Sin(_time * 3f);
                var center = new Vector2(pole.Right + flagWidth / 3f, Position.Y + 4 + flagHeight / 2f);
                spriteBatch.Draw(Assets.Glow, center, null, Active * glow, 0f, new Vector2(Assets.Glow.Width / 2f), 90f / Assets.Glow.Width, SpriteEffects.None, 0f);
            }
        }
    }
}
