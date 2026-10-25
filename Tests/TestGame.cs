using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JumpScape.Core;
using JumpScape.Gameplay;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public enum TestMode
{
    /// <summary>Every check, saving screenshots along the way.</summary>
    All,

    /// <summary>The automated player plays levels from start to finish.</summary>
    Levels,

    /// <summary>A picture of every area of every level.</summary>
    Overview,
}

/// <summary>
/// Runs the checks inside a small MonoGame window. The game's code is compiled into this project, so the
/// checks can drive it frame by frame with made-up keyboard and mouse input, and look at private fields.
/// </summary>
partial class TestGame : Game
{
    const BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    static readonly TimeSpan Step = TimeSpan.FromSeconds(1.0 / 60.0);
    const float Dt = 1f / 60f;
    static readonly List<KeyPickup> NoKeys = new List<KeyPickup>();

    readonly GraphicsDeviceManager _gdm;
    readonly string _shots;
    readonly TestMode _mode;
    readonly HashSet<int> _onlyLevels;
    SpriteBatch _sb;
    MenuBackground _bg;
    TimeSpan _total;
    MouseState _mouse;

    /// <summary>The made-up gamepad state Press hands to the game; the default is nothing pressed.</summary>
    GamePadState _pad;
    Vector2 _mousePos = new Vector2(-1000, -1000);

    public int Failures;

    /// <param name="onlyLevels">For <see cref="TestMode.Levels"/>: the levels to play, or null for all of them.</param>
    public TestGame(string screenshots, TestMode mode, HashSet<int> onlyLevels)
    {
        _gdm = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 480, PreferredBackBufferHeight = 270 };
        Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
        _shots = screenshots;
        _mode = mode;
        _onlyLevels = onlyLevels;
    }

    protected override void LoadContent()
    {
        _sb = new SpriteBatch(GraphicsDevice);
        Assets.Load(Content, GraphicsDevice);

        // The checks run thousands of frames per second, but sounds play in real time and would pile up.
        // A 1 ms silent clip finishes almost at once.
        var silence = new SoundEffect(new byte[88], 44100, AudioChannels.Mono);
        foreach (var property in typeof(Assets).GetProperties().Where(prop => prop.PropertyType == typeof(SoundEffect)))
            property.SetValue(null, silence);
        SoundEffect.MasterVolume = 0f;
        VirtualScreen.Resize(1920, 1080);

        try
        {
            switch (_mode)
            {
                case TestMode.Levels: TestLevelsArePlayable(); break;
                case TestMode.Overview: LevelOverviews(); break;
                default: RunAll(); break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("EXCEPTION: " + e);
            Failures++;
        }
        Console.WriteLine(Failures == 0 ? "ALL CHECKS PASSED" : $"{Failures} CHECK(S) FAILED");
        Exit();
    }

    void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what); if (!ok) Failures++; }
    static object GetStatic(Type t, string n) => t.GetField(n, Any).GetValue(null);
    static void SetStatic(Type t, string n, object v) => t.GetField(n, Any).SetValue(null, v);
    static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Any).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Any).SetValue(o, v);

    void Press(params Keys[] keys)
    {
        SetStatic(typeof(Input), "_previousKeyboard", GetStatic(typeof(Input), "_keyboard"));
        SetStatic(typeof(Input), "_keyboard", new KeyboardState(keys));
        SetStatic(typeof(Input), "_previousMouse", GetStatic(typeof(Input), "_mouse"));
        SetStatic(typeof(Input), "_mouse", _mouse);
        typeof(Input).GetProperty("MousePosition", Any).SetValue(null, _mousePos);
        typeof(Input).GetProperty("ViewMousePosition", Any).SetValue(null, _mousePos);
        SetStatic(typeof(Input), "_previousGamePad", GetStatic(typeof(Input), "_gamePad"));
        SetStatic(typeof(Input), "_gamePad", _pad);
    }

    /// <summary>One frame with the left mouse button pressed at a world position.</summary>
    GameplayResult ClickAt(GameplayScreen gs, Vector2 world)
    {
        float cameraTop = Get<float>(gs, "_cameraTop");
        SetMouse(world - new Vector2(0, cameraTop), true);
        var r = Frame(gs);
        SetMouse(world - new Vector2(0, cameraTop), false);
        return r;
    }

    void SetMouse(Vector2 pos, bool down)
    {
        _mousePos = pos;
        _mouse = new MouseState((int)pos.X, (int)pos.Y, 0, down ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    GameTime NextTime() { FrameworkDispatcher.Update(); _total += Step; return new GameTime(_total, Step); }

    GameplayResult Frame(GameplayScreen gs, params Keys[] keys) { Press(keys); return gs.Update(NextTime()); }

    GameplayResult Frames(GameplayScreen gs, int n, params Keys[] keys)
    {
        var r = GameplayResult.None;
        for (int i = 0; i < n && r == GameplayResult.None; i++) r = Frame(gs, keys);
        return r;
    }

    static Player PlayerOf(GameplayScreen gs) => Get<Player>(gs, "_player");
    static Area AreaOf(GameplayScreen gs) => Get<Area>(gs, "_area");

    void StandAt(GameplayScreen gs, Door door)
    {
        Teleport(PlayerOf(gs), door.ArrivalPosition(PlayerOf(gs).Bounds));
        Frames(gs, 3);
    }
    static void Teleport(Player p, Vector2 pos)
    {
        typeof(Player).GetProperty("Position", Any).SetValue(p, pos);
        Set(p, "_velocity", Vector2.Zero);
    }
    static int Health(Player p) => Get<int>(p, "_health");

    /// <summary>Jumps are lower if jump is let go on the way up, so a full jump holds it until the top.</summary>
    static bool Rising(Player p) => Get<Vector2>(p, "_velocity").Y < 0f;
    static int PH => (int)(Assets.PlayerRight.Height * 0.15f);
    static int PW => (int)(Assets.PlayerRight.Width * 0.15f);

    void Shot(string name, Action<SpriteBatch> draw, Color clear, int w = 1920, int h = 1080)
    {
        VirtualScreen.Resize(w, h);
        using var rt = new RenderTarget2D(GraphicsDevice, w, h);
        GraphicsDevice.SetRenderTarget(rt);
        GraphicsDevice.Clear(clear);
        draw(_sb);
        GraphicsDevice.SetRenderTarget(null);
        using (var fs = File.Create(Path.Combine(_shots, name + ".png")))
            rt.SaveAsPng(fs, w, h);
        VirtualScreen.Resize(1920, 1080);
    }
    void ShotGame(string name, GameplayScreen gs, int w = 1920, int h = 1080) => Shot(name, gs.Draw, gs.SkyColor, w, h);
    void ShotMenu(string name, Action<SpriteBatch> draw, int w = 1920, int h = 1080) => Shot(name, sb =>
    {
        sb.Begin(transformMatrix: VirtualScreen.Transform); _bg.Draw(sb); sb.End();
        sb.Begin(transformMatrix: VirtualScreen.CenteredTransform); draw(sb); sb.End();
    }, Color.Black, w, h);
}
