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

        public Menu(SpriteFont font)
        {
            this.font = font;
            menuItems = new List<string>();
            selectedIndex = 0;
        }

        public void AddMenuItem(string item)
        {
            menuItems.Add(item);
        }

        public int Update()
        {
            KeyboardState state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.Up))
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = menuItems.Count - 1;
            }
            else if (state.IsKeyDown(Keys.Down))
            {
                selectedIndex++;
                if (selectedIndex >= menuItems.Count) selectedIndex = 0;
            }
            else if (state.IsKeyDown(Keys.Enter))
            {
                return selectedIndex; // Return selected item
            }

            return -1; // No selection yet
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                Color color = (i == selectedIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(font, menuItems[i], position + new Vector2(0, i * 40), color);
            }
        }
    }
}
