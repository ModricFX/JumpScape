using System;
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

    /// <summary>
    /// A door, opened and entered with E. A locked door needs a key first. Exit doors finish the level;
    /// portal doors take the player to another door, often in another area of the level.
    /// </summary>
    public sealed class Door
    {
        private const float Scale = 0.15f;
        private const float PromptScale = 0.75f;
        private const int ScreenMargin = 10;
        private static readonly Color PortalGlow = Color.Magenta;

        private readonly SoundEffectInstance _openSound = Assets.DoorOpen.CreateInstance();
        private float _time;

        public Door(Vector2 position, bool isLocked, string name = null, string target = null)
        {
            Position = position;
            State = isLocked ? DoorState.Locked : DoorState.Closed;
            Name = name;
            Target = target;
        }

        public Vector2 Position { get; }

        /// <summary>The portal's name, used by other portals to lead here. Null for exit doors.</summary>
        public string Name { get; }

        /// <summary>The name of the portal this door leads to, or null if it is the level exit.</summary>
        public string Target { get; }

        public bool IsPortal => Target != null;

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

        /// <summary>Opens the door without a sound, for when the player has just come through it from the other side.</summary>
        public void OpenFromOtherSide()
        {
            State = DoorState.Open;
        }

        /// <summary>Where the player stands after coming out of this door: centered in front of it, feet at its bottom.</summary>
        public Vector2 ArrivalPosition(Rectangle playerBounds)
        {
            Rectangle bounds = Bounds;
            return new Vector2(bounds.Center.X - playerBounds.Width / 2f, bounds.Bottom - playerBounds.Height);
        }

        public void Update(float deltaTime)
        {
            _time += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Portals glow so they can't be mistaken for the exit.
            if (IsPortal)
            {
                Rectangle bounds = Bounds;
                float pulse = 0.85f + 0.15f * MathF.Sin(_time * 3f);
                float glowScale = bounds.Height * 2.4f * pulse / Assets.Glow.Width;
                var origin = new Vector2(Assets.Glow.Width / 2f);
                spriteBatch.Draw(Assets.Glow, bounds.Center.ToVector2(), null, PortalGlow, 0f, origin, glowScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(Assets.Glow, bounds.Center.ToVector2(), null, PortalGlow * 0.6f, 0f, origin, glowScale * 0.7f, SpriteEffects.None, 0f);
            }

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
                    return IsPortal ? "Press E to go through" : "Press E to enter";
            }
        }
    }
}
