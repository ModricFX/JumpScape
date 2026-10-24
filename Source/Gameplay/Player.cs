using System;
using System.Collections.Generic;
using JumpScape.Core;
using JumpScape.Menus;
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
        private const float ThrowCooldown = 0.4f;
        private const float PowerUpDuration = 15f;
        private const float HighJumpBoost = 1.25f; // About 1.5x as high.
        private const float HardLandingSpeed = 6f;
        private const float SquashDuration = 0.15f;

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
        private float _throwCooldown;
        private float _doubleJumpTimer;
        private float _highJumpTimer;
        private bool _usedAirJump;
        private float _walkTime;
        private float _squashTimer;

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

        public bool HasDoubleJump => _doubleJumpTimer > 0f;

        public bool HasHighJump => _highJumpTimer > 0f;

        public bool CanThrow => !IsDead && !ReachedExit && _throwCooldown <= 0f;

        /// <summary>True for the one frame in which the player jumped.</summary>
        public bool JustJumped { get; private set; }

        /// <summary>True for the one frame in which the player landed after a real fall.</summary>
        public bool JustLanded { get; private set; }

        /// <summary>The bottom-middle of the player, where dust appears.</summary>
        public Vector2 Feet => new Vector2(Position.X + Width / 2f, Position.Y + Height);

        private static int Width => (int)(Assets.PlayerRight.Width * Scale);

        private static int Height => (int)(Assets.PlayerRight.Height * Scale);

        public Rectangle Bounds => new Rectangle((int)MathF.Round(Position.X), (int)MathF.Round(Position.Y), Width, Height);

        public Vector2 Center => Position + new Vector2(Width / 2f, Height / 2f);

        /// <summary>Handles input, picking up and dropping keys, and the power-up, hit and death timers.</summary>
        public void Update(float deltaTime, List<KeyPickup> keys)
        {
            JustJumped = false;
            _squashTimer = Math.Max(0f, _squashTimer - deltaTime);
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= deltaTime;
            _throwCooldown = Math.Max(0f, _throwCooldown - deltaTime);
            _doubleJumpTimer = Math.Max(0f, _doubleJumpTimer - deltaTime);
            _highJumpTimer = Math.Max(0f, _highJumpTimer - deltaTime);

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

            // Holding jump keeps jumping whenever the player lands. The mid-air jump needs a fresh key press.
            bool jumpHeld = Input.IsDown(Keys.Space) || Input.IsDown(Keys.W) || Input.IsDown(Keys.Up);
            bool jumpPressed = Input.WasPressed(Keys.Space) || Input.WasPressed(Keys.W) || Input.WasPressed(Keys.Up);
            if (jumpHeld && IsOnGround)
            {
                Jump();
            }
            else if (jumpPressed && !IsOnGround && HasDoubleJump && !_usedAirJump)
            {
                Jump();
                _usedAirJump = true;
            }

            bool walking = direction != 0 && IsOnGround;
            _walkTime = walking ? _walkTime + 1f / 60f : 0f;
            if (walking && _walkingSound.State != SoundState.Playing)
                PlaySound(_walkingSound);
            else if (!walking && _walkingSound.State == SoundState.Playing)
                _walkingSound.Stop();
        }

        private void Jump()
        {
            _velocity.Y = HasHighJump ? JumpVelocity * HighJumpBoost : JumpVelocity;
            IsOnGround = false;
            JustJumped = true;
            _walkingSound.Stop();
            _jumpSound.Stop();
            PlaySound(_jumpSound);
        }

        /// <summary>Throws an orb from the player towards a point in the world.</summary>
        public Projectile Throw(Vector2 target)
        {
            _throwCooldown = ThrowCooldown;

            Vector2 start = Center;
            Vector2 direction = target - start;
            if (direction.LengthSquared() < 1f)
                direction = new Vector2(_facingRight ? 1 : -1, 0);
            _facingRight = direction.X >= 0;

            Assets.PlayerJump.Play(0.5f * GameSettings.Current.SoundVolume, 0.6f, 0f);
            return new Projectile(start, direction);
        }

        /// <summary>Applies a power-up. Returns false if it would do nothing (a heart when health is already full).</summary>
        public bool TryCollect(PowerUpType type)
        {
            if (IsDead || ReachedExit)
                return false;

            switch (type)
            {
                case PowerUpType.Health:
                    if (_health >= MaxHealth)
                        return false;
                    _health = Math.Min(MaxHealth, _health + 2);
                    break;
                case PowerUpType.DoubleJump:
                    _doubleJumpTimer = PowerUpDuration;
                    break;
                case PowerUpType.HighJump:
                    _highJumpTimer = PowerUpDuration;
                    break;
            }

            Assets.PlayerJump.Play(0.6f * GameSettings.Current.SoundVolume, 1f, 0f);
            return true;
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

        /// <summary>Called when the player goes through the level's exit door.</summary>
        public void ReachExit()
        {
            ReachedExit = true;
            _walkingSound.Stop();
        }

        /// <summary>Puts the player somewhere else, standing still (used when going through a portal).</summary>
        public void MoveTo(Vector2 position)
        {
            Position = position;
            _velocity = Vector2.Zero;
            _knockbackTimer = 0f;
            _walkingSound.Stop();
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
            JustLanded = false;

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
                    if (_velocity.Y > HardLandingSpeed)
                    {
                        JustLanded = true;
                        _squashTimer = SquashDuration;
                    }
                    Position = new Vector2(Position.X, platformBounds.Top - Height);
                    _velocity.Y = 0f;
                    IsOnGround = true;
                    _usedAirJump = false;
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
            // Once through the exit door the player is inside it.
            if (ReachedExit)
                return;

            Texture2D texture = _facingRight ? Assets.PlayerRight : Assets.PlayerLeft;
            bool flashing = IsInvincible && (int)(_invincibilityTimer / FlashInterval) % 2 == 1;
            Color tint = flashing ? Color.Orange : Color.White;

            if (IsDead)
            {
                // Rotate around the bottom-left corner so the player tips over onto the platform.
                var corner = new Vector2(Position.X, Position.Y + texture.Height * Scale);
                spriteBatch.Draw(texture, corner, null, tint, _deathRotation, new Vector2(0, texture.Height), Scale, SpriteEffects.None, 0f);
                return;
            }

            // A short squash after landing, and a small bounce while walking.
            float squash = _squashTimer / SquashDuration * 0.12f;
            var scale = new Vector2(Scale * (1f + squash), Scale * (1f - squash));
            float bounce = MathF.Abs(MathF.Sin(_walkTime * 12f)) * 4f;

            var feet = new Vector2(Position.X + Width / 2f, Position.Y + texture.Height * Scale - bounce);
            spriteBatch.Draw(texture, feet, null, tint, 0f, new Vector2(texture.Width / 2f, texture.Height), scale, SpriteEffects.None, 0f);
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
            DrawPowerUpTimers(spriteBatch);
        }

        /// <summary>Shows each active power-up under the hearts, with a bar for the time left.</summary>
        private void DrawPowerUpTimers(SpriteBatch spriteBatch)
        {
            const float iconSize = 36f;
            const int barWidth = 120;
            const int barHeight = 8;
            float y = 20 + Assets.HeartFull.Height * HeartScale + 12;

            DrawTimer(PowerUpType.DoubleJump, _doubleJumpTimer);
            DrawTimer(PowerUpType.HighJump, _highJumpTimer);

            void DrawTimer(PowerUpType type, float timeLeft)
            {
                if (timeLeft <= 0f)
                    return;

                PowerUp.DrawIcon(spriteBatch, type, new Vector2(10 + iconSize / 2f, y + iconSize / 2f), iconSize, Color.White);

                var bar = new Rectangle((int)(10 + iconSize + 10), (int)(y + (iconSize - barHeight) / 2f), barWidth, barHeight);
                spriteBatch.Draw(Assets.Pixel, bar, Color.Black * 0.5f);
                bar.Width = (int)(barWidth * timeLeft / PowerUpDuration);
                spriteBatch.Draw(Assets.Pixel, bar, PowerUp.GlowColorFor(type));
                UiDrawing.DrawOutline(spriteBatch, new Rectangle(bar.X, bar.Y, barWidth, barHeight), Color.Black * 0.6f, 1);

                y += iconSize + 8;
            }
        }
    }
}
