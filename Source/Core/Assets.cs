using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace JumpScape.Core
{
    /// <summary>
    /// Every texture, sound and font the game uses, loaded once at startup through the content pipeline
    /// (see Content/Content.mgcb) and shared by all objects.
    /// </summary>
    public static class Assets
    {
        /// <summary>Arial 32: titles, buttons and menus.</summary>
        public static SpriteFont Font { get; private set; }

        /// <summary>Arial 20: hints, labels and other small text, so it stays crisp.</summary>
        public static SpriteFont SmallFont { get; private set; }

        /// <summary>A 1x1 white texture for drawing solid rectangles in any color.</summary>
        public static Texture2D Pixel { get; private set; }

        /// <summary>A soft white circle that fades out towards the edge. Tint it to get a colored glow.</summary>
        public static Texture2D Glow { get; private set; }

        /// <summary>A solid white circle with smooth edges, for slider knobs and similar.</summary>
        public static Texture2D Circle { get; private set; }

        public static Texture2D PlayerLeft { get; private set; }
        public static Texture2D PlayerRight { get; private set; }
        public static Texture2D FrogLeft { get; private set; }
        public static Texture2D FrogRight { get; private set; }
        public static Texture2D FrogLeftAlert { get; private set; }
        public static Texture2D FrogRightAlert { get; private set; }
        public static Texture2D GhostLeft { get; private set; }
        public static Texture2D GhostRight { get; private set; }
        public static Texture2D Grass { get; private set; }
        public static Texture2D DoorLocked { get; private set; }
        public static Texture2D DoorUnlocked { get; private set; }
        public static Texture2D DoorOpened { get; private set; }
        public static Texture2D KeySpriteSheet { get; private set; }
        public static Texture2D KeyInventoryIcon { get; private set; }
        public static Texture2D DoubleJumpOrb { get; private set; }
        public static Texture2D HighJumpOrb { get; private set; }
        public static Texture2D HeartFull { get; private set; }
        public static Texture2D HeartEmpty { get; private set; }
        public static Texture2D[] InventorySlots { get; private set; }

        public static Texture2D Logo { get; private set; }
        public static Texture2D MenuBackground { get; private set; }

        /// <summary>A very soft, blurred copy of <see cref="MenuBackground"/>, used behind the levels.</summary>
        public static Texture2D BlurredBackground { get; private set; }
        public static Texture2D MenuButton { get; private set; }
        public static Texture2D WoodPanel { get; private set; }
        public static Texture2D LevelBox { get; private set; }
        public static Texture2D Star { get; private set; }

        /// <summary>Streamed from disk by <see cref="MediaPlayer"/> instead of being held in memory.</summary>
        public static Song ThemeSong { get; private set; }

        public static SoundEffect PlayerWalking { get; private set; }
        public static SoundEffect PlayerJump { get; private set; }
        public static SoundEffect PlayerHit { get; private set; }
        public static SoundEffect MonsterGrowl { get; private set; }
        public static SoundEffect PlatformCracking { get; private set; }
        public static SoundEffect PlatformBreak { get; private set; }
        public static SoundEffect DoorOpen { get; private set; }

        public static void Load(ContentManager content, GraphicsDevice graphicsDevice)
        {
            Font = content.Load<SpriteFont>("Fonts/BigFont");
            SmallFont = content.Load<SpriteFont>("Fonts/SmallFont");

            Pixel = new Texture2D(graphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
            Glow = CreateCircle(graphicsDevice, 64, distance => MathF.Pow(1f - distance, 1.5f));
            Circle = CreateCircle(graphicsDevice, 64, distance => MathHelper.Clamp((1f - distance) * 32f, 0f, 1f));

            PlayerLeft = content.Load<Texture2D>("Graphics/Player/player_left");
            PlayerRight = content.Load<Texture2D>("Graphics/Player/player_right");
            FrogLeft = content.Load<Texture2D>("Graphics/Monsters/frog_monster_left");
            FrogRight = content.Load<Texture2D>("Graphics/Monsters/frog_monster_right");
            FrogLeftAlert = content.Load<Texture2D>("Graphics/Monsters/frog_monster_left_yellow");
            FrogRightAlert = content.Load<Texture2D>("Graphics/Monsters/frog_monster_right_yellow");
            GhostLeft = content.Load<Texture2D>("Graphics/Monsters/ghost_monster_left");
            GhostRight = content.Load<Texture2D>("Graphics/Monsters/ghost_monster_right");
            Grass = content.Load<Texture2D>("Graphics/grass");
            DoorLocked = content.Load<Texture2D>("Graphics/Doors/door_locked");
            DoorUnlocked = content.Load<Texture2D>("Graphics/Doors/door_unlocked");
            DoorOpened = content.Load<Texture2D>("Graphics/Doors/door_opened");
            KeySpriteSheet = content.Load<Texture2D>("Graphics/Items/key_sprite_sheet");
            KeyInventoryIcon = content.Load<Texture2D>("Graphics/Items/key_inventory");
            DoubleJumpOrb = content.Load<Texture2D>("Graphics/Items/ball_green");
            HighJumpOrb = content.Load<Texture2D>("Graphics/Items/ball_red");
            HeartFull = content.Load<Texture2D>("Graphics/Hearts/Heart_full");
            HeartEmpty = content.Load<Texture2D>("Graphics/Hearts/Heart_empty");
            InventorySlots = new[]
            {
                content.Load<Texture2D>("Graphics/Inventory/Inventory_select_1"),
                content.Load<Texture2D>("Graphics/Inventory/Inventory_select_2"),
                content.Load<Texture2D>("Graphics/Inventory/Inventory_select_3"),
            };

            Logo = content.Load<Texture2D>("Graphics/GameLogo/JumpScapeLogo");
            MenuBackground = content.Load<Texture2D>("Graphics/Menu/background");
            BlurredBackground = CreateBlurred(graphicsDevice, MenuBackground, halvings: 3);
            MenuButton = content.Load<Texture2D>("Graphics/Menu/buttonBackground");
            WoodPanel = content.Load<Texture2D>("Graphics/Menu/woodBackground");
            LevelBox = content.Load<Texture2D>("Graphics/Menu/levelBox");
            Star = content.Load<Texture2D>("Graphics/Menu/star");

            ThemeSong = content.Load<Song>("Sounds/ThemeSong");
            PlayerWalking = content.Load<SoundEffect>("Sounds/PlayerWalking");
            PlayerJump = content.Load<SoundEffect>("Sounds/PlayerJump");
            PlayerHit = content.Load<SoundEffect>("Sounds/PlayerHit");
            MonsterGrowl = content.Load<SoundEffect>("Sounds/MonsterSound");
            PlatformCracking = content.Load<SoundEffect>("Sounds/PlatformBreaking");
            PlatformBreak = content.Load<SoundEffect>("Sounds/PlatformBreak");
            DoorOpen = content.Load<SoundEffect>("Sounds/DoorOpen");
        }

        /// <summary>
        /// Shrinks a picture to half its size a few times. Drawn stretched back to full size, the small result
        /// looks blurred. This is done once at startup, so it costs nothing while playing.
        /// </summary>
        private static Texture2D CreateBlurred(GraphicsDevice graphicsDevice, Texture2D source, int halvings)
        {
            Texture2D current = source;
            using (var spriteBatch = new SpriteBatch(graphicsDevice))
            {
                for (int i = 0; i < halvings; i++)
                {
                    var target = new RenderTarget2D(graphicsDevice, Math.Max(1, current.Width / 2), Math.Max(1, current.Height / 2),
                        false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    graphicsDevice.SetRenderTarget(target);
                    graphicsDevice.Clear(Color.Transparent);
                    spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
                    spriteBatch.Draw(current, target.Bounds, Color.White);
                    spriteBatch.End();

                    if (current != source)
                        current.Dispose();
                    current = target;
                }
            }

            graphicsDevice.SetRenderTarget(null);
            return current;
        }

        /// <summary>Makes a white circle texture. <paramref name="opacity"/> maps the distance from the center (0-1) to opacity.</summary>
        private static Texture2D CreateCircle(GraphicsDevice graphicsDevice, int size, Func<float, float> opacity)
        {
            var pixels = new Color[size * size];
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius)) / radius;
                    pixels[y * size + x] = distance >= 1f ? Color.Transparent : Color.White * MathHelper.Clamp(opacity(distance), 0f, 1f);
                }
            }

            var texture = new Texture2D(graphicsDevice, size, size);
            texture.SetData(pixels);
            return texture;
        }
    }
}
