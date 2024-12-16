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
        private GraphicsDeviceManager graphicsDeviceManager;

        public MainMenu(SpriteFont font, GraphicsDeviceManager _graphics, GraphicsDevice graphicsDevice, int width, int height)
        {
            this.font = font;
            menuItems = new List<string>();
            selectedIndex = 0;
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            this.graphicsDeviceManager = _graphics;

            // Load textures
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _buttonTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "buttonBackground.png"));
            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));

            // Initialize positions
            float logoScale = 0.4f;
            float logoWidth = _gameLogoTexture.Width * logoScale;

            _logoPosition = new Vector2(
                (graphicsDevice.Viewport.Width - logoWidth) / 2, // Centered X position
                graphicsDevice.Viewport.Height * 0.02f             // Y position
            );

            // my current screen size:
            Console.WriteLine("Width: " + _graphics.PreferredBackBufferWidth + " Height: " + _graphics.PreferredBackBufferHeight);
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
            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
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

            if (IsMouseClicked(currentMouseState, previousMouseState))
            {
                for (int i = 0; i < menuItems.Count; i++)
                {
                    Rectangle buttonBounds = GetButtonBounds(i, graphicsDevice);
                    if (buttonBounds.Contains(currentMouseState.Position))
                    {
                        selectedIndex = i;
                        return i;
                    }
                }
            }

            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;
            return -1; // No selection yet
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        private bool IsMouseClicked(MouseState current, MouseState previous)
        {
            return current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;
        }

        private Rectangle GetButtonBounds(int index, GraphicsDevice graphicsDevice)
        {
            float spacing = _buttonTexture.Height + 20;
            Vector2 position = new Vector2(
                (graphicsDevice.Viewport.Width - _buttonTexture.Width) / 2,
                graphicsDevice.Viewport.Height / 2 - (menuItems.Count * spacing) / 2 + index * spacing
            );
            return new Rectangle((int)position.X, (int)position.Y, _buttonTexture.Width, _buttonTexture.Height);
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 1.0f)
        {
            DrawBackground(spriteBatch);

            float spacing = _buttonTexture.Height + 20;
            position = new Vector2(
                (graphicsDevice.Viewport.Width - _buttonTexture.Width) / 2,
                graphicsDevice.Viewport.Height / 2 - (menuItems.Count * spacing) / 2
            );

            for (int i = 0; i < menuItems.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                Color color = isSelected ? Color.Yellow : Color.White;
                Vector2 itemPosition = position + new Vector2(0, i * spacing);

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

                spriteBatch.DrawString(
                    font,
                    menuItems[i],
                    textPosition,
                    color,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawBackground(SpriteBatch spriteBatch)
{
    spriteBatch.Draw(
        _backgroundTexture,
        new Rectangle((int)-_backgroundPosition.X, 0, _backgroundTexture.Width, _backgroundTexture.Height),
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
        new Vector2(_gameLogoTexture.Width,0), // Center origin
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
    }
}
