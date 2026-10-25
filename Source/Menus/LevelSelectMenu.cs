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

    /// <summary>
    /// A grid of numbered crates on a wooden panel, one per level, plus a Back button. Finished levels get a star,
    /// levels after the next unfinished one are locked, and the selected level's name, best time and description
    /// are shown below the grid.
    /// </summary>
    public sealed class LevelSelectMenu
    {
        private const string Title = "Choose a level";
        private const float TitleScale = 1.4f;
        private const float LogoScale = 0.384f;
        private const int PanelWidth = 1100;
        private const int PanelTop = 215;
        private const int Padding = 30;
        private const int BoxSize = 120;
        private const int Columns = 5;
        private const int Gap = 40;
        private const int StarSize = 46;
        private const int InfoHeight = 155;
        private const float LockedMessageTime = 1.5f;

        private static readonly Color Muted = new Color(210, 200, 185);
        private static readonly Color LockedTint = new Color(95, 90, 85);
        private static readonly Color Warning = new Color(255, 110, 90);

        private readonly IReadOnlyList<int> _levels;
        private readonly GameProgress _progress;
        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Rectangle _panel;
        private readonly Vector2 _titleCenter;
        private readonly Vector2 _summaryCenter;
        private readonly Rectangle[] _levelBoxes;
        private readonly Rectangle _info;
        private readonly Rectangle _backButton;
        private readonly int _columns;
        private readonly string[] _titles;
        private readonly string[] _bestTimes;
        private readonly List<string>[] _descriptions;
        private int _selected;
        private int _shownLevel;
        private float _lockedMessageTimer;
        private float _time;

        public LevelSelectMenu(IReadOnlyList<int> levels, GameProgress progress)
        {
            _levels = levels;
            _progress = progress;
            _titles = new string[levels.Count];
            _bestTimes = new string[levels.Count];
            _descriptions = new List<string>[levels.Count];

            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            int top = PanelTop + Padding;
            _titleCenter = new Vector2(VirtualScreen.Width / 2f, top + titleHeight / 2f);
            _summaryCenter = new Vector2(VirtualScreen.Width / 2f, top + titleHeight + 8 + Assets.SmallFont.LineSpacing / 2f);

            // Center the grid of crates in the panel.
            _columns = Math.Max(1, Math.Min(Columns, levels.Count));
            int rows = Math.Max(1, (levels.Count + _columns - 1) / _columns);
            int gridWidth = _columns * BoxSize + (_columns - 1) * Gap;
            int left = VirtualScreen.Width / 2 - gridWidth / 2;
            int gridTop = (int)(_summaryCenter.Y + Assets.SmallFont.LineSpacing / 2f + 35);

            _levelBoxes = new Rectangle[levels.Count];
            for (int i = 0; i < levels.Count; i++)
                _levelBoxes[i] = new Rectangle(left + i % _columns * (BoxSize + Gap), gridTop + i / _columns * (BoxSize + Gap), BoxSize, BoxSize);

            // Below the grid (and the best times under the last row): the selected level's details, then Back.
            int gridBottom = gridTop + rows * BoxSize + (rows - 1) * Gap + 40;
            _info = new Rectangle(VirtualScreen.Width / 2 - (PanelWidth - Padding * 4) / 2, gridBottom + 10, PanelWidth - Padding * 4, InfoHeight);

            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            _backButton = new Rectangle(VirtualScreen.Width / 2 - backWidth / 2, _info.Bottom + 25, backWidth, backHeight);
            _panel = new Rectangle(VirtualScreen.Width / 2 - PanelWidth / 2, PanelTop, PanelWidth, _backButton.Bottom + Padding - PanelTop);
        }

        /// <summary>Index of the Back button in the selection order (after all levels).</summary>
        private int BackIndex => _levels.Count;

        /// <summary>The level number picked when <see cref="Update"/> returns <see cref="LevelSelectChoice.Level"/>.</summary>
        public int ChosenLevel { get; private set; }

        /// <summary>A level can be played once the one before it is finished (or always, with "Unlock all levels").</summary>
        private bool IsUnlocked(int index)
        {
            return _settings.UnlockAllLevels || index == 0 || _levels[index - 1] <= _progress.HighestCompletedLevel
                || _levels[index] <= _progress.HighestCompletedLevel;
        }

        private bool IsFinished(int index) => _levels[index] <= _progress.HighestCompletedLevel;

        private int CompletedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _levels.Count; i++)
                {
                    if (IsFinished(i))
                        count++;
                }
                return count;
            }
        }

        public void Open()
        {
            // Start on the first level that isn't finished yet, which is usually the one to play next.
            _selected = 0;
            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                if (!IsFinished(i) && IsUnlocked(i))
                    _selected = i;
            }
            _shownLevel = _selected;
            _lockedMessageTimer = 0f;

            // Names and best times change after playing or editing, so read them again each time the picker opens.
            float descriptionWidth = _info.Width - Padding * 2;
            for (int i = 0; i < _levels.Count; i++)
            {
                _titles[i] = "Level " + _levels[i].ToString(CultureInfo.InvariantCulture);
                _bestTimes[i] = null;
                _descriptions[i] = new List<string>();
                try
                {
                    LevelData level = LevelData.Load(_levels[i]);
                    _titles[i] = level.Title;
                    if (level.Description != null)
                        _descriptions[i] = UiDrawing.WrapText(Assets.SmallFont, level.Description, descriptionWidth);

                    float? best = RunRecording.LoadBestTime(level.Number, level.ContentHash);
                    if (best is float seconds)
                        _bestTimes[i] = TimeSpan.FromSeconds(seconds).ToString(@"m\:ss", CultureInfo.InvariantCulture);
                }
                catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
                {
                    // A broken level just shows its number; the error appears when someone tries to play it.
                    _descriptions[i] = new List<string> { "This level file has a problem. Open it to see what is wrong." };
                }
            }
        }

        public LevelSelectChoice Update(float deltaTime)
        {
            _time += deltaTime;
            _lockedMessageTimer = Math.Max(0f, _lockedMessageTimer - deltaTime);

            MoveWithKeys();
            if (Input.MenuConfirm)
                return ChoiceFor(_selected);

            for (int i = 0; i <= BackIndex; i++)
            {
                Rectangle bounds = i == BackIndex ? _backButton : _levelBoxes[i];
                if (!Input.IsMouseOver(bounds))
                    continue;

                if (Input.MouseMoved)
                    Select(i);
                if (Input.WasClicked)
                    return ChoiceFor(i);
            }

            return LevelSelectChoice.None;
        }

        /// <summary>Arrow keys (or WASD) move around the grid; Down from the bottom row goes to Back.</summary>
        private void MoveWithKeys()
        {
            int count = _levels.Count;
            if (count == 0)
            {
                _selected = BackIndex;
                return;
            }

            bool left = Input.WasPressed(Keys.Left) || Input.WasPressed(Keys.A);
            bool right = Input.WasPressed(Keys.Right) || Input.WasPressed(Keys.D);

            if (_selected == BackIndex)
            {
                if (Input.MenuUp)
                    Select(Math.Min(count - 1, (count - 1) / _columns * _columns + _shownLevel % _columns));
                else if (Input.MenuDown)
                    Select(_shownLevel % _columns);
                return;
            }

            if (left && _selected > 0)
                Select(_selected - 1);
            else if (right && _selected < count - 1)
                Select(_selected + 1);
            else if (Input.MenuUp && _selected >= _columns)
                Select(_selected - _columns);
            else if (Input.MenuUp)
                Select(BackIndex);
            else if (Input.MenuDown && _selected + _columns < count)
                Select(_selected + _columns);
            else if (Input.MenuDown && _selected / _columns < (count - 1) / _columns)
                Select(count - 1);
            else if (Input.MenuDown)
                Select(BackIndex);
        }

        private void Select(int index)
        {
            if (index != _selected)
                _lockedMessageTimer = 0f;
            _selected = index;
            if (index != BackIndex)
                _shownLevel = index;
        }

        private LevelSelectChoice ChoiceFor(int index)
        {
            if (index == BackIndex)
                return LevelSelectChoice.Back;

            if (!IsUnlocked(index))
            {
                _lockedMessageTimer = LockedMessageTime;
                return LevelSelectChoice.None;
            }

            ChosenLevel = _levels[index];
            return LevelSelectChoice.Level;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            UiDrawing.DrawLogo(spriteBatch, LogoScale);
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, Title, _titleCenter, Color.White, TitleScale);
            string summary = $"{CompletedCount} of {_levels.Count} levels finished";
            UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, summary, _summaryCenter, Muted);

            for (int i = 0; i < _levelBoxes.Length; i++)
                DrawCrate(spriteBatch, i);

            DrawInfo(spriteBatch);
            UiDrawing.DrawButton(spriteBatch, _backButton, "Back", _selected == BackIndex, textScale: 0.85f);
        }

        private void DrawCrate(SpriteBatch spriteBatch, int index)
        {
            bool selected = index == _selected;
            bool unlocked = IsUnlocked(index);
            Rectangle box = _levelBoxes[index];

            // The next level to play pulses gently, and the selected crate glows.
            bool isNext = unlocked && !IsFinished(index);
            if (selected || isNext)
            {
                float pulse = selected ? 0.5f : 0.18f + 0.12f * MathF.Sin(_time * 3f);
                Rectangle glowBox = box;
                glowBox.Inflate(8, 8);
                float glowScale = glowBox.Width * 2.2f / Assets.Glow.Width;
                spriteBatch.Draw(Assets.Glow, glowBox.Center.ToVector2(), null, UiDrawing.Highlight * pulse, 0f, new Vector2(Assets.Glow.Width / 2f), glowScale, SpriteEffects.None, 0f);
            }
            if (selected)
                box.Inflate(8, 8);

            spriteBatch.Draw(Assets.LevelBox, box, unlocked ? Color.White : LockedTint);
            string number = _levels[index].ToString(CultureInfo.InvariantCulture);
            Color numberColor = !unlocked ? Color.White * 0.45f : selected ? UiDrawing.Highlight : Color.White;
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, number, box.Center.ToVector2(), numberColor, selected ? 1.3f : 1.15f);

            if (!unlocked)
                DrawPadlock(spriteBatch, new Vector2(box.Right - 18, box.Bottom - 22));

            if (IsFinished(index))
            {
                var star = new Rectangle(box.Right - StarSize + 12, box.Y - 12, StarSize, StarSize);
                spriteBatch.Draw(Assets.Star, star, Color.White);
            }

            if (_bestTimes[index] != null)
            {
                var below = new Vector2(_levelBoxes[index].Center.X, _levelBoxes[index].Bottom + 24);
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, _bestTimes[index], below, Color.White * 0.85f);
            }
        }

        /// <summary>A small gold padlock centered on <paramref name="center"/>.</summary>
        private static void DrawPadlock(SpriteBatch spriteBatch, Vector2 center)
        {
            var gold = new Color(240, 200, 90);
            var dark = new Color(60, 40, 20);
            var body = new Rectangle((int)center.X - 15, (int)center.Y - 6, 30, 24);

            // The shackle: two posts and a bar across the top.
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.X + 4, body.Y - 16, 5, 16), dark);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.Right - 9, body.Y - 16, 5, 16), dark);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.X + 4, body.Y - 18, body.Width - 8, 5), dark);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.X + 5, body.Y - 15, 3, 15), gold);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.Right - 8, body.Y - 15, 3, 15), gold);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.X + 5, body.Y - 17, body.Width - 10, 3), gold);

            spriteBatch.Draw(Assets.Pixel, body, gold);
            UiDrawing.DrawOutline(spriteBatch, body, dark);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(body.Center.X - 2, body.Y + 7, 4, 9), dark);
        }

        /// <summary>The selected level's name, whether it is finished or locked, and its description.</summary>
        private void DrawInfo(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Assets.Pixel, _info, Color.Black * 0.3f);
            UiDrawing.DrawOutline(spriteBatch, _info, Color.White * 0.35f);
            if (_levels.Count == 0)
            {
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, "No levels found in the Levels folder.", _info.Center.ToVector2(), Color.White);
                return;
            }

            int index = _shownLevel;
            float y = _info.Y + 14;
            float titleHeight = Assets.Font.LineSpacing * 0.9f;
            UiDrawing.DrawText(spriteBatch, _titles[index], new Vector2(_info.X + Padding, y), UiDrawing.Highlight, 0.9f);

            // Finished, not finished or locked goes on the right of the title line.
            string status;
            Color statusColor = Color.White;
            if (!IsUnlocked(index))
            {
                status = $"Locked: finish level {_levels[index - 1]} to unlock it";
                if (_lockedMessageTimer > 0f)
                    statusColor = Warning;
            }
            else if (IsFinished(index))
            {
                status = _bestTimes[index] == null ? "Finished" : "Finished  -  best time " + _bestTimes[index];
            }
            else
            {
                status = "Not finished yet";
            }
            Vector2 statusSize = Assets.SmallFont.MeasureString(status);
            var statusPosition = new Vector2(_info.Right - Padding - statusSize.X, y + (titleHeight - statusSize.Y) / 2f);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, status, statusPosition, statusColor);
            y += titleHeight + 8;

            foreach (string line in _descriptions[index])
            {
                if (y + Assets.SmallFont.LineSpacing > _info.Bottom - 8)
                    break;
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, line, new Vector2(_info.X + Padding, y), Muted);
                y += Assets.SmallFont.LineSpacing;
            }
        }
    }
}
