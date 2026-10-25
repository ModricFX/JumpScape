using System;
using System.Collections.Generic;
using JumpScape.Core;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// A friendly explorer who stands on a platform and talks when the player presses E nearby.
    /// It uses the player's sprite with a warm tint, and always turns to face the player.
    /// </summary>
    public sealed class Npc
    {
        private const float Scale = 0.15f;
        private const int TalkRange = 70;
        private static readonly Color Tint = new Color(255, 205, 150);

        private bool _facingRight;
        private float _time;

        public Npc(Vector2 position, string name, IReadOnlyList<string> lines)
        {
            Position = position;
            Name = name;
            Lines = lines;
        }

        public Vector2 Position { get; private set; }

        public string Name { get; }

        public IReadOnlyList<string> Lines { get; }

        private static int Width => (int)(Assets.PlayerRight.Width * Scale);

        private static int Height => (int)(Assets.PlayerRight.Height * Scale);

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public bool CanTalkTo(Player player)
        {
            Rectangle range = Bounds;
            range.Inflate(TalkRange, 20);
            return Lines.Count > 0 && !player.IsDead && !player.ReachedExit && range.Intersects(player.Bounds);
        }

        /// <summary>Moves the NPC down (or up) so it stands on the platform below its position.</summary>
        public void StandOn(IEnumerable<Platform> platforms)
        {
            float centerX = Position.X + Width / 2f;
            float feet = Position.Y + Height;
            float? ground = null;

            foreach (Platform platform in platforms)
            {
                Rectangle bounds = platform.Bounds;
                bool below = centerX >= bounds.Left && centerX <= bounds.Right && bounds.Top >= feet - Height / 2f;
                if (below && (ground == null || bounds.Top < ground))
                    ground = bounds.Top;
            }

            if (ground is float top)
                Position = new Vector2(Position.X, top - Height);
        }

        public void Update(float deltaTime, Player player)
        {
            _time += deltaTime;
            _facingRight = player.Center.X > Position.X + Width / 2f;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = _facingRight ? Assets.PlayerRight : Assets.PlayerLeft;

            // Breathe gently so the NPC doesn't look frozen.
            float breath = MathF.Sin(_time * 2.5f) * 0.015f;
            var scale = new Vector2(Scale * (1f - breath), Scale * (1f + breath));
            var feet = new Vector2(Position.X + Width / 2f, Position.Y + Height);
            spriteBatch.Draw(texture, feet, null, Tint, 0f, new Vector2(texture.Width / 2f, texture.Height), scale, SpriteEffects.None, 0f);
        }

        public void DrawPrompt(SpriteBatch spriteBatch, Player player)
        {
            if (CanTalkTo(player))
                UiDrawing.DrawHint(spriteBatch, new Vector2(Bounds.Center.X, Bounds.Y - 10), Controls.KeyNameFor(GameAction.Use), "Talk to " + Name);
        }
    }
}
