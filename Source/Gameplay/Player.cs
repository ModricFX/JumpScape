using System;
using System.Collections.Generic;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// The player character. Physics run in fixed 1/60 s steps, so speeds are in pixels per frame.
    /// </summary>
    public sealed class Player
    {
        public const float Gravity = 0.6f;

        private const float Scale = 0.15f;
        private const float WalkSpeed = 3f;
        private const float JumpVelocity = -21.375f;
        private const int MaxHealth = 6; // Counted in half hearts: three hearts.
        private const float InvincibilityDuration = 1.4f;
        private const float FlashInterval = 0.2f;
        private const float KnockbackDuration = 0.4f;
        private static readonly Vector2 KnockbackVelocity = new Vector2(5f, -8f);
        private const float DeathRotationSpeed = 4f;
        private const float HeartScale = 0.1f;

        private readonly SoundEffectInstance _walkingSound = Assets.PlayerWalking.CreateInstance();
        private readonly SoundEffectInstance _jumpSound = Assets.PlayerJump.CreateInstance();
        private readonly SoundEffectInstance _hitSound = Assets.PlayerHit.CreateInstance();

        private Vector2 _velocity;
        private bool _facingRight = true;
        private int _lastPressedDirection = 1;
        private int _health = MaxHealth;
        private float _invincibilityTimer;
        private float _knockbackTimer;
        private float _deathRotation;

        public Player(Vector2 spawnPosition)
        {
            Position = spawnPosition;
            _walkingSound.IsLooped = true;
        }

        public Vector2 Position { get; private set; }

        public Inventory Inventory { get; } = new Inventory();

        public bool IsOnGround { get; private set; }

        public bool IsInvincible => _invincibilityTimer > 0f;

        public bool IsDead { get; private set; }

        /// <summary>Set when the player walks through the open exit door.</summary>
        public bool ReachedExit { get; private set; }

        private static int Width => (int)(Assets.PlayerRight.Width * Scale);

        private static int Height => (int)(Assets.PlayerRight.Height * Scale);

        public Rectangle Bounds => new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y), Width, Height);

        public Vector2 Center => Position + new Vector2(Width / 2f, Height / 2f);

        /// <summary>Handles input, picking up and dropping keys, using the door and the hit/death timers.</summary>
        public void Update(float deltaTime, List<KeyPickup> keys, Door door)
        {
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= deltaTime;

            if (_knockbackTimer > 0f)
                _knockbackTimer -= deltaTime;
            else if (IsOnGround)
                _velocity.X = 0f;

            if (IsDead)
            {
                if (_knockbackTimer <= 0f && IsOnGround)
                    _deathRotation = Math.Max(_deathRotation - DeathRotationSpeed * deltaTime, -MathHelper.PiOver2);
                return;
            }

            if (ReachedExit)
                return;

            HandleMovement();
            HandleInventoryKeys();

            foreach (KeyPickup key in keys)
            {
                if (!key.IsCollected && Bounds.Intersects(key.Bounds) && Inventory.TryAdd(ItemType.Key))
                    key.Collect();
            }

            if (Input.WasPressed(Keys.Q) && IsOnGround && Inventory.SelectedItem == ItemType.Key)
                DropKey(keys);

            if (Input.WasPressed(Keys.E) && Bounds.Intersects(door.Bounds))
                UseDoor(door);
        }

        /// <summary>Puts the selected key back into the level, just in front of the player.</summary>
        private void DropKey(List<KeyPickup> keys)
        {
            Vector2 size = KeyPickup.Size;
            Rectangle bounds = Bounds;
            float x = _facingRight ? bounds.Right + 10 : bounds.Left - size.X - 10;
            x = MathHelper.Clamp(x, 0, VirtualScreen.ViewWidth - size.X);

            Inventory.RemoveSelected();
            keys.Add(new KeyPickup(new Vector2(x, bounds.Bottom - size.Y - 10)));
        }

        private void HandleMovement()
        {
            bool left = Input.IsDown(Keys.Left) || Input.IsDown(Keys.A);
            bool right = Input.IsDown(Keys.Right) || Input.IsDown(Keys.D);

            // When both directions are held, the one pressed most recently wins.
            if (Input.WasPressed(Keys.Left) || Input.WasPressed(Keys.A))
                _lastPressedDirection = -1;
            if (Input.WasPressed(Keys.Right) || Input.WasPressed(Keys.D))
                _lastPressedDirection = 1;

            int direction = left && right ? _lastPressedDirection : left ? -1 : right ? 1 : 0;
            if (direction != 0)
            {
                _facingRight = direction > 0;
                Position += new Vector2(direction * WalkSpeed, 0);
            }

            bool jumpPressed = Input.IsDown(Keys.Space) || Input.IsDown(Keys.W) || Input.IsDown(Keys.Up);
            if (jumpPressed && IsOnGround)
            {
                _velocity.Y = JumpVelocity;
                IsOnGround = false;
                _walkingSound.Stop();
                _jumpSound.Stop();
                PlaySound(_jumpSound);
            }

            bool walking = direction != 0 && IsOnGround;
            if (walking && _walkingSound.State != SoundState.Playing)
                PlaySound(_walkingSound);
            else if (!walking && _walkingSound.State == SoundState.Playing)
                _walkingSound.Stop();
        }

        private void HandleInventoryKeys()
        {
            if (Input.WasPressed(Keys.D1) || Input.WasPressed(Keys.NumPad1))
                Inventory.Select(0);
            else if (Input.WasPressed(Keys.D2) || Input.WasPressed(Keys.NumPad2))
                Inventory.Select(1);
            else if (Input.WasPressed(Keys.D3) || Input.WasPressed(Keys.NumPad3))
                Inventory.Select(2);
        }

        private void UseDoor(Door door)
        {
            switch (door.State)
            {
                case DoorState.Locked when Inventory.SelectedItem == ItemType.Key:
                    Inventory.RemoveSelected();
                    door.Unlock();
                    break;
                case DoorState.Closed:
                    door.Open();
                    break;
                case DoorState.Open:
                    ReachedExit = true;
                    _walkingSound.Stop();
                    break;
            }
        }

        public void ApplyGravity()
        {
            _velocity.Y += Gravity;
            Position += _velocity;
            Position = new Vector2(MathHelper.Clamp(Position.X, 0, VirtualScreen.ViewWidth - Width), Position.Y);
        }

        /// <summary>Pushes the player out of any platform they overlap and works out whether they are standing on one.</summary>
        public void ResolveCollisions(IEnumerable<Platform> platforms)
        {
            IsOnGround = false;

            foreach (Platform platform in platforms)
            {
                if (!platform.IsSolid)
                    continue;

                Rectangle bounds = Bounds;
                Rectangle platformBounds = platform.Bounds;
                if (!bounds.Intersects(platformBounds))
                    continue;

                Rectangle overlap = Rectangle.Intersect(bounds, platformBounds);
                if (overlap.Width < overlap.Height)
                {
                    // Walked into the side of the platform.
                    float pushX = bounds.Center.X < platformBounds.Center.X ? -overlap.Width : overlap.Width;
                    Position += new Vector2(pushX, 0);
                }
                else if (bounds.Center.Y < platformBounds.Center.Y)
                {
                    // Landed on top.
                    Position = new Vector2(Position.X, platformBounds.Top - Height);
                    _velocity.Y = 0f;
                    IsOnGround = true;
                    platform.OnPlayerLanded();
                }
                else
                {
                    // Bumped the underside while jumping.
                    Position += new Vector2(0, overlap.Height);
                    _velocity.Y = 0f;
                }
            }
        }

        /// <param name="hearts">Damage in hearts (0.5 = half a heart).</param>
        /// <param name="pushDirection">-1 to knock the player left, 1 to knock them right.</param>
        public void TakeDamage(float hearts, int pushDirection)
        {
            if (IsInvincible || IsDead)
                return;

            _health = Math.Max(0, _health - (int)MathF.Round(hearts * 2));
            _velocity = new Vector2(pushDirection * KnockbackVelocity.X, KnockbackVelocity.Y);
            _knockbackTimer = KnockbackDuration;
            _invincibilityTimer = InvincibilityDuration;
            IsOnGround = false;
            PlaySound(_hitSound);

            if (_health == 0)
            {
                IsDead = true;
                _walkingSound.Stop();
            }
        }

        private static void PlaySound(SoundEffectInstance sound)
        {
            sound.Volume = GameSettings.Current.SoundVolume;
            sound.Play();
        }

        public void StopSounds()
        {
            _walkingSound.Stop();
            _jumpSound.Stop();
            _hitSound.Stop();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = _facingRight ? Assets.PlayerRight : Assets.PlayerLeft;
            bool flashing = IsInvincible && (int)(_invincibilityTimer / FlashInterval) % 2 == 1;
            Color tint = flashing ? Color.Orange : Color.White;

            // Rotate around the bottom-left corner so the player tips over onto the platform when they die.
            var origin = new Vector2(0, texture.Height);
            var position = new Vector2(Position.X, Position.Y + texture.Height * Scale);
            spriteBatch.Draw(texture, position, null, tint, _deathRotation, origin, Scale, SpriteEffects.None, 0f);
        }

        /// <summary>Draws the hearts and inventory in screen space.</summary>
        public void DrawHud(SpriteBatch spriteBatch)
        {
            float heartSpacing = Assets.HeartFull.Width * HeartScale + 5;
            for (int i = 0; i < MaxHealth / 2; i++)
            {
                var position = new Vector2(10 + i * heartSpacing, 20);
                int halfHeartsLeft = _health - i * 2;

                if (halfHeartsLeft >= 2)
                {
                    spriteBatch.Draw(Assets.HeartFull, position, null, Color.White, 0f, Vector2.Zero, HeartScale, SpriteEffects.None, 0f);
                    continue;
                }

                // An empty heart outline, with the left half filled in when half a heart is left.
                spriteBatch.Draw(Assets.HeartEmpty, position, null, Color.White, 0f, Vector2.Zero, HeartScale, SpriteEffects.None, 0f);
                if (halfHeartsLeft == 1)
                {
                    var leftHalf = new Rectangle(0, 0, Assets.HeartFull.Width / 2, Assets.HeartFull.Height);
                    spriteBatch.Draw(Assets.HeartFull, position, leftHalf, Color.White, 0f, Vector2.Zero, HeartScale, SpriteEffects.None, 0f);
                }
            }

            Inventory.Draw(spriteBatch);
        }
    }
}
