using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;

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

        private const int MaxHearts = 5;
        private float _currentHearts;
        private readonly Texture2D _heartFullTexture;
        private readonly Texture2D _heartHalfTexture;
        private readonly Texture2D _heartEmptyTexture;
        private float _rotation = 0f;

        public bool IsInvincible { get; private set; }
        private float _invincibilityTimer;
        private const float InvincibilityDuration = 1.4f;
        private bool _isFlashing;
        private float _flashTimer;
        private const float FlashInterval = 0.2f;

        private Inventory _inventory;
        private int selectedIndex = 0;

        private float knockbackTimer = 0f;
        private const float knockbackDuration = 0.4f;
        private const float knockbackStrengthX = 5f;
        private const float knockbackStrengthY = -8f;

        public bool playerOnGround = true;
        public bool isOnPlatform = false;
        public bool isDead = false;
        private float deathRotationSpeed = 4f; 
        private float jumpStrength;
        private bool gravityStop = false;
        public bool endLevel = false;
        private bool isEKeyReleased = true; 
        private float previousY = 0;

        private float _scaleFactorX;
        private float _scaleFactorY;

        private const float BaseJumpStrength = -15f;

        // Sound instances
        private SoundEffectInstance _walkingSoundInstance;
        private SoundEffectInstance _jumpingSoundInstance;
        private SoundEffectInstance _playerHitSoundInstance;

        // Reference to GameSettings
        private GameSettings settings;

        public Player(GraphicsDevice graphicsDevice, Vector2 startPosition, int screenWidth, int screenHeight)
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

            _inventory = new Inventory(graphicsDevice);

            // Load sounds
            var walkingSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlayerWalking.wav"));
            _walkingSoundInstance = walkingSound.CreateInstance();
            _walkingSoundInstance.IsLooped = true;

            var jumpingSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlayerJump.wav"));
            _jumpingSoundInstance = jumpingSound.CreateInstance();

            var playerHitSound = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "PlayerHit.wav"));
            _playerHitSoundInstance = playerHitSound.CreateInstance();

            // Scale calculation
            const float BaseWidth = 1920f;
            const float BaseHeight = 1080f;
            _scaleFactorX = screenWidth / BaseWidth * 0.15f;
            _scaleFactorY = screenHeight / BaseHeight * 0.15f;
            jumpStrength = BaseJumpStrength * _scaleFactorY * 9.5f;

            // Load settings & apply volume
            settings = GameSettings.Load();
            setVolume();
        }

        // ----------------------------------------------------------------------------
        //  Set SFX volume based on settings.Volume (0 - 100) => (0.0 - 1.0)
        // ----------------------------------------------------------------------------
        private void setVolume()
        {
            float volumeFactor = settings.Volume / 100f; // 0..1
            _walkingSoundInstance.Volume = volumeFactor;
            _jumpingSoundInstance.Volume = volumeFactor;
            _playerHitSoundInstance.Volume = volumeFactor;
        }

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(_currentTexture.Width * _scaleFactorX),
            (int)(_currentTexture.Height * _scaleFactorY)
        );

        private float previousYUpdateTimer = 0f;
        private const float previousYUpdateDelay = 0.2f;

        public void Update(GameTime gameTime, KeyboardState keyboardState, Vector2 cameraPosition,
                           int screenWidth, float groundLevel, Item key, Door door)
        {
            // -- (1) Ensure volume is always up-to-date (in case user changed it mid-level):
            setVolume();

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
                    }
                }
            }

            // Door interaction
            if (BoundingBox.Intersects(door.BoundingBox))
            {
                if (keyboardState.IsKeyDown(Keys.E) && isEKeyReleased)
                {
                    isEKeyReleased = false; 

                    if (door.IsLocked && !HasKey)
                    {
                        // Needs key
                    }
                    else if (door.IsLocked && HasKey)
                    {
                        UnlockDoor(door);
                    }
                    else if (door.IsUnlocked)
                    {
                        door.Open();
                    }
                    else if (door.IsOpened)
                    {
                        endLevel = true; 
                    }
                }
            }
            if (keyboardState.IsKeyUp(Keys.E))
            {
                isEKeyReleased = true;
            }

            // Update previousY
            previousYUpdateTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (previousYUpdateTimer >= previousYUpdateDelay)
            {
                previousY = Position.Y;
                previousYUpdateTimer = 0f;
            }
            playerOnGround = !isFalling();

            // Knockback countdown
            if (knockbackTimer > 0)
            {
                knockbackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (playerOnGround)
            {
                Velocity = new Vector2(0, Velocity.Y);
            }

            // Horizontal movement & jump
            UpdateMovement(keyboardState);

            // Update inventory position
            int topRightScreenX = (int)(cameraPosition.X + screenWidth - _inventory._selectTextures[selectedIndex].Width * 0.7f - 20);
            _inventory.Update(gameTime, topRightScreenX, selectedIndex);

            // Invincibility flashing
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

            // Death
            if (isDead)
            {
                if (knockbackTimer <= 0)
                {
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

            // Jump
            if (keyboardState.IsKeyDown(Keys.Space) && !IsJumping && isOnPlatform)
            {
                if (_walkingSoundInstance.State == SoundState.Playing)
                    _walkingSoundInstance.Stop();

                if (_jumpingSoundInstance.State == SoundState.Playing)
                    _jumpingSoundInstance.Stop();

                Jump(jumpStrength);
            }

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

            // Walking sound
            if (!isDead && isMovingHorizontally && (playerOnGround || isOnPlatform))
            {
                if (_walkingSoundInstance.State != SoundState.Playing)
                {
                    _walkingSoundInstance.Play();
                }
            }
            else
            {
                if (_walkingSoundInstance.State == SoundState.Playing)
                {
                    _walkingSoundInstance.Stop();
                }
            }
            if (isDead && _walkingSoundInstance.State == SoundState.Playing)
            {
                _walkingSoundInstance.Stop();
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
            _jumpingSoundInstance.Play();
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

            Vector2 knockbackDirection = (damageDirection == 1)
                ? new Vector2(1, 0)
                : new Vector2(-1, 0);

            ApplyKnockback(knockbackDirection);
            TriggerInvincibility();
            _playerHitSoundInstance.Play();

            if (_currentHearts <= 0)
            {
                isDead = true;
            }
        }

        private void deathAnimation(GameTime gameTime)
        {
            if (isDead)
            {
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

        public void AddItemToInventory(string itemName)
        {
            _inventory.AddItem(itemName);
        }

        public void UnlockDoor(Door door)
        {
            if (door.IsLocked && HasKey)
            {
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
                }
            }
        }

        public bool isFalling()
        {
            return (int)Position.Y > (int)previousY;
        }

        private bool isDeadAnimation = false;

        public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, int viewportWidth,
                         float groundLevel, float topLeftScreenY, GameTime gameTime)
        {
            Color drawColor = (IsInvincible && _isFlashing) ? Color.Orange : Color.White;

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
                    new Vector2(_scaleFactorX, _scaleFactorY),
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
                    new Vector2(_scaleFactorX, _scaleFactorY),
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw hearts
            float heartScale = 0.1f;
            for (int i = 0; i < MaxHearts; i++)
            {
                Texture2D heartTexture =
                    _currentHearts >= (i + 1) * 2 ? _heartFullTexture :
                    _currentHearts >= (i * 2) + 1 ? _heartHalfTexture :
                    _heartEmptyTexture;

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

            // Inventory
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

                if (!playerOnGround)
                {
                    playerOnGround = (Position.Y >= groundLevel - BoundingBox.Height) || isOnPlatform;
                    if (Position.Y >= groundLevel - BoundingBox.Height)
                    {
                        isOnPlatform = false;
                    }
                }

                if (BoundingBox.Intersects(platformRect))
                {
                    Rectangle intersection = Rectangle.Intersect(BoundingBox, platformRect);

                    if (intersection.Width < intersection.Height)
                    {
                        if (BoundingBox.Center.X < platformRect.Center.X)
                        {
                            Position = new Vector2(Position.X - intersection.Width, Position.Y);
                        }
                        else
                        {
                            Position = new Vector2(Position.X + intersection.Width, Position.Y);
                        }
                    }
                    else
                    {
                        if (BoundingBox.Center.Y < platformRect.Center.Y)
                        {
                            // Landed on top
                            Position = new Vector2(Position.X, platformRect.Top - BoundingBox.Height);
                            Velocity = new Vector2(Velocity.X, 0);
                            IsJumping = false;
                            isOnPlatform = true;

                            if (platform.IsDisappearing)
                                platform.StartCountdown();

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

            if (!resolvedPlatform)
            {
                isOnPlatform = false;
            }
        }
    }
}
