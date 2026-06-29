using System.IO;
using System.Text.Json;

namespace OpenShot;

/// <summary>
/// Application settings persisted as JSON in %APPDATA%/OpenShot/settings.json.
/// </summary>
public sealed class AppSettings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenShot");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    // ── Persisted fields ──────────────────────────────────────────────

    public string SavePath { get; set; } = GetDefaultSavePath();
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public bool AlwaysOnTop { get; set; } = true;
    public bool LaunchAtStartup { get; set; } = false;

    // v0.3.0 — hotkeys
    public int HotkeyCaptureModifiers { get; set; } = 12;  // MOD_WIN | MOD_SHIFT
    public int HotkeyCaptureKey { get; set; } = 0x53;       // VK_S
    public int HotkeyActiveWinModifiers { get; set; } = 12; // MOD_WIN | MOD_SHIFT
    public int HotkeyActiveWinKey { get; set; } = 0x57;     // VK_W

    // v0.3.0 — sound
    public bool PlayCaptureSound { get; set; } = true;

    // v0.4.0 — filename template
    public string FilenameTemplate { get; set; } = "screenshot-{yyyy}-{MM}-{dd}-{HHmmss}";

    // v0.4.0 — history (last 20 file paths)
    public List<string> ScreenshotHistory { get; set; } = new();

    // ── Serialisation ─────────────────────────────────────────────────

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            settings ??= new AppSettings();
            settings.ScreenshotHistory ??= new();

            // Migrate old default path to Desktop
            var picturesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Screenshots",
                "OpenShot");
            if (settings.SavePath.Equals(picturesPath, StringComparison.OrdinalIgnoreCase))
            {
                settings.SavePath = GetDefaultSavePath();
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    public static string GetDefaultSavePath()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrEmpty(desktop))
            return desktop;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenShot",
            "Screenshots");
    }

    public static string EnsureFolder(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
