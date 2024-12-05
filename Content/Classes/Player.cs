using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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
        private Texture2D _keyTexture; // Texture for the key

        // Knockback variables
        private float knockbackTimer = 0f; // Timer for controlling knockback duration
        private const float knockbackDuration = 0.4f; // Time duration for knockback in seconds

        private const float knockbackStrengthX = 5f; // Knockback strength in the X direction

        private const float knockbackStrengthY = -8f; // Knockback strength in the Y direction

        public bool playerOnGround = true; // Check if the player is on the ground

        private bool isDead = false;
        private float deathRotationSpeed = 1f; // Speed at which the player rotates after death





        public Player(Texture2D textureRight, Texture2D textureLeft, Vector2 startPosition,
                      Texture2D heartFull, Texture2D heartHalf, Texture2D heartEmpty,
                      Texture2D inventoryTexture, Texture2D keyTexture)
        {
            _textureRight = textureRight;
            _textureLeft = textureLeft;
            _currentTexture = _textureRight;
            Position = startPosition;
            Velocity = Vector2.Zero;

            _currentHearts = MaxHearts * 2;
            _heartFullTexture = heartFull;
            _heartHalfTexture = heartHalf;
            _heartEmptyTexture = heartEmpty;
            _inventory = new Inventory(inventoryTexture, keyTexture);  // Create an inventory
            _keyTexture = keyTexture; // Set the key texture
        }

        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, (int)(_currentTexture.Width * 0.1f), (int)(_currentTexture.Height * 0.1f));

        public void Update(GameTime gameTime, KeyboardState keyboardState, Vector2 cameraPosition, int screenWidth)
        {

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
            // Calculate the screen Y position for hearts and inventory
            float topLeftScreenY = cameraPosition.Y + 20;

            // Calculate the X position for inventory (right-aligned)
            int TopRightScreenX = (int)(cameraPosition.X + screenWidth - _inventory.InventoryTexture.Width * 0.7f - 20);

            _inventory.Update(gameTime, TopRightScreenX); // Update the inventory (no need for cameraPosition and screenWidth here)

            UpdateMovement(keyboardState);

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
        }

        public void ApplyGravity(float gravity)
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y + gravity);
            Position += Velocity;
        }

        public void Jump(float jumpStrength)
        {
            Velocity = new Vector2(Velocity.X, jumpStrength);
            IsJumping = true;
        }

        public void ApplyKnockback(Vector2 knockbackDirection)
        {
            // Apply a small knockback force in the X direction only for a short duration
            Velocity = new Vector2(knockbackDirection.X * knockbackStrengthX, Velocity.Y);  // Move player a little bit horizontally, no Y-axis knockback
            Velocity = new Vector2(Velocity.X, knockbackStrengthY);  // Apply vertical knockback
            // Start the knockback timer
            knockbackTimer = knockbackDuration;
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
            // Log to see when the function is called
            Console.WriteLine("Player is dead");

            if (isDead)
            {
                // Log the time step per frame
                Console.WriteLine("Time since last frame: " + gameTime.ElapsedGameTime.TotalSeconds);

                // Reverse the direction of rotation (rotating counterclockwise)
                if (_rotation > -Math.PI / 2) // Ensure it rotates counterclockwise towards downward position
                {
                    _rotation -= deathRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds; // Negative increment for counterclockwise rotation
                    Console.WriteLine("Player rotation: " + _rotation);

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
                // Unlock the door and remove the key from inventory
                door.Unlock();
                _inventory.RemoveFirstKey();
                HasKey = false;
                Console.WriteLine("Key removed from inventory.");
            }
        }


        public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, int viewportWidth, float topLeftScreenY, GameTime gameTime)
        {
            if (isDead)
            {
                deathAnimation(gameTime);
            }

            // Use semi-transparent color for flashing effect
            Color drawColor = IsInvincible && _isFlashing ? Color.Orange : Color.White;

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

            // Draw hearts at the top left of the screen
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
            _inventory.Draw(spriteBatch, topLeftScreenY); // Use passed parameters
        }
    }
}
