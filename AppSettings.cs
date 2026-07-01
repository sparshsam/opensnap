using System.IO;
using System.Text.Json;

namespace OpenSnap;

/// <summary>
/// Application settings persisted as JSON in %APPDATA%/OpenSnap/settings.json.
/// </summary>
public sealed class AppSettings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenSnap");

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

    // v0.7.0 — opacity (20–100%)
    public double Opacity { get; set; } = 1.0;

    // v0.7.0 — edge snapping
    public bool EdgeSnapEnabled { get; set; } = true;
    public int EdgeSnapThreshold { get; set; } = 10;

    // v0.7.0 — auto-hide when fullscreen app is active
    public bool AutoHideFullscreen { get; set; } = false;

    // v0.8.0 — naming
    public string ProjectPrefix { get; set; } = "";
    public bool UseSequentialNumbering { get; set; } = false;
    public int SequentialCounter { get; set; } = 1;
    public bool DateSubfolders { get; set; } = false;

    // v0.8.0 — custom open-with app
    public string CustomAppPath { get; set; } = "";

    // v0.8.0 — pinned captures (paths)
    public List<string> PinnedCaptures { get; set; } = new();

    // v0.8.0 — show quick-actions popup after capture
    public bool ShowQuickActions { get; set; } = true;

    // v0.9.6 — save only (skip clipboard copy)
    public bool SaveOnly { get; set; } = false;

    // v0.9.5 — accessibility
    public bool LargeIcons { get; set; } = false;

    // v0.9.5 — localization
    public string Language { get; set; } = "en";

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
                "OpenSnap");
            if (settings.SavePath.Equals(picturesPath, StringComparison.OrdinalIgnoreCase))
            {
                settings.SavePath = GetDefaultSavePath();
            }

            // Clamp opacity to prevent near-invisible widget from old settings
            if (settings.Opacity < 0.80)
                settings.Opacity = 0.80;
            if (settings.Opacity > 1.0)
                settings.Opacity = 1.0;

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
            "OpenSnap",
            "Screenshots");
    }

    public static string EnsureFolder(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
