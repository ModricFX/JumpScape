using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>Drawing helpers shared by the menus and the in-game interface, so everything looks alike.</summary>
    public static class UiDrawing
    {
        public static readonly Color Highlight = Color.Yellow;

        private static readonly Color DarkWood = new Color(30, 18, 12);
        private static readonly Color KeyCap = new Color(244, 232, 205);
        private static readonly Color KeyCapText = new Color(70, 40, 25);

        public static void DrawLogo(SpriteBatch spriteBatch, float scale)
        {
            float width = Assets.Logo.Width * scale;
            var position = new Vector2((VirtualScreen.Width - width) / 2f, VirtualScreen.Height * 0.01f);
            spriteBatch.Draw(Assets.Logo, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Draws text with a soft drop shadow.</summary>
        public static void DrawText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, float scale = 1f)
        {
            Color shadow = Color.Black * (0.5f * color.A / 255f);
            spriteBatch.DrawString(font, text, position + new Vector2(2, 2) * scale, shadow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f)
        {
            DrawText(spriteBatch, Assets.Font, text, position, color, scale);
        }

        public static void DrawCenteredText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 center, Color color, float scale = 1f)
        {
            DrawText(spriteBatch, font, text, center - font.MeasureString(text) * scale / 2f, color, scale);
        }

        public static void DrawOutline(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness = 2)
        {
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Y, area.Width, thickness), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.X, area.Y, thickness, area.Height), color);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
        }

        /// <summary>A rectangle of the given size in the middle of the 1920x1080 menu area.</summary>
        public static Rectangle CenteredPanel(int width, int height)
        {
            return new Rectangle((VirtualScreen.Width - width) / 2, (VirtualScreen.Height - height) / 2, width, height);
        }

        /// <summary>A wooden panel, slightly darkened so text on it is easy to read.</summary>
        public static void DrawPanel(SpriteBatch spriteBatch, Rectangle area, float opacity = 1f)
        {
            spriteBatch.Draw(Assets.WoodPanel, area, Color.White * opacity);
            spriteBatch.Draw(Assets.Pixel, area, Color.Black * (0.35f * opacity));
            DrawOutline(spriteBatch, area, DarkWood * opacity, 4);
        }

        /// <summary>A wooden plank button. The selected one is a little bigger and brighter, with yellow text.</summary>
        public static void DrawButton(SpriteBatch spriteBatch, Rectangle area, string label, bool selected, float opacity = 1f, float textScale = 1f)
        {
            Rectangle plank = area;
            if (selected)
                plank.Inflate(area.Width / 25, area.Height / 12);

            // A soft shadow keeps the dark plank visible on the dark wooden panels.
            Rectangle shadow = plank;
            shadow.Offset(0, 6);
            spriteBatch.Draw(Assets.MenuButton, shadow, Color.Black * (0.45f * opacity));

            Color tint = selected ? Color.White : new Color(225, 225, 225);
            spriteBatch.Draw(Assets.MenuButton, plank, tint * opacity);

            float scale = textScale * (selected ? 1.06f : 1f);
            DrawCenteredText(spriteBatch, Assets.Font, label, area.Center.ToVector2(), (selected ? Highlight : Color.White) * opacity, scale);
        }

        /// <summary>
        /// A small dark bubble with an optional key cap in front of the text, like "[E] Open".
        /// It is drawn above <paramref name="bottomCenter"/> and kept inside the window.
        /// </summary>
        public static void DrawHint(SpriteBatch spriteBatch, Vector2 bottomCenter, string key, string text, float opacity = 1f)
        {
            const int padding = 10;
            const int gap = 10;
            SpriteFont font = Assets.SmallFont;

            Vector2 textSize = font.MeasureString(text);
            Vector2 keyTextSize = key == null ? Vector2.Zero : font.MeasureString(key);
            int capHeight = (int)textSize.Y + 6;
            int capWidth = key == null ? 0 : Math.Max(capHeight, (int)keyTextSize.X + 16);

            int width = padding * 2 + (int)textSize.X + (key == null ? 0 : capWidth + gap);
            int height = padding * 2 + capHeight;
            int x = (int)MathHelper.Clamp(bottomCenter.X - width / 2f, 8, VirtualScreen.ViewWidth - width - 8);
            var box = new Rectangle(x, (int)bottomCenter.Y - height, width, height);

            spriteBatch.Draw(Assets.Pixel, box, Color.Black * (0.7f * opacity));
            DrawOutline(spriteBatch, box, Color.White * (0.25f * opacity));

            int textX = box.X + padding;
            if (key != null)
            {
                var cap = new Rectangle(textX, box.Y + padding, capWidth, capHeight);
                spriteBatch.Draw(Assets.Pixel, cap, KeyCap * opacity);
                spriteBatch.Draw(Assets.Pixel, new Rectangle(cap.X, cap.Bottom - 5, cap.Width, 5), Color.Black * (0.2f * opacity));
                DrawOutline(spriteBatch, cap, KeyCapText * opacity);
                spriteBatch.DrawString(font, key, cap.Center.ToVector2() - keyTextSize / 2f - new Vector2(0, 2), KeyCapText * opacity);
                textX += capWidth + gap;
            }

            spriteBatch.DrawString(font, text, new Vector2(textX, box.Center.Y - textSize.Y / 2f), Color.White * opacity);
        }
    }
}
