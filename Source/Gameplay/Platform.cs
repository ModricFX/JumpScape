using System;
using System.Collections.Generic;
using JumpScape.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A grass platform. Crumbling platforms start shaking when the player lands on them,
    /// break after a few seconds and come back a few seconds later. Moving platforms glide back and forth
    /// between two places, slowing down at each end, and carry the player standing on them.
    /// </summary>
    public sealed class Platform
    {
        private const float CrumbleTime = 3f;
        private const float RespawnTime = 5f;
        private const float SoundRange = 500f;
        private const float SoundVolume = 0.8f;
        private const int DebrisCount = 12;
        private static readonly Vector2 ShadowOffset = new Vector2(8, 10);

        private static readonly Random Random = new Random();

        private readonly List<Particle> _debris = new List<Particle>();
        private readonly SoundEffectInstance _crackingSound;
        private readonly SoundEffectInstance _breakSound;
        private readonly Vector2 _start;
        private readonly Vector2? _end;
        private readonly float _moveTime;
        private float _timer;
        private bool _isCrumbling;
        private bool _isBroken;
        private float _shakeAngle;

        /// <param name="moveTo">For a moving platform, the other end of its path.</param>
        /// <param name="moveTime">Seconds a moving platform takes from one end of its path to the other.</param>
        public Platform(Vector2 position, int length, bool canCrumble, Vector2? moveTo = null, float moveTime = 3f)
        {
            Position = position;
            Length = length;
            CanCrumble = canCrumble;
            _start = position;
            _end = moveTo;
            _moveTime = moveTime;

            if (canCrumble)
            {
                _crackingSound = Assets.PlatformCracking.CreateInstance();
                _crackingSound.IsLooped = true;
                _breakSound = Assets.PlatformBreak.CreateInstance();
            }
        }

        public Vector2 Position { get; private set; }

        public int Length { get; }

        public bool CanCrumble { get; }

        public bool IsMoving => _end != null;

        /// <summary>Where a moving platform starts, and the other end of its path.</summary>
        public Vector2 Start => _start;

        public Vector2? MoveTo => _end;

        public float MoveTime => _moveTime;

        /// <summary>Seconds for a moving platform to go there and back.</summary>
        public float CycleTime => _moveTime * 2f;

        /// <summary>Seconds the platform has been moving, which says where it is on its path.</summary>
        public double Clock { get; private set; }

        /// <summary>How far the platform moved in the last <see cref="Move"/>, so it can carry the player.</summary>
        public Vector2 Delta { get; private set; }

        /// <summary>False while a crumbled platform is gone.</summary>
        public bool IsSolid => !_isBroken;

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Length, Assets.Grass.Height);

        private Vector2 Center => Bounds.Center.ToVector2();

        /// <summary>Moves a moving platform along its path. Does nothing for other platforms.</summary>
        public void Move(float deltaTime)
        {
            if (!IsMoving)
                return;

            Vector2 previous = Position;
            Clock += deltaTime;
            Position = PositionAt(Clock);
            Delta = Position - previous;
        }

        /// <summary>Puts a moving platform where it is after <paramref name="clock"/> seconds. Used by the tests.</summary>
        public void SetClock(double clock)
        {
            Clock = clock;
            Position = PositionAt(clock);
            Delta = Vector2.Zero;
        }

        /// <summary>Eases back and forth, slowing down at each end so riders aren't thrown off.</summary>
        private Vector2 PositionAt(double clock)
        {
            if (_end is not Vector2 end)
                return _start;
            float t = (float)((1.0 - Math.Cos(Math.PI * clock / _moveTime)) / 2.0);
            return Vector2.Lerp(_start, end, t);
        }

        public void OnPlayerLanded()
        {
            if (!CanCrumble || _isCrumbling || _isBroken)
                return;

            _isCrumbling = true;
            _timer = CrumbleTime;
        }

        public void Update(GameTime gameTime, Player player)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                _debris[i].Update(deltaTime);
                if (_debris[i].IsDead)
                    _debris.RemoveAt(i);
            }

            if (_isCrumbling)
            {
                _shakeAngle = MathF.Sin((float)gameTime.TotalGameTime.TotalMilliseconds * 0.03f) * 0.02f;
                PositionalSound.Apply(_crackingSound, Center, player.Center, SoundRange, SoundVolume);
                if (player.IsDead)
                    _crackingSound.Stop();
                else if (_crackingSound.State != SoundState.Playing)
                    _crackingSound.Play();

                _timer -= deltaTime;
                if (_timer <= 0f)
                    Break(player);
            }
            else if (_isBroken)
            {
                _timer -= deltaTime;
                if (_timer <= 0f)
                    _isBroken = false;
            }
        }

        private void Break(Player player)
        {
            _isCrumbling = false;
            _isBroken = true;
            _timer = RespawnTime;
            _shakeAngle = 0f;

            _crackingSound.Stop();
            if (!player.IsDead)
            {
                PositionalSound.Apply(_breakSound, Center, player.Center, SoundRange, SoundVolume);
                _breakSound.Play();
            }

            for (int i = 0; i < DebrisCount; i++)
            {
                var position = new Vector2(Position.X + (float)Random.NextDouble() * Length, Position.Y - 4);
                var velocity = new Vector2((float)Random.NextDouble() * 100f - 50f, -50f - (float)Random.NextDouble() * 25f);
                float lifetime = 1f + (float)Random.NextDouble() * 1.5f;
                _debris.Add(new Particle(position, velocity, lifetime, Color.SaddleBrown, size: 6f));
            }
        }

        public void StopSounds()
        {
            _crackingSound?.Stop();
            _breakSound?.Stop();
        }

        /// <summary>Draws the platform by repeating the grass texture along its length, tinted with the area's color.</summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle visibleArea, Color tint)
        {
            foreach (Particle particle in _debris)
                particle.Draw(spriteBatch);

            if (!_isBroken && Bounds.Intersects(visibleArea))
                DrawTiles(spriteBatch, Vector2.Zero, tint);
        }

        /// <summary>A see-through dark copy of the platform, a little lower and to the right. Draw all shadows before any platform.</summary>
        public void DrawShadow(SpriteBatch spriteBatch, Rectangle visibleArea)
        {
            if (_end is Vector2 end)
                DrawPath(spriteBatch, end);
            if (!_isBroken && Bounds.Intersects(visibleArea))
                DrawTiles(spriteBatch, ShadowOffset, Color.Black * 0.25f);
        }

        /// <summary>A dotted line along a moving platform's path, so the player can see where it goes.</summary>
        private void DrawPath(SpriteBatch spriteBatch, Vector2 end)
        {
            var half = new Vector2(Length / 2f, Assets.Grass.Height / 2f);
            Vector2 from = _start + half, to = end + half;
            float distance = Vector2.Distance(from, to);
            for (float d = 0f; d <= distance; d += 24f)
            {
                Vector2 dot = Vector2.Lerp(from, to, distance == 0f ? 0f : d / distance);
                spriteBatch.Draw(Assets.Pixel, new Rectangle((int)dot.X - 3, (int)dot.Y - 3, 6, 6), Color.White * 0.35f);
            }
        }

        private void DrawTiles(SpriteBatch spriteBatch, Vector2 offset, Color color)
        {
            Texture2D texture = Assets.Grass;
            for (int x = 0; x < Length; x += texture.Width)
            {
                int tileWidth = Math.Min(texture.Width, Length - x);
                var source = new Rectangle(0, 0, tileWidth, texture.Height);
                var origin = new Vector2(tileWidth / 2f, texture.Height / 2f);
                spriteBatch.Draw(texture, Position + offset + new Vector2(x, 0) + origin, source, color, _shakeAngle, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
