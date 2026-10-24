namespace JumpScape.Core
{
    /// <summary>Which levels the player has finished, saved to progress.json.</summary>
    public sealed class GameProgress
    {
        private const string FileName = "progress.json";

        public int HighestCompletedLevel { get; set; }

        public static GameProgress Load() => SaveFile.Load<GameProgress>(FileName);

        public void Save() => SaveFile.Save(FileName, this);
    }
}
