using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

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

        /// <summary>How much the black overlay changes per frame when switching screens (about 0.15 s each way).</summary>
        private const float ScreenFadeSpeed = 0.11f;

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
        private PanelMenu _errorMenu;

        private Action _pendingSwitch;
        private float _screenFade;

        public JumpScapeGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                SynchronizeWithVerticalRetrace = _settings.VSync,
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
            _levelSelectMenu = new LevelSelectMenu(_levels, _progress);
            _settingsMenu = new SettingsMenu(ApplySettings, ResetProgress);
            _errorMenu = new PanelMenu("This level can't be loaded", new Color(255, 90, 80), new[] { "OK" });

            MediaPlayer.IsRepeating = true;
            ApplyAudioSettings();
            MediaPlayer.Play(Assets.ThemeSong);
        }

        private void ApplySettings()
        {
            ApplyDisplaySettings();
            ApplyAudioSettings();
        }

        private void ApplyDisplaySettings(bool force = false)
        {
            if (!force && _graphics.IsFullScreen == _settings.IsFullscreen && _graphics.SynchronizeWithVerticalRetrace == _settings.VSync)
                return;

            // Fullscreen uses a borderless window at the desktop resolution instead of changing the display mode.
            DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            Point size = _settings.IsFullscreen ? new Point(display.Width, display.Height) : _windowedSize;

            _isResizing = true;
            _graphics.SynchronizeWithVerticalRetrace = _settings.VSync;
            _graphics.IsFullScreen = _settings.IsFullscreen;
            _graphics.PreferredBackBufferWidth = size.X;
            _graphics.PreferredBackBufferHeight = size.Y;
            _graphics.ApplyChanges();
            _isResizing = false;
        }

        private void ApplyAudioSettings()
        {
            SoundEffect.MasterVolume = _settings.IsMuted ? 0f : 1f;
            MediaPlayer.IsMuted = _settings.IsMuted;
            MediaPlayer.Volume = _settings.MusicVolume / 100f;
        }

        /// <summary>Forgets which levels were finished and deletes the best runs.</summary>
        private void ResetProgress()
        {
            _progress.HighestCompletedLevel = 0;
            _progress.Save();
            RunRecording.DeleteAll();
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

        /// <summary>Fades to black, runs <paramref name="switchScreen"/>, then fades back in.</summary>
        private void SwitchTo(Action switchScreen)
        {
            _pendingSwitch ??= switchScreen;
        }

        private void ShowScreen(Screen screen)
        {
            SwitchTo(() => _screen = screen);
        }

        protected override void Update(GameTime gameTime)
        {
            PresentationParameters backBuffer = GraphicsDevice.PresentationParameters;
            VirtualScreen.Resize(backBuffer.BackBufferWidth, backBuffer.BackBufferHeight);
            Input.Update();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_screen != Screen.Playing)
                _menuBackground.Update(deltaTime);

            if (_pendingSwitch != null)
            {
                // While fading out, the current screen ignores input.
                _screenFade = Math.Min(1f, _screenFade + ScreenFadeSpeed);
                if (_screenFade >= 1f)
                {
                    _pendingSwitch();
                    _pendingSwitch = null;
                }
            }
            else if (_errorMenu.IsOpen)
            {
                _screenFade = Math.Max(0f, _screenFade - ScreenFadeSpeed);
                if (_errorMenu.Update(deltaTime) == 0 || Input.Back)
                    _errorMenu.Close();
            }
            else
            {
                _screenFade = Math.Max(0f, _screenFade - ScreenFadeSpeed);
                UpdateScreen(gameTime);
            }

            // At the 30 FPS limit only every other update is drawn; the game itself still runs at 60 updates per second.
            if (_settings.FrameRateLimit == 30 && _frameCounter++ % 2 == 1)
                SuppressDraw();

            base.Update(gameTime);
        }

        private void UpdateScreen(GameTime gameTime)
        {
            switch (_screen)
            {
                case Screen.MainMenu:
                    UpdateMainMenu();
                    break;

                case Screen.LevelSelect:
                    UpdateLevelSelect((float)gameTime.ElapsedGameTime.TotalSeconds);
                    break;

                case Screen.Settings:
                    if (_settingsMenu.Update((float)gameTime.ElapsedGameTime.TotalSeconds))
                        ShowScreen(Screen.MainMenu);
                    break;

                case Screen.Playing:
                    UpdatePlaying(gameTime);
                    break;
            }
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
                        StartLevel(_levels.FirstOrDefault(level => level > _progress.HighestCompletedLevel, _levels[^1]));
                    }
                    break;

                case MainMenuChoice.LevelPicker:
                    _levelSelectMenu.Open();
                    ShowScreen(Screen.LevelSelect);
                    break;

                case MainMenuChoice.Settings:
                    _settingsMenu.Open();
                    ShowScreen(Screen.Settings);
                    break;

                case MainMenuChoice.Exit:
                    Exit();
                    break;
            }
        }

        private void UpdateLevelSelect(float deltaTime)
        {
            if (Input.Back)
            {
                ShowScreen(Screen.MainMenu);
                return;
            }

            switch (_levelSelectMenu.Update(deltaTime))
            {
                case LevelSelectChoice.Level:
                    StartLevel(_levelSelectMenu.ChosenLevel);
                    break;

                case LevelSelectChoice.Back:
                    ShowScreen(Screen.MainMenu);
                    break;
            }
        }

        private void StartLevel(int levelNumber)
        {
            LevelData level;
            try
            {
                level = LevelData.Load(levelNumber);
            }
            catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                // Show what is wrong with the level file instead of crashing, back on the menus.
                Debug.WriteLine(e);
                if (_screen == Screen.Playing)
                {
                    _gameplay.StopSounds();
                    _gameplay = null;
                    _screen = Screen.MainMenu;
                }
                _errorMenu.Open(e.Message);
                return;
            }

            SwitchTo(() =>
            {
                bool hasNextLevel = _levels.Any(number => number > levelNumber);
                _gameplay = new GameplayScreen(level, hasNextLevel);
                _screen = Screen.Playing;
            });
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            switch (_gameplay.Update(gameTime))
            {
                case GameplayResult.LevelCompleted:
                    if (_gameplay.LevelNumber > _progress.HighestCompletedLevel)
                    {
                        _progress.HighestCompletedLevel = _gameplay.LevelNumber;
                        _progress.Save();
                    }
                    break;

                case GameplayResult.NextLevel:
                    int current = _gameplay.LevelNumber;
                    StartLevel(_levels.FirstOrDefault(level => level > current, current));
                    break;

                case GameplayResult.QuitToMenu:
                    _gameplay.StopSounds();
                    SwitchTo(() =>
                    {
                        _gameplay = null;
                        _screen = Screen.MainMenu;
                    });
                    break;
            }
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
                _errorMenu.Draw(_spriteBatch);
                _spriteBatch.End();
            }

            if (_screenFade > 0f)
            {
                _spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
                _spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * _screenFade);
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _gameplay?.StopSounds();
            MediaPlayer.Stop();
            _spriteBatch?.Dispose();
            base.UnloadContent();
        }
    }
}
