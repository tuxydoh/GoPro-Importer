using System.Text.Json;

namespace GoProDownloader;

internal sealed class AppSettings
{
    private const int MaxRecentFolders = 5;
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GoProImporter");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    public string GoProUrl { get; set; } = "http://10.5.5.9/videos/DCIM/100GOPRO/";
    public List<string> RecentFolders { get; set; } = new();

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            settings.RecentFolders = settings.RecentFolders.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxRecentFolders).ToList();
            return settings;
        }
        catch { return new AppSettings(); }
    }

    public void AddRecentFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;
        folder = folder.Trim();
        RecentFolders.RemoveAll(p => string.Equals(p, folder, StringComparison.OrdinalIgnoreCase));
        RecentFolders.Insert(0, folder);
        if (RecentFolders.Count > MaxRecentFolders) RecentFolders.RemoveRange(MaxRecentFolders, RecentFolders.Count - MaxRecentFolders);
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, options));
    }
}
