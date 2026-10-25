using System;
using System.Collections.Generic;
using JumpScape.Core;
using JumpScape.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpScape.Editor
{
    internal enum WidgetKind
    {
        Label,
        Button,
        Toggle,
        TextField,
    }

    /// <summary>One control in the editor's bars and panels. They are laid out again every frame.</summary>
    internal sealed class Widget
    {
        public WidgetKind Kind;
        public Rectangle Bounds;
        public string Text;

        /// <summary>A ticked toggle, or a button shown as chosen (like the current tool).</summary>
        public bool On;

        public bool Enabled = true;
        public Action Click;

        /// <summary>For text fields: which text this is, and how to read and change it.</summary>
        public string FieldId;
        public Func<string> GetText;
        public Action<string> SetText;
    }

    /// <summary>Lays widgets out from left to right, all the same height.</summary>
    internal sealed class WidgetRow
    {
        private const int Gap = 8;
        private const int ButtonPadding = 14;

        private readonly int _y;
        private readonly int _height;
        private int _x;

        public WidgetRow(List<Widget> widgets, int x, int y, int height)
        {
            Widgets = widgets;
            _x = x;
            _y = y;
            _height = height;
        }

        public List<Widget> Widgets { get; }

        public int X => _x;

        public void Space(int pixels) => _x += pixels;

        private Rectangle Next(int width)
        {
            var bounds = new Rectangle(_x, _y, width, _height);
            _x += width + Gap;
            return bounds;
        }

        private static int TextWidth(string text) => (int)Assets.SmallFont.MeasureString(text).X;

        public void Label(string text, Color? color = null)
        {
            Widgets.Add(new Widget { Kind = WidgetKind.Label, Bounds = Next(TextWidth(text)), Text = text, On = color != null });
        }

        public Widget Button(string text, Action click, bool on = false, int width = 0, bool enabled = true)
        {
            var widget = new Widget
            {
                Kind = WidgetKind.Button,
                Bounds = Next(width > 0 ? width : TextWidth(text) + ButtonPadding * 2),
                Text = text,
                On = on,
                Enabled = enabled,
                Click = click,
            };
            Widgets.Add(widget);
            return widget;
        }

        public void Toggle(string text, bool on, Action click)
        {
            Widgets.Add(new Widget { Kind = WidgetKind.Toggle, Bounds = Next(_height + 6 + TextWidth(text)), Text = text, On = on, Click = click });
        }

        /// <summary>"Length  [-] 200 [+]".</summary>
        public void Stepper(string label, string value, Action down, Action up)
        {
            Label(label);
            Button("-", down, width: 34);
            Label(value);
            Button("+", up, width: 34);
        }

        /// <summary>"Type  [&lt;] Health [&gt;]".</summary>
        public void Choice(string label, string value, Action previous, Action next)
        {
            Label(label);
            Button("<", previous, width: 34);
            Label(value);
            Button(">", next, width: 34);
        }

        public void Text(string label, string id, Func<string> get, Action<string> set, int width)
        {
            if (label != null)
                Label(label);
            Widgets.Add(new Widget { Kind = WidgetKind.TextField, Bounds = Next(width), FieldId = id, GetText = get, SetText = set });
        }
    }

    /// <summary>Draws widgets in a plain, dark editor style.</summary>
    internal static class WidgetDrawing
    {
        public static readonly Color Bar = new Color(28, 20, 14);
        private static readonly Color ButtonColor = new Color(70, 50, 36);
        private static readonly Color ButtonHover = new Color(100, 72, 50);
        private static readonly Color Muted = new Color(210, 200, 185);

        public static void Draw(SpriteBatch spriteBatch, IEnumerable<Widget> widgets, Vector2 mouse, string focus, float time)
        {
            foreach (Widget widget in widgets)
            {
                bool hover = widget.Enabled && widget.Bounds.Contains(mouse);
                Rectangle b = widget.Bounds;
                float textY = b.Center.Y - Assets.SmallFont.LineSpacing / 2f;
                switch (widget.Kind)
                {
                    case WidgetKind.Label:
                        UiDrawing.DrawText(spriteBatch, Assets.SmallFont, widget.Text, new Vector2(b.X, textY), widget.On ? UiDrawing.Highlight : Color.White);
                        break;

                    case WidgetKind.Button:
                        Color fill = widget.On ? UiDrawing.Highlight : hover ? ButtonHover : ButtonColor;
                        spriteBatch.Draw(Assets.Pixel, b, fill * (widget.Enabled ? 1f : 0.4f));
                        UiDrawing.DrawOutline(spriteBatch, b, Color.Black * 0.6f);
                        Color text = widget.On ? Color.Black : Color.White * (widget.Enabled ? 1f : 0.5f);
                        UiDrawing.DrawCenteredText(spriteBatch, Assets.SmallFont, widget.Text, b.Center.ToVector2(), text);
                        break;

                    case WidgetKind.Toggle:
                        int size = b.Height - 16;
                        var box = new Rectangle(b.X, b.Center.Y - size / 2, size, size);
                        spriteBatch.Draw(Assets.Pixel, box, Color.Black * 0.5f);
                        UiDrawing.DrawOutline(spriteBatch, box, hover ? UiDrawing.Highlight : Muted);
                        if (widget.On)
                        {
                            Rectangle check = box;
                            check.Inflate(-5, -5);
                            spriteBatch.Draw(Assets.Pixel, check, UiDrawing.Highlight);
                        }
                        UiDrawing.DrawText(spriteBatch, Assets.SmallFont, widget.Text, new Vector2(box.Right + 8, textY), hover ? UiDrawing.Highlight : Color.White);
                        break;

                    case WidgetKind.TextField:
                        bool focused = widget.FieldId == focus;
                        spriteBatch.Draw(Assets.Pixel, b, Color.Black * 0.55f);
                        UiDrawing.DrawOutline(spriteBatch, b, focused ? UiDrawing.Highlight : hover ? Color.White : Muted * 0.7f);
                        DrawFieldText(spriteBatch, widget.GetText() ?? "", b, focused, time);
                        break;
                }
            }
        }

        /// <summary>The text, showing its end when it is too long for the box, with a blinking caret while typing.</summary>
        private static void DrawFieldText(SpriteBatch spriteBatch, string text, Rectangle box, bool focused, float time)
        {
            float room = box.Width - 20;
            string shown = text;
            while (shown.Length > 0 && Assets.SmallFont.MeasureString(shown).X > room)
                shown = shown.Substring(1);
            var position = new Vector2(box.X + 8, box.Center.Y - Assets.SmallFont.LineSpacing / 2f);
            UiDrawing.DrawText(spriteBatch, Assets.SmallFont, shown, position, Color.White);
            if (focused && time % 1f < 0.6f)
            {
                float x = position.X + Assets.SmallFont.MeasureString(shown).X + 2;
                spriteBatch.Draw(Assets.Pixel, new Rectangle((int)x, box.Y + 8, 2, box.Height - 16), UiDrawing.Highlight);
            }
        }
    }
}
