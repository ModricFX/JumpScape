using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    public enum DoorState
    {
        Locked,
        Closed,
        Open,
    }

    /// <summary>The level exit. A locked door needs a key, then it is opened and entered with E.</summary>
    public sealed class Door
    {
        private const float Scale = 0.15f;
        private const float PromptScale = 0.75f;
        private const int ScreenMargin = 10;

        private readonly SoundEffectInstance _openSound = Assets.DoorOpen.CreateInstance();

        public Door(Vector2 position, bool isLocked)
        {
            Position = position;
            State = isLocked ? DoorState.Locked : DoorState.Closed;
        }

        public Vector2 Position { get; }

        public DoorState State { get; private set; }

        private Texture2D Texture => State switch
        {
            DoorState.Locked => Assets.DoorLocked,
            DoorState.Closed => Assets.DoorUnlocked,
            _ => Assets.DoorOpened,
        };

        public Rectangle Bounds => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(Texture.Width * Scale),
            (int)(Texture.Height * Scale));

        public void Unlock()
        {
            if (State == DoorState.Locked)
                State = DoorState.Closed;
        }

        public void Open()
        {
            if (State != DoorState.Closed)
                return;

            State = DoorState.Open;
            _openSound.Volume = GameSettings.Current.SoundVolume;
            _openSound.Play();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }

        /// <summary>Shows a hint above the door while the player is standing in front of it.</summary>
        public void DrawPrompt(SpriteBatch spriteBatch, Player player)
        {
            if (!player.Bounds.Intersects(Bounds) || player.IsDead)
                return;

            string message = GetPrompt(player.Inventory);
            Vector2 size = Assets.Font.MeasureString(message) * PromptScale;
            float x = MathHelper.Clamp(Position.X, ScreenMargin, VirtualScreen.ViewWidth - size.X - ScreenMargin);
            var position = new Vector2(x, Position.Y - size.Y - 10);

            spriteBatch.DrawString(Assets.Font, message, position + new Vector2(2, 2), Color.Black * 0.5f, 0f, Vector2.Zero, PromptScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(Assets.Font, message, position, Color.White, 0f, Vector2.Zero, PromptScale, SpriteEffects.None, 0f);
        }

        private string GetPrompt(Inventory inventory)
        {
            switch (State)
            {
                case DoorState.Locked when inventory.SelectedItem == ItemType.Key:
                    return "Press E to unlock";
                case DoorState.Locked when inventory.Contains(ItemType.Key):
                    return "Select the key (1-3) to unlock";
                case DoorState.Locked:
                    return "You need a key!";
                case DoorState.Closed:
                    return "Press E to open";
                default:
                    return "Press E to enter";
            }
        }
    }
}
