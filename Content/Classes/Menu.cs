using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;

namespace JumpScape
{
    public class Menu
    {
        private SpriteFont font;
        private List<string> menuItems;
        private int selectedIndex;

        private Texture2D _backgroundTexture;
        private Texture2D _gameLogoTexture;
        private Texture2D _buttonTexture;

        private Vector2 _logoPosition;
        private Vector2 _backgroundPosition;
        private float _backgroundSpeed;
        private bool _movingRight;

        private KeyboardState previousKeyboardState;

        public Menu(SpriteFont font, GraphicsDevice graphicsDevice, int width, int height)
        {
            this.font = font;
            menuItems = new List<string>();
            selectedIndex = 0;
            previousKeyboardState = Keyboard.GetState();

            // Load textures
            _backgroundTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));
            _gameLogoTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "GameLogo", "JumpScapeLogo.png"));
            _buttonTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "buttonBackground.png"));

            // Initialize positions and speeds
            float logoScale = 0.4f; // Scale the logo to 40% size
            _logoPosition = new Vector2(
                (width - _gameLogoTexture.Width * logoScale) / 2,
                height * 0.02f // Higher position at 2% of the screen height
            );

            _backgroundPosition = Vector2.Zero;
            _backgroundSpeed = 0.3f; // Slow scrolling speed
            _movingRight = true;
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        public void ResetPreviousState()
        {
            previousKeyboardState = Keyboard.GetState();
        }

        public int Update(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // Background movement logic
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_movingRight)
            {
                _backgroundPosition.X += _backgroundSpeed * elapsed * 100;
                if (_backgroundPosition.X >= _backgroundTexture.Width - graphicsDevice.Viewport.Width * 1.1) // Width of the screen
                {
                    _movingRight = false;
                }
            }
            else
            {
                _backgroundPosition.X -= _backgroundSpeed * elapsed * 100;
                if (_backgroundPosition.X <= 0)
                {
                    _movingRight = true;
                }
            }

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

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 1.0f, bool isMenu = false)
        {
            // Draw the background
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

            // Adjust spacing for non-main menus
            float spacing = isMenu ? _buttonTexture.Height + 20 : font.LineSpacing + 10;

            // Center menu items if it's the main menu
            if (isMenu)
            {
                position = new Vector2(
                    (graphicsDevice.Viewport.Width - _buttonTexture.Width) / 2,
                    graphicsDevice.Viewport.Height / 2 - (menuItems.Count * spacing) / 2
                );
            }

            // Draw menu items
            for (int i = 0; i < menuItems.Count; i++)
            {
                Color color = (i == selectedIndex) ? Color.Yellow : Color.White;
                Vector2 itemPosition = position + new Vector2(
                    0,
                    i * spacing // Adjusted spacing
                );

                if (isMenu)
                {
                    // Draw button texture only for main menu
                    spriteBatch.Draw(
                        _buttonTexture, 
                        new Rectangle((int)itemPosition.X, (int)itemPosition.Y, _buttonTexture.Width, _buttonTexture.Height), 
                        Color.White
                    );

                    // Draw menu text centered on the button
                    Vector2 textPosition = itemPosition + new Vector2(_buttonTexture.Width / 2, _buttonTexture.Height / 2) - font.MeasureString(menuItems[i]) / 2;
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
                else
                {
                    // For non-main menus, just draw the text
                    spriteBatch.DrawString(
                        font, 
                        menuItems[i], 
                        itemPosition, 
                        color, 
                        0f, 
                        Vector2.Zero, 
                        scale, 
                        SpriteEffects.None, 
                        0f
                    );
                }
            }
        }

        public string GetSelectedItem()
        {
            if (selectedIndex >= 0 && selectedIndex < menuItems.Count)
                return menuItems[selectedIndex];
            return null;
        }
    }
}