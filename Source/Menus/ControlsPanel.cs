using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>A panel listing every action and the keys for it. Opened from the settings and the pause menu.</summary>
    public sealed class ControlsPanel
    {
        private const int Width = 1000;
        private const int Padding = 40;
        private const int RowHeight = 58;
        private const int CapHeight = 40;
        private const int CapGap = 10;
        private const float TitleScale = 1.4f;
        private const float DimAmount = 0.55f;

        private static readonly (string Action, string[] Keys)[] Rows =
        {
            ("Walk", new[] { "A", "D", "Left", "Right" }),
            ("Jump", new[] { "Space", "W", "Up" }),
            ("Throw an orb", new[] { "Click" }),
            ("Open doors and talk", new[] { "E" }),
            ("Choose an inventory slot", new[] { "1", "2", "3" }),
            ("Drop the selected key", new[] { "Q" }),
            ("Pause", new[] { "Esc" }),
        };

        private readonly Rectangle _panel;
        private readonly Vector2 _titleCenter;
        private readonly int _rowsTop;
        private readonly int _keysX;
        private readonly Rectangle _backButton;

        public ControlsPanel()
        {
            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            int height = (int)(Padding + titleHeight + 30 + Rows.Length * RowHeight + 30 + backHeight + Padding);

            _panel = UiDrawing.CenteredPanel(Width, height);
            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + Padding + titleHeight / 2f);
            _rowsTop = (int)(_panel.Y + Padding + titleHeight + 30);
            _keysX = _panel.X + Width / 2 + 20;
            _backButton = new Rectangle(_panel.Center.X - backWidth / 2, _panel.Bottom - Padding - backHeight, backWidth, backHeight);
        }

        public bool IsOpen { get; private set; }

        public void Open() => IsOpen = true;

        /// <summary>Closes the panel when the player presses Esc, Enter or Space, or clicks Back.</summary>
        public void Update()
        {
            if (Input.Back || Input.MenuConfirm || (Input.WasClicked && Input.IsMouseOver(_backButton)))
                IsOpen = false;
        }

        /// <summary>Draws in menu coordinates, so use <see cref="VirtualScreen.CenteredTransform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * DimAmount);
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Controls", _titleCenter, Color.White, TitleScale);

            float textHeight = Assets.SmallFont.LineSpacing;
            for (int i = 0; i < Rows.Length; i++)
            {
                int rowCenter = _rowsTop + i * RowHeight + RowHeight / 2;
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, Rows[i].Action, new Vector2(_panel.X + 80, rowCenter - textHeight / 2f), Color.White);

                int x = _keysX;
                foreach (string key in Rows[i].Keys)
                {
                    int width = UiDrawing.KeyCapWidth(key, CapHeight);
                    UiDrawing.DrawKeyCap(spriteBatch, new Rectangle(x, rowCenter - CapHeight / 2, width, CapHeight), key);
                    x += width + CapGap;
                }
            }

            UiDrawing.DrawButton(spriteBatch, _backButton, "Back", true, textScale: 0.85f);
        }
    }
}
