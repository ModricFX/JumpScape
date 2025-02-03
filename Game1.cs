using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using JumpScape.Classes;
using System;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;

namespace JumpScape
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D fadeTexture;
        private bool isFadingOut;

        private float fadeAlpha = 1f;
        private const float FadeSpeed = 0.005f;
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
        private float cameraFollowThreshold;
        private Vector2 _backgroundPosition;
        private float _backgroundSpeed;
        private bool _movingRight;
        private Texture2D _backgroundTexture;

        private bool isPaused;
        private DeathMenu deathMenu;
        private GameSettings settings;

        public enum GameState
        {
            MainMenu,
            Playing,
            LevelSelect,
            Settings
        }

        private GameState currentGameState;
        private MainMenu mainMenu;
        private LevelSelectorMenu levelSelectMenu;
        private SettingsMenu settingsMenu;

        private PauseMenu pauseMenu;
        private SpriteFont menuFont;
        private int lastCompletedLevel = 0; // Track the last completed level
        private int currentLevel = 1;       // Track the current level to play
        private KeyboardState previousKeyboardState;

        private int currentFPS;

        private SoundEffect themeSoundEffect;
        private SoundEffectInstance themeSoundEffectInstance;



        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                SynchronizeWithVerticalRetrace = true
            };
            Window.AllowUserResizing = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _backgroundPosition = Vector2.Zero;
            _backgroundSpeed = 0.3f; // Slow scrolling speed
            _movingRight = true;

            // Set the minimum resolution
            Window.ClientSizeChanged += (sender, e) =>
            {
                int minWidth = 1300;  // Minimum width
                int minHeight = 700; // Minimum height

                if (Window.ClientBounds.Width < minWidth || Window.ClientBounds.Height < minHeight)
                {
                    _graphics.PreferredBackBufferWidth = Math.Max(Window.ClientBounds.Width, minWidth);
                    _graphics.PreferredBackBufferHeight = Math.Max(Window.ClientBounds.Height, minHeight);
                    _graphics.ApplyChanges();
                }
            };

            _graphics.ApplyChanges();
            settings = GameSettings.Load();
        }


        protected override void LoadContent()
        {
            _graphics.PreferredBackBufferWidth = 1920;  // New window width
            _graphics.PreferredBackBufferHeight = 1080; // New window height
            _graphics.ApplyChanges();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _backgroundTexture = Texture2D.FromFile(GraphicsDevice, Path.Combine("Content", "Graphics", "Menu", "background.png"));

            // Load fade texture, font, etc.
            fadeTexture = new Texture2D(GraphicsDevice, 1, 1);
            fadeTexture.SetData(new[] { Color.Black });

            font = Content.Load<SpriteFont>("Fonts/DefaultFont");
            //menuFont = Content.Load<SpriteFont>("Fonts/FontBigger");; // For simplicity, use the same font for menus
            menuFont = Content.Load<SpriteFont>("Fonts/BigFont"); ; // For simplicity, use the same font for menus

            // Initialize Menus
            mainMenu = new MainMenu(menuFont, GraphicsDevice);
            mainMenu.AddMenuItem("Play");
            mainMenu.AddMenuItem("Level Picker");
            mainMenu.AddMenuItem("Settings");
            mainMenu.AddMenuItem("Exit");

            levelSelectMenu = new LevelSelectorMenu(font, menuFont, GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

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

            settingsMenu = new SettingsMenu(menuFont, font, _graphics, GraphicsDevice);

            pauseMenu = new PauseMenu(menuFont, GraphicsDevice);

            deathMenu = new DeathMenu(menuFont, GraphicsDevice);

            currentGameState = GameState.MainMenu;

            // music
            themeSoundEffect = SoundEffect.FromFile(Path.Combine("Content", "Sounds", "ThemeSong.wav"));

            // Create an instance so we can loop it
            themeSoundEffectInstance = themeSoundEffect.CreateInstance();
            themeSoundEffectInstance.IsLooped = true;

            ApplyMusicSettings();
            themeSoundEffectInstance.Play();
        }

        private void ApplyMusicSettings()
        {
            settings = GameSettings.Load();
            if (themeSoundEffectInstance != null && settings != null)
            {
                if (settings.IsMuted)
                    themeSoundEffectInstance.Volume = 0f;
                else
                    themeSoundEffectInstance.Volume = settings.MusicVolume / 100f; // e.g. 80 => 0.8
            }
        }

        private readonly int[] frameRates = { 30, 60, 120, -1 }; // -1 is unlimited
        private void getCurrentFPS()
        {
            GameSettings GameSettings = GameSettings.Load();
            int fps = GameSettings.FrameRateIndex;
            // i get index of fps, i need value
            int desiredFPS = frameRates[fps];
            currentFPS = desiredFPS;
        }



        private void LoadLevelData()
        {
            string levelFile = Path.Combine("Levels", "Level" + currentLevel + ".txt");
            var levelLoader = new LevelLoader();
            levelLoader.LoadLevel(levelFile, GraphicsDevice.Viewport.Height, GraphicsDevice.Viewport.Width);
            groundLevel = LevelLoader.GroundY;

            player = new Player(GraphicsDevice, levelLoader.PlayerSpawn, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
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

        private void backgroundMovement(GameTime gameTime, GraphicsDevice graphicsDevice)
        {
            // Background movement logic
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_movingRight)
            {
                _backgroundPosition.X += _backgroundSpeed * elapsed * 100;
                if (_backgroundPosition.X >= _backgroundTexture.Width - GraphicsDevice.Viewport.Width * 1.1)
                {
                    _movingRight = false;
                }
            }
            else
            {
                _backgroundPosition.X -= _backgroundSpeed * elapsed * 100;
                if (_backgroundPosition.X <= 0)
                {
                    _movingRight = true;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {

            getCurrentFPS();
            ApplyMusicSettings();
            KeyboardState ks = Keyboard.GetState();
            // Only trigger if Escape is newly pressed this frame
            if (IsKeyPressed(ks, previousKeyboardState, Keys.Escape))
            {
                switch (currentGameState)
                {
                    case GameState.Playing:
                        // pause game logic or just fall through to main menu
                        //Exit();
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

            if (currentGameState != GameState.Playing)
            {
                backgroundMovement(gameTime, _graphics.GraphicsDevice);
            }

            switch (currentGameState)
            {
                case GameState.MainMenu:
                    {
                        // reset level data
                        player = null;
                        door = null;
                        key = null;
                        platforms = new List<Platform>();
                        monsters = new List<Monster>();
                        ghosts = new List<Ghost>();

                        if (mainMenu.visible == false)
                        {
                            mainMenu.visible = true;
                            levelSelectMenu.visible = false;
                            settingsMenu.visible = false;
                        }
                        int selection = mainMenu.Update(gameTime, GraphicsDevice, _backgroundPosition);
                        if (selection == 0) // Play
                        {
                            currentLevel = lastCompletedLevel + 1;
                            if (currentLevel > levelSelectMenu.menuItems.Count - 1)
                            {
                                currentLevel = levelSelectMenu.menuItems.Count - 1;
                            }
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
                    if (IsKeyPressed(ks, previousKeyboardState, Keys.Escape))
                    {
                        if (!player.isDead)
                        {
                            isPaused = !isPaused;
                            pauseMenu.ToggleVisibility();
                        }
                    }

                    if (isPaused)
                    {
                        // Update pause menu and handle selection
                        int pauseSelection = pauseMenu.Update(gameTime, GraphicsDevice, cameraPosition);

                        if (pauseSelection == 0) // Resume
                        {
                            isPaused = false;
                            pauseMenu.ToggleVisibility();
                        }
                        else if (pauseSelection == 1) // Quit
                        {
                            currentGameState = GameState.MainMenu;
                            isPaused = false;
                            pauseMenu.ToggleVisibility();
                            mainMenu.ResetPreviousState();
                        }
                    }
                    else
                    {
                        levelSelectMenu.visible = false;
                        mainMenu.visible = false;
                        settingsMenu.visible = false;

                        isFadingOut = player.endLevel;
                        player.Update(gameTime, ks, cameraPosition, GraphicsDevice.Viewport.Width, groundLevel, key, door);
                        UpdateMonstersAndGhosts(gameTime);
                        key?.Update(gameTime);
                        player.ApplyGravity(0.6f);
                        player.CheckPlatforms(platforms, groundLevel);
                        UpdatePlatforms(gameTime);
                        UpdateCamera();
                        UpdateFadeEffect();

                        if (player.isDead)
                        {
                            deathMenu.Show();
                            int selection = deathMenu.Update(gameTime, GraphicsDevice);

                            if (selection == 0) // Retry
                            {
                                deathMenu.Hide();

                                LoadLevelData();  // Reload the same level
                                InitializeCamera();  // Reset camera
                            }
                            else if (selection == 1) // Quit
                            {
                                currentGameState = GameState.MainMenu;
                                deathMenu.Hide();
                                mainMenu.ResetPreviousState();
                            }
                        }


                        if (player.endLevel)
                        {
                            lastCompletedLevel = Math.Max(lastCompletedLevel, currentLevel);

                            if (lastCompletedLevel == levelSelectMenu.menuItems.Count)
                                lastCompletedLevel = levelSelectMenu.menuItems.Count - 1;

                            if (fadeAlpha >= 1)
                            {
                                currentGameState = GameState.MainMenu;
                                System.Threading.Thread.Sleep(1000);
                                mainMenu.ResetPreviousState();
                            }
                        }
                    }
                    break;


                case GameState.LevelSelect:
                    {
                        if (!levelSelectMenu.visible)
                        {
                            levelSelectMenu.selectedIndex = 0;
                            levelSelectMenu.visible = true;
                            mainMenu.visible = false;
                            settingsMenu.visible = false;
                        }

                        int selection = levelSelectMenu.Update(gameTime, GraphicsDevice, _backgroundPosition);

                        // If selection >= 0 means user has clicked or pressed Enter on an item
                        if (selection >= 0)
                        {
                            if (selection != levelSelectMenu.menuItems.Count - 1) // If not the "Back" button
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
                            else // "Back" button
                            {
                                currentGameState = GameState.MainMenu;
                                mainMenu.ResetPreviousState();
                            }
                        }
                    }
                    break;



                case GameState.Settings:
                    {
                        if (settingsMenu.visible == false)
                        {
                            settingsMenu.selectedIndex = 0;
                            settingsMenu.visible = true;
                            mainMenu.visible = false;
                            levelSelectMenu.visible = false;
                        }
                        int selection = settingsMenu.Update(gameTime, GraphicsDevice, _backgroundPosition, _spriteBatch);
                        if (selection >= 0)
                        {
                            if (selection != settingsMenu.menuItems.Count - 1) // If not the "Back" button
                            {

                            }
                            else // Back
                            {
                                currentGameState = GameState.MainMenu;
                                mainMenu.ResetPreviousState();
                            }
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
                platform.Update(gameTime, player);
        }

        private void UpdateCamera()
        {
            // Target position: Follow the player if they are above the threshold
            float targetY = player.Position.Y < cameraFollowThreshold
                ? player.Position.Y - GraphicsDevice.Viewport.Height * 0.3f
                : 0;

            // Smoothly transition the camera's Y position
            cameraPosition.Y = MathHelper.Lerp(cameraPosition.Y, targetY, 0.1f); // Adjust 0.1f for desired smoothness (higher = slower, lower = faster)

            // Apply the updated camera transform
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
            // add delay based on fps currentFPS
            if (currentFPS != -1)
            {
                System.Threading.Thread.Sleep(1000 / currentFPS);
            }

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

                    menuPosition.X -= 100; // Adjust so text is centered

                    // Draw the menu with the selector icon and a custom scale
                    mainMenu.Draw(_spriteBatch, GraphicsDevice, menuPosition, scale: 1.0f);

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

                    if (isPaused)
                    {
                        pauseMenu.Draw(_spriteBatch, GraphicsDevice, cameraPosition);
                    }

                    deathMenu.Draw(_spriteBatch, GraphicsDevice);

                    // Apply the fade effect considering the camera transform
                    _spriteBatch.Draw(
                        fadeTexture,
                        new Rectangle(
                            (int)cameraPosition.X, // Adjust for the current camera position
                            (int)cameraPosition.Y, // Adjust for the current camera position
                            _graphics.PreferredBackBufferWidth,
                            _graphics.PreferredBackBufferHeight
                        ),
                        new Color(0, 0, 0, fadeAlpha)
                    );

                    _spriteBatch.End();
                    break;

                case GameState.LevelSelect:
                    _spriteBatch.Begin();
                    levelSelectMenu.Draw(_spriteBatch, GraphicsDevice, new Vector2(100, 150));
                    _spriteBatch.End();
                    break;

                case GameState.Settings:
                    _spriteBatch.Begin();
                    _spriteBatch.DrawString(font, "Settings:", new Vector2(100, 100), Color.White);
                    settingsMenu.Draw(_spriteBatch, GraphicsDevice, new Vector2(100, 150));
                    _spriteBatch.End();
                    break;
            }

            base.Draw(gameTime);
        }
    }
}