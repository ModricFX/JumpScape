using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace JumpScape.Classes
{
    public class Door
    {
        private Texture2D _lockedTexture;
        private Texture2D _unlockedTexture;
        private Texture2D _openedTexture;
        private Texture2D _currentTexture;

        // The base SoundEffect plus an instance
        private SoundEffect _doorOpenSound;
        private SoundEffectInstance _doorOpenSoundInstance;

        private GameSettings _settings; // reference to your settings for Volume

        public Vector2 Position { get; }
        public bool IsLocked { get; private set; }
        public bool IsUnlocked => !IsLocked && !IsOpened;
        public bool IsOpened { get; private set; }

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X, 
            (int)Position.Y, 
            (int)(_currentTexture.Width * 0.15f), 
            (int)(_currentTexture.Height * 0.15f)
        );

        public Door(GraphicsDevice graphicsDevice, Vector2 position, bool isLocked)
        {
            Position = position;
            IsLocked = isLocked;

            // Load door textures
            _lockedTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_locked.png"));
            _unlockedTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_unlocked.png"));
            _openedTexture   = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_opened.png"));

            _currentTexture = IsLocked ? _lockedTexture : _unlockedTexture;

            // Load sound
            _doorOpenSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "DoorOpen.wav"));
            _doorOpenSoundInstance = _doorOpenSound.CreateInstance();

            _settings = GameSettings.Load(); // store reference
            setVolume();          // apply settings.Volume
        }

        /// <summary>
        /// Adjust the door open sound volume based on GameSettings.Volume (0–100).
        /// </summary>
        private void setVolume()
        {
            if (_settings != null && _doorOpenSoundInstance != null)
            {
                float volumeFactor = _settings.Volume / 100f; // Convert 0-100 to 0.0-1.0
                _doorOpenSoundInstance.Volume = volumeFactor; 
            }
        }

        public void Unlock()
        {
            if (IsLocked)
            {
                IsLocked = false;
                _currentTexture = _unlockedTexture;
            }
        }

        public void Open()
        {
            if (IsUnlocked && !IsOpened)
            {
                IsOpened = true;
                _currentTexture = _openedTexture;

                // Play the door open sound using our instance
                _doorOpenSoundInstance.Play();
            }
        }

        public void Update(SpriteBatch spriteBatch, Player player, SpriteFont font, float screenWidth)
        {
            // Optional: If your volume might change at runtime, re-apply volume each update:
            // setVolume();

            if (!player.BoundingBox.Intersects(BoundingBox))
                return;

            string message = GetInteractionMessage(player);
            Vector2 messagePosition = CalculateMessagePosition(font, message, screenWidth);

            spriteBatch.DrawString(font, message, messagePosition, Color.White);
        }

        private string GetInteractionMessage(Player player)
        {
            if (IsLocked)
                return player.HasKey ? "Press E to unlock!" : "You need a key!";

            return IsOpened ? "Press E to enter" : "Press E to open";
        }

        private Vector2 CalculateMessagePosition(SpriteFont font, string message, float screenWidth)
        {
            Vector2 messagePosition = new Vector2(Position.X, Position.Y - 40);
            Vector2 messageSize = font.MeasureString(message);

            if (messagePosition.X < 10)
                messagePosition.X = 10;
            else if (messagePosition.X + messageSize.X > screenWidth - 10)
                messagePosition.X = screenWidth - messageSize.X - 10;

            return messagePosition;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                _currentTexture, 
                Position, 
                null, 
                Color.White, 
                0f, 
                Vector2.Zero, 
                0.15f, 
                SpriteEffects.None, 
                0f
            );
        }
    }
}
