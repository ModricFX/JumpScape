using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class AnimatedItem : Item
    {
        private int _currentFrame;
        private int _totalFrames;
        private float _frameTime;
        private float _timer;
        private int _frameWidth;
        private int _frameHeight;

        public AnimatedItem(Texture2D texture, Vector2 position, int totalFrames, float frameTime) 
            : base(texture, position)
        {
            _totalFrames = totalFrames;
            _frameTime = frameTime;
            _currentFrame = 0;
            _timer = 0f;

            // Calculate the frame width and height
            _frameWidth = texture.Width / totalFrames;
            _frameHeight = texture.Height;
        }

        public override Rectangle BoundingBox => 
            new Rectangle((int)Position.X, (int)Position.Y, (int)(_frameWidth * 0.1f), (int)(_frameHeight * 0.1f));

        public override void Update(GameTime gameTime)
        {
            if (Collected) return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer > _frameTime)
            {
                _currentFrame = (_currentFrame + 1) % _totalFrames;
                _timer = 0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Collected) return;

            Rectangle sourceRectangle = new Rectangle(_currentFrame * _frameWidth, 0, _frameWidth, _frameHeight);
            spriteBatch.Draw(_texture, Position, sourceRectangle, Color.White, 0f, Vector2.Zero, 0.105f, SpriteEffects.None, 0f);
        }
    }
}
