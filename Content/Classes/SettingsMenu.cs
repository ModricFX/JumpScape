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
        string[] categories = { "Graphics", "Audio", "Miscellaneous" };

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

            menuItems = new List<string>() { resolutions[resolutionIndex], "Volume", "Back" };
            selectedIndex = 0;
            previousMouseState = Mouse.GetState();

            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _woodBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "woodBackground.png"));

        }

        public void ResetPreviousState()
        {
            previousMouseState = Mouse.GetState();
        }

        private int hoveredIndex = -1; // Track which dropdown item is hovered

        private void HandleMouseInput(MouseState currentMouseState)
        {
            // Use the actual mouse coordinates as is (no virtual scaling)
            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);

            bool mouseClicked = (currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed);

            // Calculate the exact position of the resolution dropdown as in Draw():
            // For the "Graphics" category (i = 0), we have:
            // categoriesStart, categoryBlockHeight, and category text size used in Draw().
            // Recompute those here to ensure coordinates match:

            float categoryBlockHeight = 100f;
            float scale = 1.0f;
            Vector2 boxPosition = new Vector2(
                graphicsDevice.Viewport.Bounds.Center.X - (boxWidth / 2),
                graphicsDevice.Viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            string titleText = "Settings";
            float titleScale = 1.5f;
            Vector2 titleSize = font.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);

            Vector2 categoriesStart = new Vector2(
                boxPosition.X + 60,
                titlePosition.Y + titleSize.Y + 60
            );

            // For "Graphics" category (index 0):
            int i = 0;
            Vector2 categoryPosition = new Vector2(categoriesStart.X, categoriesStart.Y + i * categoryBlockHeight);
            Vector2 categoryTextSize = font.MeasureString(categories[i]) * scale;
            Vector2 childStart = categoryPosition + new Vector2(30, categoryTextSize.Y + 20);

            // "Resolution:" label and dropdown:
            Vector2 labelSize = smallFont.MeasureString("Resolution:");
            Vector2 dropdownPosition = new Vector2(childStart.X + labelSize.X + 20, childStart.Y - 2);
            // Shifted up by 2 pixels to align visually

            Rectangle dropdownRect = new Rectangle(
                (int)dropdownPosition.X,
                (int)dropdownPosition.Y,
                200,
                isDropdownOpen ? resolutions.Length * 30 + 30 : 30
            );

            hoveredIndex = -1;

            if (isDropdownOpen)
            {
                bool clickedItem = false;
                for (int idx = 0; idx < resolutions.Length; idx++)
                {
                    Rectangle itemRect = new Rectangle(
                        (int)dropdownPosition.X,
                        (int)(dropdownPosition.Y + 30 * (idx + 1)),
                        200,
                        30
                    );

                    if (IsMouseOverRectangle(mousePosition, itemRect))
                    {
                        hoveredIndex = idx;
                        if (mouseClicked)
                        {
                            resolutionIndex = idx;
                            menuItems[0] = resolutions[resolutionIndex];
                            isDropdownOpen = false;
                            clickedItem = true;
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
                // Open the dropdown if the mouse clicked on it when closed
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
                new Vector2(_gameLogoTexture.Width / 2, 0),
                logoScale,
                SpriteEffects.None,
                0f
            );
        }


        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition, SpriteBatch spriteBatch)
        {
            _logoPosition = new Vector2(
                (graphicsDevice.Viewport.Width / 2) - _gameLogoTexture.Width / 2 * 0.4f,
                graphicsDevice.Viewport.Height * 0.01f
            );
            _backgroundPosition = backgroundPosition;

            MouseState currentMouseState = Mouse.GetState();
            HandleMouseInput(currentMouseState);

            int backIndex = menuItems.Count - 1;

            // Recalculate the "Back" button's position exactly as in Draw()
            Viewport viewport = graphicsDevice.Viewport;
            Vector2 boxPosition = new Vector2(
                viewport.Bounds.Center.X - (boxWidth / 2),
                viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            Vector2 backSize = font.MeasureString(menuItems[backIndex]);
            Vector2 backPosition = new Vector2(
                boxPosition.X + (boxWidth - backSize.X) / 2,
                boxPosition.Y + boxHeight - backSize.Y - 50
            );

            bool mouseClicked = currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed;
            Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
            Rectangle backRect = new Rectangle((int)backPosition.X, (int)backPosition.Y, (int)backSize.X, (int)backSize.Y);

            // Check if the back button was clicked
            if (backRect.Contains(mousePos) && mouseClicked)
            {
                previousMouseState = currentMouseState;
                return backIndex; // Return the back index to signal that "Back" was chosen
            }

            previousMouseState = currentMouseState;
            return -1;
        }
        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 1.0f)
        {
            DrawBackground(spriteBatch);

            Viewport viewport = graphicsDevice.Viewport;
            Vector2 boxPosition = new Vector2(
                viewport.Bounds.Center.X - (boxWidth / 2),
                viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            // Wooden box with overlay
            spriteBatch.Draw(
                _woodBoxTexture,
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.White
            );
            spriteBatch.Draw(
                GetFilledTexture(graphicsDevice, Color.Black),
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.Black * 0.4f
            );

            string titleText = "Settings";
            float titleScale = 1.5f;
            Vector2 titleSize = font.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);

            // Title with shadow
            spriteBatch.DrawString(font, titleText, titlePosition + new Vector2(2, 2), Color.Black * 0.5f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, titleText, titlePosition, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            float categoryBlockHeight = 100f;
            Vector2 categoriesStart = new Vector2(
                boxPosition.X + 60,
                titlePosition.Y + titleSize.Y + 60
            );

            int totalRows = categories.Length;
            int totalPanelHeight = (int)(totalRows * categoryBlockHeight + 40);
            Rectangle categoriesPanel = new Rectangle(
                (int)categoriesStart.X - 20,
                (int)categoriesStart.Y - 20,
                (int)(boxWidth - (60 * 2) + 40),
                totalPanelHeight
            );
            spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.Black), categoriesPanel, Color.Black * 0.3f);
            DrawOutline(spriteBatch, categoriesPanel, Color.White);

            for (int i = 0; i < categories.Length; i++)
            {
                Vector2 categoryPosition = new Vector2(categoriesStart.X, categoriesStart.Y + i * categoryBlockHeight);
                spriteBatch.DrawString(font, categories[i], categoryPosition + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, categories[i], categoryPosition, Color.Yellow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Vector2 categoryTextSize = font.MeasureString(categories[i]) * scale;
                Vector2 childStart = categoryPosition + new Vector2(30, categoryTextSize.Y + 20);

                if (i == 0) // Graphics
                {
                    // Adjust dropdown Y by -10 for better alignment
                    spriteBatch.DrawString(smallFont, "Resolution:", childStart, Color.White);
                    Vector2 labelSize = smallFont.MeasureString("Resolution:");
                    Vector2 dropdownPos = new Vector2(childStart.X + labelSize.X + 20, childStart.Y - 10);
                    DrawDropdown(spriteBatch, dropdownPos, isDropdownOpen, resolutions, resolutionIndex, hoveredIndex);
                }
                else if (i == 1) // Audio
                {
                    spriteBatch.DrawString(smallFont, "Volume: [ " + volume + " ]", childStart, Color.White);
                }
                // Remove the "Back" drawing from here to place it at the bottom
            }

            // Draw "Back" button at the bottom, similar to how it's done in LevelSelectorMenu
            int backIndex = menuItems.Count - 1;
            Vector2 backSize = font.MeasureString(menuItems[backIndex]);

            Vector2 backPosition = new Vector2(
                boxPosition.X + (boxWidth - backSize.X) / 2,
                boxPosition.Y + boxHeight - backSize.Y - 50
            );

            Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Rectangle backRect = new Rectangle((int)backPosition.X, (int)backPosition.Y, (int)backSize.X, (int)backSize.Y);
            bool hoveringBack = backRect.Contains(mousePos);

            spriteBatch.DrawString(font, menuItems[backIndex], backPosition + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, menuItems[backIndex], backPosition, hoveringBack ? Color.Yellow : Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }



        private void DrawDropdown(SpriteBatch spriteBatch, Vector2 position, bool isOpen, string[] items, int selectedIndex, int hoveredIndex)
        {
            int dropdownHeight = isOpen ? items.Length * 30 + 30 : 30;
            Rectangle dropdownRect = new Rectangle((int)position.X, (int)position.Y, 200, dropdownHeight);

            // Draw the dropdown background a bit darker
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, Color.Black), dropdownRect, Color.Black * 0.7f);
            DrawOutline(spriteBatch, dropdownRect, Color.White);

            // Draw the currently selected item at the top with smallFont and a slight shadow
            spriteBatch.DrawString(smallFont, items[selectedIndex], new Vector2(position.X + 11, position.Y + 6), Color.Black * 0.5f);
            spriteBatch.DrawString(smallFont, items[selectedIndex], new Vector2(position.X + 10, position.Y + 5), Color.White);

            if (isOpen)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    Rectangle itemRect = new Rectangle(
                        (int)position.X,
                        (int)(position.Y + 30 * (i + 1)),
                        200,
                        30
                    );

                    // Different shades for hover and normal
                    Color bgColor = (i == hoveredIndex) ? Color.DarkGray : Color.Gray;

                    spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, bgColor), itemRect, bgColor);
                    DrawOutline(spriteBatch, itemRect, Color.White);

                    // Slight shadow on text
                    spriteBatch.DrawString(smallFont, items[i], new Vector2(itemRect.X + 11, itemRect.Y + 6), Color.Black * 0.5f);
                    spriteBatch.DrawString(smallFont, items[i], new Vector2(itemRect.X + 10, itemRect.Y + 5), Color.White);
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
