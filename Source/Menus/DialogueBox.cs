using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>
    /// A wooden box at the bottom of the screen showing what an NPC says, one line at a time.
    /// The text types itself out; the Use key (E), Enter, Space or a click shows the whole line, then moves to
    /// the next. Lines can name the player's own keys with {left}, {right}, {jump}, {use} and {drop}.
    /// </summary>
    public sealed class DialogueBox
    {
        private const float CharactersPerSecond = 50f;
        private const float TextScale = 0.75f;
        private const int Padding = 36;

        private readonly Rectangle _panel = new Rectangle(VirtualScreen.Width / 2 - 600, VirtualScreen.Height - 250, 1200, 210);
        private string _name;
        private IReadOnlyList<string> _lines;
        private List<string> _wrapped;
        private int _line;
        private float _shownCharacters;

        public bool IsOpen { get; private set; }

        private int CurrentLength => _lines[_line].Length;

        public void Open(string name, IReadOnlyList<string> lines)
        {
            if (lines.Count == 0)
                return;

            _name = name;
            _lines = lines.Select(Controls.FillIn).ToList();
            IsOpen = true;
            ShowLine(0);
        }

        public void Close()
        {
            IsOpen = false;
        }

        private void ShowLine(int line)
        {
            _line = line;
            _shownCharacters = 0f;
            _wrapped = UiDrawing.WrapText(Assets.Font, _lines[line], _panel.Width - Padding * 2, TextScale);
        }

        public void Update(float deltaTime)
        {
            if (!IsOpen)
                return;

            _shownCharacters += CharactersPerSecond * deltaTime;

            if (Input.Back)
            {
                IsOpen = false;
                return;
            }

            if (!Input.WasPressed(GameAction.Use) && !Input.MenuConfirm && !Input.WasClicked)
                return;

            if (_shownCharacters < CurrentLength)
                _shownCharacters = CurrentLength;
            else if (_line + 1 < _lines.Count)
                ShowLine(_line + 1);
            else
                IsOpen = false;
        }

        /// <summary>Draws in menu coordinates, so use <see cref="VirtualScreen.CenteredTransform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawText(spriteBatch, _name, new Vector2(_panel.X + Padding, _panel.Y + 20), UiDrawing.Highlight, 0.85f);

            // Reveal the already wrapped text a character at a time, so words don't jump between lines.
            int remaining = (int)_shownCharacters;
            float y = _panel.Y + 20 + Assets.Font.LineSpacing * 0.85f + 6;
            foreach (string row in _wrapped)
            {
                if (remaining <= 0)
                    break;
                string visible = row.Substring(0, Math.Min(row.Length, remaining));
                UiDrawing.DrawText(spriteBatch, Assets.Font, visible, new Vector2(_panel.X + Padding, y), Color.White, TextScale);
                remaining -= row.Length + 1;
                y += Assets.Font.LineSpacing * TextScale;
            }

            bool finished = _shownCharacters >= CurrentLength;
            bool lastLine = _line == _lines.Count - 1;
            string page = (_line + 1).ToString(CultureInfo.InvariantCulture) + "/" + _lines.Count.ToString(CultureInfo.InvariantCulture);
            Vector2 pageSize = Assets.SmallFont.MeasureString(page);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, page, new Vector2(_panel.Right - Padding - pageSize.X, _panel.Y + 24), Color.White * 0.7f);
            if (finished)
                UiDrawing.DrawHint(spriteBatch, new Vector2(_panel.Right - 110, _panel.Bottom - 14), Controls.KeyNameFor(GameAction.Use), lastLine ? "Close" : "Next");
        }
    }
}
