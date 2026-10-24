using System;
using System.Collections.Generic;
using System.IO;
using JumpScape.Core;
using Microsoft.Xna.Framework;

namespace JumpScape.Gameplay
{
    /// <summary>
    /// Where the player was on every frame of a run through a level. The fastest run of each level is saved,
    /// and replayed as a see-through ghost to race against.
    /// </summary>
    public sealed class RunRecording
    {
        private const uint FileMarker = 0x4E52534A; // "JSRN"

        public readonly record struct Frame(Vector2 Position, int Area, bool FacingRight);

        private readonly List<Frame> _frames = new List<Frame>();

        public IReadOnlyList<Frame> Frames => _frames;

        /// <summary>How long the run took, in seconds.</summary>
        public float Time { get; private set; }

        public void Add(Vector2 position, int area, bool facingRight)
        {
            _frames.Add(new Frame(position, area, facingRight));
        }

        private static string PathFor(int level) => Path.Combine(SaveFile.Folder, $"best-run-level{level}.bin");

        /// <summary>The saved best run, or null if there is none or it was made on a different version of the level.</summary>
        public static RunRecording LoadBest(int level, uint levelHash)
        {
            try
            {
                using var reader = OpenBest(level, levelHash, out float time, out int count);
                if (reader == null)
                    return null;

                var run = new RunRecording { Time = time };
                for (int i = 0; i < count; i++)
                    run.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()), reader.ReadByte(), reader.ReadBoolean());
                return run;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>Just the time of the saved best run, without reading the whole recording.</summary>
        public static float? LoadBestTime(int level, uint levelHash)
        {
            try
            {
                using var reader = OpenBest(level, levelHash, out float time, out _);
                return reader == null ? null : time;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static BinaryReader OpenBest(int level, uint levelHash, out float time, out int count)
        {
            time = 0f;
            count = 0;
            string path = PathFor(level);
            if (!File.Exists(path))
                return null;

            var reader = new BinaryReader(File.OpenRead(path));
            if (reader.BaseStream.Length < 16 || reader.ReadUInt32() != FileMarker || reader.ReadUInt32() != levelHash)
            {
                reader.Dispose();
                return null;
            }

            time = reader.ReadSingle();
            count = reader.ReadInt32();
            return reader;
        }

        public void SaveAsBest(int level, uint levelHash, float time)
        {
            Time = time;
            try
            {
                Directory.CreateDirectory(SaveFile.Folder);
                using var writer = new BinaryWriter(File.Create(PathFor(level)));
                writer.Write(FileMarker);
                writer.Write(levelHash);
                writer.Write(time);
                writer.Write(_frames.Count);
                foreach (Frame frame in _frames)
                {
                    writer.Write(frame.Position.X);
                    writer.Write(frame.Position.Y);
                    writer.Write((byte)frame.Area);
                    writer.Write(frame.FacingRight);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Not being able to save a best run should never crash the game.
            }
        }
    }
}
