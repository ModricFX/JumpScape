using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>Small drawing helpers shared by the menus.</summary>
    internal static class MenuDrawing
    {
        public static void DrawLogo(SpriteBatch spriteBatch, float scale)
        {
            float width = Assets.Logo.Width * scale;
            var position = new Vector2((VirtualScreen.Width - width) / 2f, VirtualScreen.Height * 0.01f);
            spriteBatch.Draw(Assets.Logo, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Draws text with a soft drop shadow.</summary>
        public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f)
        {
            spriteBatch.DrawString(Assets.Font, text, position + new Vector2(2, 2), Color.Black * 0.5f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(Assets.Font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawOutline(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness = 2)
        {
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Y, area.Width, thickness), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Y, thickness, area.Height), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
        }

        /// <summary>The centered wooden panel used by the level select and settings screens.</summary>
        public static Rectangle CenteredPanel(int width, int height)
        {
            return new Rectangle((VirtualScreen.Width - width) / 2, (VirtualScreen.Height - height) / 2, width, height);
        }
    }
}
