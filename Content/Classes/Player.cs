using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio; // Add this import at the top

namespace JumpScape.Classes
{
    public class Player
    {
        private readonly Texture2D _textureRight;
        private readonly Texture2D _textureLeft;
        private Texture2D _currentTexture;

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public bool HasKey { get; set; }
        public bool IsJumping { get; set; }

        // Heart System Variables
        private const int MaxHearts = 1;
        private float _currentHearts;
        private readonly Texture2D _heartFullTexture;
        private readonly Texture2D _heartHalfTexture;
        private readonly Texture2D _heartEmptyTexture;
        private float _rotation = 0f; // Rotation angle for the player

        // Damage Cooldown and Flashing Variables
        public bool IsInvincible { get; private set; }
        private float _invincibilityTimer;
        private const float InvincibilityDuration = 1.4f;
        private bool _isFlashing;
        private float _flashTimer;
        private const float FlashInterval = 0.2f;

        // Inventory System
        private Inventory _inventory;
        private int selectedIndex = 0; // The currently selected inventory item (0, 1, or 2)

        // Knockback variables
        private float knockbackTimer = 0f;          // Timer for controlling knockback duration
        private const float knockbackDuration = 0.4f;
        private const float knockbackStrengthX = 5f;
        private const float knockbackStrengthY = -8f;

        public bool playerOnGround = true;  // Check if the player is on the ground
        public bool isOnPlatform = false;   // Check if the player is on a platform
        public bool isDead = false;
        private float deathRotationSpeed = 4f; // Speed at which the player rotates after death
        private float jumpStrength = -15f;
        private bool gravityStop = false;
        public bool endLevel = false;
        private bool isEKeyReleased = true; // Check if the E key is released
        private float previousY = 0;

        // Walking sound (looped instance)
        private SoundEffectInstance _walkingSoundInstance;

        public Player(GraphicsDevice graphicsDevice, Vector2 startPosition)
        {
            _textureRight = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_right.png"));
            _textureLeft  = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_left.png"));

            _currentTexture = _textureRight;
            Position = startPosition;
            Velocity = Vector2.Zero;

            _currentHearts = MaxHearts * 2;
            _heartFullTexture  = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_full.png"));
            _heartHalfTexture  = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_half.png"));
            _heartEmptyTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_empty.png"));

            // Inventory
            _inventory = new Inventory(graphicsDevice);

            // Load walking sound as a looped instance
            var walkingSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlayerWalking.wav"));
            _walkingSoundInstance = walkingSound.CreateInstance();
            _walkingSoundInstance.IsLooped = true;
        }

        public Rectangle BoundingBox 
            => new Rectangle((int)Position.X, (int)Position.Y,
                             (int)(_currentTexture.Width * 0.1f),
                             (int)(_currentTexture.Height * 0.1f));

        private float previousYUpdateTimer = 0f;
        private const float previousYUpdateDelay = 0.2f;

        public void Update(GameTime gameTime, KeyboardState keyboardState, Vector2 cameraPosition, 
                           int screenWidth, float groundLevel, Item key, Door door)
        {
            // Keep player within left/right screen bounds
            if (Position.X < 0)
            {
                Position = new Vector2(0, Position.Y);
            }
            else if (Position.X > screenWidth - BoundingBox.Width)
            {
                Position = new Vector2(screenWidth - BoundingBox.Width, Position.Y);
            }

            // Key pickup
            if (key != null)
            {
                if (BoundingBox.Intersects(key.BoundingBox) && !key.Collected)
                {
                    if (BoundingBox.Bottom >= key.BoundingBox.Top && BoundingBox.Top <= key.BoundingBox.Bottom)
                    {
                        key.Collect();
                        HasKey = true;
                        AddItemToInventory("Key");
                        System.Diagnostics.Debug.WriteLine("Key collected and added to inventory!");
                    }
                }
            }

            // Door interaction
            if (BoundingBox.Intersects(door.BoundingBox))
            {
                if (keyboardState.IsKeyDown(Keys.E) && isEKeyReleased)
                {
                    isEKeyReleased = false; // Prevent holding key for multiple triggers

                    if (door.IsLocked && !HasKey)
                    {
                        System.Diagnostics.Debug.WriteLine("You need a key!");
                    }
                    else if (door.IsLocked && HasKey)
                    {
                        UnlockDoor(door);
                        System.Diagnostics.Debug.WriteLine("Door unlocked. Press E to open.");
                    }
                    else if (door.IsUnlocked)
                    {
                        door.Open();
                        System.Diagnostics.Debug.WriteLine("Door opened. Press E to enter.");
                    }
                    else if (door.IsOpened)
                    {
                        System.Diagnostics.Debug.WriteLine("Entering the door...");
                        endLevel = true; // Start fading out or level transition
                    }
                }
            }

            // Update previousY every 0.2 seconds (for isFalling detection)
            previousYUpdateTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (previousYUpdateTimer >= previousYUpdateDelay)
            {
                previousY = Position.Y;
                previousYUpdateTimer = 0f;
            }

            // Correctly set playerOnGround based on falling
            playerOnGround = !isFalling();

            // Knockback timer
            if (knockbackTimer > 0)
            {
                knockbackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (playerOnGround)
            {
                // Stop horizontal movement after knockback ends
                Velocity = new Vector2(0, Velocity.Y);
            }

            // Update horizontal movement and sound
            UpdateMovement(keyboardState);

            // Update inventory (position only if needed)
            int topRightScreenX = (int)(cameraPosition.X + screenWidth - _inventory._selectTextures[selectedIndex].Width * 0.7f - 20);
            _inventory.Update(gameTime, topRightScreenX, selectedIndex);

            // Handle invincibility flashing
            if (IsInvincible)
            {
                _invincibilityTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _flashTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_flashTimer >= FlashInterval)
                {
                    _isFlashing = !_isFlashing;
                    _flashTimer = 0f;
                }

                if (_invincibilityTimer >= InvincibilityDuration)
                {
                    ResetInvincibility();
                }
            }

            // Handle death falling
            if (isDead)
            {
                if (knockbackTimer <= 0)
                {
                    // If not falling & not on any platform/ground, slowly move down until ground reached
                    if ((!isFalling() && !isOnPlatform) || (!isFalling() && !playerOnGround))
                    {
                        if (Position.Y + BoundingBox.Height / 2 <= groundLevel)
                        {
                            deathAnimation(gameTime);
                            if (!isDead)
                            {
                                Position = new Vector2(Position.X, Position.Y + 2);
                            }
                            isDeadAnimation = true;
                        }
                    }
                }
            }
        }

        private void ResetInvincibility()
        {
            IsInvincible = false;
            _isFlashing = false;
            _invincibilityTimer = 0f;
            _flashTimer = 0f;
        }

        public void UpdateMovement(KeyboardState keyboardState)
        {
            if (isDead) return;

            // Jump only if on a platform
            if (keyboardState.IsKeyDown(Keys.Space) && !IsJumping && isOnPlatform)
            {
                // Stop walking sound when jumping
                if (_walkingSoundInstance.State == SoundState.Playing)
                    _walkingSoundInstance.Stop();

                Jump(jumpStrength);
            }

            // Door interaction re-allow
            if (keyboardState.IsKeyUp(Keys.E))
            {
                isEKeyReleased = true;
            }

            // Horizontal movement
            bool isMovingHorizontally = false;

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                Position = new Vector2(Position.X - 3f, Position.Y);
                _currentTexture = _textureLeft;
                isMovingHorizontally = true;
            }
            else if (keyboardState.IsKeyDown(Keys.Right))
            {
                Position = new Vector2(Position.X + 3f, Position.Y);
                _currentTexture = _textureRight;
                isMovingHorizontally = true;
            }

            // Play walking sound if moving and on ground or platform
            if (!isDead && isMovingHorizontally && (playerOnGround || isOnPlatform))
            {
                if (_walkingSoundInstance.State != SoundState.Playing)
                {
                    System.Diagnostics.Debug.WriteLine("Playing walking sound now!");
                    _walkingSoundInstance.Play();
                }
            }
            else
            {
                // Stop if not moving or in the air
                if (_walkingSoundInstance.State == SoundState.Playing)
                {
                    _walkingSoundInstance.Stop();
                }
            }

            // Inventory selection
            if (keyboardState.IsKeyDown(Keys.D1)) selectedIndex = 0;
            else if (keyboardState.IsKeyDown(Keys.D2)) selectedIndex = 1;
            else if (keyboardState.IsKeyDown(Keys.D3)) selectedIndex = 2;
        }

        public void ApplyGravity(float gravity)
        {
            if (!gravityStop)
            {
                Velocity = new Vector2(Velocity.X, Velocity.Y + gravity);
                Position += Velocity;
            }
        }

        public void Jump(float jumpStrength)
        {
            if (isDead) return;
            Velocity = new Vector2(Velocity.X, jumpStrength);
            IsJumping = true;
            isOnPlatform = false;
        }

        public void ApplyKnockback(Vector2 knockbackDirection)
        {
            Velocity = new Vector2(knockbackDirection.X * knockbackStrengthX, Velocity.Y);
            Velocity = new Vector2(Velocity.X, knockbackStrengthY);
            knockbackTimer = knockbackDuration;
            isOnPlatform = false;
            playerOnGround = false;
        }

        public void LoseHeart(float amount, int damageDirection)
        {
            if (IsInvincible) return;

            _currentHearts -= amount * 2;
            if (_currentHearts < 0) _currentHearts = 0;

            // Determine knockback direction
            Vector2 knockbackDirection = (damageDirection == 1) 
                ? new Vector2(1, 0) 
                : new Vector2(-1, 0);

            ApplyKnockback(knockbackDirection);
            TriggerInvincibility();

            if (_currentHearts <= 0)
            {
                isDead = true;
            }
        }

        private void deathAnimation(GameTime gameTime)
        {
            if (isDead)
            {
                // Rotate counterclockwise to -90 degrees
                if (_rotation > -Math.PI / 2)
                {
                    _rotation -= deathRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_rotation < -Math.PI / 2)
                    {
                        _rotation = -(float)Math.PI / 2;
                    }
                }
            }
        }

        private void TriggerInvincibility()
        {
            IsInvincible = true;
            _flashTimer = 0f;
            _invincibilityTimer = 0f;
            _isFlashing = false;
        }

        // Add item to inventory
        public void AddItemToInventory(string itemName)
        {
            _inventory.AddItem(itemName);
        }

        // When the door is unlocked, remove the first key from inventory
        public void UnlockDoor(Door door)
        {
            if (door.IsLocked && HasKey)
            {
                // find the key index in the inventory
                int keyIndex;
                for (keyIndex = 0; keyIndex < _inventory.MaxInventoryItems; keyIndex++)
                {
                    if (_inventory._inventory[keyIndex] == "Key")
                    {
                        break;
                    }
                }
                if (selectedIndex == keyIndex)
                {
                    door.Unlock();
                    _inventory.RemoveFirstKey();
                    HasKey = false;
                    Console.WriteLine("Key removed from inventory.");
                }
            }
        }

        // Returns true if the player's Y is increasing (falling downward)
        public bool isFalling()
        {
            return (int)Position.Y > (int)previousY;
        }

        private bool isDeadAnimation = false;

        public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, int viewportWidth, 
                         float groundLevel, float topLeftScreenY, GameTime gameTime)
        {
            // Flashing color if invincible
            Color drawColor = (IsInvincible && _isFlashing) ? Color.Orange : Color.White;

            Vector2 origin           = new Vector2(0, _currentTexture.Height);
            Vector2 adjustedPosition = new Vector2(Position.X, Position.Y + _currentTexture.Height * 0.1f);

            // If playing the death animation
            if (isDeadAnimation)
            {
                spriteBatch.Draw(
                    _currentTexture,
                    adjustedPosition,
                    null,
                    drawColor,
                    _rotation,
                    origin,
                    0.1f,
                    SpriteEffects.None,
                    0f
                );
            }
            else
            {
                spriteBatch.Draw(
                    _currentTexture,
                    Position,
                    null,
                    drawColor,
                    _rotation,
                    Vector2.Zero,
                    0.1f,
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw hearts
            float heartScale = 0.1f;
            for (int i = 0; i < MaxHearts; i++)
            {
                Texture2D heartTexture = _currentHearts >= (i + 1) * 2 
                    ? _heartFullTexture
                    : _currentHearts >= (i * 2) + 1
                        ? _heartHalfTexture
                        : _heartEmptyTexture;

                spriteBatch.Draw(
                    heartTexture,
                    new Vector2(10 + i * (_heartFullTexture.Width * heartScale + 5), topLeftScreenY),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    heartScale,
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw the inventory
            _inventory.Draw(spriteBatch, topLeftScreenY);
        }

        internal void CheckPlatforms(List<Platform> platforms, float groundLevel)
        {
            bool resolvedPlatform = false;

            foreach (var platform in platforms)
            {
                if (!platform.isVisible)
                    continue;

                Rectangle platformRect = platform.BoundingBox;

                // If we are not already on the ground, check if we should set it
                if (!playerOnGround)
                {
                    // If Y is at or below the ground or on a platform
                    playerOnGround = (Position.Y >= groundLevel - BoundingBox.Height) || isOnPlatform;
                    if (Position.Y >= groundLevel - BoundingBox.Height)
                    {
                        isOnPlatform = false;
                    }
                }

                // Check collision with this platform
                if (BoundingBox.Intersects(platformRect))
                {
                    Rectangle intersection = Rectangle.Intersect(BoundingBox, platformRect);

                    // Horizontal collision
                    if (intersection.Width < intersection.Height)
                    {
                        if (BoundingBox.Center.X < platformRect.Center.X)
                        {
                            // Collided from left side
                            Position = new Vector2(Position.X - intersection.Width, Position.Y);
                        }
                        else
                        {
                            // Collided from right side
                            Position = new Vector2(Position.X + intersection.Width, Position.Y);
                        }
                    }
                    else
                    {
                        // Vertical collision
                        if (BoundingBox.Center.Y < platformRect.Center.Y)
                        {
                            // Landed on top of the platform
                            Position = new Vector2(Position.X, platformRect.Top - BoundingBox.Height);
                            Velocity = new Vector2(Velocity.X, 0);
                            IsJumping = false;
                            isOnPlatform = true;

                            if (platform.IsDisappearing)
                                platform.StartCountdown();

                            // Reset previousY to avoid flickering in isFalling
                            previousY = Position.Y;
                        }
                        else
                        {
                            // Hit from below
                            Position = new Vector2(Position.X, Position.Y + intersection.Height);
                            Velocity = new Vector2(Velocity.X, 0);
                        }
                    }

                    resolvedPlatform = true;
                }
            }

            // If we didn't resolve any platform collision, the player is not on a platform
            if (!resolvedPlatform)
            {
                isOnPlatform = false;
            }
        }
    }
}
