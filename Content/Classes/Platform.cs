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
            // Get the width of the texture
            int textureWidth = Texture.Width;

            // Calculate how many times to repeat the texture
            int numRepeats = Length / textureWidth;
            int remainder = Length % textureWidth;

            // Draw the repeated textures
            for (int i = 0; i < numRepeats; i++)
            {
                // Draw each texture at the appropriate position
                spriteBatch.Draw(Texture, new Vector2(Position.X + i * textureWidth, Position.Y), Color.White);
            }

            // If there's a remainder, draw the leftover part of the texture
            if (remainder > 0)
            {
                spriteBatch.Draw(Texture, new Rectangle((int)(Position.X + numRepeats * textureWidth), (int)Position.Y, remainder, Texture.Height), Color.White);
            }
        }
    }
}
