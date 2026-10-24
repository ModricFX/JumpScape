using System.Collections.Generic;
using JumpScape.Core;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    public enum GameplayResult
    {
        None,
        QuitToMenu,
        LevelCompleted,
    }

    /// <summary>One level being played: the world, the camera, and the pause and death menus.</summary>
    public sealed class GameplayScreen
    {
        public static readonly Color SkyColor = Color.CornflowerBlue;

        private const float FadeInSpeed = 0.005f;
        private const float FadeOutSpeed = 0.01f;
        private const float RetryFadeOutSpeed = 0.02f;
        private const float BlackScreenTime = 0.5f;
        private const float CameraSmoothing = 0.1f;

        private readonly LevelData _level;
        private readonly OverlayMenu _pauseMenu = new OverlayMenu("Paused", Color.White, new[] { "Resume", "Quit" },
            titleY: 0.1f, itemsY: 0.4f, dimAmount: 0.7f, fadeInTime: 0f);
        private readonly OverlayMenu _deathMenu = new OverlayMenu("DIED", Color.Red, new[] { "Retry", "Quit" },
            titleY: 0.3f, itemsY: 0.5f, dimAmount: 0.8f, fadeInTime: 0.6f);

        private Player _player;
        private Door _door;
        private List<KeyPickup> _keys;
        private List<Platform> _platforms;
        private List<Monster> _monsters;
        private List<Ghost> _ghosts;
        private float _cameraTop;
        private float _fade;
        private float _blackScreenTimer;
        private bool _isRetrying;

        public GameplayScreen(LevelData level)
        {
            _level = level;
            Restart();
        }

        public int LevelNumber => _level.Number;

        /// <summary>
        /// The top of the view when the camera isn't following the player. The ground stays at the bottom of
        /// the window, so a window taller than 16:9 shows extra sky above the level.
        /// </summary>
        private static float RestingCameraTop => VirtualScreen.Height - VirtualScreen.ViewHeight;

        private void Restart()
        {
            _player = new Player(_level.PlayerSpawn);
            _door = new Door(_level.DoorPosition, _level.IsDoorLocked);
            _keys = new List<KeyPickup>();
            _platforms = new List<Platform>();
            _monsters = new List<Monster>();
            _ghosts = new List<Ghost>();

            foreach (Vector2 keyPosition in _level.KeyPositions)
                _keys.Add(new KeyPickup(keyPosition));

            foreach (PlatformInfo info in _level.Platforms)
            {
                var platform = new Platform(info.Position, info.Length, info.IsCrumbling);
                _platforms.Add(platform);
                if (info.HasMonster)
                    _monsters.Add(new Monster(platform.Bounds));
            }

            foreach (GhostInfo info in _level.Ghosts)
                _ghosts.Add(new Ghost(info.Position, info.Radius));

            _deathMenu.Close();
            _cameraTop = RestingCameraTop;
            _fade = 1f;
            _blackScreenTimer = 0f;
            _isRetrying = false;
        }

        public GameplayResult Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isRetrying)
            {
                // Fade to black, then start the level again (which fades back in).
                _fade = MathHelper.Min(_fade + RetryFadeOutSpeed, 1f);
                if (_fade >= 1f)
                    Restart();
                return GameplayResult.None;
            }

            if (_pauseMenu.IsOpen)
                return UpdatePauseMenu(deltaTime);

            if (Input.Back && !_player.IsDead && !_player.ReachedExit)
            {
                StopSounds();
                _pauseMenu.Open();
                return GameplayResult.None;
            }

            UpdateWorld(gameTime);

            if (_player.IsDead)
            {
                if (!_deathMenu.IsOpen)
                    _deathMenu.Open();

                switch (_deathMenu.Update(deltaTime))
                {
                    case 0:
                        StopSounds();
                        _isRetrying = true;
                        break;
                    case 1:
                        StopSounds();
                        return GameplayResult.QuitToMenu;
                }
            }

            if (_player.ReachedExit)
            {
                _fade = MathHelper.Min(_fade + FadeOutSpeed, 1f);
                if (_fade >= 1f)
                {
                    _blackScreenTimer += deltaTime;
                    if (_blackScreenTimer >= BlackScreenTime)
                    {
                        StopSounds();
                        return GameplayResult.LevelCompleted;
                    }
                }
            }
            else
            {
                _fade = MathHelper.Max(_fade - FadeInSpeed, 0f);
            }

            return GameplayResult.None;
        }

        private GameplayResult UpdatePauseMenu(float deltaTime)
        {
            int choice = Input.Back ? 0 : _pauseMenu.Update(deltaTime);
            if (choice == 0)
            {
                _pauseMenu.Close();
            }
            else if (choice == 1)
            {
                StopSounds();
                return GameplayResult.QuitToMenu;
            }
            return GameplayResult.None;
        }

        private void UpdateWorld(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _player.Update(deltaTime, _keys, _door);
            foreach (Monster monster in _monsters)
                monster.Update(_player);
            foreach (Ghost ghost in _ghosts)
                ghost.Update(deltaTime, _player);
            foreach (KeyPickup key in _keys)
                key.Update(deltaTime);

            _player.ApplyGravity();
            _player.ResolveCollisions(_platforms);
            foreach (Platform platform in _platforms)
                platform.Update(gameTime, _player);

            UpdateCamera();
        }

        /// <summary>The camera only scrolls vertically, following the player once they climb near the top of the view.</summary>
        private void UpdateCamera()
        {
            float restingTop = RestingCameraTop;
            float followThreshold = restingTop + VirtualScreen.ViewHeight * 0.05f;
            float targetTop = _player.Position.Y < followThreshold ? _player.Position.Y - VirtualScreen.ViewHeight * 0.3f : restingTop;
            _cameraTop = MathHelper.Lerp(_cameraTop, targetTop, CameraSmoothing);
        }

        public void StopSounds()
        {
            _player.StopSounds();
            foreach (Monster monster in _monsters)
                monster.StopSounds();
            foreach (Platform platform in _platforms)
                platform.StopSounds();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Matrix camera = Matrix.CreateTranslation(0f, -_cameraTop, 0f) * VirtualScreen.Transform;
            var visibleArea = new Rectangle(0, (int)_cameraTop, (int)VirtualScreen.ViewWidth + 1, (int)VirtualScreen.ViewHeight + 1);

            spriteBatch.Begin(transformMatrix: camera);
            foreach (Platform platform in _platforms)
                platform.Draw(spriteBatch, visibleArea);
            foreach (Monster monster in _monsters)
                monster.Draw(spriteBatch);
            foreach (Ghost ghost in _ghosts)
                ghost.Draw(spriteBatch);
            foreach (KeyPickup key in _keys)
                key.Draw(spriteBatch);
            _door.Draw(spriteBatch);
            _door.DrawPrompt(spriteBatch, _player);
            _player.Draw(spriteBatch);
            spriteBatch.End();

            // The hearts and inventory stick to the corners of the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            _player.DrawHud(spriteBatch);
            spriteBatch.End();

            // Menus are centered in the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
            _pauseMenu.Draw(spriteBatch);
            _deathMenu.Draw(spriteBatch);
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * _fade);
            spriteBatch.End();
        }
    }
}
