using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Splits text into lines no wider than <paramref name="maxWidth"/>, breaking between words.
        /// A line break ("\n") in the text always starts a new line.
        /// </summary>
        public static List<string> WrapText(SpriteFont font, string text, float maxWidth, float scale = 1f)
        {
            var lines = new List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                string current = "";
                foreach (string word in paragraph.Split(' '))
                {
                    string candidate = current.Length == 0 ? word : current + " " + word;
                    if (current.Length > 0 && font.MeasureString(candidate).X * scale > maxWidth)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = candidate;
                    }
                }
                lines.Add(current);
            }
            return lines;
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

        /// <summary>A round gold, silver or bronze medal on a short ribbon, centered on <paramref name="center"/>.</summary>
        public static void DrawMedal(SpriteBatch spriteBatch, Vector2 center, float size, Gameplay.Medal medal, float opacity = 1f)
        {
            if (medal == Gameplay.Medal.None)
                return;

            Color color = medal switch
            {
                Gameplay.Medal.Gold => new Color(255, 205, 60),
                Gameplay.Medal.Silver => new Color(205, 210, 220),
                _ => new Color(210, 130, 65),
            };

            // The ribbon: a red and a blue strip meeting above the medal.
            var ribbonSize = new Vector2(size * 0.22f, size * 0.7f);
            var ribbonOrigin = new Vector2(0.5f, 1f);
            Vector2 top = center - new Vector2(0f, size * 0.3f);
            spriteBatch.Draw(Assets.Pixel, top, null, new Color(200, 50, 50) * opacity, 0.45f, ribbonOrigin, ribbonSize, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Pixel, top, null, new Color(60, 90, 200) * opacity, -0.45f, ribbonOrigin, ribbonSize, SpriteEffects.None, 0f);

            var origin = new Vector2(Assets.Circle.Width / 2f);
            float scale = size / Assets.Circle.Width;
            spriteBatch.Draw(Assets.Circle, center, null, Color.Black * (0.6f * opacity), 0f, origin, scale * 1.12f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Circle, center, null, color * opacity, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Circle, center, null, Color.Black * (0.15f * opacity), 0f, origin, scale * 0.7f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Circle, center - new Vector2(size * 0.08f), null, Color.White * (0.45f * opacity), 0f, origin, scale * 0.25f, SpriteEffects.None, 0f);
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
            int capHeight = (int)textSize.Y + 6;
            int capWidth = key == null ? 0 : KeyCapWidth(key, capHeight);

            int width = padding * 2 + (int)textSize.X + (key == null ? 0 : capWidth + gap);
            int height = padding * 2 + capHeight;
            int x = (int)MathHelper.Clamp(bottomCenter.X - width / 2f, 8, VirtualScreen.ViewWidth - width - 8);
            var box = new Rectangle(x, (int)bottomCenter.Y - height, width, height);

            spriteBatch.Draw(Assets.Pixel, box, Color.Black * (0.7f * opacity));
            DrawOutline(spriteBatch, box, Color.White * (0.25f * opacity));

            int textX = box.X + padding;
            if (key != null)
            {
                DrawKeyCap(spriteBatch, new Rectangle(textX, box.Y + padding, capWidth, capHeight), key, opacity);
                textX += capWidth + gap;
            }

            spriteBatch.DrawString(font, text, new Vector2(textX, box.Center.Y - textSize.Y / 2f), Color.White * opacity);
        }

        /// <summary>The width of a key cap of the given height, wide enough for its label.</summary>
        public static int KeyCapWidth(string key, int height)
        {
            return Math.Max(height, (int)Assets.SmallFont.MeasureString(key).X + 16);
        }

        /// <summary>A light keyboard key with a label on it, like the E in "[E] Open".</summary>
        public static void DrawKeyCap(SpriteBatch spriteBatch, Rectangle cap, string key, float opacity = 1f)
        {
            Vector2 textSize = Assets.SmallFont.MeasureString(key);
            spriteBatch.Draw(Assets.Pixel, cap, KeyCap * opacity);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(cap.X, cap.Bottom - 5, cap.Width, 5), Color.Black * (0.2f * opacity));
            DrawOutline(spriteBatch, cap, KeyCapText * opacity);
            spriteBatch.DrawString(Assets.SmallFont, key, cap.Center.ToVector2() - textSize / 2f - new Vector2(0, 2), KeyCapText * opacity);
        }
    }
}
