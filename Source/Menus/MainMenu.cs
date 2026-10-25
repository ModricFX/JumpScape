using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    public enum MainMenuChoice
    {
        None = -1,
        Play,
        LevelPicker,
        Settings,
        LevelEditor,
        Exit,
    }

    /// <summary>The title screen: logo and a column of buttons.</summary>
    public sealed class MainMenu
    {
        private const float LogoScale = 0.4f;
        private const int ButtonGap = 20;

        private static readonly string[] Labels = { "Play", "Level Picker", "Settings", "Level Editor", "Exit" };

        private readonly Rectangle[] _buttons = new Rectangle[Labels.Length];
        private int _selected;

        public MainMenu()
        {
            int width = Assets.MenuButton.Width;
            int height = Assets.MenuButton.Height;
            int spacing = height + ButtonGap;
            int x = (VirtualScreen.Width - width) / 2;
            int top = VirtualScreen.Height / 2 - Labels.Length * spacing / 2 + (int)(Assets.Logo.Height * LogoScale / 3);

            for (int i = 0; i < _buttons.Length; i++)
                _buttons[i] = new Rectangle(x, top + i * spacing, width, height);
        }

        public MainMenuChoice Update()
        {
            if (Input.MenuUp)
                _selected = (_selected + Labels.Length - 1) % Labels.Length;
            if (Input.MenuDown)
                _selected = (_selected + 1) % Labels.Length;
            if (Input.MenuConfirm)
                return (MainMenuChoice)_selected;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!Input.IsMouseOver(_buttons[i]))
                    continue;

                if (Input.MouseMoved)
                    _selected = i;
                if (Input.WasClicked)
                    return (MainMenuChoice)i;
            }

            return MainMenuChoice.None;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            UiDrawing.DrawLogo(spriteBatch, LogoScale);

            for (int i = 0; i < _buttons.Length; i++)
                UiDrawing.DrawButton(spriteBatch, _buttons[i], Labels[i], i == _selected);
        }
    }
}
