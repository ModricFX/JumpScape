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
        private Texture2D fadeTexture; // For the fade effect
        private bool isFadingOut = false;

        // Fade control variables
        private float fadeAlpha = 1f; // Start with full opacity
        private float fadeSpeed = 0.01f; // Speed of fading in
        private Player player;
        private Door door;
        private Item key;

        private SpriteFont font; // Font for rendering text

        private float groundLevel;
        private List<Platform> platforms;
        private List<Ghost> ghosts;
        private List<Monster> monsters;
        private Matrix cameraTransform;
        private Vector2 cameraPosition;
        private float cameraFollowThreshold;
        private float gravity = 0.6f;

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
            fadeTexture = new Texture2D(GraphicsDevice, 1, 1); // Create a 1x1 texture for fade
            fadeTexture.SetData([Color.Black]); // Set its color to black

            Texture2D platformTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "grass.png"));
            Texture2D keyTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Items", "key_sprite_sheet.png"));

            font = Content.Load<SpriteFont>("Fonts/DefaultFont");

            // Load the level data
            LevelLoader levelLoader = new LevelLoader();
            int windowHeight = GraphicsDevice.Viewport.Height;
            int windowWidth = GraphicsDevice.Viewport.Width;
            levelLoader.LoadLevel(Path.Combine("Levels", "Level1.txt"), windowHeight, windowWidth);

            // Set the ground level based on the level loader
            groundLevel = LevelLoader.GroundY;

            // Initialize player, door, and key
            player = new Player(GraphicsDevice, levelLoader.PlayerSpawn);
            door = new Door(GraphicsDevice, levelLoader.DoorData.Item1, levelLoader.DoorData.Item2);

            // Check if there's a valid key position before creating the key
            if (levelLoader.KeyPosition != Vector2.Zero) // Assuming Vector2.Zero means no key in the level
            {
                key = new AnimatedItem(keyTexture, levelLoader.KeyPosition, totalFrames: 16, frameTime: 0.1f, "Key");
            }
            else
            {
                key = null; // No key in this level
            }

            // Initialize platforms and monsters
            platforms = new List<Platform>();
            monsters = new List<Monster>();
            ghosts = new List<Ghost>();

            // Spawn platforms and monsters on it
            foreach (var (position, length, hasMonster, isDisappearing) in levelLoader.PlatformData)
            {
                Platform platform = new Platform(platformTexture, position, length, isDisappearing);
                platforms.Add(platform);

                if (hasMonster)
                {
                    Rectangle platformBounds = platform.BoundingBox;
                    monsters.Add(new Monster(GraphicsDevice, position, platformBounds));
                }
            }

            // Spawn ghosts
            foreach (var (position, radius) in levelLoader.GhostsData)
            {   
                Ghost ghost = new Ghost(GraphicsDevice, position, radius);
                ghosts.Add(ghost);
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

            isFadingOut = player.endLevel;

            var keyboardState = Keyboard.GetState();

            // Update player movement
            player.Update(gameTime, keyboardState, cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, key, door);

            // Limit the player so they can't go out of the left or right bounds

            // Update each monster
            foreach (var monster in monsters)
            {
                monster.Update(gameTime, player);
            }
            foreach (var ghost in ghosts)
            {
                ghost.Update(gameTime, player.Position, player);
            }

            // ---- Key Animation Update ----
            if (key is AnimatedItem animatedKey)
            {
                animatedKey.Update(gameTime);
            }
            // ---- End Key Animation Update ----

            // check if player on ground or platform isOnPlatform
            player.ApplyGravity(gravity);
            
            player.checkPlatforms(platforms, groundLevel);

            //start update platforms
            foreach (var platform in platforms)
            {
                platform.Update(gameTime);
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


            // end level
            if (fadeAlpha > 0f && !isFadingOut)
            {
                fadeAlpha -= fadeSpeed; // Decrease alpha to fade out
                if (fadeAlpha < 0f) fadeAlpha = 0f; // Make sure it doesn't go below 0
            }
            else if (fadeAlpha >= 0f && isFadingOut)
            {
                fadeAlpha += fadeSpeed * 2; // Increase alpha to fade in
                if (fadeAlpha > 1f) fadeAlpha = 1f; // Make sure it doesn't go above 1
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Begin drawing with camera transformation
            _spriteBatch.Begin(transformMatrix: cameraTransform);

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
            foreach (var ghost in ghosts)
            {
                ghost.Draw(_spriteBatch);
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

            // Draw the door
            door.Draw(_spriteBatch);

            // Draw a hovering message if the player is near the door
            door.Update(_spriteBatch, player, font, GraphicsDevice.Viewport.Width);
            
            // Draw the player
            float topLeftScreenY = cameraPosition.Y + 20;
            player.Draw(_spriteBatch, cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, topLeftScreenY, gameTime);

            _spriteBatch.Draw(fadeTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), new Color(0, 0, 0, fadeAlpha));

            _spriteBatch.End();

            base.Draw(gameTime);
        }

    }
}
