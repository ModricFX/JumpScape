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
        private MouseState previousMouseState;

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
            _logoPosition = new Vector2(
                (graphicsDevice.Viewport.Width - (_gameLogoTexture.Width * 0.3f)) / 2,
                graphicsDevice.Viewport.Height * 0.01f
            );
            _backgroundPosition = backgroundPosition;

            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            // Keyboard
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
                return selectedIndex;
            }

            // Mouse
            float boxWidth = 700;
            float boxHeight = 500;
            float levelBoxScale = 0.2f;
            float horizontalSpacing = 90f;
            float verticalSpacing = 20f;

            Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);

            for (int i = 0; i < menuItems.Count - 1; i++)
            {
                if (GetLevelBoxBounds(i, graphicsDevice, boxWidth, boxHeight, levelBoxScale, horizontalSpacing, verticalSpacing)
                    .Contains(mousePos))
                {
                    selectedIndex = i;
                    if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                    {
                        previousKeyboardState = currentKeyboardState;
                        previousMouseState = currentMouseState;
                        return selectedIndex;
                    }
                }
            }

            int backIndex = menuItems.Count - 1;
            bool backSelected = (backIndex == selectedIndex);
            float backScale = backSelected ? 1.1f : 1.0f;
            if (GetBackButtonBounds(graphicsDevice, boxWidth, boxHeight, backScale).Contains(mousePos))
            {
                selectedIndex = backIndex;
                if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    previousKeyboardState = currentKeyboardState;
                    previousMouseState = currentMouseState;
                    return selectedIndex;
                }
            }

            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;
            return -1;
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        private Vector2 GetBasePosition(GraphicsDevice graphicsDevice, float boxWidth, float boxHeight)
        {
            return new Vector2(
                graphicsDevice.Viewport.Bounds.Center.X - (boxWidth / 2),
                (graphicsDevice.Viewport.Bounds.Center.Y - (boxHeight / 2)) * 1.5f
            );
        }

        private Rectangle GetLevelBoxBounds(int index, GraphicsDevice graphicsDevice, float boxWidth, float boxHeight, float levelBoxScale, float horizontalSpacing, float verticalSpacing)
        {
            int columns = 3;
            float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
            float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;
            Vector2 boxPosition = GetBasePosition(graphicsDevice, boxWidth, boxHeight);

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

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                _backgroundTexture,
                new Rectangle((int)-_backgroundPosition.X, 0, _backgroundTexture.Width, _backgroundTexture.Height),
                Color.White
            );

            // Draw the game logo scaled
            float logoScale = 0.3f;

            // Draw the logo using its CENTER as the origin
            spriteBatch.Draw(
                _gameLogoTexture,
                _logoPosition,
                null,
                Color.White,
                0f,
                new Vector2(0, 0),
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

            float boxWidth = 700;
            float boxHeight = 500;
            Vector2 boxPosition = GetBasePosition(graphicsDevice, boxWidth, boxHeight);

            spriteBatch.Draw(
                _woodBoxTexture,
                new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxWidth, (int)boxHeight),
                Color.White
            );

            string titleText = "Level Selector";
            float titleScale = 1.5f;
            Vector2 titleSize = bigFont.MeasureString(titleText) * titleScale;
            Vector2 titlePosition = boxPosition + new Vector2((boxWidth - titleSize.X) / 2, 30);
            spriteBatch.DrawString(bigFont, titleText, titlePosition, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            float levelBoxScale = 0.2f;
            float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
            float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;
            float horizontalSpacing = 90f;
            float verticalSpacing = 20f;

            for (int i = 0; i < menuItems.Count - 1; i++)
            {
                bool isSelected = (i == selectedIndex);
                bool isHovering = GetLevelBoxBounds(i, graphicsDevice, boxWidth, boxHeight, levelBoxScale, horizontalSpacing, verticalSpacing)
                    .Contains(Mouse.GetState().Position);

                Color textColor = (isSelected || isHovering) ? Color.Yellow : Color.White;

                Rectangle levelRect = GetLevelBoxBounds(i, graphicsDevice, boxWidth, boxHeight, levelBoxScale, horizontalSpacing, verticalSpacing);

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

                string levelNumber = (i + 1).ToString();
                Vector2 textSize = bigFont.MeasureString(levelNumber);
                Vector2 textPosition = new Vector2(
                    levelRect.X + (levelRect.Width - textSize.X) / 2,
                    levelRect.Y + (levelRect.Height - textSize.Y) / 2
                );
                spriteBatch.DrawString(bigFont, levelNumber, textPosition, textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);


                // STAR LOGIC
                // float starScale = 0.02f;
                // float starYOffset = 25f;

                //     Vector2 topStarPosition = new Vector2(
                //         levelRect.X + levelRect.Width / 2 - (_starTexture.Width * starScale) / 2,
                //         levelRect.Y + levelRect.Height - 20f - (_starTexture.Height * starScale) + starYOffset
                //     );

                //     Vector2 leftStarPosition = new Vector2(
                //         levelRect.X + levelRect.Width / 2 - (_starTexture.Width * starScale) - 10f,
                //         levelRect.Y + levelRect.Height - (_starTexture.Height * starScale) - 10f + starYOffset
                //     );

                //     Vector2 rightStarPosition = new Vector2(
                //         levelRect.X + levelRect.Width / 2 + 10f,
                //         levelRect.Y + levelRect.Height - (_starTexture.Height * starScale) - 10f + starYOffset
                //     );

                //     spriteBatch.Draw(_starTexture, topStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
                //     spriteBatch.Draw(_starTexture, leftStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
                //     spriteBatch.Draw(_starTexture, rightStarPosition, null, Color.White, 0f, Vector2.Zero, starScale, SpriteEffects.None, 0f);
            }

            int backIndex = menuItems.Count - 1;
            bool backSelected = (backIndex == selectedIndex);
            bool backHover = GetBackButtonBounds(graphicsDevice, boxWidth, boxHeight, backSelected ? 1.1f : 1.0f)
                .Contains(Mouse.GetState().Position);
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
