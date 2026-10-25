using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JumpScape.Gameplay;

namespace JumpScape.Editor
{
    /// <summary>
    /// The level being edited: the same classes the level file is read into, so saving writes back exactly what is
    /// shown. Undo keeps whole copies of the level as JSON text.
    /// </summary>
    internal sealed class LevelDocument
    {
        private const int UndoLimit = 200;

        private readonly Stack<string> _undo = new Stack<string>();
        private readonly Stack<string> _redo = new Stack<string>();

        private LevelDocument(int number, LevelFile file)
        {
            Number = number;
            File = file;
        }

        /// <summary>Folders to save into. When null, see <see cref="SaveFolders"/>. The tests point this somewhere else.</summary>
        public static IReadOnlyList<string> SaveFoldersOverride { get; set; }

        public int Number { get; }

        public LevelFile File { get; private set; }

        /// <summary>True when there are changes that haven't been saved.</summary>
        public bool HasChanges { get; private set; }

        public bool CanUndo => _undo.Count > 0;

        public bool CanRedo => _redo.Count > 0;

        public string FileName => $"Level{Number}.json";

        public static LevelDocument Load(int number)
        {
            string json = System.IO.File.ReadAllText(Path.Combine(LevelData.LevelsFolder, $"Level{number}.json"));
            return new LevelDocument(number, LevelData.ParseFile(number, json));
        }

        /// <summary>A new level: the ground, the start on the left and an exit door on the right.</summary>
        public static LevelDocument CreateNew(int number)
        {
            var file = new LevelFile
            {
                Schema = "level.schema.json",
                Name = "New level",
                Description = "",
                Spawn = new PointJson { X = 40, Y = LevelData.GroundY - 100 },
                Areas = new List<AreaJson> { NewArea("main", null) },
            };
            file.Areas[0].Doors.Add(new DoorJson { X = 1700, Y = LevelData.GroundY - 135 });
            return new LevelDocument(number, file) { HasChanges = true };
        }

        /// <summary>An empty area with just the ground.</summary>
        public static AreaJson NewArea(string name, string sky)
        {
            var area = new AreaJson { Name = name, Sky = sky };
            area.Platforms.Add(new PlatformJson { X = -50, Y = LevelData.GroundY, Length = LevelData.GroundLength });
            return area;
        }

        public string ToJson() => LevelWriter.Write(File);

        /// <summary>Checks the level and turns it into something the game can play. Throws <see cref="InvalidDataException"/>.</summary>
        public LevelData Build() => LevelData.FromJson(Number, ToJson());

        /// <summary>Call before every change, so it can be undone.</summary>
        public void RememberForUndo()
        {
            _undo.Push(ToJson());
            if (_undo.Count > UndoLimit)
            {
                // Stacks can't drop their oldest item, so rebuild without it.
                var kept = _undo.Take(UndoLimit).Reverse().ToList();
                _undo.Clear();
                foreach (string json in kept)
                    _undo.Push(json);
            }
            _redo.Clear();
            HasChanges = true;
        }

        public bool Undo() => Swap(_undo, _redo);

        public bool Redo() => Swap(_redo, _undo);

        private bool Swap(Stack<string> from, Stack<string> to)
        {
            if (from.Count == 0)
                return false;
            to.Push(ToJson());
            File = LevelData.ParseFile(Number, from.Pop());
            HasChanges = true;
            return true;
        }

        /// <summary>
        /// Saves the level next to the game, so it can be played at once, and also into the project's Levels folder
        /// when the game runs from its source code, so the file ends up in the project. Returns the folders used.
        /// </summary>
        public IReadOnlyList<string> Save()
        {
            string json = ToJson();
            IReadOnlyList<string> folders = SaveFoldersOverride ?? SaveFolders();
            foreach (string folder in folders)
            {
                Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(Path.Combine(folder, FileName), json);
            }
            HasChanges = false;
            return folders;
        }

        /// <summary>The game's Levels folder, plus the project's when there is a JumpScape.csproj above the game.</summary>
        public static IReadOnlyList<string> SaveFolders()
        {
            var folders = new List<string> { LevelData.LevelsFolder };
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && directory != null; i++, directory = directory.Parent)
            {
                string project = Path.Combine(directory.FullName, "Levels");
                if (System.IO.File.Exists(Path.Combine(directory.FullName, "JumpScape.csproj")) && Directory.Exists(project))
                {
                    if (!string.Equals(Path.GetFullPath(project), Path.GetFullPath(LevelData.LevelsFolder), StringComparison.OrdinalIgnoreCase))
                        folders.Add(project);
                    break;
                }
            }
            return folders;
        }
    }
}
