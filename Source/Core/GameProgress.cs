using System.Collections.Generic;
using System.Linq;

namespace JumpScape.Core
{
    /// <summary>Which levels the player has finished and which gems they found, saved to progress.json.</summary>
    public sealed class GameProgress
    {
        private const string FileName = "progress.json";

        public int HighestCompletedLevel { get; set; }

        /// <summary>For each level, the numbers of the gems found on finished runs.</summary>
        public Dictionary<int, List<int>> FoundGems { get; set; } = new Dictionary<int, List<int>>();

        public static GameProgress Load() => SaveFile.Load<GameProgress>(FileName);

        public void Save() => SaveFile.Save(FileName, this);

        public IReadOnlyCollection<int> GemsFoundIn(int level)
        {
            return FoundGems.TryGetValue(level, out List<int> gems) ? gems : new List<int>();
        }

        /// <summary>Remembers gems found on a finished run, on top of the ones found before.</summary>
        public void AddGems(int level, IEnumerable<int> gems)
        {
            FoundGems[level] = GemsFoundIn(level).Union(gems).OrderBy(gem => gem).ToList();
        }
    }
}
