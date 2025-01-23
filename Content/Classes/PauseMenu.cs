using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape
{
    public class PauseMenu
    {
        private SpriteFont font;
        private Texture2D backgroundTexture;
        private string[] menuItems = { "Resume", "Quit" };
        private int selectedIndex = -1;
        private bool isVisible;

        public PauseMenu(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            this.font = font;
            backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.Black * 0.7f });
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 cameraPosition)
        {
            if (!isVisible) return -1;

            KeyboardState ks = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            // Keyboard Navigation
            if (ks.IsKeyDown(Keys.W) || ks.IsKeyDown(Keys.Up))
                selectedIndex = Math.Max(0, selectedIndex - 1);

            if (ks.IsKeyDown(Keys.S) || ks.IsKeyDown(Keys.Down))
                selectedIndex = Math.Min(menuItems.Length - 1, selectedIndex + 1);

            if (ks.IsKeyDown(Keys.Enter) || ks.IsKeyDown(Keys.Space))
                return selectedIndex;

            // Adjust mouse position to account for camera position
            Point adjustedMousePosition = new Point(
                ms.Position.X + (int)cameraPosition.X,
                ms.Position.Y + (int)cameraPosition.Y
            );

            // Mouse Navigation and Click Detection
            for (int i = 0; i < menuItems.Length; i++)
            {
                Vector2 menuItemPosition = GetMenuItemPosition(i, graphicsDevice, cameraPosition);
                Rectangle itemRect = new Rectangle(
                    (int)menuItemPosition.X,
                    (int)menuItemPosition.Y,
                    (int)font.MeasureString(menuItems[i]).X,
                    (int)font.MeasureString(menuItems[i]).Y
                );

                if (itemRect.Contains(adjustedMousePosition))
                {
                    selectedIndex = i;

                    if (ms.LeftButton == ButtonState.Pressed)
                    {
                        return i; // Return index of clicked item
                    }
                }
            }

            return -1;
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 cameraPosition)
        {
            if (!isVisible) return;

            // Draw semi-transparent background
            spriteBatch.Draw(
                backgroundTexture,
                new Rectangle((int)cameraPosition.X, (int)cameraPosition.Y, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height),
                Color.White
            );

            string title = "Paused";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePosition = new Vector2(
                cameraPosition.X + (graphicsDevice.Viewport.Width - titleSize.X) / 2,
                cameraPosition.Y + graphicsDevice.Viewport.Height * 0.1f
            );

            spriteBatch.DrawString(font, title, titlePosition, Color.White);

            for (int i = 0; i < menuItems.Length; i++)
            {
                Vector2 textPosition = GetMenuItemPosition(i, graphicsDevice, cameraPosition);

                Color color = (i == selectedIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(font, menuItems[i], textPosition, color);
            }
        }

        private Vector2 GetMenuItemPosition(int index, GraphicsDevice graphicsDevice, Vector2 cameraPosition)
        {
            string text = menuItems[index];
            Vector2 textSize = font.MeasureString(text);

            return new Vector2(
                cameraPosition.X + (graphicsDevice.Viewport.Width - textSize.X) / 2,
                cameraPosition.Y + graphicsDevice.Viewport.Height * 0.4f + (index * 60)
            );
        }

    }
}
