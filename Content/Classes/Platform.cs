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
            // Texture width
            int textureWidth = Texture.Width;

            // Calculate how many times the texture needs to be drawn fully
            int numRepeats = Length / textureWidth;
            int remainder = Length % textureWidth;

            // Draw the repeated textures
            for (int i = 0; i < numRepeats; i++)
            {
                spriteBatch.Draw(Texture, new Vector2(Position.X + i * textureWidth, Position.Y), Color.White);
            }

            // Draw the remaining portion only if necessary
            if (remainder > 0)
            {
                Rectangle sourceRectangle = new Rectangle(0, 0, remainder, Texture.Height);
                Vector2 drawPosition = new Vector2(Position.X + numRepeats * textureWidth, Position.Y);

                spriteBatch.Draw(Texture, drawPosition, sourceRectangle, Color.White);
            }
        }
    }
}
