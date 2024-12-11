using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

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
        private const int MaxHearts = 3;
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
        private float knockbackTimer = 0f; // Timer for controlling knockback duration
        private const float knockbackDuration = 0.4f; // Time duration for knockback in seconds

        private const float knockbackStrengthX = 5f; // Knockback strength in the X direction

        private const float knockbackStrengthY = -8f; // Knockback strength in the Y direction

        public bool playerOnGround = true; // Check if the player is on the ground

        public bool isOnPlatform = false; // Check if the player is on a platform

        public bool isDead = false;
        private float deathRotationSpeed = 4f; // Speed at which the player rotates after death

        private float jumpStrength = -15f;

        private bool gravityStop = false;

        public bool endLevel = false;

        private bool isEKeyReleased = true; // Check if the E key is released

        private float previousY = 0;
        private float previousX = 0;

        public Player(GraphicsDevice graphicsDevice, Vector2 startPosition)
        {
            _textureRight = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_right.png"));
            _textureLeft = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_left.png"));

            _currentTexture = _textureRight;
            Position = startPosition;
            Velocity = Vector2.Zero;

            _currentHearts = MaxHearts * 2;
            _heartFullTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_full.png"));
            _heartHalfTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_half.png"));
            _heartEmptyTexture = Texture2D.FromFile(graphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_empty.png"));

            _inventory = new Inventory(graphicsDevice);  // Create an inventory
        }

        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, (int)(_currentTexture.Width * 0.1f), (int)(_currentTexture.Height * 0.1f));

        private float previousYUpdateTimer = 0f; // Timer for controlling the delay
        private const float previousYUpdateDelay = 0.2f; // 0.2 seconds delay

        public void Update(GameTime gameTime, KeyboardState keyboardState, Vector2 cameraPosition, int screenWidth, float groundLevel, Item key, Door door)
        {
            // Limit the player so they can't go out of the left or right bounds
            if (Position.X < 0)
            {
                Position = new Vector2(0, Position.Y); // Limit the left bound
            }
            else if (Position.X > screenWidth - BoundingBox.Width)
            {
                Position = new Vector2(screenWidth - BoundingBox.Width, Position.Y); // Limit the right bound
            }

            // Ensure the player picks up the key only when they are directly over it
            if (BoundingBox.Intersects(key.BoundingBox) && !key.Collected)
            {
                // Check if the player's feet are at the same level as the top of the key
                if (BoundingBox.Bottom >= key.BoundingBox.Top && BoundingBox.Top <= key.BoundingBox.Bottom)
                {
                    key.Collect();
                    HasKey = true;
                    AddItemToInventory("Key");  // Add the key to the player's inventory
                    System.Diagnostics.Debug.WriteLine("Key collected and added to inventory!");
                }
            }

            if (BoundingBox.Intersects(door.BoundingBox))
            {
                if (keyboardState.IsKeyDown(Keys.E) && isEKeyReleased)
                {
                    isEKeyReleased = false; // Prevent holding down the key to trigger multiple actions

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
                        endLevel = true; // Start fading out
                        Position = new Vector2(-100, -100); // Move the player out of view to simulate entering
                    }
                }
            }

            // Accumulate elapsed time
            previousYUpdateTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Check if enough time has passed (0.2 seconds)
            if (previousYUpdateTimer >= previousYUpdateDelay)
            {
                previousY = Position.Y; // Update previousY after 0.2 seconds
                previousYUpdateTimer = 0f; // Reset the timer
            }
            playerOnGround = isFalling();

            // Decrease the knockback timer if it's active
            if (knockbackTimer > 0)
            {
                knockbackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (playerOnGround)
            {
                // If knockback duration is over, stop moving the player
                Velocity = new Vector2(0, Velocity.Y);  // Stop the horizontal knockback effect (keep vertical velocity)
            }

            // Calculate the X position for inventory (right-aligned)
            int TopRightScreenX = (int)(cameraPosition.X + screenWidth - _inventory._selectTextures[selectedIndex].Width * 0.7f - 20);

            UpdateMovement(keyboardState);

            _inventory.Update(gameTime, TopRightScreenX, selectedIndex); // Update the inventory (no need for cameraPosition and screenWidth here)

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

            if (isDead)
            {
                //deathAnimation(gameTime);

                // Console.WriteLine("Is on platform: " + isOnPlatform);
                // Console.WriteLine("Is on ground: " + playerOnGround);

                if (knockbackTimer <= 0) // Ensure knockback is finished
                {
                    // Check if the player is not falling and not on any platform/ground
                    if ((!isFalling() && !isOnPlatform) || (!isFalling() && !playerOnGround))
                    {
                        // add logic to move player slowly downwards (increase Position.Y slowly) until he reaches the ground. math: GroundLevel - (boundingBox.Height/2)
                        //Console.WriteLine("Position Y: " + Position.Y + " Ground Level: " + (groundLevel - (BoundingBox.Height / 20)));
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
            // Update jump logic to ensure player can only jump when on ground or platform
            if (keyboardState.IsKeyDown(Keys.Space) && !IsJumping && isOnPlatform)
            {
                Jump(jumpStrength);
            }
            if (keyboardState.IsKeyUp(Keys.E))
            {
                isEKeyReleased = true; // Allow the E key action again when the key is released
            }
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                Position = new Vector2(Position.X - 3f, Position.Y);
                _currentTexture = _textureLeft;
                playerOnGround = true;
            }
            else if (keyboardState.IsKeyDown(Keys.Right))
            {
                Position = new Vector2(Position.X + 3f, Position.Y);
                _currentTexture = _textureRight;
                playerOnGround = true;
            }
            if (keyboardState.IsKeyDown(Keys.D1))
            {
                selectedIndex = 0;
            }
            else if (keyboardState.IsKeyDown(Keys.D2))
            {
                selectedIndex = 1;
            }
            else if (keyboardState.IsKeyDown(Keys.D3))
            {
                selectedIndex = 2;
            }
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
            // Apply a small knockback force in the X direction only for a short duration
            Velocity = new Vector2(knockbackDirection.X * knockbackStrengthX, Velocity.Y);  // Move player a little bit horizontally, no Y-axis knockback
            Velocity = new Vector2(Velocity.X, knockbackStrengthY);  // Apply vertical knockback
            // Start the knockback timer
            knockbackTimer = knockbackDuration;
            isOnPlatform = false;
            playerOnGround = false;
        }

        public void LoseHeart(float amount, int damageDirection)
        {
            if (IsInvincible) return;

            _currentHearts -= amount * 2;
            if (_currentHearts < 0) _currentHearts = 0;

            // Define the knockback direction based on the monster's facing direction
            Vector2 knockbackDirection = (damageDirection == 1) ? new Vector2(1, 0) : new Vector2(-1, 0);

            // Apply the knockback only once (i.e., when the player takes damage)
            ApplyKnockback(knockbackDirection);

            // Trigger invincibility to prevent further damage for a short time
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
                //Reverse the direction of rotation (rotating counterclockwise)
                if (_rotation > -Math.PI / 2) // Ensure it rotates counterclockwise towards downward position
                {
                    _rotation -= deathRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds; // Negative increment for counterclockwise rotation

                    // Clamp the rotation to stop at -Math.PI / 2 (downwards position)
                    if (_rotation < -Math.PI / 2)
                    {
                        _rotation = -(float)Math.PI / 2; // Clamp at -90 degrees (downwards)
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
            _inventory.AddItem(itemName); // Add item to the inventory
        }

        // When the door is unlocked, remove the first key from inventory
        public void UnlockDoor(Door door)
        {
            if (door.IsLocked && HasKey)
            {
                // check if key is selected
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
                    // Unlock the door and remove the key from inventory
                    door.Unlock();
                    _inventory.RemoveFirstKey();
                    HasKey = false;
                    Console.WriteLine("Key removed from inventory.");
                }
            }
        }

        // function to check if y is bigger than previous y
        public bool isFalling()
        {
            //Console.WriteLine("Current Y: " + (int)Position.Y + " Previous Y: " + (int)previousY);
            return (int)Position.Y > (int)previousY;
        }

        private bool isDeadAnimation = false;

        public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, int viewportWidth, float groundLevel, float topLeftScreenY, GameTime gameTime)
        {
            // Player texture drawing logic
            Color drawColor = IsInvincible && _isFlashing ? Color.Orange : Color.White;
            Vector2 origin = new Vector2(0, _currentTexture.Height);

            Vector2 adjustedPosition = new Vector2(Position.X, Position.Y + _currentTexture.Height * 0.1f);

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
                Texture2D heartTexture = _currentHearts >= (i + 1) * 2 ? _heartFullTexture
                                        : _currentHearts >= (i * 2) + 1 ? _heartHalfTexture
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
            // Assume we start off not resolving any platform collision this frame
            bool resolvedPlatform = false;

            foreach (var platform in platforms)
            {
                if (!platform.isVisible)
                    continue;

                Rectangle platformRect = platform.BoundingBox;

                // Update if the player is on the ground (if not on a platform)
                if (!playerOnGround)
                {
                    playerOnGround = Position.Y >= groundLevel - BoundingBox.Height || isOnPlatform;
                    if (Position.Y >= groundLevel - BoundingBox.Height)
                    {
                        isOnPlatform = false;
                    }
                }

                // Check intersection with the platform
                if (BoundingBox.Intersects(platformRect))
                {
                    // Find how much they overlap
                    Rectangle intersection = Rectangle.Intersect(BoundingBox, platformRect);

                    // Determine which dimension has the minimal overlap
                    // This tells us if it's primarily a vertical collision or a horizontal collision.
                    if (intersection.Width < intersection.Height)
                    {
                        // Horizontal collision resolution
                        if (BoundingBox.Center.X < platformRect.Center.X)
                        {
                            // Player hit the platform from the left side
                            Position = new Vector2(Position.X - intersection.Width, Position.Y);
                        }
                        else
                        {
                            // Player hit from the right side
                            Position = new Vector2(Position.X + intersection.Width, Position.Y);
                        }

                        // If we hit from left or right, we shouldn't affect vertical velocity directly
                        // unless we specifically want to stop vertical motion as well.
                    }
                    else
                    {
                        // Vertical collision resolution
                        if (BoundingBox.Center.Y < platformRect.Center.Y)
                        {
                            // Player landed on top of the platform
                            Position = new Vector2(Position.X, Position.Y - intersection.Height);
                            Velocity = new Vector2(Velocity.X, 0);
                            IsJumping = false;
                            isOnPlatform = true;

                            if (platform.IsDisappearing)
                                platform.StartCountdown();
                        }
                        else
                        {
                            // Player hit the platform from below
                            Position = new Vector2(Position.X, Position.Y + intersection.Height);
                            // Stop upward movement
                            Velocity = new Vector2(Velocity.X, 0);
                        }
                    }

                    resolvedPlatform = true;
                }
            }

            // If not on platform and at or below ground level, adjust to ground
            if (!resolvedPlatform && Position.Y >= groundLevel - BoundingBox.Height)
            {
                if (!isDead)
                {
                    Position = new Vector2(Position.X, groundLevel - BoundingBox.Height);
                    Velocity = new Vector2(Velocity.X, 0);
                    IsJumping = false;
                    isOnPlatform = true;
                }
            }
        }

    }
}
