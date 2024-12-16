using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace JumpScape
{
    public class SettingsMenu
    {
        private SpriteFont font;
        private SpriteFont smallFont;
        public List<string> menuItems;
        public int selectedIndex;
        public bool visible;
        private bool isDropdownOpen = false;

        private Texture2D _backgroundTexture;
        private Texture2D _gameLogoTexture;
        private Texture2D _woodBoxTexture;

        private Vector2 _logoPosition;
        private Vector2 _backgroundPosition;

        private MouseState previousMouseState;
        private int resolutionIndex = 1;
        private int volume = 80;
        private int sensitivity = 5;

        private readonly string[] resolutions = { "1280x720", "1920x1080", "2560x1440" };
        private const int SLIDER_WIDTH = 200;
        private bool isDraggingVolume = false;
        private bool isDraggingSensitivity = false;

        private GraphicsDevice graphicsDevice; // Store this so we can use it in HandleMouseInput
        private GraphicsDeviceManager graphicsDeviceManager;
        private float boxWidth = 800;
        private float boxHeight = 600;

        public SettingsMenu(SpriteFont font, SpriteFont smallFont, GraphicsDeviceManager _graphics, GraphicsDevice graphicsDevice, int width, int height)
        {
            this.font = font;
            this.smallFont = smallFont;
            this.graphicsDevice = graphicsDevice;
            this.graphicsDeviceManager = _graphics;

            menuItems = new List<string>() { resolutions[resolutionIndex], "Volume", "Sensitivity", "Back" };
            selectedIndex = 0;
            previousMouseState = Mouse.GetState();

            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _woodBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "woodBackground.png"));

            float logoScale = 0.4f;
            _logoPosition = new Vector2(
                (graphicsDeviceManager.PreferredBackBufferWidth - _gameLogoTexture.Width * logoScale) / 2,
                height * 0.02f
            );
        }

        public void ResetPreviousState()
        {
            previousMouseState = Mouse.GetState();
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition, SpriteBatch spriteBatch)
        {
            MouseState currentMouseState = Mouse.GetState();
            _backgroundPosition = backgroundPosition;

            HandleMouseInput(currentMouseState);

            previousMouseState = currentMouseState;
            return -1;
        }

        private int hoveredIndex = -1; // Track which dropdown item is hovered

        private void HandleMouseInput(MouseState currentMouseState)
        {
            // Raw mouse coordinates in window space
            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);

            // Suppose your UI and game logic is based on a virtual resolution:
            int virtualWidth = 1920;
            int virtualHeight = 1080;

            Viewport viewport = graphicsDevice.Viewport;

            // Calculate how the viewport scales the virtual resolution
            float scaleX = (float)viewport.Width / (float)virtualWidth;
            float scaleY = (float)viewport.Height / (float)virtualHeight;

            // Translate mouse from window coordinates into viewport space
            mousePosition.X -= viewport.X;
            mousePosition.Y -= viewport.Y;

            // Scale mouse position back to virtual resolution space
            mousePosition.X /= scaleX;
            mousePosition.Y /= scaleY;

            // Now position your UI in virtual coordinates as well
            Vector2 boxPosition = new Vector2(
                (virtualWidth / 2f) - (boxWidth / 2f),
                (virtualHeight / 2f) - (boxHeight / 2f)
            );

            Vector2 menuStartPosition = new Vector2(
                boxPosition.X + 60,
                boxPosition.Y + 130
            );

            Vector2 resolutionPosition = new Vector2(menuStartPosition.X + 300, menuStartPosition.Y);
            Rectangle dropdownRect = new Rectangle(
                (int)resolutionPosition.X,
                (int)resolutionPosition.Y,
                200,
                isDropdownOpen ? resolutions.Length * 30 + 30 : 30
            );

            bool mouseClicked = currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed;
            hoveredIndex = -1;

            if (isDropdownOpen)
            {
                bool clickedItem = false;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    // Subtracting 10 pixels from the Y-position to align with visible text:
                    Rectangle itemRect = new Rectangle(
                        (int)resolutionPosition.X,
                        (int)(resolutionPosition.Y + 30 * (i + 1) + 20),
                        200,
                        30
                    );

                    if (IsMouseOverRectangle(mousePosition, itemRect))
                    {
                        hoveredIndex = i;
                        if (mouseClicked)
                        {
                            resolutionIndex = i;
                            menuItems[0] = resolutions[resolutionIndex];
                            isDropdownOpen = false;
                            break;
                        }
                    }
                }


                if (!clickedItem && mouseClicked && !IsMouseOverRectangle(mousePosition, dropdownRect))
                {
                    isDropdownOpen = false;
                }
            }
            else
            {
                if (IsMouseOverRectangle(mousePosition, dropdownRect) && mouseClicked)
                {
                    isDropdownOpen = true;
                }
            }
        }


        private bool IsMouseOverRectangle(Vector2 mousePosition, Rectangle rect)
        {
            return mousePosition.X >= rect.X && mousePosition.X <= rect.X + rect.Width &&
                   mousePosition.Y >= rect.Y && mousePosition.Y <= rect.Y + rect.Height;
        }

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                _backgroundTexture,
                new Rectangle((int)-_backgroundPosition.X, 0, _backgroundTexture.Width, _backgroundTexture.Height),
                Color.White
            );

            float logoScale = 0.4f;
            spriteBatch.Draw(
                _gameLogoTexture,
                _logoPosition,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                logoScale,
                SpriteEffects.None,
                0f
            );
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 1.0f)
        {
            DrawBackground(spriteBatch);

            Viewport viewport = graphicsDevice.Viewport;
            Vector2 boxPosition = new Vector2(
                viewport.Bounds.Center.X - (boxWidth / 2),
                viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            spriteBatch.Draw(
                _woodBoxTexture,
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.White
            );

            string titleText = "Settings";
            float titleScale = 1.5f;
            Vector2 titleSize = font.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2(
                (boxWidth - titleSize.X) / 2,
                30
            );
            // Title with main font
            spriteBatch.DrawString(
                font, titleText, titlePosition, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f
            );

            Vector2 menuStartPosition = new Vector2(
                boxPosition.X + 60,
                titlePosition.Y + titleSize.Y + 60
            );

            float rowHeight = 80f;
            string[] categories = { "Graphics", "Audio", "Mouse Sensitivity", "Miscellaneous" };

            for (int i = 0; i < categories.Length; i++)
            {
                // Categories with main font
                Vector2 categoryPosition = new Vector2(menuStartPosition.X, menuStartPosition.Y + i * rowHeight);
                spriteBatch.DrawString(font, categories[i], categoryPosition, Color.Yellow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Vector2 settingPosition = new Vector2(menuStartPosition.X + 300, menuStartPosition.Y + i * rowHeight);

                // Settings and dropdown items with smallFont
                if (i == 0) // Graphics dropdown
                {
                    DrawDropdown(spriteBatch, settingPosition, isDropdownOpen, resolutions, resolutionIndex, hoveredIndex);
                }
                else if (i == 1)
                {
                    // Volume slider would be drawn with smallFont if implemented
                }
                else if (i == 2)
                {
                    // Sensitivity slider would be drawn with smallFont if implemented
                }
                else
                {
                    // Miscellaneous items (like "Back") with smallFont
                    spriteBatch.DrawString(smallFont, menuItems[i], settingPosition, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawDropdown(SpriteBatch spriteBatch, Vector2 position, bool isOpen, string[] items, int selectedIndex, int hoveredIndex)
        {
            int dropdownHeight = isOpen ? items.Length * 30 + 30 : 30;
            Rectangle dropdownRect = new Rectangle((int)position.X, (int)position.Y, 200, dropdownHeight);

            // Draw the dropdown background
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, Color.Gray), dropdownRect, Color.Gray);
            DrawOutline(spriteBatch, dropdownRect, Color.Black);

            // Draw the currently selected item at the top with smallFont
            spriteBatch.DrawString(smallFont, items[selectedIndex], new Vector2(position.X + 10, position.Y + 5), Color.White);

            if (isOpen)
            {
                // Draw the dropdown items using smallFont
                for (int i = 0; i < items.Length; i++)
                {
                    Rectangle itemRect = new Rectangle(
                        (int)position.X,
                        (int)(position.Y + 30 * (i + 1)),
                        200,
                        30
                    );

                    if (i == hoveredIndex)
                    {
                        // Highlighted item
                        spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, Color.DarkGray), itemRect, Color.DarkGray);
                        DrawOutline(spriteBatch, itemRect, Color.Black);
                        spriteBatch.DrawString(smallFont, items[i], new Vector2(itemRect.X + 10, itemRect.Y + 5), Color.White);
                    }
                    else
                    {
                        // Normal item
                        spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, Color.Gray), itemRect, Color.Gray);
                        DrawOutline(spriteBatch, itemRect, Color.Black);
                        spriteBatch.DrawString(smallFont, items[i], new Vector2(itemRect.X + 10, itemRect.Y + 5), Color.White);
                    }
                }
            }
        }

        private void DrawOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            // Top
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, color), new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
            // Left
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, color), new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
            // Right
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, color), new Rectangle(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), color);
            // Bottom
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, color), new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), color);
        }

        private Texture2D GetFilledTexture(GraphicsDevice graphicsDevice, Color color)
        {
            Texture2D texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            return texture;
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }
    }
}
