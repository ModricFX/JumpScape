using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>A darkened overlay with a title and a short list of choices, used for the pause and death screens.</summary>
    public sealed class OverlayMenu
    {
        private const int ItemSpacing = 60;

        private readonly string _title;
        private readonly Color _titleColor;
        private readonly string[] _items;
        private readonly float _titleY;
        private readonly float _itemsY;
        private readonly float _dimAmount;
        private readonly float _fadeInTime;
        private float _openTime;
        private int _selected;

        /// <param name="titleY">Title position as a fraction of the screen height.</param>
        /// <param name="itemsY">Position of the first item as a fraction of the screen height.</param>
        /// <param name="dimAmount">How dark the game behind the menu gets (0-1).</param>
        /// <param name="fadeInTime">Seconds to fade in. Input is ignored until the menu is fully visible.</param>
        public OverlayMenu(string title, Color titleColor, string[] items, float titleY, float itemsY, float dimAmount, float fadeInTime)
        {
            _title = title;
            _titleColor = titleColor;
            _items = items;
            _titleY = titleY;
            _itemsY = itemsY;
            _dimAmount = dimAmount;
            _fadeInTime = fadeInTime;
        }

        public bool IsOpen { get; private set; }

        private float Visibility => _fadeInTime <= 0f ? 1f : MathHelper.Clamp(_openTime / _fadeInTime, 0f, 1f);

        public void Open()
        {
            IsOpen = true;
            _openTime = 0f;
            _selected = 0;
        }

        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>Returns the index of the chosen item, or -1 if nothing was chosen this frame.</summary>
        public int Update(float deltaTime)
        {
            if (!IsOpen)
                return -1;

            _openTime += deltaTime;
            if (Visibility < 1f)
                return -1;

            if (Input.MenuUp)
                _selected = (_selected + _items.Length - 1) % _items.Length;
            if (Input.MenuDown)
                _selected = (_selected + 1) % _items.Length;
            if (Input.MenuConfirm)
                return _selected;

            for (int i = 0; i < _items.Length; i++)
            {
                if (!Input.IsMouseOver(GetItemBounds(i)))
                    continue;

                if (Input.MouseMoved)
                    _selected = i;
                if (Input.WasClicked)
                    return i;
            }

            return -1;
        }

        /// <summary>Draws in menu coordinates, so use <see cref="VirtualScreen.CenteredTransform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            float visibility = Visibility;
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * (_dimAmount * visibility));

            Vector2 titleSize = Assets.Font.MeasureString(_title);
            var titlePosition = new Vector2((VirtualScreen.Width - titleSize.X) / 2f, VirtualScreen.Height * _titleY);
            spriteBatch.DrawString(Assets.Font, _title, titlePosition, _titleColor * visibility);

            for (int i = 0; i < _items.Length; i++)
            {
                Color color = i == _selected ? Color.Yellow : Color.White;
                spriteBatch.DrawString(Assets.Font, _items[i], GetItemBounds(i).Location.ToVector2(), color * visibility);
            }
        }

        private Rectangle GetItemBounds(int index)
        {
            Vector2 size = Assets.Font.MeasureString(_items[index]);
            int x = (int)((VirtualScreen.Width - size.X) / 2f);
            int y = (int)(VirtualScreen.Height * _itemsY) + index * ItemSpacing;
            return new Rectangle(x, y, (int)size.X, (int)size.Y);
        }
    }
}
