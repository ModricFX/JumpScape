using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JumpScape.Core;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Gameplay
{
    public enum GameplayResult
    {
        None,

        /// <summary>The player just finished the level. Reported once, when the level complete screen opens.</summary>
        LevelCompleted,

        NextLevel,
        QuitToMenu,
    }

    /// <summary>One level being played: its areas, the camera, effects, and the pause, death and level complete screens.</summary>
    public sealed class GameplayScreen
    {
        private const float FadeInSpeed = 0.02f;
        private const float RetryFadeOutSpeed = 0.04f;
        private const float PortalFadeSpeed = 0.06f;
        private const float CompleteScreenDelay = 0.6f;
        private const float CameraSmoothing = 0.1f;
        private const float MessageTime = 2.5f;

        private readonly LevelData _level;
        private readonly bool _hasNextLevel;
        private readonly PanelMenu _pauseMenu = new PanelMenu("Paused", Color.White, new[] { "Resume", "Main menu" });
        private readonly PanelMenu _deathMenu = new PanelMenu("You died", new Color(255, 90, 80), new[] { "Try again", "Main menu" }, fadeInTime: 0.5f);
        private readonly PanelMenu _completeMenu;
        private readonly Banner _banner = new Banner();
        private readonly Effects _effects = new Effects();
        private readonly List<Projectile> _projectiles = new List<Projectile>();

        private List<Area> _areas;
        private Area _area;
        private Player _player;
        private string _message;
        private float _messageTimer;
        private float _cameraTop;
        private float _fade;
        private float _playTime;
        private float _exitTimer;
        private bool _isRetrying;
        private Door _portalDestination;
        private Area _portalDestinationArea;

        public GameplayScreen(LevelData level, bool hasNextLevel)
        {
            _level = level;
            _hasNextLevel = hasNextLevel;
            _completeMenu = new PanelMenu("Level complete!", UiDrawing.Highlight,
                hasNextLevel ? new[] { "Next level", "Play again", "Main menu" } : new[] { "Play again", "Main menu" },
                fadeInTime: 0.4f);
            Restart();
        }

        public int LevelNumber => _level.Number;

        /// <summary>Background color of the area the player is in.</summary>
        public Color SkyColor => _area.Sky;

        /// <summary>
        /// The top of the view when the camera isn't following the player. The ground stays at the bottom of
        /// the window, so a window taller than 16:9 shows extra sky above the level.
        /// </summary>
        private static float RestingCameraTop => VirtualScreen.Height - VirtualScreen.ViewHeight;

        private void Restart()
        {
            _areas = _level.Areas.Select(data => new Area(data)).ToList();
            _area = _areas[_level.StartArea];
            _player = new Player(_level.PlayerSpawn);
            _projectiles.Clear();
            _effects.Clear();
            _messageTimer = 0f;
            _banner.Show("Level " + _level.Number.ToString(CultureInfo.InvariantCulture));

            _deathMenu.Close();
            _completeMenu.Close();
            _cameraTop = RestingCameraTop;
            _fade = 1f;
            _playTime = 0f;
            _exitTimer = 0f;
            _isRetrying = false;
            _portalDestination = null;
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

            if (_portalDestination != null)
            {
                // Fade to black, move the player through the portal, then fade back in.
                _fade = MathHelper.Min(_fade + PortalFadeSpeed, 1f);
                if (_fade >= 1f)
                    ArriveThroughPortal();
                return GameplayResult.None;
            }

            _fade = MathHelper.Max(_fade - FadeInSpeed, 0f);
            _banner.Update(deltaTime);

            if (_player.ReachedExit)
                return UpdateLevelComplete(deltaTime);

            if (_pauseMenu.IsOpen)
                return UpdatePauseMenu(deltaTime);

            if (Input.Back && !_player.IsDead)
            {
                StopSounds();
                _pauseMenu.Open();
                return GameplayResult.None;
            }

            UpdateWorld(gameTime);

            if (!_player.IsDead)
                return GameplayResult.None;

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
            return GameplayResult.None;
        }

        private GameplayResult UpdatePauseMenu(float deltaTime)
        {
            int choice = Input.Back ? 0 : _pauseMenu.Update(deltaTime);
            if (choice == 0)
                _pauseMenu.Close();
            else if (choice == 1)
                return GameplayResult.QuitToMenu;
            return GameplayResult.None;
        }

        /// <summary>After going through the exit the world stops, and a moment later the level complete screen opens.</summary>
        private GameplayResult UpdateLevelComplete(float deltaTime)
        {
            if (!_completeMenu.IsOpen)
            {
                _exitTimer += deltaTime;
                _effects.Update(deltaTime);
                if (_exitTimer < CompleteScreenDelay)
                    return GameplayResult.None;

                StopSounds();
                _completeMenu.Open("Time: " + FormatTime(_playTime));
                return GameplayResult.LevelCompleted;
            }

            int choice = _completeMenu.Update(deltaTime);
            if (!_hasNextLevel && choice >= 0)
                choice++; // There is no "Next level" button.

            switch (choice)
            {
                case 0:
                    return GameplayResult.NextLevel;
                case 1:
                    _isRetrying = true;
                    break;
                case 2:
                    return GameplayResult.QuitToMenu;
            }
            return GameplayResult.None;
        }

        private static string FormatTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return time.ToString(time.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture);
        }

        private void UpdateWorld(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!_player.IsDead)
                _playTime += deltaTime;

            if (Input.WasClicked && _player.CanThrow)
            {
                Vector2 mouseInWorld = Input.ViewMousePosition + new Vector2(0f, _cameraTop);
                _projectiles.Add(_player.Throw(mouseInWorld));
            }

            if (Input.WasPressed(Keys.E) && !_player.IsDead)
            {
                Door door = _area.Doors.FirstOrDefault(d => _player.Bounds.Intersects(d.Bounds));
                if (door != null)
                    UseDoor(door);
            }

            _player.Update(deltaTime, _area.Keys);
            foreach (Monster monster in _area.Monsters)
                monster.Update(_player);
            foreach (Ghost ghost in _area.Ghosts)
                ghost.Update(deltaTime, _player);
            foreach (KeyPickup key in _area.Keys)
                key.Update(deltaTime);
            foreach (Door door in _area.Doors)
                door.Update(deltaTime);

            _player.ApplyGravity();
            _player.ResolveCollisions(_area.Platforms);
            foreach (Platform platform in _area.Platforms)
                platform.Update(gameTime, _player);

            if (_player.JustJumped)
                _effects.Dust(_player.Feet, 6);
            if (_player.JustLanded)
                _effects.Dust(_player.Feet, 10);

            UpdatePowerUps(deltaTime);
            UpdateProjectiles(deltaTime);
            _effects.Update(deltaTime);
            _cameraTop = MathHelper.Lerp(_cameraTop, CameraTarget(), CameraSmoothing);
        }

        /// <summary>
        /// E at a door: unlock it with the selected key, open it, or go through it. Each locked door
        /// uses up one key.
        /// </summary>
        private void UseDoor(Door door)
        {
            switch (door.State)
            {
                case DoorState.Locked when _player.Inventory.SelectedItem == ItemType.Key:
                    _player.Inventory.RemoveSelected();
                    door.Unlock();
                    _effects.Burst(door.Bounds.Center.ToVector2(), Color.Gold, 10);
                    break;

                case DoorState.Closed:
                    door.Open();
                    break;

                case DoorState.Open:
                    if (FindPortal(door.Target, out Door destination, out Area destinationArea))
                    {
                        _portalDestination = destination;
                        _portalDestinationArea = destinationArea;
                        StopSounds();
                    }
                    else
                    {
                        // Exit doors (and portals whose target is missing) finish the level.
                        _player.ReachExit();
                        _effects.Burst(door.Bounds.Center.ToVector2(), UiDrawing.Highlight, 20);
                    }
                    break;
            }
        }

        private bool FindPortal(string name, out Door portal, out Area area)
        {
            portal = null;
            area = null;
            if (name == null)
                return false;

            foreach (Area candidate in _areas)
            {
                portal = candidate.Doors.FirstOrDefault(door => string.Equals(door.Name, name, StringComparison.OrdinalIgnoreCase));
                if (portal != null)
                {
                    area = candidate;
                    return true;
                }
            }
            return false;
        }

        private void ArriveThroughPortal()
        {
            _area = _portalDestinationArea;
            _portalDestination.OpenFromOtherSide();
            _player.MoveTo(_portalDestination.ArrivalPosition(_player.Bounds));
            _portalDestination = null;

            _projectiles.Clear();
            _effects.Clear();
            _cameraTop = CameraTarget();
            _fade = 1f;

            if (!string.Equals(_area.Name, "main", StringComparison.OrdinalIgnoreCase))
                _banner.Show(char.ToUpperInvariant(_area.Name[0]) + _area.Name.Substring(1), duration: 1.8f);
        }

        private void UpdatePowerUps(float deltaTime)
        {
            _messageTimer = Math.Max(0f, _messageTimer - deltaTime);

            foreach (PowerUp powerUp in _area.PowerUps)
            {
                powerUp.Update(deltaTime);
                if (powerUp.IsCollected || !_player.Bounds.Intersects(powerUp.Bounds) || !_player.TryCollect(powerUp.Type))
                    continue;

                powerUp.Collect();
                _effects.Burst(powerUp.Bounds.Center.ToVector2(), PowerUp.GlowColorFor(powerUp.Type), 12);
                _message = powerUp.Type switch
                {
                    PowerUpType.Health => "+1 heart",
                    PowerUpType.DoubleJump => "Double jump! Press jump again in the air",
                    _ => "High jump! You jump much higher for a while",
                };
                _messageTimer = MessageTime;
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            foreach (Projectile projectile in _projectiles)
            {
                projectile.Update(deltaTime);

                foreach (Monster monster in _area.Monsters)
                {
                    if (!projectile.IsFinished && projectile.Bounds.Intersects(monster.Bounds))
                    {
                        monster.Defeat();
                        projectile.Finish();
                        OnEnemyDefeated(monster.Center, Color.Crimson);
                    }
                }

                foreach (Ghost ghost in _area.Ghosts)
                {
                    if (!projectile.IsFinished && projectile.Bounds.Intersects(ghost.Bounds))
                    {
                        ghost.Defeat();
                        projectile.Finish();
                        OnEnemyDefeated(ghost.Center, Color.WhiteSmoke);
                    }
                }

                foreach (Platform platform in _area.Platforms)
                {
                    if (!projectile.IsFinished && platform.IsSolid && platform.Bounds.Contains(projectile.Position))
                    {
                        projectile.Finish();
                        _effects.Burst(projectile.Position, Color.Gold, 6);
                    }
                }
            }

            _projectiles.RemoveAll(projectile => projectile.IsFinished);
            _area.Monsters.RemoveAll(monster => monster.IsDefeated);
            _area.Ghosts.RemoveAll(ghost => ghost.IsDefeated);
        }

        private void OnEnemyDefeated(Vector2 position, Color color)
        {
            _effects.Burst(position, color, 16);
            float pan = MathHelper.Clamp((position.X - _player.Center.X) / 500f, -1f, 1f);
            Assets.PlayerHit.Play(0.8f * GameSettings.Current.SoundVolume, 0.5f, pan);
        }

        /// <summary>The camera only scrolls vertically, following the player once they climb near the top of the view.</summary>
        private float CameraTarget()
        {
            float restingTop = RestingCameraTop;
            float followThreshold = restingTop + VirtualScreen.ViewHeight * 0.05f;
            return _player.Position.Y < followThreshold ? _player.Position.Y - VirtualScreen.ViewHeight * 0.3f : restingTop;
        }

        public void StopSounds()
        {
            _player.StopSounds();
            foreach (Area area in _areas)
                area.StopSounds();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // The forest backdrop scrolls a little as the camera climbs.
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            Backdrop.Draw(spriteBatch, _area.Sky, RestingCameraTop - _cameraTop);
            spriteBatch.End();

            Matrix camera = Matrix.CreateTranslation(0f, -_cameraTop, 0f) * VirtualScreen.Transform;
            var visibleArea = new Rectangle(0, (int)_cameraTop, (int)VirtualScreen.ViewWidth + 1, (int)VirtualScreen.ViewHeight + 1);

            spriteBatch.Begin(transformMatrix: camera);
            foreach (Platform platform in _area.Platforms)
                platform.DrawShadow(spriteBatch, visibleArea);
            foreach (Platform platform in _area.Platforms)
                platform.Draw(spriteBatch, visibleArea, _area.Tint);
            foreach (Door door in _area.Doors)
                door.Draw(spriteBatch);
            foreach (Monster monster in _area.Monsters)
                monster.Draw(spriteBatch);
            foreach (KeyPickup key in _area.Keys)
                key.Draw(spriteBatch);
            foreach (PowerUp powerUp in _area.PowerUps)
                powerUp.Draw(spriteBatch);
            _player.Draw(spriteBatch);
            foreach (Ghost ghost in _area.Ghosts)
                ghost.Draw(spriteBatch);
            foreach (Projectile projectile in _projectiles)
                projectile.Draw(spriteBatch);
            _effects.Draw(spriteBatch);
            foreach (Door door in _area.Doors)
                door.DrawPrompt(spriteBatch, _player);
            spriteBatch.End();

            // The hearts and inventory stick to the corners of the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            _player.DrawHud(spriteBatch);
            if (_messageTimer > 0f)
            {
                float opacity = MathHelper.Clamp(_messageTimer / 0.5f, 0f, 1f);
                UiDrawing.DrawHint(spriteBatch, new Vector2(VirtualScreen.ViewWidth / 2f, 90f), null, _message, opacity);
            }
            spriteBatch.End();

            // Banners and menus are centered in the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
            _banner.Draw(spriteBatch);
            _pauseMenu.Draw(spriteBatch);
            _deathMenu.Draw(spriteBatch);
            _completeMenu.Draw(spriteBatch);
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * _fade);
            spriteBatch.End();
        }
    }
}
