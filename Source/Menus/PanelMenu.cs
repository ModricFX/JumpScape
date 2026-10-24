using System;
using System.Collections.Generic;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>
    /// A wooden panel with a title and a column of plank buttons, shown over the darkened game.
    /// Used for the pause, death and level complete screens.
    /// </summary>
    public sealed class PanelMenu
    {
        private const int MinimumWidth = 620;
        private const int Padding = 40;
        private const int ButtonGap = 18;
        private const float TitleScale = 1.4f;
        private const float DimAmount = 0.55f;

        private readonly string _title;
        private readonly Color _titleColor;
        private readonly string[] _items;
        private readonly float _fadeInTime;
        private readonly Rectangle[] _buttons;
        private readonly int _panelWidth;
        private Rectangle _panel;
        private Vector2 _titleCenter;
        private Vector2 _subtitleTop;
        private List<string> _subtitleLines = new List<string>();
        private float _openTime;
        private int _selected;

        /// <param name="fadeInTime">Seconds to fade in. Input is ignored until the menu is fully visible.</param>
        public PanelMenu(string title, Color titleColor, string[] items, float fadeInTime = 0f)
        {
            _title = title;
            _titleColor = titleColor;
            _items = items;
            _fadeInTime = fadeInTime;
            _buttons = new Rectangle[items.Length];

            // Long titles get a wider panel instead of spilling over its edges.
            _panelWidth = Math.Max(MinimumWidth, (int)(Assets.Font.MeasureString(title).X * TitleScale) + Padding * 2);
        }

        public bool IsOpen { get; private set; }

        private float Visibility => _fadeInTime <= 0f ? 1f : MathHelper.Clamp(_openTime / _fadeInTime, 0f, 1f);

        /// <param name="subtitle">An optional smaller line under the title, such as the time taken.</param>
        public void Open(string subtitle = null)
        {
            IsOpen = true;
            _openTime = 0f;
            _selected = 0;
            _subtitleLines = subtitle == null ? new List<string>() : UiDrawing.WrapText(Assets.SmallFont, subtitle, _panelWidth - Padding * 2);
            Layout();
        }

        public void Close()
        {
            IsOpen = false;
        }

        private void Layout()
        {
            int buttonWidth = Assets.MenuButton.Width;
            int buttonHeight = Assets.MenuButton.Height;
            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            float subtitleHeight = _subtitleLines.Count == 0 ? 0f : _subtitleLines.Count * Assets.SmallFont.LineSpacing + 12f;
            int buttonsHeight = _items.Length * (buttonHeight + ButtonGap) - ButtonGap;
            int height = (int)(Padding + titleHeight + subtitleHeight + 30 + buttonsHeight + Padding);

            _panel = UiDrawing.CenteredPanel(_panelWidth, height);
            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + Padding + titleHeight / 2f);
            _subtitleTop = new Vector2(_panel.Center.X, _panel.Y + Padding + titleHeight + 8f);

            int top = (int)(_panel.Y + Padding + titleHeight + subtitleHeight + 30);
            for (int i = 0; i < _items.Length; i++)
                _buttons[i] = new Rectangle(_panel.Center.X - buttonWidth / 2, top + i * (buttonHeight + ButtonGap), buttonWidth, buttonHeight);
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

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!Input.IsMouseOver(_buttons[i]))
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
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * (DimAmount * visibility));

            // The panel slides up a little as it fades in.
            int slide = (int)((1f - visibility) * 40f);
            Rectangle panel = _panel;
            panel.Y += slide;
            UiDrawing.DrawPanel(spriteBatch, panel, visibility);

            var offset = new Vector2(0, slide);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, _title, _titleCenter + offset, _titleColor * visibility, TitleScale);
            for (int i = 0; i < _subtitleLines.Count; i++)
            {
                var center = _subtitleTop + offset + new Vector2(0, (i + 0.5f) * Assets.SmallFont.LineSpacing);
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, _subtitleLines[i], center, Color.White * visibility);
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                Rectangle button = _buttons[i];
                button.Y += slide;
                UiDrawing.DrawButton(spriteBatch, button, _items[i], i == _selected, visibility);
            }
        }
    }
}
