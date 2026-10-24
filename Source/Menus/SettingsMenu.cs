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
        private const float TitleScale = 1.4f;
        private const int RowHeight = 48;
        private const int HeadingGap = 8;
        private const int CategoryGap = 22;
        private const int SliderWidth = 240;
        private const int SliderHeight = 8;
        private const int KnobSize = 26;
        private const int CheckboxSize = 26;
        private const int DropdownWidth = 200;
        private const int DropdownRowHeight = 34;

        private static readonly Color Muted = new Color(210, 200, 185);

        private const string GhostText = "Race your best run";

        private enum Slider
        {
            None,
            Volume,
            Music,
        }

        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Action _applySettings;
        private readonly Rectangle _panel = UiDrawing.CenteredPanel(PanelSize, PanelSize);

        private readonly Vector2 _titleCenter;
        private readonly Rectangle _categoriesBox;
        private readonly Vector2 _gameplayHeading;
        private readonly Vector2 _ghostLabel;
        private readonly Vector2 _graphicsHeading;
        private readonly Vector2 _audioHeading;
        private readonly Vector2 _frameRateLabel;
        private readonly Vector2 _fullscreenLabel;
        private readonly Vector2 _volumeLabel;
        private readonly Vector2 _musicLabel;
        private readonly Vector2 _muteLabel;
        private readonly Rectangle _ghostBox;
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

            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + 40 + titleHeight / 2f);

            // Lay out the headings and rows from top to bottom.
            var categoriesStart = new Vector2(_panel.X + 80, _panel.Y + 40 + titleHeight + 50);
            float y = categoriesStart.Y;
            Vector2 Heading()
            {
                var position = new Vector2(categoriesStart.X, y);
                y += Assets.Font.LineSpacing + HeadingGap;
                return position;
            }
            Vector2 Row()
            {
                var position = new Vector2(categoriesStart.X + 30, y);
                y += RowHeight;
                return position;
            }

            _gameplayHeading = Heading();
            _ghostLabel = Row();
            y += CategoryGap;
            _graphicsHeading = Heading();
            _frameRateLabel = Row();
            _fullscreenLabel = Row();
            y += CategoryGap;
            _audioHeading = Heading();
            _volumeLabel = Row();
            _musicLabel = Row();
            _muteLabel = Row();
            _categoriesBox = new Rectangle((int)categoriesStart.X - 30, (int)categoriesStart.Y - 24, _panel.Width - 100, (int)(y - categoriesStart.Y) + 34);

            // All controls line up in one column to the right of the longest label.
            float labelWidth = Assets.SmallFont.MeasureString(GhostText).X;
            int controlX = (int)(_ghostLabel.X + labelWidth + 50);
            float labelHeight = Assets.SmallFont.LineSpacing;
            int RowCenter(Vector2 label) => (int)(label.Y + labelHeight / 2f);

            _ghostBox = new Rectangle(controlX, RowCenter(_ghostLabel) - CheckboxSize / 2, CheckboxSize, CheckboxSize);
            _dropdown = new Rectangle(controlX, RowCenter(_frameRateLabel) - DropdownRowHeight / 2, DropdownWidth, DropdownRowHeight);
            _fullscreenBox = new Rectangle(controlX, RowCenter(_fullscreenLabel) - CheckboxSize / 2, CheckboxSize, CheckboxSize);
            _volumeSlider = new Rectangle(controlX, RowCenter(_volumeLabel) - SliderHeight / 2, SliderWidth, SliderHeight);
            _musicSlider = new Rectangle(controlX, RowCenter(_musicLabel) - SliderHeight / 2, SliderWidth, SliderHeight);
            _muteBox = new Rectangle(controlX, RowCenter(_muteLabel) - CheckboxSize / 2, CheckboxSize, CheckboxSize);

            int backWidth = Assets.MenuButton.Width * 3 / 4;
            int backHeight = Assets.MenuButton.Height * 3 / 4;
            _backButton = new Rectangle(_panel.Center.X - backWidth / 2, _panel.Bottom - backHeight - 40, backWidth, backHeight);
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
            else if (Input.IsMouseOver(ClickArea(_ghostBox)))
                Change(() => _settings.ShowRaceGhost = !_settings.ShowRaceGhost);
            else if (Input.IsMouseOver(ClickArea(_fullscreenBox)))
                Change(() => _settings.IsFullscreen = !_settings.IsFullscreen);
            else if (Input.IsMouseOver(ClickArea(_muteBox)))
                Change(() => _settings.IsMuted = !_settings.IsMuted);
            else if (Input.IsMouseOver(ClickArea(_volumeSlider)))
                _dragging = Slider.Volume;
            else if (Input.IsMouseOver(ClickArea(_musicSlider)))
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

        /// <summary>Controls are easier to hit with a bit of extra room around them.</summary>
        private static Rectangle ClickArea(Rectangle control)
        {
            Rectangle area = control;
            area.Inflate(KnobSize / 2 + 4, Math.Max(0, (KnobSize - control.Height) / 2 + 4));
            return area;
        }

        private Rectangle DropdownOption(int index)
        {
            return new Rectangle(_dropdown.X, _dropdown.Bottom + index * DropdownRowHeight, DropdownWidth, DropdownRowHeight);
        }

        private static string FrameRateLabel(int frameRate) => frameRate.ToString(CultureInfo.InvariantCulture) + " FPS";

        private bool IsHovered(Rectangle control) => !_dropdownOpen && (_dragging == Slider.None) && Input.IsMouseOver(ClickArea(control));

        public void Draw(SpriteBatch spriteBatch)
        {
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Settings", _titleCenter, Color.White, TitleScale);

            spriteBatch.Draw(Assets.Pixel, _categoriesBox, Color.Black * 0.3f);
            UiDrawing.DrawOutline(spriteBatch, _categoriesBox, Color.White * 0.35f);
            UiDrawing.DrawText(spriteBatch, "Gameplay", _gameplayHeading, UiDrawing.Highlight);
            UiDrawing.DrawText(spriteBatch, "Graphics", _graphicsHeading, UiDrawing.Highlight);
            UiDrawing.DrawText(spriteBatch, "Audio", _audioHeading, UiDrawing.Highlight);

            DrawLabel(spriteBatch, GhostText, _ghostLabel, IsHovered(_ghostBox));
            DrawCheckbox(spriteBatch, _ghostBox, _settings.ShowRaceGhost);

            DrawLabel(spriteBatch, "Frame rate", _frameRateLabel, IsHovered(_dropdown) || _dropdownOpen);
            DrawLabel(spriteBatch, "Fullscreen", _fullscreenLabel, IsHovered(_fullscreenBox));
            DrawCheckbox(spriteBatch, _fullscreenBox, _settings.IsFullscreen);

            DrawLabel(spriteBatch, "Game volume", _volumeLabel, IsHovered(_volumeSlider) || _dragging == Slider.Volume);
            DrawSlider(spriteBatch, _volumeSlider, _settings.Volume);
            DrawLabel(spriteBatch, "Music volume", _musicLabel, IsHovered(_musicSlider) || _dragging == Slider.Music);
            DrawSlider(spriteBatch, _musicSlider, _settings.MusicVolume);
            DrawLabel(spriteBatch, "Mute", _muteLabel, IsHovered(_muteBox));
            DrawCheckbox(spriteBatch, _muteBox, _settings.IsMuted);

            UiDrawing.DrawButton(spriteBatch, _backButton, "Back", !_dropdownOpen && Input.IsMouseOver(_backButton), textScale: 0.85f);

            // Drawn last so the open list appears on top of the rows below it.
            DrawDropdown(spriteBatch);
        }

        private static void DrawLabel(SpriteBatch spriteBatch, string text, Vector2 position, bool hovered)
        {
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, position, hovered ? UiDrawing.Highlight : Color.White);
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

        private void DrawDropdown(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Assets.Pixel, _dropdown, Color.Black * 0.6f);
            UiDrawing.DrawOutline(spriteBatch, _dropdown, _dropdownOpen || IsHovered(_dropdown) ? UiDrawing.Highlight : Muted);
            DrawOptionText(spriteBatch, FrameRateLabel(_settings.FrameRateLimit), _dropdown, Color.White);

            // A small arrow on the right shows that this is a list.
            var arrow = new Vector2(_dropdown.Right - 22, _dropdown.Center.Y);
            for (int row = 0; row < 6; row++)
                spriteBatch.Draw(Assets.Pixel, new Rectangle((int)arrow.X - 6 + row, (int)arrow.Y - 3 + row, 12 - row * 2, 1), Muted);

            if (!_dropdownOpen)
                return;

            for (int i = 0; i < GameSettings.FrameRateOptions.Length; i++)
            {
                Rectangle option = DropdownOption(i);
                spriteBatch.Draw(Assets.Pixel, option, i == _hoveredOption ? new Color(90, 65, 40) : new Color(50, 35, 25));
                UiDrawing.DrawOutline(spriteBatch, option, Muted, 1);
                Color color = GameSettings.FrameRateOptions[i] == _settings.FrameRateLimit ? UiDrawing.Highlight : Color.White;
                DrawOptionText(spriteBatch, FrameRateLabel(GameSettings.FrameRateOptions[i]), option, color);
            }
        }

        private static void DrawOptionText(SpriteBatch spriteBatch, string text, Rectangle area, Color color)
        {
            float textHeight = Assets.SmallFont.LineSpacing;
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2(area.X + 12, area.Center.Y - textHeight / 2f), color);
        }
    }
}
