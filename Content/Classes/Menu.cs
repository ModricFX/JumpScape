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
        private Texture2D _woodBoxTexture;
        private Texture2D _levelBoxTexture;
        private Texture2D _starTexture;

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
            _woodBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "woodBackground.png"));
            _levelBoxTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "levelBox.png"));
            _starTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Menu", "star.png"));

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
                if (_backgroundPosition.X >= _backgroundTexture.Width - graphicsDevice.Viewport.Width * 1.1)
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

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Vector2 position, float scale = 3.0f, bool isMenu = false)
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

            if (isMenu)
            {
                // MAIN MENU LOGIC
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

                    // Draw button texture
                    spriteBatch.Draw(
                        _buttonTexture,
                        new Rectangle((int)itemPosition.X, (int)itemPosition.Y, _buttonTexture.Width, _buttonTexture.Height),
                        Color.White
                    );

                    // Draw menu text centered on the button
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
            else
            {
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
                Vector2 titleSize = font.MeasureString(titleText) * titleScale;
                Vector2 titlePosition = boxPosition + new Vector2(
                    (boxWidth - titleSize.X) / 2,
                    30
                );
                spriteBatch.DrawString(
                    font,
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
                // Layout for Level Boxes
                float levelBoxScale = 0.2f;
                float levelBoxWidth = _levelBoxTexture.Width * levelBoxScale;
                float levelBoxHeight = _levelBoxTexture.Height * levelBoxScale;

                int columns = 3; // Number of columns for the grid layout
                float horizontalSpacing = 90f; // Spacing between boxes horizontally
                float verticalSpacing = 20f; // Spacing between boxes vertically

                // Starting position for the first level box (top-left of the wooden box)
                Vector2 levelStartPosition = new Vector2(
                    boxPosition.X + 50f, // Padding from the left edge of the wooden box
                    titlePosition.Y + titleSize.Y + 40f // Below the title with some padding
                );

                for (int i = 0; i < menuItems.Count - 1; i++) // Exclude the "Back" button
                {
                    bool isSelected = (i == selectedIndex);
                    Color textColor = isSelected ? Color.Yellow : Color.White;

                    // Calculate grid position
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

                    // Draw Level Text
                    Vector2 textSize = font.MeasureString(menuItems[i]) * scale;
                    Vector2 textPosition = new Vector2(
                        levelBoxPosition.X + (levelBoxWidth - textSize.X) / 2,
                        levelBoxPosition.Y - textSize.Y/2
                    );
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

                    // Draw Stars
                    float starScale = 0.03f;
                    float starSpacing = 5f; // Spacing between stars
                    Vector2 starStartPosition = new Vector2(
                        levelBoxPosition.X + (levelBoxWidth - (3 * (_starTexture.Width * starScale) + 2 * starSpacing)) / 2,
                        levelBoxPosition.Y + levelBoxHeight - _starTexture.Height/2  * starScale - 10
                    );

                    for (int s = 0; s < 3; s++)
                    {
                        spriteBatch.Draw(
                            _starTexture,
                            new Vector2(
                                starStartPosition.X + s * (_starTexture.Width * starScale + starSpacing),
                                starStartPosition.Y
                            ),
                            null,
                            Color.White,
                            0f,
                            Vector2.Zero,
                            starScale,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }

                // Draw "Back" Button
                int backIndex = menuItems.Count - 1;
                bool backSelected = (backIndex == selectedIndex);
                Color backColor = backSelected ? Color.Yellow : Color.White;
                float backScale = scale * (backSelected ? 1.1f : 1.0f);
                Vector2 backTextSize = font.MeasureString(menuItems[backIndex]) * backScale;
                Vector2 backPosition = new Vector2(
                    boxPosition.X + (boxWidth - backTextSize.X) / 2,
                    boxPosition.Y + boxHeight - backTextSize.Y - 50
                );

                spriteBatch.DrawString(
                    font,
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

        public string GetSelectedItem()
        {
            if (selectedIndex >= 0 && selectedIndex < menuItems.Count)
                return menuItems[selectedIndex];
            return null;
        }
    }
}
