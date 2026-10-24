using System.Collections.Generic;
using System.Globalization;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Menus
{
    public enum LevelSelectChoice
    {
        None,
        Back,
        Level,
    }

    /// <summary>A grid of numbered level boxes on a wooden panel, plus a Back button.</summary>
    public sealed class LevelSelectMenu
    {
        private const string Title = "Level Selector";
        private const float TitleScale = 1.5f;
        private const float LogoScale = 0.384f;
        private const float BoxScale = 0.2f;
        private const int Columns = 3;
        private const int ColumnGap = 90;
        private const int RowGap = 20;
        private const float SelectedBackScale = 1.1f;

        private readonly IReadOnlyList<int> _levels;
        private readonly Rectangle _panel = MenuDrawing.CenteredPanel(VirtualScreen.Width / 2, VirtualScreen.Height / 2);
        private readonly Vector2 _titlePosition;
        private readonly Rectangle[] _levelBoxes;
        private int _selected;

        public LevelSelectMenu(IReadOnlyList<int> levels)
        {
            _levels = levels;

            Vector2 titleSize = Assets.Font.MeasureString(Title) * TitleScale;
            _titlePosition = new Vector2(_panel.X + (_panel.Width - titleSize.X) / 2f, _panel.Y + 30);

            int boxWidth = (int)(Assets.LevelBox.Width * BoxScale);
            int boxHeight = (int)(Assets.LevelBox.Height * BoxScale);
            int left = _panel.X + 50;
            int top = (int)(_titlePosition.Y + titleSize.Y + 40);

            _levelBoxes = new Rectangle[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                _levelBoxes[i] = new Rectangle(left + column * (boxWidth + ColumnGap), top + row * (boxHeight + RowGap), boxWidth, boxHeight);
            }
        }

        /// <summary>Index of the Back button in the selection order (after all levels).</summary>
        private int BackIndex => _levels.Count;

        public void Open()
        {
            _selected = 0;
        }

        /// <summary>The level number picked when <see cref="Update"/> returns <see cref="LevelSelectChoice.Level"/>.</summary>
        public int ChosenLevel { get; private set; }

        public LevelSelectChoice Update()
        {
            int count = _levels.Count + 1;
            if (Input.MenuUp || Input.WasPressed(Keys.Left))
                _selected = (_selected + count - 1) % count;
            if (Input.MenuDown || Input.WasPressed(Keys.Right))
                _selected = (_selected + 1) % count;
            if (Input.MenuConfirm)
                return ChoiceFor(_selected);

            for (int i = 0; i < count; i++)
            {
                Rectangle bounds = i == BackIndex ? GetBackBounds(i == _selected) : _levelBoxes[i];
                if (!Input.IsMouseOver(bounds))
                    continue;

                if (Input.MouseMoved)
                    _selected = i;
                if (Input.WasClicked)
                    return ChoiceFor(i);
            }

            return LevelSelectChoice.None;
        }

        private LevelSelectChoice ChoiceFor(int index)
        {
            if (index == BackIndex)
                return LevelSelectChoice.Back;

            ChosenLevel = _levels[index];
            return LevelSelectChoice.Level;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            MenuDrawing.DrawLogo(spriteBatch, LogoScale);
            spriteBatch.Draw(Assets.WoodPanel, _panel, Color.White);
            MenuDrawing.DrawText(spriteBatch, Title, _titlePosition, Color.White, TitleScale);

            for (int i = 0; i < _levelBoxes.Length; i++)
            {
                Rectangle box = _levelBoxes[i];
                spriteBatch.Draw(Assets.LevelBox, box, Color.White);

                string number = _levels[i].ToString(CultureInfo.InvariantCulture);
                Vector2 textPosition = box.Center.ToVector2() - Assets.Font.MeasureString(number) / 2f;
                spriteBatch.DrawString(Assets.Font, number, textPosition, i == _selected ? Color.Yellow : Color.White);
            }

            bool backSelected = _selected == BackIndex;
            Rectangle back = GetBackBounds(backSelected);
            MenuDrawing.DrawText(spriteBatch, "Back", back.Location.ToVector2(), backSelected ? Color.Yellow : Color.White, backSelected ? SelectedBackScale : 1f);
        }

        private Rectangle GetBackBounds(bool selected)
        {
            Vector2 size = Assets.Font.MeasureString("Back") * (selected ? SelectedBackScale : 1f);
            int x = (int)(_panel.X + (_panel.Width - size.X) / 2f);
            int y = (int)(_panel.Bottom - size.Y - 50);
            return new Rectangle(x, y, (int)size.X, (int)size.Y);
        }
    }
}
