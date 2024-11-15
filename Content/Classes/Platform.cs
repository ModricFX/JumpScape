using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape
{
    public class Platform
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public int Length { get; set; } // New property for platform length
        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, Length, Texture.Height);

        public Platform(Texture2D texture, Vector2 position, int length)
        {
            Texture = texture;
            Position = position;
            Length = length;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, new Rectangle(0, 0, Length, Texture.Height), Color.White);
        }
    }
}
