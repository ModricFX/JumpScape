using System;
using System.Collections.Generic;
using System.Linq;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Menus
{
    /// <summary>
    /// Lists every action and its keys, and lets the player choose their own: click a box (or select it and
    /// press Enter), then press the new key. Backspace clears the selected box. Opened from the settings and
    /// the pause menu. Throwing, the inventory slots and pause can't be changed.
    /// </summary>
    public sealed class ControlsPanel
    {
        private const int Width = 1120;
        private const int Padding = 40;
        private const int RowHeight = 56;
        private const int BoxWidth = 170;
        private const int BoxHeight = 44;
        private const int BoxGap = 16;
        private const float TitleScale = 1.4f;
        private const float DimAmount = 0.55f;
        private const string DefaultMessage = "Click a box, then press the key you want. Backspace clears the selected box.";

        private static readonly GameAction[] Actions = Enum.GetValues<GameAction>();

        private static readonly (string Action, string[] Keys)[] FixedRows =
        {
            ("Throw an orb", new[] { "Click" }),
            ("Choose an inventory slot", new[] { "1", "2", "3" }),
            ("Pause", new[] { "Esc" }),
        };

        private static readonly Color Muted = new Color(210, 200, 185);

        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Rectangle _panel;
        private readonly Vector2 _titleCenter;
        private readonly int _rowsTop;
        private readonly int _boxesX;
        private readonly Vector2 _messageCenter;
        private readonly Rectangle[] _buttons;

        // The selection: a row of key boxes (one per action) or, below them, the buttons.
        private int _row;
        private int _column;
        private bool _waitingForKey;
        private string _message = DefaultMessage;
        private float _time;

        public ControlsPanel()
        {
            float titleHeight = Assets.Font.LineSpacing * TitleScale;
            int buttonWidth = Assets.MenuButton.Width * 3 / 4;
            int buttonHeight = Assets.MenuButton.Height * 3 / 4;
            int rows = Actions.Length + FixedRows.Length;
            int height = (int)(Padding + titleHeight + 24 + rows * RowHeight + 50 + buttonHeight + Padding);

            _panel = UiDrawing.CenteredPanel(Width, height);
            _titleCenter = new Vector2(_panel.Center.X, _panel.Y + Padding + titleHeight / 2f);
            _rowsTop = (int)(_panel.Y + Padding + titleHeight + 24);
            _boxesX = _panel.Right - 70 - Controls.KeysPerAction * BoxWidth - (Controls.KeysPerAction - 1) * BoxGap;
            _messageCenter = new Vector2(_panel.Center.X, _rowsTop + rows * RowHeight + 22);

            int buttonsTop = _panel.Bottom - Padding - buttonHeight;
            _buttons = new[]
            {
                new Rectangle(_panel.Center.X - buttonWidth - 20, buttonsTop, buttonWidth, buttonHeight),
                new Rectangle(_panel.Center.X + 20, buttonsTop, buttonWidth, buttonHeight),
            };
        }

        public bool IsOpen { get; private set; }

        private int ButtonsRow => Actions.Length;

        public void Open()
        {
            IsOpen = true;
            _row = 0;
            _column = 0;
            _waitingForKey = false;
            _message = DefaultMessage;
        }

        public void Update()
        {
            if (!IsOpen)
                return;
            _time += 1f / 60f;

            if (_waitingForKey)
            {
                UpdateWaiting();
                return;
            }

            if (Input.Back)
            {
                IsOpen = false;
                return;
            }

            MoveWithKeys();
            if (Input.MenuConfirm)
                Choose();
            else if ((Input.WasPressed(Keys.Back) || Input.WasPressed(Keys.Delete)) && _row < ButtonsRow)
                Clear(Actions[_row], _column);

            UpdateMouse();
        }

        private void MoveWithKeys()
        {
            if (Input.MenuUp)
                _row = (_row + ButtonsRow) % (ButtonsRow + 1);
            if (Input.MenuDown)
                _row = (_row + 1) % (ButtonsRow + 1);

            int columns = _row == ButtonsRow ? _buttons.Length : Controls.KeysPerAction;
            if (Input.MenuLeft)
                _column--;
            if (Input.MenuRight)
                _column++;
            _column = Math.Clamp(_column, 0, columns - 1);
        }

        private void UpdateMouse()
        {
            for (int row = 0; row <= ButtonsRow; row++)
            {
                int columns = row == ButtonsRow ? _buttons.Length : Controls.KeysPerAction;
                for (int column = 0; column < columns; column++)
                {
                    Rectangle area = row == ButtonsRow ? _buttons[column] : Box(row, column);
                    if (!Input.IsMouseOver(area))
                        continue;

                    if (Input.MouseMoved || Input.WasClicked)
                    {
                        _row = row;
                        _column = column;
                    }
                    if (Input.WasClicked)
                        Choose();
                }
            }
        }

        /// <summary>Enter or a click: start picking a key for the selected box, or press the selected button.</summary>
        private void Choose()
        {
            if (_row < ButtonsRow)
            {
                _waitingForKey = true;
                _message = $"Press a key for \"{Controls.ActionName(Actions[_row])}\". Esc cancels.";
            }
            else if (_column == 0)
            {
                _settings.KeyBindings = Controls.DefaultBindings();
                _settings.Save();
                _message = "The keys are back to how they started.";
            }
            else
            {
                IsOpen = false;
            }
        }

        private void UpdateWaiting()
        {
            // Clicking anywhere stops waiting; a click on another box starts waiting there instead.
            if (Input.WasClicked)
            {
                _waitingForKey = false;
                _message = DefaultMessage;
                UpdateMouse();
                return;
            }

            Keys key = Input.PressedKey;
            if (key == Keys.None)
                return;

            if (key == Keys.Escape)
            {
                _waitingForKey = false;
                _message = DefaultMessage;
            }
            else if (!Controls.CanBind(key))
            {
                _message = $"{Controls.KeyName(key)} is for the inventory slots. Press another key, or Esc to cancel.";
            }
            else
            {
                _waitingForKey = false;
                Assign(Actions[_row], _column, key);
            }
        }

        /// <summary>
        /// Puts a key in a box. If another action already uses that key, the two swap: the other action gets the
        /// key that was in this box, so nothing is left without a key.
        /// </summary>
        private void Assign(GameAction action, int slot, Keys key)
        {
            Dictionary<GameAction, Keys[]> bindings = _settings.KeyBindings;
            var keys = bindings[action].ToList();
            Keys replaced = slot < keys.Count ? keys[slot] : Keys.None;
            string name = Controls.KeyName(key);
            _message = $"{Controls.ActionName(action)}: {name}.";

            int sameAction = keys.IndexOf(key);
            if (sameAction >= 0)
            {
                // Already one of this action's keys: just move it to this box.
                keys.RemoveAt(sameAction);
                keys.Insert(Math.Min(slot, keys.Count), key);
                bindings[action] = keys.ToArray();
                _settings.Save();
                return;
            }

            foreach (GameAction other in Actions.Where(other => other != action))
            {
                var otherKeys = bindings[other].ToList();
                int index = otherKeys.IndexOf(key);
                if (index < 0)
                    continue;

                if (replaced != Keys.None)
                {
                    otherKeys[index] = replaced;
                    _message = $"{name} and {Controls.KeyName(replaced)} swapped between {Controls.ActionName(action)} and {Controls.ActionName(other)}.";
                }
                else if (otherKeys.Count > 1)
                {
                    otherKeys.RemoveAt(index);
                    _message = $"{name} was moved here from {Controls.ActionName(other)}.";
                }
                else
                {
                    _message = $"{name} is the only key for {Controls.ActionName(other)}. Give it another key first.";
                    return;
                }
                bindings[other] = otherKeys.ToArray();
            }

            if (slot < keys.Count)
                keys[slot] = key;
            else
                keys.Add(key);
            bindings[action] = keys.ToArray();
            _settings.Save();
        }

        private void Clear(GameAction action, int slot)
        {
            var keys = _settings.KeyBindings[action].ToList();
            if (slot >= keys.Count)
                return;
            if (keys.Count == 1)
            {
                _message = "Every action needs at least one key.";
                return;
            }

            keys.RemoveAt(slot);
            _settings.KeyBindings[action] = keys.ToArray();
            _settings.Save();
            _message = DefaultMessage;
        }

        private Rectangle Box(int row, int column)
        {
            int rowCenter = _rowsTop + row * RowHeight + RowHeight / 2;
            return new Rectangle(_boxesX + column * (BoxWidth + BoxGap), rowCenter - BoxHeight / 2, BoxWidth, BoxHeight);
        }

        /// <summary>Draws in menu coordinates, so use <see cref="VirtualScreen.CenteredTransform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * DimAmount);
            UiDrawing.DrawPanel(spriteBatch, _panel);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, "Controls", _titleCenter, Color.White, TitleScale);

            for (int row = 0; row < Actions.Length; row++)
            {
                IReadOnlyList<Keys> keys = Controls.KeysFor(Actions[row]);
                DrawLabel(spriteBatch, Controls.ActionName(Actions[row]), row, row == _row ? UiDrawing.Highlight : Color.White);
                for (int column = 0; column < Controls.KeysPerAction; column++)
                    DrawBox(spriteBatch, Box(row, column), column < keys.Count ? Controls.KeyName(keys[column]) : null, row == _row && column == _column);
            }

            // The keys that can't be changed, shown as plain key caps.
            for (int i = 0; i < FixedRows.Length; i++)
            {
                int row = Actions.Length + i;
                DrawLabel(spriteBatch, FixedRows[i].Action, row, Color.White * 0.75f);
                int x = _boxesX;
                foreach (string key in FixedRows[i].Keys)
                {
                    int width = UiDrawing.KeyCapWidth(key, 40);
                    Rectangle box = Box(row, 0);
                    UiDrawing.DrawKeyCap(spriteBatch, new Rectangle(x, box.Center.Y - 20, width, 40), key, 0.75f);
                    x += width + 10;
                }
            }

            Color messageColor = _waitingForKey ? UiDrawing.Highlight : Muted;
            UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, _message, _messageCenter, messageColor);

            UiDrawing.DrawButton(spriteBatch, _buttons[0], "Reset keys", _row == ButtonsRow && _column == 0, textScale: 0.75f);
            UiDrawing.DrawButton(spriteBatch, _buttons[1], "Back", _row == ButtonsRow && _column == 1, textScale: 0.85f);
        }

        private void DrawLabel(SpriteBatch spriteBatch, string text, int row, Color color)
        {
            float y = _rowsTop + row * RowHeight + (RowHeight - Assets.SmallFont.LineSpacing) / 2f;
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2(_panel.X + 70, y), color);
        }

        private void DrawBox(SpriteBatch spriteBatch, Rectangle box, string key, bool selected)
        {
            if (selected && _waitingForKey)
            {
                // Waiting for a key: a dark box with a gently pulsing outline.
                float pulse = 0.6f + 0.4f * MathF.Sin(_time * 8f);
                spriteBatch.Draw(Assets.Pixel, box, Color.Black * 0.7f);
                UiDrawing.DrawOutline(spriteBatch, box, UiDrawing.Highlight * pulse, 3);
                UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, "Press a key", box.Center.ToVector2(), UiDrawing.Highlight);
                return;
            }

            if (key != null)
            {
                UiDrawing.DrawKeyCap(spriteBatch, box, key);
            }
            else
            {
                spriteBatch.Draw(Assets.Pixel, box, Color.Black * 0.35f);
                UiDrawing.DrawOutline(spriteBatch, box, Muted * 0.4f);
            }

            if (selected)
            {
                Rectangle outline = box;
                outline.Inflate(4, 4);
                UiDrawing.DrawOutline(spriteBatch, outline, UiDrawing.Highlight, 3);
            }
        }
    }
}
