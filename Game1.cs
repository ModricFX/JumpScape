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
        private Texture2D fadeTexture;
        private bool isFadingOut;

        private float fadeAlpha = 1f;
        private const float FadeSpeed = 0.01f;
        private Player player;
        private Door door;
        private Item key;

        private SpriteFont font;
        private float groundLevel;
        private List<Platform> platforms;
        private List<Ghost> ghosts;
        private List<Monster> monsters;
        private Matrix cameraTransform;
        private Vector2 cameraPosition;
        private const float Gravity = 0.6f;
        private float cameraFollowThreshold;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.ApplyChanges();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            fadeTexture = new Texture2D(GraphicsDevice, 1, 1);
            fadeTexture.SetData(new[] { Color.Black });

            font = Content.Load<SpriteFont>("Fonts/DefaultFont");

            LoadLevelData();
            InitializeCamera();
        }

        private void LoadLevelData()
        {
            var levelLoader = new LevelLoader();
            levelLoader.LoadLevel(Path.Combine("Levels", "Level1.txt"), GraphicsDevice.Viewport.Height, GraphicsDevice.Viewport.Width);
            groundLevel = LevelLoader.GroundY;

            player = new Player(GraphicsDevice, levelLoader.PlayerSpawn);
            door = new Door(GraphicsDevice, levelLoader.DoorData.Item1, levelLoader.DoorData.Item2);

            if (levelLoader.KeyPosition != Vector2.Zero)
                key = new AnimatedItem(Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Items", "key_sprite_sheet.png")), levelLoader.KeyPosition, 16, 0.1f, "Key");

            platforms = new List<Platform>();
            monsters = new List<Monster>();
            ghosts = new List<Ghost>();

            foreach (var (position, length, hasMonster, isDisappearing) in levelLoader.PlatformData)
            {
                var platform = new Platform(Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "grass.png")), position, length, isDisappearing);
                platforms.Add(platform);

                if (hasMonster)
                    monsters.Add(new Monster(GraphicsDevice, position, platform.BoundingBox));
            }

            foreach (var (position, radius) in levelLoader.GhostsData)
                ghosts.Add(new Ghost(GraphicsDevice, position, radius));
        }

        private void InitializeCamera()
        {
            cameraPosition = Vector2.Zero;
            cameraFollowThreshold = GraphicsDevice.Viewport.Height * 0.05f;
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            isFadingOut = player.endLevel;
            player.Update(gameTime, Keyboard.GetState(), cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, key, door);
            UpdateMonstersAndGhosts(gameTime);
            key?.Update(gameTime);
            player.ApplyGravity(Gravity);
            player.CheckPlatforms(platforms, groundLevel);
            UpdatePlatforms(gameTime);
            UpdateCamera();
            UpdateFadeEffect();

            base.Update(gameTime);
        }

        private void UpdateMonstersAndGhosts(GameTime gameTime)
        {
            foreach (var monster in monsters)
                monster.Update(gameTime, player);

            foreach (var ghost in ghosts)
                ghost.Update(gameTime, player.Position, player);
        }

        private void UpdatePlatforms(GameTime gameTime)
        {
            foreach (var platform in platforms)
                platform.Update(gameTime);
        }

        private void UpdateCamera()
        {
            cameraPosition.Y = player.Position.Y < cameraFollowThreshold
                ? player.Position.Y - GraphicsDevice.Viewport.Height * 0.05f
                : 0;
            cameraTransform = Matrix.CreateTranslation(new Vector3(0, -cameraPosition.Y, 0));
        }

        private void UpdateFadeEffect()
        {
            fadeAlpha = isFadingOut
                ? MathHelper.Clamp(fadeAlpha + FadeSpeed * 2, 0, 1)
                : MathHelper.Clamp(fadeAlpha - FadeSpeed, 0, 1);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(transformMatrix: cameraTransform);

            foreach (var platform in platforms)
                platform.Draw(_spriteBatch);

            foreach (var monster in monsters)
                monster.Draw(_spriteBatch);

            foreach (var ghost in ghosts)
                ghost.Draw(_spriteBatch);

            key?.Draw(_spriteBatch);
            door.Draw(_spriteBatch);
            door.Update(_spriteBatch, player, font, GraphicsDevice.Viewport.Width);
            player.Draw(_spriteBatch, cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, cameraPosition.Y + 20, gameTime);

            _spriteBatch.Draw(fadeTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), new Color(0, 0, 0, fadeAlpha));
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
} 
