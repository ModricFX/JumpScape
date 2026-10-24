using System;
using System.IO;
using System.Text.Json;

namespace JumpScape.Core
{
    /// <summary>
    /// Reads and writes small JSON files in the user's app data folder
    /// (%APPDATA%\JumpScape on Windows, ~/.config/JumpScape on macOS and Linux).
    /// </summary>
    internal static class SaveFile
    {
        private static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JumpScape");

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        public static T Load<T>(string fileName) where T : new()
        {
            try
            {
                string path = Path.Combine(Folder, fileName);
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? new T();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            {
                // A missing or broken save file just means we start with the defaults.
            }
            return new T();
        }

        public static void Save<T>(string fileName, T data)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, fileName), JsonSerializer.Serialize(data, Options));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Not being able to save settings should never crash the game.
            }
        }
    }
}
