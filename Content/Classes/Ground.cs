using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class Ground
    {
        private Texture2D _texture;
        private Vector2 _position;
        private Rectangle _bounds;
        private int _length;


        public Ground(Texture2D texture, Vector2 position, int length)
        {
            _texture = texture;
            _position = position;
            _length = length;
            _bounds = new Rectangle((int)position.X, (int)position.Y, length, texture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _position, new Rectangle(0, 0, _length, _texture.Height), Color.White);
        }

        public Rectangle GetBounds()
        {
            return _bounds;
        }

    }
}