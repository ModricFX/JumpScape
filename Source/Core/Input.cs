using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Core
{
    /// <summary>
    /// Keyboard and mouse state, read once per frame so every screen sees the same input and
    /// "was pressed this frame" checks work everywhere.
    /// </summary>
    public static class Input
    {
        private static KeyboardState _keyboard, _previousKeyboard;
        private static MouseState _mouse, _previousMouse;

        /// <summary>Mouse position in the 1920x1080 menu coordinates (see <see cref="VirtualScreen.CenteredTransform"/>).</summary>
        public static Vector2 MousePosition { get; private set; }

        public static void Update()
        {
            _previousKeyboard = _keyboard;
            _previousMouse = _mouse;
            _keyboard = Keyboard.GetState();
            _mouse = Mouse.GetState();
            MousePosition = VirtualScreen.ToCentered(_mouse.Position);
        }

        public static bool IsDown(Keys key) => _keyboard.IsKeyDown(key);

        public static bool WasPressed(Keys key) => _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

        public static bool IsMouseDown => _mouse.LeftButton == ButtonState.Pressed;

        public static bool WasClicked => IsMouseDown && _previousMouse.LeftButton == ButtonState.Released;

        public static bool MouseMoved => _mouse.Position != _previousMouse.Position;

        public static bool IsMouseOver(Rectangle area) => area.Contains(MousePosition);

        public static bool MenuUp => WasPressed(Keys.Up) || WasPressed(Keys.W);

        public static bool MenuDown => WasPressed(Keys.Down) || WasPressed(Keys.S);

        public static bool MenuConfirm => WasPressed(Keys.Enter) || WasPressed(Keys.Space);

        public static bool Back => WasPressed(Keys.Escape);
    }
}
