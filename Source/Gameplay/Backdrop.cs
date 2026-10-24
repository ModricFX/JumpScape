using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A blurred copy of the forest picture from the menus, drawn behind the level. It is tinted towards the
    /// area's sky color and covered with a light haze so it stays in the background, and it scrolls more slowly
    /// than the level when the camera moves up (parallax).
    /// </summary>
    public static class Backdrop
    {
        private const float Parallax = 0.25f;
        private const float ExtraHeight = 1.3f;
        private const float HazeAmount = 0.35f;

        /// <param name="cameraOffset">How far the camera has moved up from its resting position, in pixels.</param>
        public static void Draw(SpriteBatch spriteBatch, Color sky, float cameraOffset)
        {
            Texture2D texture = Assets.BlurredBackground;
            float scale = Math.Max(VirtualScreen.ViewWidth / texture.Width, VirtualScreen.ViewHeight * ExtraHeight / texture.Height);
            float width = texture.Width * scale;
            float height = texture.Height * scale;

            // The picture's bottom edge sits at the bottom of the window; climbing reveals more of its top.
            float y = Math.Min(VirtualScreen.ViewHeight - height + cameraOffset * Parallax, 0f);
            var destination = new Rectangle((int)((VirtualScreen.ViewWidth - width) / 2f), (int)y, (int)Math.Ceiling(width), (int)Math.Ceiling(height));

            // Darker skies (like the nether) give a darker, more strongly tinted forest.
            float brightness = (sky.R * 0.299f + sky.G * 0.587f + sky.B * 0.114f) / 255f;
            Color tint = Color.Lerp(sky, Color.White, 0.3f + 0.4f * brightness);

            spriteBatch.Draw(texture, destination, tint);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(0, 0, (int)VirtualScreen.ViewWidth + 1, (int)VirtualScreen.ViewHeight + 1), sky * HazeAmount);
        }
    }
}
