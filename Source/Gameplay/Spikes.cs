using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>A row of metal spikes. Touching them costs a heart and bounces the player away.</summary>
    public sealed class Spikes
    {
        public const int Height = 30;
        public const int SpikeWidth = 30;

        private static readonly Color Metal = new Color(205, 210, 220);
        private static readonly Color Shadow = new Color(120, 125, 140);
        private static readonly Color Base = new Color(70, 70, 80);

        public Spikes(Vector2 position, int length)
        {
            Bounds = new Rectangle((int)position.X, (int)position.Y, length, Height);
        }

        public Rectangle Bounds { get; }

        /// <summary>A little smaller than the drawing, so only really touching a spike hurts.</summary>
        public Rectangle Hitbox => new Rectangle(Bounds.X + 4, Bounds.Y + 10, Bounds.Width - 8, Bounds.Height - 10);

        public void Draw(SpriteBatch spriteBatch)
        {
            // Each spike is a triangle drawn one row at a time, lit on the left and shaded on the right.
            for (int left = Bounds.X; left < Bounds.Right; left += SpikeWidth)
            {
                int width = Math.Min(SpikeWidth, Bounds.Right - left);
                for (int row = 0; row < Height - 4; row++)
                {
                    int rowWidth = (int)(width * (row + 1f) / (Height - 4));
                    int x = left + (width - rowWidth) / 2;
                    int y = Bounds.Y + row;
                    spriteBatch.Draw(Assets.Pixel, new Rectangle(x, y, rowWidth / 2, 1), Metal);
                    spriteBatch.Draw(Assets.Pixel, new Rectangle(x + rowWidth / 2, y, rowWidth - rowWidth / 2, 1), Shadow);
                }
            }
            spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.X, Bounds.Bottom - 4, Bounds.Width, 4), Base);
        }
    }
}
