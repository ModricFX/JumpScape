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

        private Vector2 _backgroundPosition;

        private MouseState previousMouseState;
        private int frameRateIndex = 1;
        private int volume = 80;
        private int sensitivity = 5;

        private readonly string[] frameRates = { "30 FPS", "60 FPS", "120 FPS", "Unlimited" };
        string[] categories = { "Graphics", "Audio", "Miscellaneous" };

        private const int SLIDER_WIDTH = 200;
        private bool isDraggingVolume = false;
        private bool isDraggingSensitivity = false;

        private GraphicsDevice graphicsDevice; // Store this so we can use it in HandleMouseInput
        private GraphicsDeviceManager graphicsDeviceManager;
        private float boxWidth = 800;
        private float boxHeight = 800;
        private bool isFullscreen = false; // Track whether fullscreen is enabled


        public SettingsMenu(SpriteFont font, SpriteFont smallFont, GraphicsDeviceManager graphics, GraphicsDevice graphicsDevice)
        {
            this.font = font;
            this.smallFont = smallFont;
            this.graphicsDevice = graphicsDevice;
            this.graphicsDeviceManager = graphics;

            menuItems = new List<string> { frameRates[frameRateIndex], "Volume", "Back" };
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


        private void ApplyGraphicsChanges()
        {
            // Set frame rate cap based on user selection
            graphicsDeviceManager.GraphicsDevice.PresentationParameters.PresentationInterval = frameRateIndex switch
            {
                0 => PresentInterval.Two,       // 30 FPS (Assumes VSync enabled)
                1 => PresentInterval.One,       // 60 FPS (Standard VSync)
                2 => PresentInterval.Immediate, // 120 FPS
                3 => PresentInterval.Immediate, // Unlimited (No VSync)
                _ => PresentInterval.One
            };

            // Apply changes without adjusting the window size
            graphicsDeviceManager.ApplyChanges();
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
        }

        private void HandleMouseInput(MouseState currentMouseState)
        {
            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
            bool mouseClicked = (currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed);
            bool mouseDown = (currentMouseState.LeftButton == ButtonState.Pressed);

            // Match these values with Draw()
            float categoryBlockHeight = 150f; // same as in Draw()
            float scale = 1.0f;

            // Calculate boxPosition as in Draw()
            Viewport viewport = graphicsDevice.Viewport;
            Vector2 boxPosition = new Vector2(
                viewport.Bounds.Center.X - (boxWidth / 2),
                viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            string titleText = "Settings";
            float titleScale = 1.5f;
            Vector2 titleSize = font.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);
            Vector2 categoriesStart = new Vector2(
                boxPosition.X + 60,
                titlePosition.Y + titleSize.Y + 60
            );

            // Audio category is index 1
            int audioIndex = 1;
            Vector2 audioCategoryPosition = new Vector2(categoriesStart.X, categoriesStart.Y + audioIndex * categoryBlockHeight);
            Vector2 audioCatTextSize = font.MeasureString(categories[audioIndex]) * scale;
            Vector2 childStart = audioCategoryPosition + new Vector2(30, audioCatTextSize.Y + 10);
            float textScale = 0.5f;
            string volumeLabel = "Game Volume";
            Vector2 gvSize = font.MeasureString(volumeLabel) * textScale;

            int sliderWidth = 200;
            int sliderHeight = 4;
            Vector2 sliderPos = childStart + new Vector2(0, 20) + new Vector2(gvSize.X + 40, (gvSize.Y - sliderHeight) / 2);
            Rectangle sliderRect = new Rectangle((int)sliderPos.X, (int)sliderPos.Y, sliderWidth, sliderHeight);

            float volumePercent = volume / 100f;
            int knobRadius = 8;
            int knobX = (int)(sliderPos.X + volumePercent * sliderWidth);
            int knobY = (int)(sliderPos.Y + sliderHeight / 2);
            Rectangle knobRect = new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2);

            // Volume dragging logic
            if (mouseDown && !isDraggingVolume)
            {
                if (knobRect.Contains(mousePosition.ToPoint()))
                {
                    isDraggingVolume = true;
                }
            }

            if (!mouseDown)
            {
                isDraggingVolume = false;
            }

            if (isDraggingVolume)
            {
                float relativeX = MathHelper.Clamp(mousePosition.X, sliderRect.X, sliderRect.X + sliderRect.Width);
                volume = (int)(((relativeX - sliderRect.X) / sliderRect.Width) * 100f);
                volume = MathHelper.Clamp(volume, 0, 100);
            }

            // Dropdown logic for the Graphics category (index 0)
            int graphicsIndex = 0;
            Vector2 categoryPosition = new Vector2(categoriesStart.X, categoriesStart.Y + graphicsIndex * categoryBlockHeight);
            Vector2 categoryTextSize = font.MeasureString(categories[graphicsIndex]) * scale;
            Vector2 childStartGraphics = categoryPosition + new Vector2(30, categoryTextSize.Y + 10);
            Vector2 labelSize = font.MeasureString("Frame rates:") * 0.5f;
            Vector2 dropdownPosition = new Vector2(childStartGraphics.X + labelSize.X + 20, childStartGraphics.Y);

            Rectangle dropdownRect = new Rectangle(
                (int)dropdownPosition.X,
                (int)dropdownPosition.Y,
                200,
                isDropdownOpen ? frameRates.Length * 30 + 30 : 30
            );

            hoveredIndex = -1;

            if (isDropdownOpen)
            {
                bool clickedItem = false;
                for (int idx = 0; idx < frameRates.Length; idx++)
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
                            frameRateIndex = idx;
                            menuItems[0] = frameRates[frameRateIndex];
                            isDropdownOpen = false;
                            clickedItem = true;

                            ApplyGraphicsChanges();

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
                // Check if we clicked on the dropdown area to open it
                if (IsMouseOverRectangle(mousePosition, dropdownRect) && mouseClicked)
                {
                    isDropdownOpen = true;
                }
            }

            // Fullscreen checkbox logic
            Vector2 fullscreenOffset = new Vector2(0, 40);
            Vector2 fullscreenLabelPos = childStartGraphics + fullscreenOffset;
            float checkboxScale = 0.5f;
            string fullscreenLabel = "Fullscreen:";
            Vector2 fsLabelSize = font.MeasureString(fullscreenLabel) * checkboxScale;
            Vector2 fsCheckboxPos = fullscreenLabelPos + new Vector2(fsLabelSize.X + 20, (fsLabelSize.Y - 20) / 2);

            Rectangle fsCheckboxRect = new Rectangle((int)fsCheckboxPos.X, (int)fsCheckboxPos.Y, 20, 20);

            if (IsMouseOverRectangle(mousePosition, fsCheckboxRect) && mouseClicked)
            {
                isFullscreen = !isFullscreen;
                ApplyGraphicsChanges();
            }
        }
        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition, SpriteBatch spriteBatch)
        {
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

            float categoryBlockHeight = 150f;
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
                Vector2 childStart = categoryPosition + new Vector2(30, categoryTextSize.Y + 10);

                if (i == 0) // Graphics
                {
                    // Draw Frame rates line
                    spriteBatch.DrawString(font, "Frame rates:", childStart, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    Vector2 labelSize = font.MeasureString("Frame rates:") * 0.5f;
                    Vector2 dropdownPos = new Vector2(childStart.X + labelSize.X + 20, childStart.Y);

                    // Draw Fullscreen line and checkbox FIRST
                    Vector2 fullscreenOffset = new Vector2(0, 40);
                    Vector2 fullscreenLabelPos = childStart + fullscreenOffset;
                    float checkboxScale = 0.5f;
                    string fullscreenLabel = "Fullscreen:";
                    spriteBatch.DrawString(font, fullscreenLabel, fullscreenLabelPos, Color.White, 0f, Vector2.Zero, checkboxScale, SpriteEffects.None, 0f);

                    Vector2 fsLabelSize = font.MeasureString(fullscreenLabel) * checkboxScale;
                    Vector2 fsCheckboxPos = fullscreenLabelPos + new Vector2(fsLabelSize.X + 20, (fsLabelSize.Y - 20) / 2);

                    // Draw checkbox
                    Color boxColor = Color.White;
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, boxColor),
                        new Rectangle((int)fsCheckboxPos.X, (int)fsCheckboxPos.Y, 20, 20),
                        boxColor);

                    if (isFullscreen)
                    {
                        // Draw a small checkmark inside the box
                        Color checkColor = Color.Green;
                        spriteBatch.Draw(GetFilledTexture(graphicsDevice, checkColor),
                            new Rectangle((int)fsCheckboxPos.X + 4, (int)fsCheckboxPos.Y + 4, 12, 12),
                            checkColor);
                    }

                    // NOW draw the dropdown on top, so it covers the checkbox if opened
                    DrawDropdown(spriteBatch, dropdownPos, isDropdownOpen, frameRates, frameRateIndex, hoveredIndex, scale: 0.5f);
                }
                else if (i == 1) // Audio
                {
                    // Increase vertical spacing to avoid overlap
                    Vector2 audioStart = childStart + new Vector2(0, 20);

                    float textScale = 0.5f;
                    string volumeLabel = "Game Volume";
                    spriteBatch.DrawString(font, volumeLabel, audioStart, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                    Vector2 gvSize = font.MeasureString(volumeLabel) * textScale;
                    int sliderWidth = 200;
                    int sliderHeight = 4;
                    Vector2 sliderPos = audioStart + new Vector2(gvSize.X + 40, (gvSize.Y - sliderHeight) / 2);

                    // Draw slider line
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.Gray),
                        new Rectangle((int)sliderPos.X, (int)sliderPos.Y, sliderWidth, sliderHeight),
                        Color.Gray);

                    // Calculate knob position from current volume
                    float volumePercent = volume / 100f;
                    int knobRadius = 8;
                    int knobX = (int)(sliderPos.X + volumePercent * sliderWidth);
                    int knobY = (int)(sliderPos.Y + sliderHeight / 2);

                    // Draw knob (rectangle as placeholder)
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.White),
                        new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2),
                        Color.White);

                    // Draw volume percentage to the right of the slider
                    string volText = volume.ToString() + "%";
                    Vector2 volTextSize = font.MeasureString(volText) * 0.5f;
                    Vector2 volTextPos = new Vector2(sliderPos.X + sliderWidth + 20, sliderPos.Y + (sliderHeight / 2) - (volTextSize.Y / 2));
                    spriteBatch.DrawString(font, volText, volTextPos + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, volText, volTextPos, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                }

            }

            // Draw "Back" button at the bottom
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


        private void DrawDropdown(SpriteBatch spriteBatch, Vector2 position, bool isOpen, string[] items, int selectedIndex, int hoveredIndex, float scale = 0.5f)
        {
            int dropdownHeight = isOpen ? items.Length * 30 + 30 : 30;
            Rectangle dropdownRect = new Rectangle((int)position.X, (int)position.Y, 200, dropdownHeight);

            // Background
            spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, Color.Black), dropdownRect, Color.Black * 0.7f);
            DrawOutline(spriteBatch, dropdownRect, Color.White);

            // Selected item
            Vector2 selectedOffset = new Vector2(10, 5);
            spriteBatch.DrawString(font, items[selectedIndex], position + selectedOffset + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, items[selectedIndex], position + selectedOffset, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

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

                    Color bgColor = (i == hoveredIndex) ? Color.DarkGray : Color.Gray;
                    spriteBatch.Draw(GetFilledTexture(spriteBatch.GraphicsDevice, bgColor), itemRect, bgColor);
                    DrawOutline(spriteBatch, itemRect, Color.White);

                    Vector2 itemOffset = new Vector2(10, 5);
                    spriteBatch.DrawString(font, items[i], new Vector2(itemRect.X, itemRect.Y) + itemOffset + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, items[i], new Vector2(itemRect.X, itemRect.Y) + itemOffset, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
