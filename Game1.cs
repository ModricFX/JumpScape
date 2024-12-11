using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using JumpScape.Classes;
using System;

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

        public enum GameState
        {
            MainMenu,
            Playing,
            LevelSelect,
            Settings
        }

        private GameState currentGameState;
        private Menu mainMenu;
        private Menu levelSelectMenu;
        private Menu settingsMenu;

        private SpriteFont menuFont;
        private int lastCompletedLevel = 0; // Track the last completed level
        private int currentLevel = 1;       // Track the current level to play
        private KeyboardState previousKeyboardState;




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

            // Load fade texture, font, etc.
            fadeTexture = new Texture2D(GraphicsDevice, 1, 1);
            fadeTexture.SetData(new[] { Color.Black });

            font = Content.Load<SpriteFont>("Fonts/DefaultFont");
            menuFont = font; // For simplicity, use the same font for menus

            // Initialize Menus
            mainMenu = new Menu(menuFont, GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            mainMenu.AddMenuItem("Play");
            mainMenu.AddMenuItem("Level Picker");
            mainMenu.AddMenuItem("Settings");
            mainMenu.AddMenuItem("Exit");

            levelSelectMenu = new Menu(menuFont, GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Dynamically load levels from the Levels directory
            string levelsDirectory = "Levels";
            if (!Directory.Exists(levelsDirectory))
            {
                Directory.CreateDirectory(levelsDirectory);
            }

            // Find all files matching Level*.txt
            string[] levelFiles = Directory.GetFiles(levelsDirectory, "Level*.txt");
            // Sort them to ensure a logical order (e.g., Level1, Level2, etc.)
            Array.Sort(levelFiles, StringComparer.InvariantCultureIgnoreCase);

            foreach (string filePath in levelFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath); // e.g. "Level1"
                                                                                              // Attempt to parse the number from the filename
                                                                                              // Assuming file name format is strictly "LevelX"
                if (fileNameWithoutExtension.StartsWith("Level", StringComparison.InvariantCultureIgnoreCase))
                {
                    string numberPart = fileNameWithoutExtension.Substring("Level".Length);
                    if (int.TryParse(numberPart, out int levelNumber))
                    {
                        levelSelectMenu.AddMenuItem("Level " + levelNumber);
                    }
                    // If we fail to parse the number, ignore this file.
                }
                // If the file doesn't start with "Level", ignore it.
            }
            levelSelectMenu.AddMenuItem("Back");

            settingsMenu = new Menu(menuFont, GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            settingsMenu.AddMenuItem("Toggle Fullscreen");
            settingsMenu.AddMenuItem("Back");

            currentGameState = GameState.MainMenu;
        }



        private void LoadLevelData()
        {
            string levelFile = Path.Combine("Levels", "Level" + currentLevel + ".txt");
            var levelLoader = new LevelLoader();
            levelLoader.LoadLevel(levelFile, GraphicsDevice.Viewport.Height, GraphicsDevice.Viewport.Width);
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

        private bool IsKeyPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && previous.IsKeyUp(key);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState ks = Keyboard.GetState();
            // Only trigger if Escape is newly pressed this frame
            if (IsKeyPressed(ks, previousKeyboardState, Keys.Escape))
            {
                switch (currentGameState)
                {
                    case GameState.Playing:
                        // pause game logic or just fall through to main menu
                        Exit();
                        break;

                    case GameState.LevelSelect:
                        currentGameState = GameState.MainMenu;
                        mainMenu.ResetPreviousState();
                        break;

                    case GameState.Settings:
                        currentGameState = GameState.MainMenu;
                        mainMenu.ResetPreviousState();
                        break;

                    case GameState.MainMenu:
                    default:
                        Exit();
                        break;
                }
            }

            switch (currentGameState)
            {
                case GameState.MainMenu:
                    {
                        int selection = mainMenu.Update(gameTime, GraphicsDevice);
                        if (selection == 0) // Play
                        {
                            currentLevel = lastCompletedLevel + 1;
                            LoadLevelData();
                            InitializeCamera();
                            currentGameState = GameState.Playing;
                        }
                        else if (selection == 1) // Level Picker
                        {
                            currentGameState = GameState.LevelSelect;
                            levelSelectMenu.ResetPreviousState();
                        }
                        else if (selection == 2) // Settings
                        {
                            currentGameState = GameState.Settings;
                            settingsMenu.ResetPreviousState();
                        }
                        else if (selection == 3) // Exit
                        {
                            Exit();
                        }
                    }
                    break;

                case GameState.Playing:
                    {
                        isFadingOut = player.endLevel;
                        player.Update(gameTime, ks, cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, key, door);
                        UpdateMonstersAndGhosts(gameTime);
                        key?.Update(gameTime);
                        player.ApplyGravity(0.6f);
                        player.CheckPlatforms(platforms, groundLevel);
                        UpdatePlatforms(gameTime);
                        UpdateCamera();
                        UpdateFadeEffect();

                        if (player.endLevel)
                        {
                            lastCompletedLevel = Math.Max(lastCompletedLevel, currentLevel);
                            currentGameState = GameState.MainMenu;
                            mainMenu.ResetPreviousState();
                        }
                    }
                    break;

                case GameState.LevelSelect:
                    {
                        int selection = levelSelectMenu.Update(gameTime, GraphicsDevice);
                        if (selection == 0)
                        {
                            string selectedLevelName = levelSelectMenu.GetSelectedItem();
                            if (selectedLevelName.StartsWith("Level", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string numberPart = selectedLevelName.Substring("Level".Length);
                                if (int.TryParse(numberPart, out int lvlNum))
                                {
                                    currentLevel = lvlNum;
                                }
                            }

                            LoadLevelData();
                            InitializeCamera();
                            currentGameState = GameState.Playing;
                        }
                        else if (selection == 1) // Back
                        {
                            currentGameState = GameState.MainMenu;
                            mainMenu.ResetPreviousState();
                        }
                    }
                    break;

                case GameState.Settings:
                    {
                        int selection = settingsMenu.Update(gameTime, GraphicsDevice);
                        if (selection == 0) // Toggle Fullscreen
                        {
                            _graphics.IsFullScreen = !_graphics.IsFullScreen;
                            _graphics.ApplyChanges();
                            // After toggling, reset menu state to avoid immediate re-trigger
                            settingsMenu.ResetPreviousState();
                        }
                        else if (selection == 1) // Back
                        {
                            currentGameState = GameState.MainMenu;
                            mainMenu.ResetPreviousState();
                        }
                    }
                    break;
            }
            previousKeyboardState = ks;
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

            switch (currentGameState)
            {
                case GameState.MainMenu:
                    _spriteBatch.Begin();

                    // Position the menu below the logo
                    Vector2 menuPosition = new Vector2(
                        GraphicsDevice.Viewport.Width / 2,
                        GraphicsDevice.Viewport.Height * 0.4f
                    );

                    // Adjust so text is centered
                    // We'll align text center by subtracting half of the item width, but for simplicity, 
                    // we can assume that the longest item fits in 200px.
                    menuPosition.X -= 100; // A rough offset to center menu items

                    // Draw the menu with the selector icon and a custom scale
                    mainMenu.Draw(_spriteBatch, GraphicsDevice, menuPosition, scale: 3.0f, isMenu: true);

                    _spriteBatch.End();
                    break;


                case GameState.Playing:
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
                    break;

                case GameState.LevelSelect:
                    _spriteBatch.Begin();
                    _spriteBatch.DrawString(font, "Select a Level:", new Vector2(100, 100), Color.White);
                    levelSelectMenu.Draw(_spriteBatch, GraphicsDevice, new Vector2(100, 150), isMenu: false);
                    _spriteBatch.End();
                    break;

                case GameState.Settings:
                    _spriteBatch.Begin();
                    _spriteBatch.DrawString(font, "Settings:", new Vector2(100, 100), Color.White);
                    settingsMenu.Draw(_spriteBatch, GraphicsDevice, new Vector2(100, 150), isMenu: false);
                    _spriteBatch.End();
                    break;
            }

            base.Draw(gameTime);
        }
    }
}