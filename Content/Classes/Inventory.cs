using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace JumpScape.Classes
{
    public class Inventory
    {
        public int MaxInventoryItems = 3;
        public string[] _inventory;
        private Texture2D _keyTexture;
        public Texture2D[] _selectTextures; // Textures for selected inventory items
        private int _topRightScreenX;
        private int _selectedIndex; // The currently selected inventory item (0, 1, or 2)


        public Inventory(Texture2D inventoryTexture, Texture2D keyTexture, Texture2D select1, Texture2D select2, Texture2D select3)
        {
            _keyTexture = keyTexture;
            _inventory = new string[MaxInventoryItems];
            _selectTextures = new Texture2D[] { select1, select2, select3 }; // Initialize select textures
            _selectedIndex = 0; // Default to the first item being selected
        }


        // Add item to inventory
        public void AddItem(string itemName)
        {
            for (int i = 0; i < MaxInventoryItems; i++)
            {
                if (_inventory[i] == null) // Check for an empty slot
                {
                    _inventory[i] = itemName;
                    break;
                }
            }
        }

        // Remove the first "Key" and shift the items
        public void RemoveFirstKey()
        {
            for (int i = 0; i < MaxInventoryItems; i++)
            {
                if (_inventory[i] == "Key")
                {
                    // Shift the items to the left after removing the key
                    for (int j = i; j < MaxInventoryItems - 1; j++)
                    {
                        _inventory[j] = _inventory[j + 1];
                    }

                    _inventory[MaxInventoryItems - 1] = null;  // Set the last slot to null
                    break;
                }
            }
        }

        public string GetItemName(int index)
        {
            if (index >= 0 && index < _inventory.Length && !string.IsNullOrEmpty(_inventory[index]))
            {
                return _inventory[index];
            }
            return string.Empty;
        }

        public Texture2D GetItemTexture(int index)
        {
            if (index >= 0 && index < MaxInventoryItems && !string.IsNullOrEmpty(_inventory[index]))
            {
                return _inventory[index] == "Key" ? _keyTexture : null;
            }
            return null;
        }


        public void RemoveItem(int index)
        {
            if (index >= 0 && index < _inventory.Length)
            {
                _inventory[index] = null;  // Clear the item slot
            }
        }


        public void Update(GameTime gameTime, int topRightScreenX, int selectedIndex)
        {
            // Calculate the top-right corner for the inventory
            _topRightScreenX = topRightScreenX;

            _selectedIndex = selectedIndex;

        }
        // Draw method that will handle drawing the inventory items and background
        public void Draw(SpriteBatch spriteBatch, float topLeftScreenY)
        {


            float inventoryScale = 0.7f; // Adjust the scale for the inventory icon
            spriteBatch.Draw(
                _selectTextures[_selectedIndex],
                new Vector2(_topRightScreenX, topLeftScreenY),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                inventoryScale,
                SpriteEffects.None,
                0f
            );


            // Draw the items in the inventory
            for (int i = 0; i < MaxInventoryItems; i++)
            {
                if (_inventory[i] != null)  // If there is an item in the slot
                {
                    Texture2D itemTexture = _inventory[i] == "Key" ? _keyTexture : null;  // Replace this with the item texture

                    // Draw the selection highlight if the item is selected


                    // Draw the item
                    spriteBatch.Draw(
                        itemTexture,
                        new Vector2(_topRightScreenX + 10 + (i * 40),
                                    topLeftScreenY + (_selectTextures[_selectedIndex].Height * inventoryScale / 2) - (_keyTexture.Height * 0.07f / 2)),  // Center the item vertically
                        null,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        0.07f, // Scale for items
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }
    }
}
