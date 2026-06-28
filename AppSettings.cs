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

    // ── Serialisation ─────────────────────────────────────────────────

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
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

    /// <summary>
    /// Default save path: C:\Users\&lt;user&gt;\Desktop
    /// </summary>
    public static string GetDefaultSavePath()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrEmpty(desktop))
            return desktop;

        // Last-resort fallback
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenShot",
            "Screenshots");
    }

    /// <summary>Ensures the configured save folder exists, creating it if needed.</summary>
    public static string EnsureFolder(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
