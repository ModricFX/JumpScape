using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Core
{
    /// <summary>
    /// Keyboard, mouse and gamepad state, read once per frame so every screen sees the same input and
    /// "was pressed this frame" checks work everywhere. The gamepad's sticks count as buttons once they are
    /// pushed more than halfway.
    /// </summary>
    public static class Input
    {
        private const float StickThreshold = 0.5f;
        private const float AimDeadZone = 0.3f;

        private static KeyboardState _keyboard, _previousKeyboard;
        private static MouseState _mouse, _previousMouse;
        private static GamePadState _gamePad, _previousGamePad;

        /// <summary>Mouse position in the 1920x1080 menu coordinates (see <see cref="VirtualScreen.CenteredTransform"/>).</summary>
        public static Vector2 MousePosition { get; private set; }

        /// <summary>Mouse position in view coordinates: the top-left of the window is (0, 0).</summary>
        public static Vector2 ViewMousePosition { get; private set; }

        /// <summary>True after the gamepad was used, until a key or the mouse is used again. Hints show gamepad buttons.</summary>
        public static bool UsingGamepad { get; private set; }

        public static void Update()
        {
            _previousKeyboard = _keyboard;
            _previousMouse = _mouse;
            _previousGamePad = _gamePad;
            _keyboard = Keyboard.GetState();
            _mouse = Mouse.GetState();
            _gamePad = GamePad.GetState(PlayerIndex.One);
            MousePosition = VirtualScreen.ToCentered(_mouse.Position);
            ViewMousePosition = VirtualScreen.ToView(_mouse.Position);
            NoticeDevice();
        }

        /// <summary>Remembers whether the gamepad or the keyboard and mouse were used last.</summary>
        private static void NoticeDevice()
        {
            if (_keyboard.GetPressedKeyCount() > 0 || MouseMoved || IsMouseDown)
                UsingGamepad = false;
            else if (_gamePad.IsConnected && _gamePad != _previousGamePad && (_gamePad.Buttons != _previousGamePad.Buttons
                || _gamePad.DPad != _previousGamePad.DPad || StickPushed(_gamePad.ThumbSticks.Left) || StickPushed(_gamePad.ThumbSticks.Right)
                || _gamePad.Triggers.Right > StickThreshold))
                UsingGamepad = true;
        }

        private static bool StickPushed(Vector2 stick) => stick.Length() > StickThreshold;

        public static bool IsDown(Keys key) => _keyboard.IsKeyDown(key);

        public static bool WasPressed(Keys key) => _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

        public static bool IsDown(Buttons button) => IsDown(_gamePad, button);

        public static bool WasPressed(Buttons button) => IsDown(_gamePad, button) && !IsDown(_previousGamePad, button);

        /// <summary>Gamepad buttons, with the left stick and the right trigger working like the D-pad and a button.</summary>
        private static bool IsDown(GamePadState state, Buttons button)
        {
            Vector2 stick = state.ThumbSticks.Left;
            return button switch
            {
                Buttons.DPadLeft => state.IsButtonDown(Buttons.DPadLeft) || stick.X < -StickThreshold,
                Buttons.DPadRight => state.IsButtonDown(Buttons.DPadRight) || stick.X > StickThreshold,
                Buttons.DPadUp => state.IsButtonDown(Buttons.DPadUp) || stick.Y > StickThreshold,
                Buttons.DPadDown => state.IsButtonDown(Buttons.DPadDown) || stick.Y < -StickThreshold,
                Buttons.RightTrigger => state.Triggers.Right > StickThreshold,
                _ => state.IsButtonDown(button),
            };
        }

        /// <summary>True while any of the player's keys (or the gamepad button) for the action is held.</summary>
        public static bool IsDown(GameAction action) => Controls.KeysFor(action).Any(IsDown) || IsDown(Controls.ButtonFor(action));

        /// <summary>True in the frame one of the player's keys (or the gamepad button) for the action was pressed.</summary>
        public static bool WasPressed(GameAction action) => Controls.KeysFor(action).Any(WasPressed) || WasPressed(Controls.ButtonFor(action));

        /// <summary>A key pressed this frame, or <see cref="Keys.None"/>. Used when the player picks a new key.</summary>
        public static Keys PressedKey => _keyboard.GetPressedKeys().FirstOrDefault(key => _previousKeyboard.IsKeyUp(key));

        public static bool IsMouseDown => _mouse.LeftButton == ButtonState.Pressed;

        public static bool WasClicked => IsMouseDown && _previousMouse.LeftButton == ButtonState.Released;

        public static bool MouseMoved => _mouse.Position != _previousMouse.Position;

        public static bool IsMouseOver(Rectangle area) => area.Contains(MousePosition);

        /// <summary>The right trigger throws an orb.</summary>
        public static bool ThrowPressed => WasPressed(Buttons.RightTrigger);

        /// <summary>
        /// Where the right stick points, with y growing downwards like the screen, or straight ahead if the stick
        /// isn't pushed.
        /// </summary>
        public static Vector2 AimDirection(bool facingRight)
        {
            Vector2 stick = _gamePad.ThumbSticks.Right;
            if (stick.Length() < AimDeadZone)
                return new Vector2(facingRight ? 1f : -1f, -0.15f);
            return Vector2.Normalize(new Vector2(stick.X, -stick.Y));
        }

        public static bool MenuUp => WasPressed(Keys.Up) || WasPressed(Keys.W) || WasPressed(Buttons.DPadUp);

        public static bool MenuDown => WasPressed(Keys.Down) || WasPressed(Keys.S) || WasPressed(Buttons.DPadDown);

        public static bool MenuLeft => WasPressed(Keys.Left) || WasPressed(Keys.A) || WasPressed(Buttons.DPadLeft);

        public static bool MenuRight => WasPressed(Keys.Right) || WasPressed(Keys.D) || WasPressed(Buttons.DPadRight);

        public static bool MenuConfirm => WasPressed(Keys.Enter) || WasPressed(Keys.Space) || WasPressed(Buttons.A);

        /// <summary>Esc or the gamepad's B: closes a menu or goes back.</summary>
        public static bool Back => WasPressed(Keys.Escape) || WasPressed(Buttons.B);

        /// <summary>Esc or the gamepad's Start: pauses the game.</summary>
        public static bool Pause => WasPressed(Keys.Escape) || WasPressed(Buttons.Start);
    }
}
