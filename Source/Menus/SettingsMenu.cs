using System;
using System.Collections.Generic;
using System.Globalization;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Menus
{
    /// <summary>
    /// Gameplay, graphics and audio settings in two columns. Every change is applied right away and saved.
    /// Works with the mouse or the keyboard: arrows move, Left and Right change sliders, Enter toggles.
    /// </summary>
    public sealed class SettingsMenu
    {
        private const int PanelWidth = 1400;
        private const int ColumnGap = 40;
        private const int ColumnMargin = 60;
        private const float TitleScale = 1.4f;
        private const int RowHeight = 48;
        private const int HeadingGap = 8;
        private const int CategoryGap = 22;
        private const int SliderWidth = 200;
        private const int SliderHeight = 8;
        private const int SliderStep = 5;
        private const int KnobSize = 26;
        private const int CheckboxSize = 26;
        private const int DropdownWidth = 200;
        private const int DropdownRowHeight = 34;
        private const int SmallButtonWidth = 180;
        private const int SmallButtonHeight = 42;

        private static readonly Color Muted = new Color(210, 200, 185);

        private enum Kind
        {
            Checkbox,
            Slider,
            FrameRate,
            Button,
        }

        /// <summary>One labelled control: a checkbox, a slider, the frame rate list or a button.</summary>
        private sealed class Option
        {
            public string Heading;
            public string Label;
            public Kind Kind;
            public int Column;
            public Vector2 LabelPosition;
            public Rectangle Control;
            public Func<bool> IsChecked;
            public Action Toggle;
            public Func<int> Value;
            public Action<int> SetValue;
            public string ButtonText;
            public Action Press;
        }

        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Action _applySettings;
        private readonly Action _resetProgress;
        private readonly Rectangle _panel;
        private readonly Vector2 _titleCenter;
        private readonly List<Option> _options = new List<Option>();
        private readonly List<(string Text, Vector2 Position)> _headings = new List<(string, Vector2)>();
        private readonly Rectangle[] _columnBoxes = new Rectangle[2];
        private readonly Rectangle _backButton;
        private readonly ControlsPanel _controls = new ControlsPanel();
        private readonly PanelMenu _resetConfirm = new PanelMenu("Reset progress?", new Color(255, 90, 80), new[] { "Reset", "Cancel" });

        private int _selected;
        private bool _dropdownOpen;
        private int _hoveredOption = -1;
        private Option _dragging;

        /// <param name="applySettings">Called after any setting changes so the game can apply it.</param>
        /// <param name="resetProgress">Called when the player confirms they want to start over.</param>
        public SettingsMenu(Action applySettings, Action resetProgress)
        {
            _applySettings = applySettings;
            _resetProgress = resetProgress;

            AddOption(0, "Race your best run", Checkbox(() => _settings.ShowRaceGhost, () => _settings.ShowRaceGhost = !_settings.ShowRaceGhost), "Gameplay");
            AddOption(0, "Show timer", Checkbox(() => _settings.ShowTimer, () => _settings.ShowTimer = !_settings.ShowTimer));
            AddOption(0, "Extra hearts (easier)", Checkbox(() => _settings.ExtraHearts, () => _settings.ExtraHearts = !_settings.ExtraHearts));
            AddOption(0, "Unlock all levels", Checkbox(() => _settings.UnlockAllLevels, () => _settings.UnlockAllLevels = !_settings.UnlockAllLevels));
            AddOption(0, "Frame rate", new Option { Kind = Kind.FrameRate }, "Graphics");
            AddOption(0, "Fullscreen", Checkbox(() => _settings.IsFullscreen, () => _settings.IsFullscreen = !_settings.IsFullscreen));
            AddOption(0, "VSync", Checkbox(() => _settings.VSync, () => _settings.VSync = !_settings.VSync));

            AddOption(1, "Game volume", Slider(() => _settings.Volume, value => _settings.Volume = value), "Audio");
            AddOption(1, "Music volume", Slider(() => _settings.MusicVolume, value => _settings.MusicVolume = value));
            AddOption(1, "Mute", Checkbox(() => _settings.IsMuted, () => _settings.IsMuted = !_settings.IsMuted));
            AddOption(1, "Keys and mouse", new Option { Kind = Kind.Button, ButtonText = "Controls", Press = _controls.Open }, "More");
            AddOption(1, "Your progress", new Option
            {
                Kind = Kind.Button,
                ButtonText = "Reset",
                Press = () => _resetConfirm.Open("Your finished levels and best times will be deleted."),
            });

            // Lay out both columns from top to bottom, then size the panel to fit the taller one.
            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            int columnWidth = (PanelWidth - ColumnMargin * 2 - ColumnGap) / 2;
            int columnsTop = (int)(40 + titleHeight + 50);
            int columnsBottom = columnsTop;
            float labelHeight = Assets.SmallFont.LineSpacing;
            for (int column = 0; column < 2; column++)
            {
                int left = ColumnMargin + column * (columnWidth + ColumnGap);
                float y = columnsTop + 24;
                float labelWidth = 0f;
                foreach (Option option in _options)
                {
                    if (option.Column == column)
                        labelWidth = Math.Max(labelWidth, Assets.SmallFont.MeasureString(option.Label).X);
                }
                int controlX = (int)(left + 60 + labelWidth + 30);

                bool first = true;
                foreach (Option option in _options)
                {
                    if (option.Column != column)
                        continue;
                    if (option.Heading != null)
                    {
                        if (!first)
                            y += CategoryGap;
                        _headings.Add((option.Heading, new Vector2(left + 30, y)));
                        y += Assets.Font.LineSpacing + HeadingGap;
                    }
                    first = false;

                    option.LabelPosition = new Vector2(left + 60, y);
                    int rowCenter = (int)(y + labelHeight / 2f);
                    option.Control = option.Kind switch
                    {
                        Kind.Checkbox => new Rectangle(controlX, rowCenter - CheckboxSize / 2, CheckboxSize, CheckboxSize),
                        Kind.Slider => new Rectangle(controlX, rowCenter - SliderHeight / 2, SliderWidth, SliderHeight),
                        Kind.FrameRate => new Rectangle(controlX, rowCenter - DropdownRowHeight / 2, DropdownWidth, DropdownRowHeight),
                        _ => new Rectangle(controlX, rowCenter - SmallButtonHeight / 2, SmallButtonWidth, SmallButtonHeight),
                    };
                    y += RowHeight;
                }

                _columnBoxes[column] = new Rectangle(left, columnsTop, columnWidth, (int)(y - columnsTop) + 10);
                columnsBottom = Math.Max(columnsBottom, _columnBoxes[column].Bottom);
            }
            for (int column = 0; column < 2; column++)
                _columnBoxes[column].Height = columnsBottom - columnsTop;

            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            int panelHeight = columnsBottom + 40 + backHeight + 40;
            _panel = UiDrawing.CenteredPanel(PanelWidth, panelHeight);

            // Everything above was laid out from the panel's top-left corner; move it into place.
            var offset = new Point(_panel.X, _panel.Y);
            foreach (Option option in _options)
            {
                option.LabelPosition += offset.ToVector2();
                option.Control = new Rectangle(option.Control.Location + offset, option.Control.Size);
            }
            for (int i = 0; i < _headings.Count; i++)
                _headings[i] = (_headings[i].Text, _headings[i].Position + offset.ToVector2());
            for (int i = 0; i < _columnBoxes.Length; i++)
                _columnBoxes[i].Offset(offset);

            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + 40 + titleHeight / 2f);
            _backButton = new Rectangle(_panel.Center.X - backWidth / 2, _panel.Bottom - backHeight - 40, backWidth, backHeight);
        }

        /// <param name="heading">Starts a new category above this option.</param>
        private void AddOption(int column, string label, Option option, string heading = null)
        {
            option.Column = column;
            option.Label = label;
            option.Heading = heading;
            _options.Add(option);
        }

        private static Option Checkbox(Func<bool> isChecked, Action toggle) => new Option { Kind = Kind.Checkbox, IsChecked = isChecked, Toggle = toggle };

        private static Option Slider(Func<int> value, Action<int> setValue) => new Option { Kind = Kind.Slider, Value = value, SetValue = setValue };

        /// <summary>Selection index of the Back button, after all the options.</summary>
        private int BackIndex => _options.Count;

        /// <summary>Starts with nothing highlighted until the keyboard or mouse is used.</summary>
        public void Open()
        {
            _selected = -1;
            _dropdownOpen = false;
            _dragging = null;
        }

        /// <summary>Returns true when the player wants to leave the settings screen.</summary>
        public bool Update(float deltaTime)
        {
            if (_controls.IsOpen)
            {
                _controls.Update();
                return false;
            }

            if (_resetConfirm.IsOpen)
            {
                int choice = Input.Back ? 1 : _resetConfirm.Update(deltaTime);
                if (choice == 0)
                    _resetProgress();
                if (choice >= 0)
                    _resetConfirm.Close();
                return false;
            }

            if (_dropdownOpen)
            {
                UpdateDropdown();
                return false;
            }

            if (Input.Back)
                return true;

            if (_dragging != null)
            {
                UpdateDrag();
                return false;
            }

            return UpdateKeyboard() || UpdateMouse();
        }

        private bool UpdateKeyboard()
        {
            int count = _options.Count + 1;
            if (Input.MenuUp)
                _selected = _selected < 0 ? 0 : (_selected + count - 1) % count;
            if (Input.MenuDown)
                _selected = _selected < 0 ? 0 : (_selected + 1) % count;

            int direction = (Input.WasPressed(Keys.Right) || Input.WasPressed(Keys.D) ? 1 : 0) - (Input.WasPressed(Keys.Left) || Input.WasPressed(Keys.A) ? 1 : 0);
            if (direction != 0 && _selected >= 0 && _selected < _options.Count)
            {
                Option option = _options[_selected];
                if (option.Kind == Kind.Slider)
                    Change(() => option.SetValue(Math.Clamp(option.Value() + direction * SliderStep, 0, 100)));
                else if (option.Kind == Kind.FrameRate)
                    Change(() => _settings.FrameRateLimit = NextFrameRate(direction));
                else
                    _selected = ClosestInColumn(1 - option.Column, option.LabelPosition.Y);
            }

            if (!Input.MenuConfirm || _selected < 0)
                return false;
            if (_selected == BackIndex)
                return true;

            Use(_options[_selected]);
            return false;
        }

        private bool UpdateMouse()
        {
            for (int i = 0; i < _options.Count; i++)
            {
                if (Input.MouseMoved && Input.IsMouseOver(RowArea(_options[i])))
                    _selected = i;
            }
            if (Input.MouseMoved && Input.IsMouseOver(_backButton))
                _selected = BackIndex;

            if (!Input.WasClicked)
                return false;
            if (Input.IsMouseOver(_backButton))
                return true;

            foreach (Option option in _options)
            {
                if (!Input.IsMouseOver(ClickArea(option.Control)))
                    continue;

                if (option.Kind == Kind.Slider)
                {
                    _dragging = option;
                    UpdateDrag();
                }
                else if (option.Kind == Kind.FrameRate)
                {
                    _dropdownOpen = true;
                }
                else
                {
                    Use(option);
                }
                break;
            }
            return false;
        }

        /// <summary>What Enter (or a click) does: toggle a checkbox, step the frame rate, or press a button.</summary>
        private void Use(Option option)
        {
            switch (option.Kind)
            {
                case Kind.Checkbox:
                    Change(option.Toggle);
                    break;
                case Kind.FrameRate:
                    Change(() => _settings.FrameRateLimit = NextFrameRate(1));
                    break;
                case Kind.Button:
                    option.Press();
                    break;
            }
        }

        private int NextFrameRate(int direction)
        {
            int[] options = GameSettings.FrameRateOptions;
            int index = Array.IndexOf(options, _settings.FrameRateLimit);
            return options[(index + direction + options.Length) % options.Length];
        }

        private int ClosestInColumn(int column, float y)
        {
            int best = _selected;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _options.Count; i++)
            {
                float distance = Math.Abs(_options[i].LabelPosition.Y - y);
                if (_options[i].Column == column && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private void UpdateDropdown()
        {
            _hoveredOption = -1;
            for (int i = 0; i < GameSettings.FrameRateOptions.Length; i++)
            {
                if (Input.IsMouseOver(DropdownOption(i)))
                    _hoveredOption = i;
            }

            if (Input.Back)
            {
                _dropdownOpen = false;
            }
            else if (Input.WasClicked)
            {
                if (_hoveredOption >= 0)
                {
                    int frameRate = GameSettings.FrameRateOptions[_hoveredOption];
                    Change(() => _settings.FrameRateLimit = frameRate);
                }
                _dropdownOpen = false;
            }
        }

        private void UpdateDrag()
        {
            Rectangle slider = _dragging.Control;
            _dragging.SetValue((int)MathF.Round(MathHelper.Clamp((Input.MousePosition.X - slider.X) / slider.Width, 0f, 1f) * 100f));
            _applySettings();

            if (!Input.IsMouseDown)
            {
                _dragging = null;
                _settings.Save();
            }
        }

        private void Change(Action change)
        {
            change();
            _settings.Save();
            _applySettings();
        }

        /// <summary>Controls are easier to hit with a bit of extra room around them.</summary>
        private static Rectangle ClickArea(Rectangle control)
        {
            Rectangle area = control;
            area.Inflate(KnobSize / 2 + 4, Math.Max(0, (KnobSize - control.Height) / 2 + 4));
            return area;
        }

        /// <summary>The whole row of an option, from its label to its control.</summary>
        private Rectangle RowArea(Option option)
        {
            Rectangle column = _columnBoxes[option.Column];
            return new Rectangle(column.X, (int)option.LabelPosition.Y - (RowHeight - Assets.SmallFont.LineSpacing) / 2, column.Width, RowHeight);
        }

        private Option FrameRateOption => _options.Find(option => option.Kind == Kind.FrameRate);

        private Rectangle DropdownOption(int index)
        {
            Rectangle dropdown = FrameRateOption.Control;
            return new Rectangle(dropdown.X, dropdown.Bottom + index * DropdownRowHeight, DropdownWidth, DropdownRowHeight);
        }

        private static string FrameRateLabel(int frameRate) => frameRate.ToString(CultureInfo.InvariantCulture) + " FPS";

        private bool IsSelected(Option option) => !_dropdownOpen && _selected >= 0 && _selected < _options.Count && _options[_selected] == option;

        public void Draw(SpriteBatch spriteBatch)
        {
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Settings", _titleCenter, Color.White, TitleScale);

            foreach (Rectangle box in _columnBoxes)
            {
                spriteBatch.Draw(Assets.Pixel, box, Color.Black * 0.3f);
                UiDrawing.DrawOutline(spriteBatch, box, Color.White * 0.35f);
            }
            foreach ((string text, Vector2 position) in _headings)
                UiDrawing.DrawText(spriteBatch, text, position, UiDrawing.Highlight);

            foreach (Option option in _options)
            {
                bool selected = IsSelected(option) || _dragging == option || (_dropdownOpen && option.Kind == Kind.FrameRate);
                if (selected)
                    spriteBatch.Draw(Assets.Pixel, RowArea(option), Color.White * 0.08f);
                UiDrawing.DrawText(spriteBatch, Assets.SmallFont, option.Label, option.LabelPosition, selected ? UiDrawing.Highlight : Color.White);

                switch (option.Kind)
                {
                    case Kind.Checkbox:
                        DrawCheckbox(spriteBatch, option.Control, option.IsChecked());
                        break;
                    case Kind.Slider:
                        DrawSlider(spriteBatch, option.Control, option.Value());
                        break;
                    case Kind.Button:
                        UiDrawing.DrawButton(spriteBatch, option.Control, option.ButtonText, selected, textScale: 0.62f);
                        break;
                }
            }

            UiDrawing.DrawButton(spriteBatch, _backButton, "Back", !_dropdownOpen && _selected == BackIndex, textScale: 0.85f);

            // Drawn last so the open list appears on top of the rows below it.
            DrawDropdown(spriteBatch, FrameRateOption);
            _resetConfirm.Draw(spriteBatch);
            _controls.Draw(spriteBatch);
        }

        private static void DrawCheckbox(SpriteBatch spriteBatch, Rectangle box, bool isChecked)
        {
            spriteBatch.Draw(Assets.Pixel, box, Color.Black * 0.5f);
            UiDrawing.DrawOutline(spriteBatch, box, Muted);
            if (isChecked)
            {
                Rectangle check = box;
                check.Inflate(-6, -6);
                spriteBatch.Draw(Assets.Pixel, check, UiDrawing.Highlight);
            }
        }

        private static void DrawSlider(SpriteBatch spriteBatch, Rectangle track, int value)
        {
            int filled = track.Width * value / 100;
            spriteBatch.Draw(Assets.Pixel, track, Muted * 0.35f);
            spriteBatch.Draw(Assets.Pixel, new Rectangle(track.X, track.Y, filled, track.Height), UiDrawing.Highlight);

            var knobCenter = new Vector2(track.X + filled, track.Center.Y);
            float scale = (KnobSize + 4f) / Assets.Circle.Width;
            var origin = new Vector2(Assets.Circle.Width / 2f);
            spriteBatch.Draw(Assets.Circle, knobCenter, null, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Assets.Circle, knobCenter, null, Color.White, 0f, origin, (float)KnobSize / Assets.Circle.Width, SpriteEffects.None, 0f);

            string text = value.ToString(CultureInfo.InvariantCulture) + "%";
            float textHeight = Assets.SmallFont.LineSpacing;
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2(track.Right + 26, track.Center.Y - textHeight / 2f), Color.White);
        }

        private void DrawDropdown(SpriteBatch spriteBatch, Option option)
        {
            Rectangle dropdown = option.Control;
            spriteBatch.Draw(Assets.Pixel, dropdown, Color.Black * 0.6f);
            UiDrawing.DrawOutline(spriteBatch, dropdown, _dropdownOpen || IsSelected(option) ? UiDrawing.Highlight : Muted);
            DrawOptionText(spriteBatch, FrameRateLabel(_settings.FrameRateLimit), dropdown, Color.White);

            // A small arrow on the right shows that this is a list.
            var arrow = new Vector2(dropdown.Right - 22, dropdown.Center.Y);
            for (int row = 0; row < 6; row++)
                spriteBatch.Draw(Assets.Pixel, new Rectangle((int)arrow.X - 6 + row, (int)arrow.Y - 3 + row, 12 - row * 2, 1), Muted);

            if (!_dropdownOpen)
                return;

            for (int i = 0; i < GameSettings.FrameRateOptions.Length; i++)
            {
                Rectangle row = DropdownOption(i);
                spriteBatch.Draw(Assets.Pixel, row, i == _hoveredOption ? new Color(90, 65, 40) : new Color(50, 35, 25));
                UiDrawing.DrawOutline(spriteBatch, row, Muted, 1);
                Color color = GameSettings.FrameRateOptions[i] == _settings.FrameRateLimit ? UiDrawing.Highlight : Color.White;
                DrawOptionText(spriteBatch, FrameRateLabel(GameSettings.FrameRateOptions[i]), row, color);
            }
        }

        private static void DrawOptionText(SpriteBatch spriteBatch, string text, Rectangle area, Color color)
        {
            float textHeight = Assets.SmallFont.LineSpacing;
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2(area.X + 12, area.Center.Y - textHeight / 2f), color);
        }
    }
}
