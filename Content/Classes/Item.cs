using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class Item
    {
        protected Texture2D _texture;
        public Vector2 Position { get; }
        public bool Collected { get; private set; } = false;

        public Item(Texture2D texture, Vector2 position)
        {
            _texture = texture;
            Position = position;
        }

        public virtual Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, (int)(_texture.Width * 0.1f), (int)(_texture.Height * 0.1f));

        public void Collect()
        {
            Collected = true;
        }

        public virtual void Update(GameTime gameTime) { }

        // Make the Draw method virtual so it can be overridden
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!Collected)
            {
                spriteBatch.Draw(_texture, Position, null, Color.White, 0f, Vector2.Zero, 0.1f, SpriteEffects.None, 0f);
            }
        }
    }
}
