using System.Text.Json;
using System;
using System.IO;

public class GameSettings
{
    public int Volume { get; set; } = 80;
    public int FrameRateIndex { get; set; } = 1;
    public bool IsFullscreen { get; set; } = false;

    public static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");


    // Save settings to file
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    // Load settings from file
    public static GameSettings Load()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
        }
        return new GameSettings(); // Return default settings if no file exists
    }
}
