using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    public enum LevelSelectChoice
    {
        None,
        Back,
        Level,
    }

    /// <summary>
    /// A grid of numbered crates on a wooden panel, one per level, plus a Back button. Finished levels get a star
    /// and their best time and medal, levels after the next unfinished one are locked, and the selected level's
    /// name, description, medal times and gems are shown below the grid.
    /// </summary>
    public sealed class LevelSelectMenu
    {
        private const string Title = "Choose a level";
        private const float TitleScale = 1.4f;
        private const float LogoScale = 0.384f;
        private const int PanelWidth = 1100;
        private const int PanelTop = 205;
        private const int Padding = 30;
        private const int BoxSize = 120;
        private const int Gap = 40;
        private const int StarSize = 46;
        private const int InfoHeight = 190;
        private const int MedalSize = 26;
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
        private readonly float?[] _bestTimes;
        private readonly MedalTimes[] _medalTimes;
        private readonly int[] _gemCounts;
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
            _bestTimes = new float?[levels.Count];
            _medalTimes = new MedalTimes[levels.Count];
            _gemCounts = new int[levels.Count];
            _descriptions = new List<string>[levels.Count];

            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            int top = PanelTop + Padding;
            _titleCenter = new Vector2(VirtualScreen.Width / 2f, top + titleHeight / 2f);
            _summaryCenter = new Vector2(VirtualScreen.Width / 2f, top + titleHeight + 8 + Assets.SmallFont.LineSpacing / 2f);

            // Five crates to a row, or six once there are more than ten levels. If the rows still don't fit
            // on the screen, the crates get smaller.
            _columns = Math.Max(1, Math.Min(levels.Count > 10 ? 6 : 5, levels.Count));
            int rows = Math.Max(1, (levels.Count + _columns - 1) / _columns);
            int gridTop = (int)(_summaryCenter.Y + Assets.SmallFont.LineSpacing / 2f + 28);
            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            int belowGrid = 40 + 10 + InfoHeight + 20 + backHeight + Padding;
            float available = VirtualScreen.Height - 10 - gridTop - belowGrid;
            float scale = Math.Min(1f, available / (rows * BoxSize + (rows - 1) * Gap));
            int boxSize = (int)(BoxSize * scale), gap = (int)(Gap * scale);

            // Center the grid of crates in the panel.
            int gridWidth = _columns * boxSize + (_columns - 1) * gap;
            int left = VirtualScreen.Width / 2 - gridWidth / 2;
            _levelBoxes = new Rectangle[levels.Count];
            for (int i = 0; i < levels.Count; i++)
                _levelBoxes[i] = new Rectangle(left + i % _columns * (boxSize + gap), gridTop + i / _columns * (boxSize + gap), boxSize, boxSize);

            // Below the grid (and the best times under the last row): the selected level's details, then Back.
            int gridBottom = gridTop + rows * boxSize + (rows - 1) * gap + 40;
            _info = new Rectangle(VirtualScreen.Width / 2 - (PanelWidth - Padding * 4) / 2, gridBottom + 10, PanelWidth - Padding * 4, InfoHeight);

            _backButton = new Rectangle(VirtualScreen.Width / 2 - backWidth / 2, _info.Bottom + 20, backWidth, backHeight);
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
                _medalTimes[i] = null;
                _gemCounts[i] = 0;
                _descriptions[i] = new List<string>();
                try
                {
                    LevelData level = LevelData.Load(_levels[i]);
                    _titles[i] = level.Title;
                    if (level.Description != null)
                        _descriptions[i] = UiDrawing.WrapText(Assets.SmallFont, Controls.FillIn(level.Description), descriptionWidth);

                    _bestTimes[i] = RunRecording.LoadBestTime(level.Number, level.ContentHash);
                    _medalTimes[i] = level.Medals;
                    _gemCounts[i] = level.GemCount;
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

            bool left = Input.MenuLeft;
            bool right = Input.MenuRight;

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
            UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, Summary(), _summaryCenter, Muted);

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

            if (_bestTimes[index] is float best)
            {
                // The best time under the crate, with the medal it earned in front of it.
                string text = FormatTime(best);
                var below = new Vector2(_levelBoxes[index].Center.X, _levelBoxes[index].Bottom + 24);
                Medal medal = MedalFor(index);
                if (medal != Medal.None)
                {
                    below.X += MedalSize / 2f + 2;
                    float textWidth = Assets.SmallFont.MeasureString(text).X;
                    UiDrawing.DrawMedal(spriteBatch, new Vector2(below.X - textWidth / 2f - MedalSize / 2f - 6, below.Y + 2), MedalSize, medal);
                }
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, text, below, Color.White * 0.85f);
            }
        }

        private Medal MedalFor(int index)
        {
            return _bestTimes[index] is float best && _medalTimes[index] != null ? _medalTimes[index].For(best) : Medal.None;
        }

        private static string FormatTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return time.ToString(time.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>"4 of 10 levels finished  ·  Gems 7/27  ·  Best times add up to 3:12".</summary>
        private string Summary()
        {
            string summary = $"{CompletedCount} of {_levels.Count} levels finished";
            int gems = _gemCounts.Sum();
            if (gems > 0)
                summary += $"  ·  Gems {_levels.Sum(level => _progress.GemsFoundIn(level).Count)}/{gems}";
            if (_bestTimes.Any(time => time != null))
                summary += "  ·  Best times add up to " + FormatTime(_bestTimes.Sum(time => time ?? 0f));
            return summary;
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

        /// <summary>The selected level's name, whether it is finished or locked, its description, medal times and gems.</summary>
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
                status = _bestTimes[index] is float best ? "Finished  -  best time " + FormatTime(best) : "Finished";
            }
            else
            {
                status = "Not finished yet";
            }
            Vector2 statusSize = Assets.SmallFont.MeasureString(status);
            var statusPosition = new Vector2(_info.Right - Padding - statusSize.X, y + (titleHeight - statusSize.Y) / 2f);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, status, statusPosition, statusColor);
            y += titleHeight + 8;

            float bottomRow = _info.Bottom - 12 - Assets.SmallFont.LineSpacing;
            foreach (string line in _descriptions[index])
            {
                if (y + Assets.SmallFont.LineSpacing > bottomRow - 4)
                    break;
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, line, new Vector2(_info.X + Padding, y), Muted);
                y += Assets.SmallFont.LineSpacing;
            }

            DrawMedalTimes(spriteBatch, index, bottomRow);
            if (_gemCounts[index] > 0)
            {
                // How many of the level's gems have been found, on the right of the bottom row.
                string gems = $"Gems {_progress.GemsFoundIn(_levels[index]).Count}/{_gemCounts[index]}";
                Vector2 size = Assets.SmallFont.MeasureString(gems);
                var position = new Vector2(_info.Right - Padding - size.X, bottomRow);
                Gem.DrawIcon(spriteBatch, new Vector2(position.X - 20, position.Y + size.Y / 2f), 24f);
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, gems, position, Color.White);
            }
        }

        /// <summary>The three medal times. The medals already earned are bright, the others faded.</summary>
        private void DrawMedalTimes(SpriteBatch spriteBatch, int index, float y)
        {
            MedalTimes times = _medalTimes[index];
            if (times == null)
                return;

            Medal earned = MedalFor(index);
            float x = _info.X + Padding;
            foreach ((Medal medal, float seconds) in new[] { (Medal.Gold, times.Gold), (Medal.Silver, times.Silver), (Medal.Bronze, times.Bronze) })
            {
                float opacity = earned >= medal ? 1f : 0.4f;
                string text = FormatTime(seconds);
                UiDrawing.DrawMedal(spriteBatch, new Vector2(x + MedalSize / 2f, y + Assets.SmallFont.LineSpacing / 2f + 2), MedalSize, medal, opacity);
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2(x + MedalSize + 8, y), Color.White * (0.4f + 0.6f * opacity));
                x += MedalSize + 8 + Assets.SmallFont.MeasureString(text).X + 28;
            }
        }
    }
}
