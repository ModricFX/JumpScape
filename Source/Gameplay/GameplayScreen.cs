using System;
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
        private const float MessageTime = 2f;

        private static readonly Random Random = new Random();

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
        private List<PowerUp> _powerUps;
        private List<Projectile> _projectiles;
        private List<Particle> _effects;
        private string _message;
        private float _messageTimer;
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
            _powerUps = new List<PowerUp>();
            _projectiles = new List<Projectile>();
            _effects = new List<Particle>();
            _messageTimer = 0f;

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

            foreach (PowerUpInfo info in _level.PowerUps)
                _powerUps.Add(new PowerUp(info.Position, info.Type));

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

            if (Input.WasClicked && _player.CanThrow)
            {
                Vector2 mouseInWorld = Input.ViewMousePosition + new Vector2(0f, _cameraTop);
                _projectiles.Add(_player.Throw(mouseInWorld));
            }

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

            UpdatePowerUps(deltaTime);
            UpdateProjectiles(deltaTime);
            UpdateEffects(deltaTime);
            UpdateCamera();
        }

        private void UpdatePowerUps(float deltaTime)
        {
            _messageTimer = Math.Max(0f, _messageTimer - deltaTime);

            foreach (PowerUp powerUp in _powerUps)
            {
                powerUp.Update(deltaTime);
                if (powerUp.IsCollected || !_player.Bounds.Intersects(powerUp.Bounds) || !_player.TryCollect(powerUp.Type))
                    continue;

                powerUp.Collect();
                Burst(powerUp.Bounds.Center.ToVector2(), PowerUp.GlowColorFor(powerUp.Type), 12);
                _message = powerUp.Type switch
                {
                    PowerUpType.Health => "+1 heart",
                    PowerUpType.DoubleJump => "Double jump! Press jump again in the air",
                    _ => "High jump!",
                };
                _messageTimer = MessageTime;
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            foreach (Projectile projectile in _projectiles)
            {
                projectile.Update(deltaTime);

                foreach (Monster monster in _monsters)
                {
                    if (!projectile.IsFinished && projectile.Bounds.Intersects(monster.Bounds))
                    {
                        monster.Defeat();
                        projectile.Finish();
                        OnEnemyDefeated(monster.Center, Color.Crimson);
                    }
                }

                foreach (Ghost ghost in _ghosts)
                {
                    if (!projectile.IsFinished && projectile.Bounds.Intersects(ghost.Bounds))
                    {
                        ghost.Defeat();
                        projectile.Finish();
                        OnEnemyDefeated(ghost.Center, Color.WhiteSmoke);
                    }
                }

                foreach (Platform platform in _platforms)
                {
                    if (!projectile.IsFinished && platform.IsSolid && platform.Bounds.Contains(projectile.Position))
                    {
                        projectile.Finish();
                        Burst(projectile.Position, Color.Gold, 6);
                    }
                }
            }

            _projectiles.RemoveAll(projectile => projectile.IsFinished);
            _monsters.RemoveAll(monster => monster.IsDefeated);
            _ghosts.RemoveAll(ghost => ghost.IsDefeated);
        }

        private void OnEnemyDefeated(Vector2 position, Color color)
        {
            Burst(position, color, 16);
            float pan = MathHelper.Clamp((position.X - _player.Center.X) / 500f, -1f, 1f);
            Assets.PlayerHit.Play(0.8f * GameSettings.Current.SoundVolume, 0.5f, pan);
        }

        /// <summary>A small explosion of colored squares.</summary>
        private void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var velocity = new Vector2((float)Random.NextDouble() * 300f - 150f, -60f - (float)Random.NextDouble() * 180f);
                float lifetime = 0.4f + (float)Random.NextDouble() * 0.4f;
                _effects.Add(new Particle(position, velocity, lifetime, color, size: 6f));
            }
        }

        private void UpdateEffects(float deltaTime)
        {
            foreach (Particle particle in _effects)
                particle.Update(deltaTime);
            _effects.RemoveAll(particle => particle.IsDead);
        }

        /// <summary>The camera only scrolls vertically, following the player once they climb near the top of the view.</summary>
        private void UpdateCamera()
        {
            float restingTop = RestingCameraTop;
            float followThreshold = restingTop + VirtualScreen.ViewHeight * 0.05f;
            float targetTop = _player.Position.Y < followThreshold ? _player.Position.Y - VirtualScreen.ViewHeight * 0.3f : restingTop;
            _cameraTop = MathHelper.Lerp(_cameraTop, targetTop, CameraSmoothing);
        }

        /// <summary>Briefly shows what a collected power-up does, at the top of the screen.</summary>
        private void DrawMessage(SpriteBatch spriteBatch)
        {
            if (_messageTimer <= 0f)
                return;

            const float scale = 0.8f;
            float alpha = MathHelper.Clamp(_messageTimer / 0.5f, 0f, 1f);
            Vector2 size = Assets.Font.MeasureString(_message) * scale;
            var position = new Vector2((VirtualScreen.ViewWidth - size.X) / 2f, 30f);
            spriteBatch.DrawString(Assets.Font, _message, position + new Vector2(2, 2), Color.Black * (0.5f * alpha), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(Assets.Font, _message, position, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
            foreach (PowerUp powerUp in _powerUps)
                powerUp.Draw(spriteBatch);
            _door.Draw(spriteBatch);
            _door.DrawPrompt(spriteBatch, _player);
            _player.Draw(spriteBatch);
            foreach (Projectile projectile in _projectiles)
                projectile.Draw(spriteBatch);
            foreach (Particle particle in _effects)
                particle.Draw(spriteBatch);
            spriteBatch.End();

            // The hearts and inventory stick to the corners of the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            _player.DrawHud(spriteBatch);
            DrawMessage(spriteBatch);
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
