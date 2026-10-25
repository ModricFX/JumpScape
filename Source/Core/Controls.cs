using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace JumpScape.Core
{
    /// <summary>The things the player can do with keys they choose themselves in the settings.</summary>
    public enum GameAction
    {
        MoveLeft,
        MoveRight,
        Jump,
        Use,
        DropKey,
    }

    /// <summary>
    /// Which keys do what. Every action has one to <see cref="KeysPerAction"/> keys, saved in the settings.
    /// The inventory slots (1-3), pause (Esc) and throwing (the mouse) can't be changed, and neither can the
    /// gamepad buttons.
    /// </summary>
    public static class Controls
    {
        public const int KeysPerAction = 3;

        private static readonly Keys[] FixedKeys =
        {
            Keys.None, Keys.Escape, Keys.D1, Keys.D2, Keys.D3, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3,
        };

        private static readonly Dictionary<Keys, string> Names = new Dictionary<Keys, string>
        {
            [Keys.Back] = "Backspace",
            [Keys.Escape] = "Esc",
            [Keys.LeftControl] = "Left Ctrl",
            [Keys.RightControl] = "Right Ctrl",
            [Keys.OemComma] = ",",
            [Keys.OemPeriod] = ".",
            [Keys.OemMinus] = "-",
            [Keys.OemPlus] = "=",
            [Keys.OemQuestion] = "/",
            [Keys.OemSemicolon] = ";",
            [Keys.OemQuotes] = "'",
            [Keys.OemOpenBrackets] = "[",
            [Keys.OemCloseBrackets] = "]",
            [Keys.OemPipe] = "\\",
            [Keys.OemBackslash] = "\\",
            [Keys.OemTilde] = "`",
        };

        /// <summary>Placeholders that NPC lines can use, like "Jump with {jump}."</summary>
        private static readonly (string Placeholder, GameAction Action)[] Placeholders =
        {
            ("{left}", GameAction.MoveLeft),
            ("{right}", GameAction.MoveRight),
            ("{jump}", GameAction.Jump),
            ("{use}", GameAction.Use),
            ("{drop}", GameAction.DropKey),
        };

        public static Dictionary<GameAction, Keys[]> DefaultBindings()
        {
            return new Dictionary<GameAction, Keys[]>
            {
                [GameAction.MoveLeft] = new[] { Keys.A, Keys.Left },
                [GameAction.MoveRight] = new[] { Keys.D, Keys.Right },
                [GameAction.Jump] = new[] { Keys.Space, Keys.W, Keys.Up },
                [GameAction.Use] = new[] { Keys.E },
                [GameAction.DropKey] = new[] { Keys.Q },
            };
        }

        /// <summary>The keys for an action, most important first.</summary>
        public static IReadOnlyList<Keys> KeysFor(GameAction action) => GameSettings.Current.KeyBindings[action];

        /// <summary>
        /// The name of the first key for an action, for hints like "[E] Open". While the player uses a gamepad
        /// it is the gamepad button instead.
        /// </summary>
        public static string KeyNameFor(GameAction action) => Input.UsingGamepad ? ButtonName(action) : KeyName(KeysFor(action)[0]);

        /// <summary>The keys (or gamepad buttons) that choose an inventory slot.</summary>
        public static string SlotKeysName => Input.UsingGamepad ? "LB/RB" : "1-3";

        /// <summary>The gamepad button for an action. The left stick also walks.</summary>
        public static Buttons ButtonFor(GameAction action)
        {
            return action switch
            {
                GameAction.MoveLeft => Buttons.DPadLeft,
                GameAction.MoveRight => Buttons.DPadRight,
                GameAction.Jump => Buttons.A,
                GameAction.Use => Buttons.X,
                _ => Buttons.Y,
            };
        }

        public static string ButtonName(GameAction action)
        {
            return action switch
            {
                GameAction.MoveLeft => "D-pad left",
                GameAction.MoveRight => "D-pad right",
                GameAction.Jump => "A",
                GameAction.Use => "X",
                _ => "Y",
            };
        }

        /// <summary>False for keys that already do something fixed: Esc and the inventory slots.</summary>
        public static bool CanBind(Keys key) => Array.IndexOf(FixedKeys, key) < 0;

        public static string ActionName(GameAction action)
        {
            return action switch
            {
                GameAction.MoveLeft => "Walk left",
                GameAction.MoveRight => "Walk right",
                GameAction.Jump => "Jump",
                GameAction.Use => "Open doors and talk",
                _ => "Drop the selected key",
            };
        }

        /// <summary>A short, readable name for a key: "Space", "Left Shift", "7", ",".</summary>
        public static string KeyName(Keys key)
        {
            if (Names.TryGetValue(key, out string name))
                return name;
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((int)(key - Keys.D0)).ToString(CultureInfo.InvariantCulture);
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return "Num " + ((int)(key - Keys.NumPad0)).ToString(CultureInfo.InvariantCulture);

            // "LeftShift" becomes "Left Shift".
            string text = key.ToString();
            var spaced = new StringBuilder(text.Length + 4);
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                    spaced.Append(' ');
                spaced.Append(text[i]);
            }
            return spaced.ToString();
        }

        /// <summary>Replaces {left}, {right}, {jump}, {use} and {drop} with the player's own keys.</summary>
        public static string FillIn(string text)
        {
            foreach ((string placeholder, GameAction action) in Placeholders)
            {
                if (text.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                    text = text.Replace(placeholder, KeyNameFor(action), StringComparison.OrdinalIgnoreCase);
            }
            return text;
        }

        /// <summary>
        /// Makes saved bindings safe to use: every action gets at least one key, no key does two things,
        /// and fixed keys are left out. Missing or broken entries fall back to the defaults.
        /// </summary>
        public static Dictionary<GameAction, Keys[]> Validate(Dictionary<GameAction, Keys[]> saved)
        {
            Dictionary<GameAction, Keys[]> defaults = DefaultBindings();
            var result = new Dictionary<GameAction, Keys[]>();
            var used = new HashSet<Keys>();
            foreach (GameAction action in Enum.GetValues<GameAction>())
            {
                Keys[] keys = (saved != null && saved.TryGetValue(action, out Keys[] value) ? value : null) ?? Array.Empty<Keys>();
                keys = keys.Where(key => Enum.IsDefined(key) && CanBind(key) && used.Add(key)).Take(KeysPerAction).ToArray();
                if (keys.Length == 0)
                    keys = defaults[action].Where(used.Add).ToArray();
                result[action] = keys;
            }
            return result;
        }
    }
}
