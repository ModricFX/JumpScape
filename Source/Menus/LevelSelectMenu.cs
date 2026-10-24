using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JumpScape.Core;
using JumpScape.Gameplay;
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

    /// <summary>A grid of numbered crates on a wooden panel, one per level, plus a Back button. Finished levels get a star.</summary>
    public sealed class LevelSelectMenu
    {
        private const string Title = "Choose a level";
        private const float TitleScale = 1.4f;
        private const float LogoScale = 0.384f;
        private const int BoxSize = 120;
        private const int Columns = 5;
        private const int Gap = 50;
        private const int StarSize = 46;

        private readonly IReadOnlyList<int> _levels;
        private readonly GameProgress _progress;
        private readonly Rectangle _panel = UiDrawing.CenteredPanel(VirtualScreen.Width / 2, VirtualScreen.Height / 2);
        private readonly Vector2 _titleCenter;
        private readonly Rectangle[] _levelBoxes;
        private readonly Rectangle _backButton;
        private readonly string[] _bestTimes;
        private int _selected;

        public LevelSelectMenu(IReadOnlyList<int> levels, GameProgress progress)
        {
            _levels = levels;
            _progress = progress;
            _bestTimes = new string[levels.Count];
            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + 40 + Assets.Font.LineSpacing * TitleScale / 2f);

            // Center the grid of crates in the panel.
            int columns = Math.Max(1, Math.Min(Columns, levels.Count));
            int gridWidth = columns * BoxSize + (columns - 1) * Gap;
            int left = _panel.Center.X - gridWidth / 2;
            int top = (int)(_titleCenter.Y + Assets.Font.LineSpacing * TitleScale / 2f + 50);

            _levelBoxes = new Rectangle[levels.Count];
            for (int i = 0; i < levels.Count; i++)
                _levelBoxes[i] = new Rectangle(left + i % columns * (BoxSize + Gap), top + i / columns * (BoxSize + Gap), BoxSize, BoxSize);

            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            _backButton = new Rectangle(_panel.Center.X - backWidth / 2, _panel.Bottom - backHeight - 40, backWidth, backHeight);
        }

        /// <summary>Index of the Back button in the selection order (after all levels).</summary>
        private int BackIndex => _levels.Count;

        /// <summary>The level number picked when <see cref="Update"/> returns <see cref="LevelSelectChoice.Level"/>.</summary>
        public int ChosenLevel { get; private set; }

        public void Open()
        {
            _selected = 0;

            // Best times change after playing, so read them again each time the picker opens.
            for (int i = 0; i < _levels.Count; i++)
            {
                _bestTimes[i] = null;
                try
                {
                    LevelData level = LevelData.Load(_levels[i]);
                    float? best = RunRecording.LoadBestTime(level.Number, level.ContentHash);
                    if (best is float seconds)
                        _bestTimes[i] = TimeSpan.FromSeconds(seconds).ToString(@"m\:ss", CultureInfo.InvariantCulture);
                }
                catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
                {
                    // A broken level just shows no best time; the error appears when someone tries to play it.
                }
            }
        }

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
                Rectangle bounds = i == BackIndex ? _backButton : _levelBoxes[i];
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
            UiDrawing.DrawLogo(spriteBatch, LogoScale);
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, Title, _titleCenter, Color.White, TitleScale);

            for (int i = 0; i < _levelBoxes.Length; i++)
            {
                bool selected = i == _selected;
                Rectangle box = _levelBoxes[i];
                if (selected)
                {
                    box.Inflate(8, 8);
                    float glowScale = box.Width * 2.2f / Assets.Glow.Width;
                    spriteBatch.Draw(Assets.Glow, box.Center.ToVector2(), null, UiDrawing.Highlight * 0.5f, 0f, new Vector2(Assets.Glow.Width / 2f), glowScale, SpriteEffects.None, 0f);
                }

                spriteBatch.Draw(Assets.LevelBox, box, Color.White);
                string number = _levels[i].ToString(CultureInfo.InvariantCulture);
                UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, number, box.Center.ToVector2(), selected ? UiDrawing.Highlight : Color.White, selected ? 1.3f : 1.15f);

                if (_levels[i] <= _progress.HighestCompletedLevel)
                {
                    var star = new Rectangle(box.Right - StarSize + 12, box.Y - 12, StarSize, StarSize);
                    spriteBatch.Draw(Assets.Star, star, Color.White);
                }

                if (_bestTimes[i] != null)
                {
                    var below = new Vector2(_levelBoxes[i].Center.X, _levelBoxes[i].Bottom + 24);
                    UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, _bestTimes[i], below, Color.White * 0.85f);
                }
            }

            UiDrawing.DrawButton(spriteBatch, _backButton, "Back", _selected == BackIndex, textScale: 0.85f);
        }
    }
}
