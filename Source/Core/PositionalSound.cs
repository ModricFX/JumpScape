using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace JumpScape.Core
{
    /// <summary>Makes a sound quieter the further it is from the player and pans it left or right.</summary>
    public static class PositionalSound
    {
        private const float MinDistance = 20f;

        public static void Apply(SoundEffectInstance sound, Vector2 source, Vector2 listener, float maxDistance, float maxVolume)
        {
            float distance = MathHelper.Clamp(Vector2.Distance(source, listener), MinDistance, maxDistance);
            float closeness = 1f - (distance - MinDistance) / (maxDistance - MinDistance);

            sound.Volume = closeness * maxVolume * GameSettings.Current.SoundVolume;
            sound.Pan = MathHelper.Clamp((source.X - listener.X) / (maxDistance * 0.5f), -1f, 1f);
        }
    }
}
