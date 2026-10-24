using System;
using System.Text.Json.Serialization;

namespace JumpScape.Core
{
    /// <summary>Player settings, saved to settings.json. Loaded once and shared through <see cref="Current"/>.</summary>
    public sealed class GameSettings
    {
        public static readonly int[] FrameRateOptions = { 30, 60 };

        private const string FileName = "settings.json";

        public static GameSettings Current { get; } = SaveFile.Load<GameSettings>(FileName).Validated();

        /// <summary>Sound effect volume, 0-100.</summary>
        public int Volume { get; set; } = 80;

        /// <summary>Music volume, 0-100.</summary>
        public int MusicVolume { get; set; } = 10;

        public bool IsMuted { get; set; }

        /// <summary>Maximum frames drawn per second; one of <see cref="FrameRateOptions"/>.</summary>
        public int FrameRateLimit { get; set; } = 60;

        public bool IsFullscreen { get; set; } = true;

        /// <summary>Show a see-through ghost replaying your best run of the level.</summary>
        public bool ShowRaceGhost { get; set; } = true;

        /// <summary>Sound effect volume as a 0-1 multiplier. Mute is applied globally, not here.</summary>
        [JsonIgnore]
        public float SoundVolume => Volume / 100f;

        public void Save() => SaveFile.Save(FileName, this);

        private GameSettings Validated()
        {
            Volume = Math.Clamp(Volume, 0, 100);
            MusicVolume = Math.Clamp(MusicVolume, 0, 100);
            if (Array.IndexOf(FrameRateOptions, FrameRateLimit) < 0)
                FrameRateLimit = 60;
            return this;
        }
    }
}
