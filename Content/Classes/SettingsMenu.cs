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

        // Store these so we can modify them in sliders and checkboxes
        private int frameRateIndex = 1;
        private int volume = 80;
        private int musicVolume = 80;
        private bool isMuted = false;
        private bool isFullscreen = false; 

        private bool isDraggingVolume = false;
        private bool isDraggingMusicVolume = false;

        private readonly string[] frameRates = { "30 FPS", "60 FPS", "120 FPS", "Unlimited" };
        string[] categories = { "Graphics", "Audio"};

        private GraphicsDevice graphicsDevice; 
        private GraphicsDeviceManager graphicsDeviceManager;
        
        private float boxWidth = 800;
        private float boxHeight = 800;
        private float backgroundScale;

        private GameSettings settings;

        public SettingsMenu(SpriteFont font, SpriteFont smallFont, GraphicsDeviceManager graphics, GraphicsDevice graphicsDevice)
        {
            this.font = font;
            this.smallFont = smallFont;
            this.graphicsDevice = graphicsDevice;
            this.graphicsDeviceManager = graphics;

            // Load settings
            settings = GameSettings.Load();

            // Initialize fields from loaded settings
            frameRateIndex = settings.FrameRateIndex;
            volume = settings.Volume;
            musicVolume = settings.MusicVolume;
            isMuted = settings.IsMuted;
            isFullscreen = settings.IsFullscreen;

            // Apply fullscreen and frame rate settings
            ApplyGraphicsChanges();

            menuItems = new List<string> { frameRates[frameRateIndex], "Volume", "Back" };
            previousMouseState = Mouse.GetState();

            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _woodBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "woodBackground.png"));
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        }

        public void ResetPreviousState()
        {
            previousMouseState = Mouse.GetState();
        }

        private int hoveredIndex = -1; 

        private void ApplyGraphicsChanges()
        {
            // Restore the frame rate code. 
            // For actual 30, 60, 120 FPS, etc., you may need to manually set the targetElapsedTime 
            // or refresh logic in your Game1.cs. 
            // Using PresentInterval is one approach:
            graphicsDeviceManager.GraphicsDevice.PresentationParameters.PresentationInterval = frameRateIndex switch
            {
                0 => PresentInterval.Two,       // ~30 FPS (actually half the monitor refresh if 60Hz)
                1 => PresentInterval.One,       // 60 FPS
                2 => PresentInterval.Immediate, // 120 FPS (best-effort)
                3 => PresentInterval.Immediate, // Unlimited
                _ => PresentInterval.One
            };

            graphicsDeviceManager.IsFullScreen = isFullscreen;
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
                new Rectangle(
                    (int)-_backgroundPosition.X,
                    (int)-_backgroundPosition.Y,
                    (int)(_backgroundTexture.Width * backgroundScale),
                    (int)(_backgroundTexture.Height * backgroundScale)
                ),
                Color.White
            );

            // Draw a second copy of the background for seamless wrapping
            spriteBatch.Draw(
                _backgroundTexture,
                new Rectangle(
                    (int)-_backgroundPosition.X + (int)(_backgroundTexture.Width * backgroundScale),
                    (int)-_backgroundPosition.Y,
                    (int)(_backgroundTexture.Width * backgroundScale),
                    (int)(_backgroundTexture.Height * backgroundScale)
                ),
                Color.White
            );
        }
        
        private void CalculateBackgroundScale(int screenWidth, int screenHeight)
        {
            // Determine the scaling factor for the background to fit the screen
            float scaleX = (float)screenWidth / _backgroundTexture.Width * 1.1f;
            float scaleY = (float)screenHeight / _backgroundTexture.Height * 1.1f;

            // Use the larger scale to ensure the background covers the entire screen
            backgroundScale = Math.Max(scaleX, scaleY);
        }

        private void UpdateBackgroundPosition(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _backgroundPosition.X += 0.3f * elapsed * 100; 
            _backgroundPosition.Y += 0.1f * elapsed * 100;

            float scaledWidth = _backgroundTexture.Width * backgroundScale;
            float scaledHeight = _backgroundTexture.Height * backgroundScale;

            // Wrap horizontally
            if (_backgroundPosition.X >= scaledWidth)
                _backgroundPosition.X -= scaledWidth;
            else if (_backgroundPosition.X <= -scaledWidth)
                _backgroundPosition.X += scaledWidth;

            // Wrap vertically
            if (_backgroundPosition.Y >= scaledHeight)
                _backgroundPosition.Y -= scaledHeight;
            else if (_backgroundPosition.Y <= -scaledHeight)
                _backgroundPosition.Y += scaledHeight;
        }

        private void HandleMouseInput(MouseState currentMouseState)
        {
            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
            bool mouseClicked = (currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed);
            bool mouseDown = (currentMouseState.LeftButton == ButtonState.Pressed);

            float categoryBlockHeight = 150f; 
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

            // AUDIO CATEGORY (Index = 1)
            int audioIndex = 1;
            Vector2 audioCategoryPosition = new Vector2(categoriesStart.X, categoriesStart.Y + audioIndex * categoryBlockHeight);
            Vector2 audioCatTextSize = font.MeasureString(categories[audioIndex]) * scale;
            Vector2 audioChildStart = audioCategoryPosition + new Vector2(30, audioCatTextSize.Y + 10);

            // We'll lay out: 
            //   [Game Volume slider]
            //   [Music Volume slider]
            //   [Mute checkbox]

            // 1) GAME VOLUME
            float textScale = 0.5f;
            string volumeLabel = "Game Volume";
            Vector2 volumeLabelSize = font.MeasureString(volumeLabel) * textScale;
            Vector2 volumeLabelPos = audioChildStart;

            int sliderWidth = 200;
            int sliderHeight = 4;

            // Position the slider to the right of the label
            Vector2 gameVolumeSliderPos = volumeLabelPos + new Vector2(volumeLabelSize.X + 40, (volumeLabelSize.Y - sliderHeight) / 2);

            Rectangle gameVolumeSliderRect = new Rectangle((int)gameVolumeSliderPos.X, (int)gameVolumeSliderPos.Y, sliderWidth, sliderHeight);
            float volumePercent = volume / 100f;
            int knobRadius = 8;
            int knobX = (int)(gameVolumeSliderPos.X + volumePercent * sliderWidth);
            int knobY = (int)(gameVolumeSliderPos.Y + sliderHeight / 2);
            Rectangle knobRect = new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2);

            // Volume dragging
            if (mouseDown && !isDraggingVolume)
            {
                if (knobRect.Contains(mousePosition.ToPoint()))
                {
                    isDraggingVolume = true;
                }
            }
            if (!mouseDown) isDraggingVolume = false;
            if (isDraggingVolume)
            {
                float relativeX = MathHelper.Clamp(mousePosition.X, gameVolumeSliderRect.X, gameVolumeSliderRect.X + gameVolumeSliderRect.Width);
                volume = (int)(((relativeX - gameVolumeSliderRect.X) / gameVolumeSliderRect.Width) * 100f);
                volume = MathHelper.Clamp(volume, 0, 100);

                settings.Volume = volume;
                settings.Save();
            }

            // 2) MUSIC VOLUME
            // Offset the next row by ~40 pixels from the top of the game volume slider
            Vector2 musicVolumeOffset = new Vector2(0, 40);
            Vector2 musicLabelPos = volumeLabelPos + musicVolumeOffset;
            string musicLabel = "Music Volume";
            Vector2 musicLabelSize = font.MeasureString(musicLabel) * textScale;
            Vector2 musicVolumeSliderPos = musicLabelPos + new Vector2(musicLabelSize.X + 40, (musicLabelSize.Y - sliderHeight) / 2);

            Rectangle musicVolumeSliderRect = new Rectangle((int)musicVolumeSliderPos.X, (int)musicVolumeSliderPos.Y, sliderWidth, sliderHeight);
            float musicVolumePercent = musicVolume / 100f;
            knobX = (int)(musicVolumeSliderPos.X + musicVolumePercent * sliderWidth);
            knobY = (int)(musicVolumeSliderPos.Y + sliderHeight / 2);
            Rectangle musicKnobRect = new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2);

            // Music volume dragging
            if (mouseDown && !isDraggingMusicVolume)
            {
                if (musicKnobRect.Contains(mousePosition.ToPoint()))
                {
                    isDraggingMusicVolume = true;
                }
            }
            if (!mouseDown) isDraggingMusicVolume = false;
            if (isDraggingMusicVolume)
            {
                float relativeX = MathHelper.Clamp(mousePosition.X, musicVolumeSliderRect.X, musicVolumeSliderRect.X + musicVolumeSliderRect.Width);
                musicVolume = (int)(((relativeX - musicVolumeSliderRect.X) / musicVolumeSliderRect.Width) * 100f);
                musicVolume = MathHelper.Clamp(musicVolume, 0, 100);

                settings.MusicVolume = musicVolume;
                settings.Save();
            }

            // 3) MUTE CHECKBOX
            Vector2 muteOffset = new Vector2(0, 80);
            Vector2 muteLabelPos = volumeLabelPos + muteOffset;
            float checkboxScale = 0.5f;
            string muteLabel = "Mute:";
            Vector2 muteLabelSize = font.MeasureString(muteLabel) * checkboxScale;
            Vector2 muteCheckboxPos = muteLabelPos + new Vector2(muteLabelSize.X + 20, (muteLabelSize.Y - 20) / 2);

            Rectangle muteCheckboxRect = new Rectangle((int)muteCheckboxPos.X, (int)muteCheckboxPos.Y, 20, 20);
            if (IsMouseOverRectangle(mousePosition, muteCheckboxRect) && mouseClicked)
            {
                isMuted = !isMuted;
                settings.IsMuted = isMuted;
                settings.Save();
            }

            // GRAPHICS CATEGORY (Index = 0) - handle dropdown & fullscreen
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

                            settings.FrameRateIndex = frameRateIndex;
                            settings.Save();

                            ApplyGraphicsChanges();
                            break;
                        }
                    }
                }

                // Close dropdown if clicked outside
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

            // Fullscreen checkbox
            Vector2 fullscreenOffset = new Vector2(0, 40);
            Vector2 fullscreenLabelPos = childStartGraphics + fullscreenOffset;
            checkboxScale = 0.5f;
            string fullscreenLabel = "Fullscreen:";
            Vector2 fsLabelSize = font.MeasureString(fullscreenLabel) * checkboxScale;
            Vector2 fsCheckboxPos = fullscreenLabelPos + new Vector2(fsLabelSize.X + 20, (fsLabelSize.Y - 20) / 2);

            Rectangle fsCheckboxRect = new Rectangle((int)fsCheckboxPos.X, (int)fsCheckboxPos.Y, 20, 20);
            if (IsMouseOverRectangle(mousePosition, fsCheckboxRect) && mouseClicked)
            {
                isFullscreen = !isFullscreen;
                settings.IsFullscreen = isFullscreen;
                settings.Save();
                ApplyGraphicsChanges();
            }
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition, SpriteBatch spriteBatch)
        {
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            _backgroundPosition = backgroundPosition;
            UpdateBackgroundPosition(gameTime);

            MouseState currentMouseState = Mouse.GetState();
            HandleMouseInput(currentMouseState);

            int backIndex = menuItems.Count - 1;

            // Calculate the position of the "Back" text for collision detection
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
                return backIndex; // "Back" was chosen
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

            // Wooden box
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
                    // Frame rates
                    spriteBatch.DrawString(font, "Frame rates:", childStart, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    Vector2 labelSize = font.MeasureString("Frame rates:") * 0.5f;
                    Vector2 dropdownPos = new Vector2(childStart.X + labelSize.X + 20, childStart.Y);

                    // Fullscreen
                    Vector2 fullscreenOffset = new Vector2(0, 40);
                    Vector2 fullscreenLabelPos = childStart + fullscreenOffset;
                    float checkboxScale = 0.5f;
                    string fullscreenLabel = "Fullscreen:";
                    spriteBatch.DrawString(font, fullscreenLabel, fullscreenLabelPos, Color.White, 0f, Vector2.Zero, checkboxScale, SpriteEffects.None, 0f);

                    Vector2 fsLabelSize = font.MeasureString(fullscreenLabel) * checkboxScale;
                    Vector2 fsCheckboxPos = fullscreenLabelPos + new Vector2(fsLabelSize.X + 20, (fsLabelSize.Y - 20) / 2);

                    // Checkbox
                    Color boxColor = Color.White;
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, boxColor),
                        new Rectangle((int)fsCheckboxPos.X, (int)fsCheckboxPos.Y, 20, 20),
                        boxColor);

                    if (isFullscreen)
                    {
                        Color checkColor = Color.Green;
                        spriteBatch.Draw(GetFilledTexture(graphicsDevice, checkColor),
                            new Rectangle((int)fsCheckboxPos.X + 4, (int)fsCheckboxPos.Y + 4, 12, 12),
                            checkColor);
                    }

                    // Draw the dropdown last so it appears on top
                    DrawDropdown(spriteBatch, dropdownPos, isDropdownOpen, frameRates, frameRateIndex, hoveredIndex, scale: 0.5f);
                }
                else if (i == 1) // Audio
                {
                    float textScale = 0.5f;

                    // 1) Game Volume
                    string volumeLabel = "Game Volume";
                    Vector2 volumeLabelPos = childStart;
                    spriteBatch.DrawString(font, volumeLabel, volumeLabelPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                    Vector2 volumeLabelSize = font.MeasureString(volumeLabel) * textScale;
                    int sliderWidth = 200;
                    int sliderHeight = 4;
                    Vector2 gameVolumeSliderPos = volumeLabelPos + new Vector2(volumeLabelSize.X + 40, (volumeLabelSize.Y - sliderHeight) / 2);

                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.Gray),
                        new Rectangle((int)gameVolumeSliderPos.X, (int)gameVolumeSliderPos.Y, sliderWidth, sliderHeight),
                        Color.Gray);

                    float volumePercent = volume / 100f;
                    int knobRadius = 8;
                    int knobX = (int)(gameVolumeSliderPos.X + volumePercent * sliderWidth);
                    int knobY = (int)(gameVolumeSliderPos.Y + sliderHeight / 2);

                    // Knob
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.White),
                        new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2),
                        Color.White);

                    // Volume text
                    string volText = volume + "%";
                    Vector2 volTextSize = font.MeasureString(volText) * 0.5f;
                    Vector2 volTextPos = new Vector2(gameVolumeSliderPos.X + sliderWidth + 20, gameVolumeSliderPos.Y + (sliderHeight / 2) - (volTextSize.Y / 2));
                    spriteBatch.DrawString(font, volText, volTextPos + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, volText, volTextPos, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                    // 2) Music Volume
                    Vector2 musicVolumeOffset = new Vector2(0, 40);
                    Vector2 musicLabelPos = volumeLabelPos + musicVolumeOffset;
                    string musicLabel = "Music Volume";
                    spriteBatch.DrawString(font, musicLabel, musicLabelPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                    Vector2 musicLabelSize = font.MeasureString(musicLabel) * textScale;
                    Vector2 musicVolumeSliderPos = musicLabelPos + new Vector2(musicLabelSize.X + 40, (musicLabelSize.Y - sliderHeight) / 2);

                    // Slider line
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.Gray),
                        new Rectangle((int)musicVolumeSliderPos.X, (int)musicVolumeSliderPos.Y, sliderWidth, sliderHeight),
                        Color.Gray);

                    float musicPercent = musicVolume / 100f;
                    knobX = (int)(musicVolumeSliderPos.X + musicPercent * sliderWidth);
                    knobY = (int)(musicVolumeSliderPos.Y + sliderHeight / 2);

                    // Knob
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.White),
                        new Rectangle(knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2),
                        Color.White);

                    // Music volume text
                    string musicVolText = musicVolume + "%";
                    Vector2 musicVolTextSize = font.MeasureString(musicVolText) * 0.5f;
                    Vector2 musicVolTextPos = new Vector2(musicVolumeSliderPos.X + sliderWidth + 20, musicVolumeSliderPos.Y + (sliderHeight / 2) - (musicVolTextSize.Y / 2));
                    spriteBatch.DrawString(font, musicVolText, musicVolTextPos + new Vector2(1, 1), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, musicVolText, musicVolTextPos, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                    // 3) Mute Checkbox
                    Vector2 muteOffset = new Vector2(0, 80);
                    Vector2 muteLabelPos = volumeLabelPos + muteOffset;
                    string muteLabel = "Mute:";
                    spriteBatch.DrawString(font, muteLabel, muteLabelPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                    Vector2 muteLabelSize = font.MeasureString(muteLabel) * textScale;
                    Vector2 muteCheckboxPos = muteLabelPos + new Vector2(muteLabelSize.X + 20, (muteLabelSize.Y - 20) / 2);

                    // Draw the checkbox
                    spriteBatch.Draw(GetFilledTexture(graphicsDevice, Color.White),
                        new Rectangle((int)muteCheckboxPos.X, (int)muteCheckboxPos.Y, 20, 20),
                        Color.White);

                    if (isMuted)
                    {
                        Color checkColor = Color.Red;
                        spriteBatch.Draw(GetFilledTexture(graphicsDevice, checkColor),
                            new Rectangle((int)muteCheckboxPos.X + 4, (int)muteCheckboxPos.Y + 4, 12, 12),
                            checkColor);
                    }
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
