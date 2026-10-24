using System;
using System.Globalization;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>Graphics and audio settings. Every change is applied right away and saved.</summary>
    public sealed class SettingsMenu
    {
        private const int PanelSize = 800;
        private const float TitleScale = 1.5f;
        private const float LabelScale = 0.5f;
        private const int CategoryHeight = 150;
        private const int RowHeight = 40;
        private const int SliderWidth = 200;
        private const int SliderHeight = 4;
        private const int KnobSize = 16;
        private const int CheckboxSize = 20;
        private const int DropdownWidth = 200;
        private const int DropdownRowHeight = 30;

        private enum Slider
        {
            None,
            Volume,
            Music,
        }

        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Action _applySettings;
        private readonly Rectangle _panel = MenuDrawing.CenteredPanel(PanelSize, PanelSize);

        private readonly Vector2 _titlePosition;
        private readonly Rectangle _categoriesBox;
        private readonly Vector2 _graphicsHeading;
        private readonly Vector2 _audioHeading;
        private readonly Vector2 _frameRateLabel;
        private readonly Vector2 _fullscreenLabel;
        private readonly Vector2 _volumeLabel;
        private readonly Vector2 _musicLabel;
        private readonly Vector2 _muteLabel;
        private readonly Rectangle _dropdown;
        private readonly Rectangle _fullscreenBox;
        private readonly Rectangle _volumeSlider;
        private readonly Rectangle _musicSlider;
        private readonly Rectangle _muteBox;
        private readonly Rectangle _backButton;

        private bool _dropdownOpen;
        private int _hoveredOption = -1;
        private Slider _dragging;

        /// <param name="applySettings">Called after any setting changes so the game can apply it.</param>
        public SettingsMenu(Action applySettings)
        {
            _applySettings = applySettings;

            Vector2 titleSize = Assets.Font.MeasureString("Settings") * TitleScale;
            _titlePosition = new Vector2(_panel.X + (_panel.Width - titleSize.X) / 2f, _panel.Y + 30);

            var categoriesStart = new Vector2(_panel.X + 60, _titlePosition.Y + titleSize.Y + 60);
            _categoriesBox = new Rectangle((int)categoriesStart.X - 20, (int)categoriesStart.Y - 20, _panel.Width - 80, 2 * CategoryHeight + 60);
            _graphicsHeading = categoriesStart;
            _audioHeading = categoriesStart + new Vector2(0, CategoryHeight);

            float headingHeight = Assets.Font.MeasureString("Audio").Y;
            Vector2 graphicsRows = _graphicsHeading + new Vector2(30, headingHeight + 10);
            Vector2 audioRows = _audioHeading + new Vector2(30, headingHeight + 10);

            _frameRateLabel = graphicsRows;
            _fullscreenLabel = graphicsRows + new Vector2(0, RowHeight);
            _volumeLabel = audioRows;
            _musicLabel = audioRows + new Vector2(0, RowHeight);
            _muteLabel = audioRows + new Vector2(0, 2 * RowHeight);

            // All controls line up in one column to the right of the longest label.
            float labelWidth = Assets.Font.MeasureString("Music volume:").X * LabelScale;
            int controlX = (int)(graphicsRows.X + labelWidth + 30);
            float labelHeight = Assets.Font.LineSpacing * LabelScale;
            int RowCenter(Vector2 label) => (int)(label.Y + labelHeight / 2f);

            _dropdown = new Rectangle(controlX, RowCenter(_frameRateLabel) - DropdownRowHeight / 2, DropdownWidth, DropdownRowHeight);
            _fullscreenBox = new Rectangle(controlX, RowCenter(_fullscreenLabel) - CheckboxSize / 2, CheckboxSize, CheckboxSize);
            _volumeSlider = new Rectangle(controlX, RowCenter(_volumeLabel) - SliderHeight / 2, SliderWidth, SliderHeight);
            _musicSlider = new Rectangle(controlX, RowCenter(_musicLabel) - SliderHeight / 2, SliderWidth, SliderHeight);
            _muteBox = new Rectangle(controlX, RowCenter(_muteLabel) - CheckboxSize / 2, CheckboxSize, CheckboxSize);

            Vector2 backSize = Assets.Font.MeasureString("Back");
            _backButton = new Rectangle((int)(_panel.X + (_panel.Width - backSize.X) / 2f), (int)(_panel.Bottom - backSize.Y - 50), (int)backSize.X, (int)backSize.Y);
        }

        /// <summary>Returns true when the player wants to leave the settings screen.</summary>
        public bool Update()
        {
            if (_dropdownOpen)
            {
                UpdateDropdown();
                return false;
            }

            if (Input.Back)
                return true;

            if (_dragging != Slider.None)
            {
                UpdateDrag();
                return false;
            }

            if (!Input.WasClicked)
                return false;

            if (Input.IsMouseOver(_backButton))
                return true;

            if (Input.IsMouseOver(_dropdown))
                _dropdownOpen = true;
            else if (Input.IsMouseOver(_fullscreenBox))
                Change(() => _settings.IsFullscreen = !_settings.IsFullscreen);
            else if (Input.IsMouseOver(_muteBox))
                Change(() => _settings.IsMuted = !_settings.IsMuted);
            else if (Input.IsMouseOver(SliderHitArea(_volumeSlider)))
                _dragging = Slider.Volume;
            else if (Input.IsMouseOver(SliderHitArea(_musicSlider)))
                _dragging = Slider.Music;

            if (_dragging != Slider.None)
                UpdateDrag();

            return false;
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
            Rectangle slider = _dragging == Slider.Volume ? _volumeSlider : _musicSlider;
            int value = (int)MathF.Round(MathHelper.Clamp((Input.MousePosition.X - slider.X) / slider.Width, 0f, 1f) * 100f);

            if (_dragging == Slider.Volume)
                _settings.Volume = value;
            else
                _settings.MusicVolume = value;
            _applySettings();

            if (!Input.IsMouseDown)
            {
                _dragging = Slider.None;
                _settings.Save();
            }
        }

        private void Change(Action change)
        {
            change();
            _settings.Save();
            _applySettings();
        }

        private static Rectangle SliderHitArea(Rectangle slider)
        {
            Rectangle area = slider;
            area.Inflate(KnobSize, KnobSize);
            return area;
        }

        private Rectangle DropdownOption(int index)
        {
            return new Rectangle(_dropdown.X, _dropdown.Bottom + index * DropdownRowHeight, DropdownWidth, DropdownRowHeight);
        }

        private static string FrameRateLabel(int frameRate) => frameRate.ToString(CultureInfo.InvariantCulture) + " FPS";

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Assets.WoodPanel, _panel, Color.White);
            spriteBatch.Draw(Assets.Pixel, _panel, Color.Black * 0.4f);
            MenuDrawing.DrawText(spriteBatch, "Settings", _titlePosition, Color.White, TitleScale);

            spriteBatch.Draw(Assets.Pixel, _categoriesBox, Color.Black * 0.3f);
            MenuDrawing.DrawOutline(spriteBatch, _categoriesBox, Color.White);
            MenuDrawing.DrawText(spriteBatch, "Graphics", _graphicsHeading, Color.Yellow);
            MenuDrawing.DrawText(spriteBatch, "Audio", _audioHeading, Color.Yellow);

            DrawLabel(spriteBatch, "Frame rate:", _frameRateLabel);
            DrawLabel(spriteBatch, "Fullscreen:", _fullscreenLabel);
            DrawCheckbox(spriteBatch, _fullscreenBox, _settings.IsFullscreen, Color.Green);

            DrawLabel(spriteBatch, "Game volume:", _volumeLabel);
            DrawSlider(spriteBatch, _volumeSlider, _settings.Volume);
            DrawLabel(spriteBatch, "Music volume:", _musicLabel);
            DrawSlider(spriteBatch, _musicSlider, _settings.MusicVolume);
            DrawLabel(spriteBatch, "Mute:", _muteLabel);
            DrawCheckbox(spriteBatch, _muteBox, _settings.IsMuted, Color.Red);

            bool backHovered = !_dropdownOpen && Input.IsMouseOver(_backButton);
            MenuDrawing.DrawText(spriteBatch, "Back", _backButton.Location.ToVector2(), backHovered ? Color.Yellow : Color.White);

            // Drawn last so the open list appears on top of the rows below it.
            DrawDropdown(spriteBatch);
        }

        private static void DrawLabel(SpriteBatch spriteBatch, string text, Vector2 position)
        {
            MenuDrawing.DrawText(spriteBatch, text, position, Color.White, LabelScale);
        }

        private static void DrawCheckbox(SpriteBatch spriteBatch, Rectangle box, bool isChecked, Color checkColor)
        {
            spriteBatch.Draw(Assets.Pixel, box, Color.White);
            if (isChecked)
            {
                Rectangle check = box;
                check.Inflate(-4, -4);
                spriteBatch.Draw(Assets.Pixel, check, checkColor);
            }
        }

        private static void DrawSlider(SpriteBatch spriteBatch, Rectangle track, int value)
        {
            spriteBatch.Draw(Assets.Pixel, track, Color.Gray);

            int knobX = track.X + track.Width * value / 100;
            var knob = new Rectangle(knobX - KnobSize / 2, track.Center.Y - KnobSize / 2, KnobSize, KnobSize);
            spriteBatch.Draw(Assets.Pixel, knob, Color.White);

            string text = value.ToString(CultureInfo.InvariantCulture) + "%";
            float textHeight = Assets.Font.LineSpacing * LabelScale;
            MenuDrawing.DrawText(spriteBatch, text, new Vector2(track.Right + 20, track.Center.Y - textHeight / 2f), Color.White, LabelScale);
        }

        private void DrawDropdown(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Assets.Pixel, _dropdown, Color.Black * 0.7f);
            MenuDrawing.DrawOutline(spriteBatch, _dropdown, Color.White);
            MenuDrawing.DrawText(spriteBatch, FrameRateLabel(_settings.FrameRateLimit), _dropdown.Location.ToVector2() + new Vector2(10, 5), Color.White, LabelScale);

            if (!_dropdownOpen)
                return;

            for (int i = 0; i < GameSettings.FrameRateOptions.Length; i++)
            {
                Rectangle option = DropdownOption(i);
                spriteBatch.Draw(Assets.Pixel, option, i == _hoveredOption ? Color.DarkGray : Color.Gray);
                MenuDrawing.DrawOutline(spriteBatch, option, Color.White);
                MenuDrawing.DrawText(spriteBatch, FrameRateLabel(GameSettings.FrameRateOptions[i]), option.Location.ToVector2() + new Vector2(10, 5), Color.White, LabelScale);
            }
        }
    }
}
