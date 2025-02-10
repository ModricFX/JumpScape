using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace JumpScape
{
    public class LevelSelectorMenu
    {
        private SpriteFont font;
        private SpriteFont bigFont;
        public List<string> menuItems;
        public int selectedIndex;
        public bool visible;

        private Texture2D _backgroundTexture;
        private Texture2D _gameLogoTexture;
        private Texture2D _buttonTexture;
        private Texture2D _woodBoxTexture;
        private Texture2D _levelBoxTexture;
        private Texture2D _starTexture;

        private bool _waitForMouseRelease = true;


        private Vector2 _logoPosition;
        private Vector2 _backgroundPosition;

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;
        private float backgroundScale;

        // We’ll store the dynamically calculated logo scale here
        private float _dynamicLogoScale;

        public LevelSelectorMenu(SpriteFont font, SpriteFont bigFont, GraphicsDevice graphicsDevice, int width, int height)
        {
            this.font = font;
            this.bigFont = bigFont;
            menuItems = new List<string>();
            selectedIndex = 0;
            previousKeyboardState = Keyboard.GetState();

            // Load textures
            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _buttonTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "buttonBackground.png"));
            _woodBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "woodBackground.png"));
            _levelBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "levelBox.png"));
            _starTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "star.png"));
            _waitForMouseRelease = true;
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        /// <summary>
        /// Calculates how large the background should be to fill the screen.
        /// </summary>
        private void CalculateBackgroundScale(int screenWidth, int screenHeight)
        {
            float scaleX = (float)screenWidth / _backgroundTexture.Width * 1.1f;
            float scaleY = (float)screenHeight / _backgroundTexture.Height * 1.1f;
            backgroundScale = Math.Max(scaleX, scaleY);
        }

        /// <summary>
        /// Calculates the logo’s position and scale so it remains proportional at different resolutions.
        /// </summary>
        private void CalculateLogoScaleAndPosition(GraphicsDevice graphicsDevice)
        {
            // For example, let’s make the logo width about 20% of the screen width:
            float desiredLogoWidth = graphicsDevice.Viewport.Width * 0.2f;
            // The scale is how much we must multiply the base logo texture width to get that desired width
            _dynamicLogoScale = desiredLogoWidth / _gameLogoTexture.Width * 0.5f;

            // Position the logo near the top-center. 
            // The height offset (0.01f of viewport) is arbitrary, adjust as you see fit.
            _logoPosition = new Vector2(
                (graphicsDevice.Viewport.Width - (_gameLogoTexture.Width * _dynamicLogoScale)) / 2f,
                graphicsDevice.Viewport.Height * 0.01f
            );
        }

        /// <summary>
        /// Update the parallax/scrolling background position for smooth movement.
        /// </summary>
        private void UpdateBackgroundPosition(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Instead of a fixed 0.3f * elapsed * 100, divide by backgroundScale 
            // so the background speed remains consistent even if the image is huge
            float effectiveSpeedX = 0.3f * elapsed * 100 / backgroundScale;
            float effectiveSpeedY = 0.1f * elapsed * 100 / backgroundScale;

            _backgroundPosition.X += effectiveSpeedX;
            _backgroundPosition.Y += effectiveSpeedY;

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

        public void ResetPreviousState()
        {
            previousKeyboardState = Keyboard.GetState();
        }

        /// <summary>
        /// Handle input and return an integer to indicate if an item was selected.
        /// </summary>
        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition)
        {
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            _backgroundPosition = backgroundPosition;
            UpdateBackgroundPosition(gameTime);

            CalculateLogoScaleAndPosition(graphicsDevice);

            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            // 4. Keyboard navigation
            if (IsKeyPressed(currentKeyboardState, previousKeyboardState, Keys.Up))
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = menuItems.Count - 1;
            }
            else if (IsKeyPressed(currentKeyboardState, previousKeyboardState, Keys.Down))
            {
                selectedIndex++;
                if (selectedIndex >= menuItems.Count) selectedIndex = 0;
            }
            else if (IsKeyPressed(currentKeyboardState, previousKeyboardState, Keys.Enter))
            {
                previousKeyboardState = currentKeyboardState;
                previousMouseState = currentMouseState;
                return selectedIndex; // Return the item selected by pressing Enter
            }

            float boxWidth = graphicsDevice.Viewport.Width * 0.5f;  // 50% of screen
            float boxHeight = graphicsDevice.Viewport.Height * 0.5f; // 50% of screen

            float levelBoxScale = 0.2f;
            float horizontalSpacing = 90f;
            float verticalSpacing = 20f;

            // We'll use this for collision checks
            Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);

            if (_waitForMouseRelease)
            {
                // If the mouse is currently not pressed, we're good—stop waiting
                if (currentMouseState.LeftButton == ButtonState.Released)
                {
                    _waitForMouseRelease = false;
                }
            }
            else
            {
                // Normal mouse-based selection logic
                // Level boxes (except the last "Back" item)
                for (int i = 0; i < menuItems.Count - 1; i++)
                {
                    Rectangle levelBoxBounds = GetLevelBoxBounds(
                        i,
                        graphicsDevice,
                        boxWidth,
                        boxHeight,
                        levelBoxScale,
                        horizontalSpacing,
                        verticalSpacing
                    );

                    if (levelBoxBounds.Contains(mousePos))
                    {
                        selectedIndex = i;
                        if (currentMouseState.LeftButton == ButtonState.Pressed &&
                            previousMouseState.LeftButton == ButtonState.Released)
                        {
                            // Player actually clicked on level i
                            previousKeyboardState = currentKeyboardState;
                            previousMouseState = currentMouseState;
                            return selectedIndex;
                        }
                    }
                }

                // Handle the "Back" button (the last item in the list)
                int backIndex = menuItems.Count - 1;
                Rectangle backBounds = GetBackButtonBounds(graphicsDevice, boxWidth, boxHeight, 1.0f);

                // Check if we hover over back and click
                if (backBounds.Contains(mousePos))
                {
                    selectedIndex = backIndex; // highlight the back item
                    if (currentMouseState.LeftButton == ButtonState.Pressed &&
                        previousMouseState.LeftButton == ButtonState.Released)
                    {
                        previousKeyboardState = currentKeyboardState;
                        previousMouseState = currentMouseState;
                        return selectedIndex;  // The "Back" item
                    }
                }
            }

            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;

            // 8. Return -1 if no selection occurred
            return -1;
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        /// <summary>
        /// Center position of the big "wood box" on screen.
        /// </summary>
        private Vector2 GetBasePosition(GraphicsDevice graphicsDevice, float boxWidth, float boxHeight)
        {
            return new Vector2(
                graphicsDevice.Viewport.Bounds.Center.X - (boxWidth / 2),
                graphicsDevice.Viewport.Bounds.Center.Y - (boxHeight / 2)
            );
        }

        /// <summary>
        /// The bounding rectangle of each smaller level box (the clickable area).
        /// </summary>
        private Rectangle GetLevelBoxBounds(
            int index,
            GraphicsDevice graphicsDevice,
            float boxWidth,
            float boxHeight,
            float levelBoxScale,
            float horizontalSpacing,
            float verticalSpacing)
        {
            // Suppose we want 3 columns of levels
            int columns = 3;
            float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
            float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;

            Vector2 boxPosition = GetBasePosition(graphicsDevice, boxWidth, boxHeight);

            // Title text
            string titleText = "Level Selector";
            float titleScale = 1.5f;
            Vector2 titleSize = bigFont.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);

            Vector2 levelStartPosition = new Vector2(
                boxPosition.X + 50f,
                titlePosition.Y + titleSize.Y + 40f
            );

            int row = index / columns;
            int col = index % columns;

            Vector2 levelBoxPosition = new Vector2(
                levelStartPosition.X + col * (levelBoxWidth + horizontalSpacing),
                levelStartPosition.Y + row * (levelBoxHeight + verticalSpacing)
            );

            return new Rectangle(
                (int)levelBoxPosition.X,
                (int)levelBoxPosition.Y,
                (int)levelBoxWidth,
                (int)levelBoxHeight
            );
        }

        /// <summary>
        /// The bounding rectangle of the "Back" button.
        /// </summary>
        private Rectangle GetBackButtonBounds(GraphicsDevice graphicsDevice, float boxWidth, float boxHeight, float backScale)
        {
            Vector2 boxPosition = GetBasePosition(graphicsDevice, boxWidth, boxHeight);

            Vector2 backTextSize = bigFont.MeasureString(menuItems[menuItems.Count - 1]) * backScale;

            Vector2 backPosition = new Vector2(
                boxPosition.X + (boxWidth - backTextSize.X) / 2,
                boxPosition.Y + boxHeight - backTextSize.Y - 50
            );

            return new Rectangle(
                (int)backPosition.X,
                (int)backPosition.Y,
                (int)backTextSize.X,
                (int)backTextSize.Y
            );
        }

        /// <summary>
        /// Draws the moving background and the (now dynamically scaled) logo.
        /// </summary>
        private void DrawBackground(SpriteBatch spriteBatch)
        {
            // Draw the first copy of the background
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

            // Draw the second copy for wrapping
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

            // Draw the logo with the dynamic scale
            spriteBatch.Draw(
                _gameLogoTexture,
                _logoPosition,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                _dynamicLogoScale,
                SpriteEffects.None,
                0f
            );
        }

        public string GetSelectedItem()
        {
            if (selectedIndex >= 0 && selectedIndex < menuItems.Count)
                return menuItems[selectedIndex];
            return null;
        }

        /// <summary>
        /// Draw everything for the Level Selector: background, main box, level boxes, back button, etc.
        /// </summary>
        internal void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 3.0f)
        {
            // Draw the parallax background and logo first
            DrawBackground(spriteBatch);

            // Calculate the size of the main box
            float boxWidth = graphicsDevice.Viewport.Width * 0.5f;   // 50% of screen width
            float boxHeight = graphicsDevice.Viewport.Height * 0.5f; // 50% of screen height

            Vector2 boxPosition = GetBasePosition(graphicsDevice, boxWidth, boxHeight);

            // Draw the wooden background (main box)
            spriteBatch.Draw(
                _woodBoxTexture,
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.White
            );

            // Draw the title text
            string titleText = "Level Selector";
            float titleScale = 1.5f;
            Vector2 titleSize = bigFont.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);
            spriteBatch.DrawString(
                bigFont,
                titleText,
                titlePosition,
                Color.White,
                0f,
                Vector2.Zero,
                titleScale,
                SpriteEffects.None,
                0f
            );

            // Level box parameters
            float levelBoxScale = 0.2f;
            float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
            float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;
            float horizontalSpacing = 90f;
            float verticalSpacing = 20f;

            // Draw each level box (except the last item, which is "Back")
            for (int i = 0; i < menuItems.Count - 1; i++)
            {
                bool isSelected = (i == selectedIndex);
                bool isHovering = GetLevelBoxBounds(
                    i,
                    graphicsDevice,
                    boxWidth,
                    boxHeight,
                    levelBoxScale,
                    horizontalSpacing,
                    verticalSpacing
                ).Contains(Mouse.GetState().Position);

                Color textColor = (isSelected || isHovering) ? Color.Yellow : Color.White;

                Rectangle levelRect = GetLevelBoxBounds(
                    i,
                    graphicsDevice,
                    boxWidth,
                    boxHeight,
                    levelBoxScale,
                    horizontalSpacing,
                    verticalSpacing
                );

                // Draw the small level box
                spriteBatch.Draw(
                    _levelBoxTexture,
                    new Vector2(levelRect.X, levelRect.Y),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    levelBoxScale,
                    SpriteEffects.None,
                    0f
                );

                // Number in the center of the small box
                string levelNumber = (i + 1).ToString();
                Vector2 textSize = bigFont.MeasureString(levelNumber);
                Vector2 textPosition = new Vector2(
                    levelRect.X + (levelRect.Width - textSize.X) / 2,
                    levelRect.Y + (levelRect.Height - textSize.Y) / 2
                );
                spriteBatch.DrawString(
                    bigFont,
                    levelNumber,
                    textPosition,
                    textColor,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0f
                );

                // Example star logic if you need it (commented out):
                /*
                float starScale = 0.02f;
                float starYOffset = 25f;
                Vector2 topStarPosition = new Vector2(
                    levelRect.X + levelRect.Width / 2 - (_starTexture.Width * starScale) / 2,
                    levelRect.Y + levelRect.Height - 20f - (_starTexture.Height * starScale) + starYOffset
                );
                Vector2 leftStarPosition = new Vector2(
                    levelRect.X + levelRect.Width / 2 - (_starTexture.Width * starScale) - 10f,
                    levelRect.Y + levelRect.Height - (_starTexture.Height * starScale) - 10f + starYOffset
                );
                Vector2 rightStarPosition = new Vector2(
                    levelRect.X + levelRect.Width / 2 + 10f,
                    levelRect.Y + levelRect.Height - (_starTexture.Height * starScale) - 10f + starYOffset
                );
                spriteBatch.Draw(_starTexture, topStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(_starTexture, leftStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(_starTexture, rightStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
                */
            }

            // "Back" button (last item in menu)
            int backIndex = menuItems.Count - 1;
            bool backSelected = (backIndex == selectedIndex);
            bool backHover = GetBackButtonBounds(
                graphicsDevice,
                boxWidth,
                boxHeight,
                backSelected ? 1.1f : 1.0f
            ).Contains(Mouse.GetState().Position);

            Color backColor = (backSelected || backHover) ? Color.Yellow : Color.White;
            float backScale = backSelected || backHover ? 1.1f : 1.0f;

            Rectangle backBounds = GetBackButtonBounds(graphicsDevice, boxWidth, boxHeight, backScale);
            spriteBatch.DrawString(
                bigFont,
                menuItems[backIndex],
                new Vector2(backBounds.X, backBounds.Y),
                backColor,
                0f,
                Vector2.Zero,
                backScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
