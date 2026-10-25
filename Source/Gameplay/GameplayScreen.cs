using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JumpScape.Core;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        private const float GhostOpacity = 0.4f;
        private static readonly Color GhostColor = new Color(170, 210, 255);

        private readonly LevelData _level;
        private readonly bool _hasNextLevel;
        private readonly PanelMenu _pauseMenu = new PanelMenu("Paused", Color.White, new[] { "Resume", "Restart level", "Controls", "Main menu" });
        private readonly ControlsPanel _controls = new ControlsPanel();
        private readonly PanelMenu _deathMenu = new PanelMenu("You died", new Color(255, 90, 80), new[] { "Try again", "Main menu" }, fadeInTime: 0.5f);
        private readonly PanelMenu _checkpointDeathMenu = new PanelMenu("You died", new Color(255, 90, 80), new[] { "Back to checkpoint", "Restart level", "Main menu" }, fadeInTime: 0.5f);
        private readonly PanelMenu _completeMenu;
        private readonly Banner _banner = new Banner();
        private readonly DialogueBox _dialogue = new DialogueBox();
        private readonly GameSettings _settings = GameSettings.Current;
        private readonly Effects _effects = new Effects();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly IReadOnlyCollection<int> _gemsFoundBefore;
        private readonly HashSet<int> _gemsFound = new HashSet<int>();

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
        private bool _isRespawning;
        private Checkpoint _checkpoint;
        private Area _checkpointArea;

        /// <summary>Set once the player comes back at a checkpoint. Such a run doesn't count as a best time.</summary>
        private bool _usedCheckpoint;

        /// <summary>The medal this run earned, shown on the level complete screen.</summary>
        private Medal _medal;

        private Door _portalDestination;
        private Area _portalDestinationArea;
        private RunRecording _recording;
        private RunRecording _bestRun;

        /// <summary>The best run when this attempt started. The race ghost replays it, even after a new best is saved.</summary>
        private RunRecording _raceGhost;

        /// <summary>Which frame of <see cref="_raceGhost"/> to show. It keeps counting after the player reaches the exit.</summary>
        private int _raceFrame;

        /// <param name="gemsFoundBefore">Numbers of the level's gems found on earlier runs; they are drawn see-through.</param>
        public GameplayScreen(LevelData level, bool hasNextLevel, IReadOnlyCollection<int> gemsFoundBefore = null)
        {
            _level = level;
            _hasNextLevel = hasNextLevel;
            _gemsFoundBefore = gemsFoundBefore ?? Array.Empty<int>();
            _bestRun = RunRecording.LoadBest(level.Number, level.ContentHash);
            _completeMenu = new PanelMenu("Level complete!", UiDrawing.Highlight,
                hasNextLevel ? new[] { "Next level", "Play again", "Main menu" } : new[] { "Play again", "Main menu" },
                fadeInTime: 0.4f);
            Restart();
        }

        public int LevelNumber => _level.Number;

        /// <summary>Numbers of the gems collected on this run.</summary>
        public IReadOnlyCollection<int> GemsFound => _gemsFound;

        /// <summary>Background color of the area the player is in.</summary>
        public Color SkyColor => _area.Sky;

        /// <summary>
        /// The top of the view when the camera isn't following the player. The ground stays at the bottom of
        /// the window, so a window taller than 16:9 shows extra sky above the level.
        /// </summary>
        private static float RestingCameraTop => VirtualScreen.Height - VirtualScreen.ViewHeight;

        private void Restart()
        {
            _areas = new List<Area>();
            int firstGem = 0;
            foreach (AreaData data in _level.Areas)
            {
                _areas.Add(new Area(data, firstGem, _gemsFoundBefore));
                firstGem += data.Gems.Count;
            }
            _area = _areas[_level.StartArea];
            _gemsFound.Clear();
            _medal = Medal.None;
            _checkpoint = null;
            _checkpointArea = null;
            _usedCheckpoint = false;
            _player = new Player(_level.PlayerSpawn, _settings.Hearts);
            _recording = new RunRecording();
            _raceGhost = _bestRun;
            _raceFrame = 0;
            _projectiles.Clear();
            _effects.Clear();
            _messageTimer = 0f;
            _banner.Show(_level.Title);

            _pauseMenu.Close();
            _deathMenu.Close();
            _checkpointDeathMenu.Close();
            _completeMenu.Close();
            _dialogue.Close();
            _cameraTop = RestingCameraTop;
            _fade = 1f;
            _playTime = 0f;
            _exitTimer = 0f;
            _isRetrying = false;
            _isRespawning = false;
            _portalDestination = null;
        }

        /// <summary>Brings the player back at the last checkpoint. The rest of the level stays as it was.</summary>
        private void RespawnAtCheckpoint()
        {
            _area = _checkpointArea;
            _player.Respawn(_checkpoint.ArrivalPosition(_player.Bounds));
            _usedCheckpoint = true;
            _projectiles.Clear();
            _effects.Clear();
            _deathMenu.Close();
            _checkpointDeathMenu.Close();
            _cameraTop = CameraTarget();
            _fade = 1f;
            _isRespawning = false;
        }

        public GameplayResult Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isRetrying || _isRespawning)
            {
                // Fade to black, then start the level again or go back to the checkpoint (which fades back in).
                _fade = MathHelper.Min(_fade + RetryFadeOutSpeed, 1f);
                if (_fade >= 1f && _isRespawning)
                    RespawnAtCheckpoint();
                else if (_fade >= 1f)
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

            // While someone is talking the world waits, and Esc closes the conversation instead of pausing.
            if (_dialogue.IsOpen)
            {
                _dialogue.Update(deltaTime);
                return GameplayResult.None;
            }

            if (Input.Pause && !_player.IsDead)
            {
                StopSounds();
                _pauseMenu.Open();
                return GameplayResult.None;
            }

            UpdateWorld(gameTime);

            if (!_player.IsDead)
                return GameplayResult.None;

            return UpdateDeathMenu(deltaTime);
        }

        /// <summary>After a checkpoint the death screen offers to go back to it; otherwise the level starts again.</summary>
        private GameplayResult UpdateDeathMenu(float deltaTime)
        {
            PanelMenu menu = _checkpoint != null ? _checkpointDeathMenu : _deathMenu;
            if (!menu.IsOpen)
                menu.Open(_checkpoint != null ? "Your keys and gems stay with you." : null);

            int choice = menu.Update(deltaTime);
            if (_checkpoint == null && choice >= 0)
                choice++; // There is no "Back to checkpoint" button.

            switch (choice)
            {
                case 0:
                    StopSounds();
                    _isRespawning = true;
                    break;
                case 1:
                    StopSounds();
                    _isRetrying = true;
                    break;
                case 2:
                    StopSounds();
                    return GameplayResult.QuitToMenu;
            }
            return GameplayResult.None;
        }

        private GameplayResult UpdatePauseMenu(float deltaTime)
        {
            if (_controls.IsOpen)
            {
                _controls.Update();
                return GameplayResult.None;
            }

            int choice = Input.Back || Input.Pause ? 0 : _pauseMenu.Update(deltaTime);
            switch (choice)
            {
                case 0:
                    _pauseMenu.Close();
                    break;
                case 1:
                    _isRetrying = true;
                    break;
                case 2:
                    _controls.Open();
                    break;
                case 3:
                    return GameplayResult.QuitToMenu;
            }
            return GameplayResult.None;
        }

        /// <summary>After going through the exit the world stops, and a moment later the level complete screen opens.</summary>
        private GameplayResult UpdateLevelComplete(float deltaTime)
        {
            // A ghost that is still behind keeps running to its own exit, then fades out.
            _raceFrame++;

            if (!_completeMenu.IsOpen)
            {
                _exitTimer += deltaTime;
                _effects.Update(deltaTime);
                if (_exitTimer < CompleteScreenDelay)
                    return GameplayResult.None;

                StopSounds();
                _completeMenu.Open(SaveRunAndDescribe());
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

        /// <summary>Saves this run if it is the fastest so far, and returns the lines shown under "Level complete!".</summary>
        private string SaveRunAndDescribe()
        {
            string text = "Time: " + FormatTime(_playTime);
            RunRecording previousBest = _bestRun;
            if (_usedCheckpoint)
                text += "\nA checkpoint was used, so this run doesn't count as a best time.";
            else if (previousBest != null && previousBest.Time <= _playTime)
                text += "   (best " + FormatTime(previousBest.Time) + ")";
            else
            {
                _recording.SaveAsBest(_level.Number, _level.ContentHash, _playTime);
                _bestRun = _recording;
                if (previousBest != null)
                    text += "  -  new best!";
            }

            _medal = _usedCheckpoint || _level.Medals == null ? Medal.None : _level.Medals.For(_playTime);
            if (_level.Medals != null && !_usedCheckpoint)
                text += "\n" + DescribeMedal(_level.Medals, _medal);

            if (_level.GemCount > 0)
                text += $"\nGems found: {_gemsFound.Count} of {_level.GemCount}";
            return text;
        }

        /// <summary>"Silver medal! Beat 0:20 for gold." or "No medal yet. Beat 0:50 for bronze."</summary>
        private static string DescribeMedal(MedalTimes times, Medal medal)
        {
            string earned = medal == Medal.None ? "No medal yet." : $"{medal} medal!";
            if (times.NextTarget(medal) is not float target)
                return earned;

            string next = medal switch
            {
                Medal.None => "bronze",
                Medal.Bronze => "silver",
                _ => "gold",
            };
            return $"{earned} Beat {FormatTime(target)} for {next}.";
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
            else if (Input.ThrowPressed && _player.CanThrow)
            {
                // On a gamepad the right trigger throws where the right stick points, or straight ahead.
                _projectiles.Add(_player.Throw(_player.Center + Input.AimDirection(_player.FacingRight) * 500f));
            }

            if (Input.WasPressed(GameAction.Use) && !_player.IsDead)
            {
                Door door = _area.Doors.FirstOrDefault(d => _player.Bounds.Intersects(d.Bounds));
                Npc npc = _area.Npcs.FirstOrDefault(n => n.CanTalkTo(_player));
                if (door != null)
                {
                    UseDoor(door);
                }
                else if (npc != null)
                {
                    _player.StopSounds();
                    _dialogue.Open(npc.Name, npc.Lines);
                    return;
                }
            }

            // Moving platforms move first and carry whoever stands on them.
            foreach (Platform platform in _area.Platforms)
                platform.Move(deltaTime);
            _player.RideAlong();

            _player.Update(deltaTime, _area.Keys);
            foreach (Monster monster in _area.Monsters)
                monster.Update(_player);
            foreach (Ghost ghost in _area.Ghosts)
                ghost.Update(deltaTime, _player);
            foreach (KeyPickup key in _area.Keys)
                key.Update(deltaTime);
            foreach (Door door in _area.Doors)
                door.Update(deltaTime);
            foreach (Npc npc in _area.Npcs)
                npc.Update(deltaTime, _player);

            _player.ApplyGravity();
            _player.ResolveCollisions(_area.Platforms);
            foreach (Platform platform in _area.Platforms)
                platform.Update(gameTime, _player);

            if (_player.JustJumped)
                _effects.Dust(_player.Feet, 6);
            if (_player.JustLanded)
                _effects.Dust(_player.Feet, 10);

            if (!_player.IsDead && !_player.ReachedExit)
            {
                _recording.Add(_player.Position, _areas.IndexOf(_area), _player.FacingRight);
                _raceFrame++;
            }

            UpdateLevelObjects(deltaTime);
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

        private void UpdateLevelObjects(float deltaTime)
        {
            foreach (Checkpoint checkpoint in _area.Checkpoints)
            {
                checkpoint.Update(deltaTime);
                if (!checkpoint.IsActive && !_player.IsDead && _player.Bounds.Intersects(checkpoint.Bounds))
                {
                    foreach (Checkpoint other in _areas.SelectMany(area => area.Checkpoints))
                        other.Deactivate();
                    checkpoint.Activate();
                    _checkpoint = checkpoint;
                    _checkpointArea = _area;
                    _effects.Burst(checkpoint.Bounds.Center.ToVector2(), Color.LightGreen, 14);
                    ShowMessage("Checkpoint! If you die, you can come back here.");
                }
            }

            foreach (Spikes spikes in _area.Spikes)
            {
                if (spikes.Hitbox.Intersects(_player.Bounds))
                    _player.TakeDamage(1f, _player.Center.X < spikes.Bounds.Center.X ? -1 : 1);
            }

            foreach (BouncePad pad in _area.BouncePads)
            {
                pad.Update(deltaTime);
                if (pad.TryLaunch(_player))
                {
                    _effects.Dust(_player.Feet, 12);
                    Assets.PlayerJump.Play(0.8f * GameSettings.Current.SoundVolume, -0.4f, 0f);
                }
            }

            foreach (Gem gem in _area.Gems)
            {
                gem.Update(deltaTime);
                if (gem.IsCollected || _player.IsDead || !_player.Bounds.Intersects(gem.Bounds))
                    continue;

                gem.Collect();
                _gemsFound.Add(gem.Index);
                _effects.Burst(gem.Center, Color.Cyan, 16);
                Assets.PlayerJump.Play(0.6f * GameSettings.Current.SoundVolume, 0.8f, 0f);
                ShowMessage($"Gem found! {_gemsFound.Count} of {_level.GemCount}");
            }
        }

        private void ShowMessage(string message)
        {
            _message = message;
            _messageTimer = MessageTime;
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
                ShowMessage(powerUp.Type switch
                {
                    PowerUpType.Health => "+1 heart",
                    PowerUpType.DoubleJump => "Double jump! Press jump again in the air",
                    _ => "High jump! You jump much higher for a while",
                });
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

        /// <summary>
        /// Replays the best run as a see-through ghost, frame for frame, so the player can race it.
        /// It fades out shortly after reaching the exit.
        /// </summary>
        private void DrawRaceGhost(SpriteBatch spriteBatch)
        {
            if (!_settings.ShowRaceGhost || _usedCheckpoint || _raceGhost == null || _raceGhost.Frames.Count == 0)
                return;

            int index = _raceFrame;
            float opacity = GhostOpacity;
            if (index >= _raceGhost.Frames.Count)
            {
                opacity *= 1f - (index - _raceGhost.Frames.Count) / 60f;
                index = _raceGhost.Frames.Count - 1;
            }

            RunRecording.Frame frame = _raceGhost.Frames[index];
            if (opacity > 0f && frame.Area == _areas.IndexOf(_area))
                Player.DrawGhost(spriteBatch, frame.Position, frame.FacingRight, GhostColor * opacity);
        }

        /// <summary>How many of the level's gems were found on this run, under the inventory.</summary>
        private void DrawGemCounter(SpriteBatch spriteBatch)
        {
            string text = $"{_gemsFound.Count}/{_level.GemCount}";
            Vector2 size = Assets.SmallFont.MeasureString(text);
            var textPosition = new Vector2(VirtualScreen.ViewWidth - 24 - size.X, 124f);
            Gem.DrawIcon(spriteBatch, new Vector2(textPosition.X - 24, textPosition.Y + size.Y / 2f), 28f);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, textPosition, Color.White);
        }

        /// <summary>The time so far, and the best time to beat, at the top of the screen.</summary>
        private void DrawTimer(SpriteBatch spriteBatch)
        {
            string text = FormatTime(_playTime);
            if (_bestRun != null)
                text += "   Best " + FormatTime(_bestRun.Time);

            Vector2 size = Assets.SmallFont.MeasureString(text);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, text, new Vector2((VirtualScreen.ViewWidth - size.X) / 2f, 24f), Color.White);
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
            foreach (Spikes spikes in _area.Spikes)
                spikes.Draw(spriteBatch);
            foreach (BouncePad pad in _area.BouncePads)
                pad.Draw(spriteBatch);
            foreach (Checkpoint checkpoint in _area.Checkpoints)
                checkpoint.Draw(spriteBatch);
            foreach (Door door in _area.Doors)
                door.Draw(spriteBatch);
            foreach (Monster monster in _area.Monsters)
                monster.Draw(spriteBatch);
            foreach (KeyPickup key in _area.Keys)
                key.Draw(spriteBatch);
            foreach (PowerUp powerUp in _area.PowerUps)
                powerUp.Draw(spriteBatch);
            foreach (Gem gem in _area.Gems)
                gem.Draw(spriteBatch);
            foreach (Npc npc in _area.Npcs)
                npc.Draw(spriteBatch);
            DrawRaceGhost(spriteBatch);
            _player.Draw(spriteBatch);
            foreach (Ghost ghost in _area.Ghosts)
                ghost.Draw(spriteBatch);
            foreach (Projectile projectile in _projectiles)
                projectile.Draw(spriteBatch);
            _effects.Draw(spriteBatch);
            foreach (Door door in _area.Doors)
                door.DrawPrompt(spriteBatch, _player);
            if (!_dialogue.IsOpen && !_area.Doors.Any(door => _player.Bounds.Intersects(door.Bounds)))
            {
                foreach (Npc npc in _area.Npcs)
                    npc.DrawPrompt(spriteBatch, _player);
            }
            spriteBatch.End();

            // The hearts and inventory stick to the corners of the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.Transform);
            _player.DrawHud(spriteBatch);
            if (_settings.ShowTimer)
                DrawTimer(spriteBatch);
            if (_level.GemCount > 0)
                DrawGemCounter(spriteBatch);
            if (_messageTimer > 0f)
            {
                float opacity = MathHelper.Clamp(_messageTimer / 0.5f, 0f, 1f);
                UiDrawing.DrawHint(spriteBatch, new Vector2(VirtualScreen.ViewWidth / 2f, 112f), null, _message, opacity);
            }
            spriteBatch.End();

            // Banners and menus are centered in the window.
            spriteBatch.Begin(transformMatrix: VirtualScreen.CenteredTransform);
            _banner.Draw(spriteBatch);
            _dialogue.Draw(spriteBatch);
            _pauseMenu.Draw(spriteBatch);
            _controls.Draw(spriteBatch);
            _deathMenu.Draw(spriteBatch);
            _checkpointDeathMenu.Draw(spriteBatch);
            _completeMenu.Draw(spriteBatch);
            if (_completeMenu.IsOpen && _medal != Medal.None)
            {
                // The medal hangs over the top-right corner of the level complete panel.
                Rectangle panel = _completeMenu.Bounds;
                UiDrawing.DrawMedal(spriteBatch, new Vector2(panel.Right - 20, panel.Y + 30), 76f, _medal, _completeMenu.Visibility);
            }
            spriteBatch.Draw(Assets.Pixel, VirtualScreen.CenteredViewBounds, Color.Black * _fade);
            spriteBatch.End();
        }
    }
}
