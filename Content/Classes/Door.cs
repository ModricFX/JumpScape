using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Classes
{
    public class Door
    {
        private Texture2D _lockedTexture;
        private Texture2D _unlockedTexture;
        private Texture2D _openedTexture;
        private Texture2D _currentTexture;
        public Vector2 Position { get; }
        public bool IsLocked { get; private set; }
        public bool IsUnlocked { get; private set; }
        public bool IsOpened { get; private set; }

        public Door(Texture2D lockedTexture, Texture2D unlockedTexture, Texture2D openedTexture, Vector2 position, bool isLocked)
        {
            _lockedTexture = lockedTexture;
            _unlockedTexture = unlockedTexture;
            _openedTexture = openedTexture;
            Position = position;
            IsLocked = isLocked;

            // If the door is locked, use the locked texture; otherwise, use the unlocked texture.
            if (IsLocked)
            {
                _currentTexture = _lockedTexture;
                IsUnlocked = false;
            }
            else
            {
                _currentTexture = _unlockedTexture;
                IsUnlocked = true;
            }
        }

        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, (int)(_currentTexture.Width * 0.15f), (int)(_currentTexture.Height * 0.15f));

        public void Unlock()
        {
            if (IsLocked)
            {
                IsLocked = false;
                IsUnlocked = true;
                _currentTexture = _unlockedTexture;
            }
        }

        public void Open()
        {
            // Allow opening the door if it is either unlocked or was already unlocked at the start
            if (IsUnlocked && !IsOpened)
            {
                IsUnlocked = false;
                IsOpened = true;
                _currentTexture = _openedTexture;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_currentTexture, Position, null, Color.White, 0f, Vector2.Zero, 0.15f, SpriteEffects.None, 0f);
        }
    }
}
