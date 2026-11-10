using System.Text.Json;
using SudokuGame.Engine;

namespace SudokuGame;

/// <summary>Preferences kept between runs, stored as JSON in the user's AppData folder.</summary>
internal sealed class AppSettings
{
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SudokuGame",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
            if (settings != null && Enum.IsDefined(typeof(Difficulty), settings.Difficulty))
            {
                return settings;
            }
        }
        catch (Exception)
        {
            // A missing or unreadable file just means defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch (Exception)
        {
            // Losing a preference is not worth interrupting the player.
        }
    }
}
