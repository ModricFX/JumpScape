using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace JumpScape
{
    public class MainMenu
    {
        private SpriteFont font;
        public List<string> menuItems;
        public int selectedIndex;
        public bool visible;

        private Texture2D _backgroundTexture;
        private Texture2D _gameLogoTexture;
        private Texture2D _buttonTexture;

        private Vector2 _logoPosition;
        private Vector2 _backgroundPosition;

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        private float backgroundScale;

        // Background movement variables
        private float _backgroundSpeedX = 0.5f; // Speed of background movement along X
        private float _backgroundSpeedY = 0.2f; // Speed of background movement along Y

        public MainMenu(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            this.font = font;
            menuItems = new List<string>();
            selectedIndex = 0;
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();

            // Load textures
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _buttonTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "buttonBackground.png"));
            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));

            // Calculate initial background scale
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        public void ResetPreviousState()
        {
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice, Vector2 backgroundPosition)
        {
            // Update the background scale if the resolution changes
            CalculateBackgroundScale(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);

            _logoPosition = new Vector2(
                (graphicsDevice.Viewport.Width - (_gameLogoTexture.Width * 0.4f)) / 2, // Centered X position
                graphicsDevice.Viewport.Height * 0.01f // Y position
            );

            // Update background position for movement
            UpdateBackgroundPosition(gameTime);

            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
            _backgroundPosition = backgroundPosition;

            Vector2 mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);

            // Check for keyboard navigation
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

            // Mouse hover and click detection
            for (int i = 0; i < menuItems.Count; i++)
            {
                Rectangle buttonBounds = GetButtonBounds(i, graphicsDevice);
                if (buttonBounds.Contains(mousePosition.ToPoint()))
                {
                    selectedIndex = i;
                    if (IsMouseClicked(currentMouseState, previousMouseState))
                    {
                        return i;
                    }
                }
            }

            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;
            return -1; // No selection yet
        }

        private void UpdateBackgroundPosition(GameTime gameTime)
        {
            // Move the background
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _backgroundPosition.X += _backgroundSpeedX * elapsed * 100;
            _backgroundPosition.Y += _backgroundSpeedY * elapsed * 100;

            // Calculate the scaled background width and height
            float scaledWidth = _backgroundTexture.Width * backgroundScale;
            float scaledHeight = _backgroundTexture.Height * backgroundScale;

            // Wrap the background position horizontally
            if (_backgroundPosition.X >= scaledWidth)
                _backgroundPosition.X -= scaledWidth;
            else if (_backgroundPosition.X <= -scaledWidth)
                _backgroundPosition.X += scaledWidth;

            // Wrap the background position vertically
            if (_backgroundPosition.Y >= scaledHeight)
                _backgroundPosition.Y -= scaledHeight;
            else if (_backgroundPosition.Y <= -scaledHeight)
                _backgroundPosition.Y += scaledHeight;
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        private bool IsMouseClicked(MouseState current, MouseState previous)
        {
            return current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;
        }

        private Vector2 GetBasePosition(GraphicsDevice graphicsDevice, float spacing, float scale = 1.0f)
        {
            return new Vector2(
                (graphicsDevice.Viewport.Width - (_buttonTexture.Width * scale)) / 2,
                graphicsDevice.Viewport.Height / 2 - (menuItems.Count * spacing) / 2 + (_gameLogoTexture.Height * 0.4f / 3)
            );
        }

        private Rectangle GetButtonBounds(int index, GraphicsDevice graphicsDevice, float scale = 1.0f)
        {
            float spacing = (_buttonTexture.Height * scale) + 20;
            Vector2 basePosition = GetBasePosition(graphicsDevice, spacing, scale);
            Vector2 itemPosition = basePosition + new Vector2(0, index * spacing);

            return new Rectangle(
                (int)itemPosition.X,
                (int)itemPosition.Y,
                (int)(_buttonTexture.Width * scale),
                (int)(_buttonTexture.Height * scale)
            );
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 1.0f)
        {
            DrawBackground(spriteBatch, graphicsDevice);

            float spacing = _buttonTexture.Height + 20;
            Vector2 basePosition = GetBasePosition(graphicsDevice, spacing, scale);

            for (int i = 0; i < menuItems.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                Color color = isSelected ? Color.Yellow : Color.White;
                Vector2 itemPosition = basePosition + new Vector2(0, i * spacing);

                spriteBatch.Draw(
                    _buttonTexture,
                    new Rectangle((int)itemPosition.X, (int)itemPosition.Y, _buttonTexture.Width, _buttonTexture.Height),
                    Color.White
                );

                Vector2 textSize = font.MeasureString(menuItems[i]) * scale;
                Vector2 textPosition = itemPosition + new Vector2(
                    (_buttonTexture.Width - textSize.X) / 2,
                    (_buttonTexture.Height - textSize.Y) / 2
                );

                // Check mouse hover
                bool isHovering = GetButtonBounds(i, graphicsDevice).Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y));
                Color textColor = (isSelected || isHovering) ? Color.Yellow : Color.White;

                spriteBatch.DrawString(
                    font,
                    menuItems[i],
                    textPosition,
                    textColor,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawBackground(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // Draw the moving background with scaling
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

            float logoScale = 0.4f;

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

        private void CalculateBackgroundScale(int screenWidth, int screenHeight)
        {
            // Determine the scaling factor for the background to fit the screen
            float scaleX = (float)screenWidth / _backgroundTexture.Width * 1.1f;
            float scaleY = (float)screenHeight / _backgroundTexture.Height * 1.1f;

            // Use the larger scale to ensure the background covers the entire screen
            backgroundScale = Math.Max(scaleX, scaleY);
        }

        public string GetSelectedItem()
        {
            if (selectedIndex >= 0 && selectedIndex < menuItems.Count)
                return menuItems[selectedIndex];
            return null;
        }
    }
}