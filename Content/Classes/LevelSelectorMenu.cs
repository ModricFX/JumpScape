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

        private Vector2 _logoPosition;
        private Vector2 _backgroundPosition;

        private KeyboardState previousKeyboardState;

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

            // Initialize positions and speeds
            float logoScale = 0.4f; // Scale the logo to 40% size
            _logoPosition = new Vector2(
                (width - _gameLogoTexture.Width * logoScale) / 2,
                height * 0.02f // Higher position at 2% of the screen height
            );
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        public void ResetPreviousState()
        {
            previousKeyboardState = Keyboard.GetState();
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            _backgroundPosition = backgroundPosition;

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
                return selectedIndex;
            }

            previousKeyboardState = currentKeyboardState;
            return -1; // No selection yet
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                _backgroundTexture,
                new Rectangle((int)-_backgroundPosition.X, 0, _backgroundTexture.Width, _backgroundTexture.Height),
                Color.White
            );

            // Draw the game logo scaled
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

        public string GetSelectedItem()
        {
            if (selectedIndex >= 0 && selectedIndex < menuItems.Count)
                return menuItems[selectedIndex];
            return null;
        }

        internal void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 3.0f)
        {
            DrawBackground(spriteBatch);

            // LEVEL SELECTOR LOGIC
            float boxWidth = 700;
            float boxHeight = 500;
            Vector2 boxPosition = new Vector2(
                graphicsDevice.Viewport.Bounds.Center.X - (boxWidth / 2),
                graphicsDevice.Viewport.Bounds.Center.Y - (boxHeight / 2)
            );

            // Draw Wooden Box
            spriteBatch.Draw(
                _woodBoxTexture,
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.White
            );

            // Draw Title
            string titleText = "Level Selector";
            float titleScale = 1.5f;
            Vector2 titleSize = bigFont.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2(
                (boxWidth - titleSize.X) / 2,
                30
            );
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

            // Layout for Level Boxes
            float levelBoxScale = 0.2f;
            float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
            float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;

            int columns = 3; // Number of columns for the grid layout
            float horizontalSpacing = 90f;
            float verticalSpacing = 20f;

            Vector2 levelStartPosition = new Vector2(
                boxPosition.X + 50f,
                titlePosition.Y + titleSize.Y + 40f
            );

            for (int i = 0; i < menuItems.Count - 1; i++) // Exclude the "Back" button
            {
                bool isSelected = (i == selectedIndex);
                Color textColor = isSelected ? Color.Yellow : Color.White;

                int row = i / columns;
                int col = i % columns;

                Vector2 levelBoxPosition = new Vector2(
                    levelStartPosition.X + col * (levelBoxWidth + horizontalSpacing),
                    levelStartPosition.Y + row * (levelBoxHeight + verticalSpacing)
                );

                // Draw Level Box
                spriteBatch.Draw(
                    _levelBoxTexture,
                    levelBoxPosition,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    levelBoxScale,
                    SpriteEffects.None,
                    0f
                );

                // Draw Level Number (centered inside the box)
                string levelNumber = (i + 1).ToString();
                Vector2 textSize = bigFont.MeasureString(levelNumber) * 1f;
                Vector2 textPosition = new Vector2(
                    levelBoxPosition.X + (levelBoxWidth - textSize.X) / 2,
                    levelBoxPosition.Y + (levelBoxHeight - textSize.Y) / 2
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

                float starScale = 0.02f;
                float starYOffset = 25f; // Additional offset to move the stars downward

                Vector2 topStarPosition = new Vector2(
                    levelBoxPosition.X + levelBoxWidth / 2 - (_starTexture.Width * starScale) / 2,
                    levelBoxPosition.Y + levelBoxHeight - 20f - (_starTexture.Height * starScale) + starYOffset
                );

                Vector2 leftStarPosition = new Vector2(
                    levelBoxPosition.X + levelBoxWidth / 2 - (_starTexture.Width * starScale) - 10f,
                    levelBoxPosition.Y + levelBoxHeight - (_starTexture.Height * starScale) - 10f + starYOffset
                );

                Vector2 rightStarPosition = new Vector2(
                    levelBoxPosition.X + levelBoxWidth / 2 + 10f,
                    levelBoxPosition.Y + levelBoxHeight - (_starTexture.Height * starScale) - 10f + starYOffset
                );

                // Draw the top star
                spriteBatch.Draw(
                    _starTexture,
                    topStarPosition,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    starScale,
                    SpriteEffects.None,
                    0f
                );

                // Draw the left star
                spriteBatch.Draw(
                    _starTexture,
                    leftStarPosition,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    starScale,
                    SpriteEffects.None,
                    0f
                );

                // Draw the right star
                spriteBatch.Draw(
                    _starTexture,
                    rightStarPosition,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    starScale,
                    SpriteEffects.None,
                    0f
                );

            }

            // Draw "Back" Button
            int backIndex = menuItems.Count - 1;
            bool backSelected = (backIndex == selectedIndex);
            Color backColor = backSelected ? Color.Yellow : Color.White;
            float backScale = backSelected ? 1.1f : 1.0f;
            Vector2 backTextSize = bigFont.MeasureString(menuItems[backIndex]) * backScale;
            Vector2 backPosition = new Vector2(
                boxPosition.X + (boxWidth - backTextSize.X) / 2,
                boxPosition.Y + boxHeight - backTextSize.Y - 50
            );

            spriteBatch.DrawString(
                bigFont,
                menuItems[backIndex],
                backPosition,
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
