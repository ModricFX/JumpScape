using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using JumpScape.Classes;

namespace JumpScape
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D groundTexture;
        private Player player;
        private Door door;
        private Item key;

        private SpriteFont font; // Font for rendering text

        private float groundLevel;
        private List<Platform> platforms;
        private List<Monster> monsters;
        private bool isEKeyReleased = true;
        private Matrix cameraTransform;
        private Vector2 cameraPosition;
        private float cameraFollowThreshold;
        private float gravity = 0.6f;
        private float jumpStrength = -15f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load textures
            groundTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "grass.png"));

            Texture2D platformTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "grass.png"));
            Texture2D frogTextureLeft = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left.png"));
            Texture2D frogTextureRight = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right.png"));
            Texture2D playerRightTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_right.png"));
            Texture2D playerLeftTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Player", "player_left.png"));
            Texture2D lockedDoorTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_locked.png"));
            Texture2D doorUnlockedTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_unlocked.png"));
            Texture2D doorOpenedTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Doors", "door_opened.png"));
            Texture2D keyTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Items", "key_sprite_sheet.png"));
            Texture2D keyTextureInventory = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Items", "key_inventory.png"));
            Texture2D textureLeftYellow = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_left_yellow.png"));
            Texture2D textureRightYellow = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Monsters", "frog_monster_right_yellow.png"));

            Texture2D heartFullTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_full.png"));
            Texture2D heartHalfTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_half.png"));
            Texture2D heartEmptyTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Hearts", "Heart_empty.png"));

            Texture2D inventoryTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Inventory", "Inventory.png"));

            font = Content.Load<SpriteFont>("Fonts/DefaultFont");

            // Set the ground level
            groundLevel = GraphicsDevice.Viewport.Height - groundTexture.Height - 150;

            // Load the level data
            LevelLoader levelLoader = new LevelLoader();
            levelLoader.LoadLevel(Path.Combine("Levels", "Level1.txt"));

            // Initialize player, door, and key
            player = new Player(playerRightTexture, playerLeftTexture, levelLoader.PlayerSpawn, heartFullTexture, heartHalfTexture, heartEmptyTexture, inventoryTexture, keyTextureInventory);
            door = new Door(lockedDoorTexture, doorUnlockedTexture, doorOpenedTexture, levelLoader.DoorData.Item1, levelLoader.DoorData.Item2);

            // Check if there's a valid key position before creating the key
            if (levelLoader.KeyPosition != Vector2.Zero) // Assuming Vector2.Zero means no key in the level
            {
                key = new AnimatedItem(keyTexture, levelLoader.KeyPosition, totalFrames: 16, frameTime: 0.1f);
            }
            else
            {
                key = null; // No key in this level
            }

            // Initialize platforms and monsters
            platforms = new List<Platform>();
            monsters = new List<Monster>();
            foreach (var (position, length, hasMonster) in levelLoader.PlatformData)
            {
                Platform platform = new Platform(platformTexture, position, length);
                platforms.Add(platform);

                if (hasMonster)
                {
                    Vector2 monsterPosition = new Vector2(position.X, position.Y - frogTextureLeft.Height / 19);
                    Rectangle platformBounds = platform.BoundingBox;
                    monsters.Add(new Monster(frogTextureLeft, frogTextureRight, textureLeftYellow, textureRightYellow, monsterPosition, platformBounds));
                }
            }

            // Initialize camera position
            cameraPosition = Vector2.Zero;

            // Set the threshold for the camera to start following the player
            cameraFollowThreshold = GraphicsDevice.Viewport.Height * 0.05f; // 20% from the top
        }


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            var keyboardState = Keyboard.GetState();
            Vector2 previousPosition = player.Position; // Store the previous position for collision correction

            
            // Update player movement
            player.Update(gameTime, keyboardState, cameraPosition, GraphicsDevice.Viewport.Width);


            // Limit the player so they can't go out of the left or right bounds
            if (player.Position.X < 0)
            {
                player.Position = new Vector2(0, player.Position.Y); // Limit the left bound
            }

            // Update each monster
            foreach (var monster in monsters)
            {
                monster.Update(gameTime, player.Position);
            }

            // Check for collisions between the player and monsters
            foreach (var monster in monsters)
            {
                if (player.BoundingBox.Intersects(monster.BoundingBox))
                {
                    bool isMonsterFacingPlayer = monster.IsFacingPlayer(player.Position);

                    if (!player.IsInvincible)
                    {
                        if (isMonsterFacingPlayer)
                            player.LoseHeart(1);
                        else
                            player.LoseHeart(0.5f);
                    }
                }
            }




            // ---- Key Animation Update ----
            if (key is AnimatedItem animatedKey)
            {
                animatedKey.Update(gameTime);
            }
            // ---- End Key Animation Update ----

            // Check for interaction with the key
            Rectangle playerRect = player.BoundingBox;
            Rectangle keyRect = key.BoundingBox;

            // Ensure the player picks up the key only when they are directly over it
            if (playerRect.Intersects(keyRect) && !key.Collected)
            {
                // Check if the player's feet are at the same level as the top of the key
                if (playerRect.Bottom >= keyRect.Top && playerRect.Top <= keyRect.Bottom)
                {
                    key.Collect();
                    player.HasKey = true;
                    player.AddItemToInventory("Key");  // Add the key to the player's inventory
                    System.Diagnostics.Debug.WriteLine("Key collected and added to inventory!");
                }
            }

            // Check for interaction with the door
            Rectangle doorRect = door.BoundingBox;

            if (playerRect.Intersects(doorRect))
            {
                if (keyboardState.IsKeyDown(Keys.E) && isEKeyReleased)
                {
                    isEKeyReleased = false; // Prevent holding down the key to trigger multiple actions

                    if (door.IsLocked && !player.HasKey)
                    {
                        System.Diagnostics.Debug.WriteLine("You need a key!");
                    }
                    else if (door.IsLocked && player.HasKey)
                    {
                        player.UnlockDoor(door);
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
                        player.Position = new Vector2(-100, -100); // Move the player out of view to simulate entering
                    }
                }
            }

            if (keyboardState.IsKeyUp(Keys.E))
            {
                isEKeyReleased = true; // Allow the E key action again when the key is released
            }

            // Platform collision logic
            player.ApplyGravity(gravity);
            bool isOnPlatform = false;

            foreach (var platform in platforms)
            {
                playerRect = player.BoundingBox;
                Rectangle platformRect = platform.BoundingBox;

                if (playerRect.Intersects(platformRect))
                {
                    // Ensure the player is landing from above
                    if (player.Velocity.Y > 0 && playerRect.Bottom >= platformRect.Top && playerRect.Bottom - player.Velocity.Y <= platformRect.Top)
                    {
                        player.Position = new Vector2(player.Position.X, platform.Position.Y - playerRect.Height);
                        player.Velocity = new Vector2(player.Velocity.X, 0);
                        player.IsJumping = false;
                        isOnPlatform = true;
                    }
                    // Ensure the player doesn't pass through the platform from below
                    else if (player.Velocity.Y < 0 && playerRect.Top <= platformRect.Bottom && previousPosition.Y >= platformRect.Bottom)
                    {
                        player.Position = new Vector2(player.Position.X, platformRect.Bottom);
                        player.Velocity = new Vector2(player.Velocity.X, 0);
                    }
                    // Prevent the player from walking through the platform from the sides
                    else if (playerRect.Right > platformRect.Left && previousPosition.X + playerRect.Width <= platformRect.Left)
                    {
                        player.Position = new Vector2(platformRect.Left - playerRect.Width, player.Position.Y);
                    }
                    else if (playerRect.Left < platformRect.Right && previousPosition.X >= platformRect.Right)
                    {
                        player.Position = new Vector2(platformRect.Right, player.Position.Y);
                    }
                }
            }

            // Check if the player is on the ground if not on a platform
            if (!isOnPlatform && player.Position.Y >= groundLevel - player.BoundingBox.Height)
            {
                player.Position = new Vector2(player.Position.X, groundLevel - player.BoundingBox.Height);
                player.Velocity = new Vector2(player.Velocity.X, 0);
                player.IsJumping = false;
                isOnPlatform = true;
            }

            // Update jump logic to ensure player can only jump when on ground or platform
            if (keyboardState.IsKeyDown(Keys.Space) && !player.IsJumping && isOnPlatform)
            {
                player.Jump(jumpStrength);
            }

            // Update camera position to follow the player vertically only if the player is near the top
            if (player.Position.Y < cameraFollowThreshold)
            {
                cameraPosition.Y = player.Position.Y - GraphicsDevice.Viewport.Height * 0.05f;
            }
            else
            {
                cameraPosition.Y = 0;
            }
            cameraTransform = Matrix.CreateTranslation(new Vector3(0, -cameraPosition.Y, 0));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Begin drawing with camera transformation
            _spriteBatch.Begin(transformMatrix: cameraTransform);

            // Draw the ground
            for (int i = 0; i < GraphicsDevice.Viewport.Width * 2; i += groundTexture.Width)
            {
                _spriteBatch.Draw(groundTexture, new Vector2(i, groundLevel), Color.White);
            }

            // Draw platforms
            foreach (var platform in platforms)
            {
                platform.Draw(_spriteBatch);
            }

            // Draw monsters
            foreach (var monster in monsters)
            {
                monster.Draw(_spriteBatch);
            }

            // Draw the key (animated or static)
            if (key is AnimatedItem animatedKey)
            {
                animatedKey.Draw(_spriteBatch);
            }
            else
            {
                key.Draw(_spriteBatch);
            }

            // Update door texture before drawing
            if (door.IsOpened)
            {
                // Door texture will already be updated based on its state
            }
            else if (door.IsUnlocked)
            {
                // Door texture will already be updated based on its state
            }

            // Draw the door
            door.Draw(_spriteBatch);

            // Draw a hovering message if the player is near the door
            Rectangle playerRect = player.BoundingBox;
            Rectangle doorRect = door.BoundingBox;

            if (playerRect.Intersects(doorRect))
            {
                string message = "Press E to open";
                if (door.IsLocked)
                {
                    message = player.HasKey ? "Press E to unlock!" : "You need a key!";
                }
                else if (door.IsOpened)
                {
                    message = "Press E to enter";
                }

                Vector2 messagePosition = new Vector2(door.Position.X, door.Position.Y - 40);
                Vector2 messageSize = font.MeasureString(message);
                if (messagePosition.X < 10)
                {
                    messagePosition.X = 10;
                }
                else if (messagePosition.X + messageSize.X > GraphicsDevice.Viewport.Width - 10)
                {
                    messagePosition.X = GraphicsDevice.Viewport.Width - messageSize.X - 10;
                }

                _spriteBatch.DrawString(font, message, messagePosition, Color.White);
            }

            // Draw the player
            float topLeftScreenY = cameraPosition.Y + 20;
            player.Draw(_spriteBatch, cameraPosition, GraphicsDevice.Viewport.Width, topLeftScreenY);


            _spriteBatch.End();

            base.Draw(gameTime);
        }

    }
}
