using System;
using Microsoft.Xna.Framework;

namespace JumpScape.Core
{
    /// <summary>
    /// Levels and menus are designed for a 1920x1080 screen, and that area is always fully visible.
    /// The game is scaled to fit the window, and when the window has a different shape the visible
    /// area grows wider or taller to fill it, so there are never black bars.
    /// </summary>
    public static class VirtualScreen
    {
        /// <summary>Width of the area every level and menu is designed for.</summary>
        public const int Width = 1920;

        /// <summary>Height of the area every level and menu is designed for.</summary>
        public const int Height = 1080;

        /// <summary>Width of the visible area in game units: 1920 or more.</summary>
        public static float ViewWidth { get; private set; } = Width;

        /// <summary>Height of the visible area in game units: 1080 or more.</summary>
        public static float ViewHeight { get; private set; } = Height;

        /// <summary>Window pixels per game unit.</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>Scales game units to window pixels.</summary>
        public static Matrix Transform => Matrix.CreateScale(Scale, Scale, 1f);

        /// <summary>Like <see cref="Transform"/>, but puts the 1920x1080 design area in the middle of the window. Used by menus.</summary>
        public static Matrix CenteredTransform => Matrix.CreateTranslation(CenterOffset.X, CenterOffset.Y, 0f) * Transform;

        /// <summary>The whole visible area, in the coordinates used by <see cref="CenteredTransform"/>.</summary>
        public static Rectangle CenteredViewBounds => new Rectangle(
            (int)MathF.Floor(-CenterOffset.X), (int)MathF.Floor(-CenterOffset.Y),
            (int)MathF.Ceiling(ViewWidth) + 1, (int)MathF.Ceiling(ViewHeight) + 1);

        private static Vector2 CenterOffset => new Vector2((ViewWidth - Width) / 2f, (ViewHeight - Height) / 2f);

        public static void Resize(int windowWidth, int windowHeight)
        {
            if (windowWidth <= 0 || windowHeight <= 0)
                return;

            Scale = Math.Min((float)windowWidth / Width, (float)windowHeight / Height);
            ViewWidth = windowWidth / Scale;
            ViewHeight = windowHeight / Scale;
        }

        /// <summary>Converts a point in window pixels (e.g. the mouse) to the coordinates used by <see cref="CenteredTransform"/>.</summary>
        public static Vector2 ToCentered(Point windowPoint) => ToView(windowPoint) - CenterOffset;

        /// <summary>Converts a point in window pixels to view coordinates (the ones used by <see cref="Transform"/>).</summary>
        public static Vector2 ToView(Point windowPoint) => windowPoint.ToVector2() / Scale;
    }
}
