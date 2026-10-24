using System;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Menus
{
    /// <summary>The background picture shared by all menus. It fills the window and slowly pans from side to side.</summary>
    public sealed class MenuBackground
    {
        private const float Overscan = 1.1f;
        private const float PanSpeed = 30f;

        private float _offset;
        private int _direction = 1;

        private static float Scale => Overscan * Math.Max(
            VirtualScreen.ViewWidth / Assets.MenuBackground.Width,
            VirtualScreen.ViewHeight / Assets.MenuBackground.Height);

        public void Update(float deltaTime)
        {
            float maxOffset = Assets.MenuBackground.Width * Scale - VirtualScreen.ViewWidth;
            _offset = MathHelper.Clamp(_offset + _direction * PanSpeed * deltaTime, 0f, maxOffset);

            if (_offset >= maxOffset)
                _direction = -1;
            else if (_offset <= 0f)
                _direction = 1;
        }

        /// <summary>Draws in view coordinates, so use <see cref="VirtualScreen.Transform"/>.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = Assets.MenuBackground;
            var destination = new Rectangle(-(int)_offset, 0, (int)MathF.Ceiling(texture.Width * Scale), (int)MathF.Ceiling(texture.Height * Scale));
            spriteBatch.Draw(texture, destination, Color.White);
        }
    }
}
