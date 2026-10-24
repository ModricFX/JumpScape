using System;
using System.Collections.Generic;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape
{
    /// <summary>Sets up the window, switches between the menus and the level being played, and draws everything.</summary>
    public sealed class JumpScapeGame : Game
    {
        private enum Screen
        {
            MainMenu,
            LevelSelect,
            Settings,
            Playing,
        }

        /// <summary>The game logic is tuned for exactly 60 updates per second.</summary>
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.0 / 60.0);

        private readonly GraphicsDeviceManager _graphics;
        private readonly GameSettings _settings = GameSettings.Current;
        private readonly GameProgress _progress = GameProgress.Load();

        private SpriteBatch _spriteBatch;
        private Point _windowedSize;
        private bool _isResizing;
        private int _frameCounter;

        private Screen _screen = Screen.MainMenu;
        private List<int> _levels;
        private MenuBackground _menuBackground;
        private MainMenu _mainMenu;
        private LevelSelectMenu _levelSelectMenu;
        private SettingsMenu _settingsMenu;
        private GameplayScreen _gameplay;
        private SoundEffectInstance _music;

        public JumpScapeGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                SynchronizeWithVerticalRetrace = true,
                HardwareModeSwitch = false,
            };
            Content.RootDirectory = "Content";
            IsFixedTimeStep = true;
            TargetElapsedTime = UpdateInterval;
            IsMouseVisible = true;
            Window.Title = "JumpScape";
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnWindowResized;
        }

        protected override void Initialize()
        {
            // Start with the largest 16:9 window that comfortably fits on the screen, up to 1920x1080.
            DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            float scale = Math.Min(1f, Math.Min(display.Width * 0.85f / VirtualScreen.Width, display.Height * 0.85f / VirtualScreen.Height));
            _windowedSize = new Point((int)(VirtualScreen.Width * scale), (int)(VirtualScreen.Height * scale));

            ApplyDisplaySettings(force: true);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Assets.Load(Content, GraphicsDevice);

            _levels = LevelData.FindLevelNumbers();
            _menuBackground = new MenuBackground();
            _mainMenu = new MainMenu();
            _levelSelectMenu = new LevelSelectMenu(_levels);
            _settingsMenu = new SettingsMenu(ApplySettings);

            _music = Assets.ThemeSong.CreateInstance();
            _music.IsLooped = true;
            ApplyAudioSettings();
            _music.Play();
        }

        private void ApplySettings()
        {
            ApplyDisplaySettings();
            ApplyAudioSettings();
        }

        private void ApplyDisplaySettings(bool force = false)
        {
            if (!force && _graphics.IsFullScreen == _settings.IsFullscreen)
                return;

            // Fullscreen uses a borderless window at the desktop resolution instead of changing the display mode.
            DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            Point size = _settings.IsFullscreen ? new Point(display.Width, display.Height) : _windowedSize;

            _isResizing = true;
            _graphics.IsFullScreen = _settings.IsFullscreen;
            _graphics.PreferredBackBufferWidth = size.X;
            _graphics.PreferredBackBufferHeight = size.Y;
            _graphics.ApplyChanges();
            _isResizing = false;
        }

        private void ApplyAudioSettings()
        {
            SoundEffect.MasterVolume = _settings.IsMuted ? 0f : 1f;
            if (_music != null)
                _music.Volume = _settings.MusicVolume / 100f;
        }

        /// <summary>Keeps the back buffer the same size as the window when the player resizes it.</summary>
        private void OnWindowResized(object sender, EventArgs e)
        {
            Rectangle client = Window.ClientBounds;
            if (_isResizing || _graphics.IsFullScreen || client.Width <= 0 || client.Height <= 0)
                return;

            _windowedSize = new Point(client.Width, client.Height);
            if (_graphics.PreferredBackBufferWidth == client.Width && _graphics.PreferredBackBufferHeight == client.Height)
                return;

            _isResizing = true;
            _graphics.PreferredBackBufferWidth = client.Width;
            _graphics.PreferredBackBufferHeight = client.Height;
            _graphics.ApplyChanges();
            _isResizing = false;
        }

        protected override void Update(GameTime gameTime)
        {
            PresentationParameters backBuffer = GraphicsDevice.PresentationParameters;
            VirtualScreen.Resize(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
            Input.Update();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_screen != Screen.Playing)
                _menuBackground.Update(deltaTime);

            switch (_screen)
            {
                case Screen.MainMenu:
                    UpdateMainMenu();
                    break;

                case Screen.LevelSelect:
                    UpdateLevelSelect();
                    break;

                case Screen.Settings:
                    if (_settingsMenu.Update())
                        _screen = Screen.MainMenu;
                    break;

                case Screen.Playing:
                    UpdatePlaying(gameTime);
                    break;
            }

            // At the 30 FPS limit only every other update is drawn; the game itself still runs at 60 updates per second.
            if (_settings.FrameRateLimit == 30 && _frameCounter++ % 2 == 1)
                SuppressDraw();

            base.Update(gameTime);
        }

        private void UpdateMainMenu()
        {
            if (Input.Back)
            {
                Exit();
                return;
            }

            switch (_mainMenu.Update())
            {
                case MainMenuChoice.Play:
                    if (_levels.Count > 0)
                    {
                        // Continue with the first level after the highest one completed, or replay the last level.
                        int next = _levels.FirstOrDefault(level => level > _progress.HighestCompletedLevel, _levels[^1]);
                        StartLevel(next);
                    }
                    break;

                case MainMenuChoice.LevelPicker:
                    _levelSelectMenu.Open();
                    _screen = Screen.LevelSelect;
                    break;

                case MainMenuChoice.Settings:
                    _screen = Screen.Settings;
                    break;

                case MainMenuChoice.Exit:
                    Exit();
                    break;
            }
        }

        private void UpdateLevelSelect()
        {
            if (Input.Back)
            {
                _screen = Screen.MainMenu;
                return;
            }

            switch (_levelSelectMenu.Update())
            {
                case LevelSelectChoice.Level:
                    StartLevel(_levelSelectMenu.ChosenLevel);
                    break;

                case LevelSelectChoice.Back:
                    _screen = Screen.MainMenu;
                    break;
            }
        }

        private void StartLevel(int levelNumber)
        {
            _gameplay = new GameplayScreen(LevelData.Load(levelNumber));
            _screen = Screen.Playing;
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            GameplayResult result = _gameplay.Update(gameTime);
            if (result == GameplayResult.None)
                return;

            if (result == GameplayResult.LevelCompleted && _gameplay.LevelNumber > _progress.HighestCompletedLevel)
            {
                _progress.HighestCompletedLevel = _gameplay.LevelNumber;
                _progress.Save();
            }

            _gameplay = null;
            _screen = Screen.MainMenu;
        }

        protected override void Draw(GameTime gameTime)
        {
            PresentationParameters backBuffer = GraphicsDevice.PresentationParameters;
            VirtualScreen.Resize(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);

            if (_screen == Screen.Playing)
            {
                GraphicsDevice.Clear(_gameplay.SkyColor);
                _gameplay.Draw(_spriteBatch);
            }
            else
            {
                GraphicsDevice.Clear(Color.Black);

                _spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
                _menuBackground.Draw(_spriteBatch);
                _spriteBatch.End();

                _spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
                if (_screen == Screen.MainMenu)
                    _mainMenu.Draw(_spriteBatch);
                else if (_screen == Screen.LevelSelect)
                    _levelSelectMenu.Draw(_spriteBatch);
                else
                    _settingsMenu.Draw(_spriteBatch);
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _gameplay?.StopSounds();
            _spriteBatch?.Dispose();
            base.UnloadContent();
        }
    }
}
