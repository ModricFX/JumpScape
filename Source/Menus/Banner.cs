using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>Big text on a wooden plank that fades in and out, such as "Level 2" when a level starts.</summary>
    public sealed class Banner
    {
        private const float FadeTime = 0.4f;
        private const float TextScale = 1.2f;

        private string _text;
        private float _time;
        private float _duration;

        public void Show(string text, float duration = 2.2f)
        {
            _text = text;
            _time = 0f;
            _duration = duration;
        }

        public void Update(float deltaTime)
        {
            _time += deltaTime;
        }

        /// <summary>Draws in menu coordinates, so use <see cref="VirtualScreen.CenteredTransform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_text == null || _time >= _duration)
                return;

            float opacity = Math.Min(1f, Math.Min(_time / FadeTime, (_duration - _time) / FadeTime));
            Vector2 textSize = Assets.Font.MeasureString(_text) * TextScale;
            int width = (int)textSize.X + 160;
            const int height = 110;
            var plank = new Rectangle(VirtualScreen.Width / 2 - width / 2, (int)(VirtualScreen.Height * 0.22f), width, height);

            spriteBatch.Draw(Assets.MenuButton, plank, Color.White * opacity);
            UiDrawing.DrawCenteredText(spriteBatch, Assets.Font, _text, plank.Center.ToVector2(), UiDrawing.Highlight * opacity, TextScale);
        }
    }
}
