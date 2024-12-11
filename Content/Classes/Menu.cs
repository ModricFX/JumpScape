using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace JumpScape
{
    public class Menu
    {
        private SpriteFont font;
        private List<string> menuItems;
        private int selectedIndex;

        private KeyboardState previousKeyboardState;

        public Menu(SpriteFont font)
        {
            this.font = font;
            menuItems = new List<string>();
            selectedIndex = 0;
            previousKeyboardState = Keyboard.GetState();
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        public void ResetPreviousState()
        {
            previousKeyboardState = Keyboard.GetState();
        }

        public int Update()
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

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
                // Return selected item index
                return selectedIndex;
            }

            previousKeyboardState = currentKeyboardState;
            return -1; // No selection yet
        }

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                Color color = (i == selectedIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(font, menuItems[i], position + new Vector2(0, i * 40), color);
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
