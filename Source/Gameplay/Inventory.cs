using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    public enum ItemType
    {
        Key,
    }

    /// <summary>The player's three item slots, shown in the top-right corner.</summary>
    public sealed class Inventory
    {
        public const int SlotCount = 3;

        private const float Scale = 0.7f;
        private const float ItemScale = 0.07f;
        private const int ScreenMargin = 20;

        private readonly ItemType?[] _slots = new ItemType?[SlotCount];

        public int SelectedSlot { get; private set; }

        public ItemType? SelectedItem => _slots[SelectedSlot];

        public bool Contains(ItemType item)
        {
            foreach (ItemType? slot in _slots)
            {
                if (slot == item)
                    return true;
            }
            return false;
        }

        /// <summary>Puts the item in the first empty slot. Returns false if the inventory is full.</summary>
        public bool TryAdd(ItemType item)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = item;
                    return true;
                }
            }
            return false;
        }

        public void Select(int slot)
        {
            if (slot >= 0 && slot < SlotCount)
                SelectedSlot = slot;
        }

        public void RemoveSelected()
        {
            _slots[SelectedSlot] = null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D background = Assets.InventorySlots[SelectedSlot];
            float width = background.Width * Scale;
            float height = background.Height * Scale;
            var position = new Vector2(VirtualScreen.ViewWidth - width - ScreenMargin, ScreenMargin);

            spriteBatch.Draw(background, position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);

            float slotWidth = width / SlotCount;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] is not ItemType item)
                    continue;

                Texture2D icon = GetIcon(item);
                var iconSize = new Vector2(icon.Width, icon.Height) * ItemScale;
                var slotCenter = new Vector2(position.X + slotWidth * (i + 0.5f), position.Y + height / 2f);
                spriteBatch.Draw(icon, slotCenter - iconSize / 2f, null, Color.White, 0f, Vector2.Zero, ItemScale, SpriteEffects.None, 0f);
            }
        }

        private static Texture2D GetIcon(ItemType item)
        {
            return item switch
            {
                ItemType.Key => Assets.KeyInventoryIcon,
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
            };
        }
    }
}
