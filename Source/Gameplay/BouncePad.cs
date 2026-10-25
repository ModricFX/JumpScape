using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>A spring on a platform. Landing on it, or walking onto it, launches the player high into the air.</summary>
    public sealed class BouncePad
    {
        public const int Width = 80;
        public const int Height = 30;

        /// <summary>Launch speed in pixels per frame: about 600 px high, a bit more than a high jump.</summary>
        public const float LaunchSpeed = 26.8f;

        private const float SquashTime = 0.25f;
        private static readonly Color Plate = new Color(220, 60, 50);
        private static readonly Color Spring = new Color(200, 205, 215);
        private static readonly Color Base = new Color(70, 70, 80);

        private float _squash;

        public BouncePad(Vector2 position)
        {
            Bounds = new Rectangle((int)position.X, (int)position.Y, Width, Height);
        }

        public Rectangle Bounds { get; }

        /// <summary>Launches the player if they touch the pad while not already flying upwards.</summary>
        public bool TryLaunch(Player player)
        {
            if (player.IsDead || player.Velocity.Y < 0f || !player.Bounds.Intersects(Bounds))
                return false;

            player.Bounce(LaunchSpeed);
            _squash = SquashTime;
            return true;
        }

        public void Update(float deltaTime)
        {
            _squash = Math.Max(0f, _squash - deltaTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Right after a launch the spring is pressed down and springs back up.
            int press = (int)(12 * _squash / SquashTime);
            int plateTop = Bounds.Y + press;

            spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.X + 4, Bounds.Bottom - 6, Width - 8, 6), Base);

            // A zigzag spring between the base and the plate.
            int springTop = plateTop + 8, springBottom = Bounds.Bottom - 6;
            int coils = 4;
            for (int i = 0; i < coils; i++)
            {
                float y = MathHelper.Lerp(springTop, springBottom, (i + 0.5f) / coils);
                int width = i % 2 == 0 ? Width - 24 : Width - 36;
                spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.Center.X - width / 2, (int)y - 2, width, 4), Spring);
            }
            spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.Center.X - 3, springTop, 6, springBottom - springTop), Spring * 0.8f);

            spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.X, plateTop, Width, 8), Plate);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(Bounds.X, plateTop, Width, 2), Color.White * 0.4f);
        }
    }
}
