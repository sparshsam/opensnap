using System.IO;
using System.Text.Json;

namespace OpenSnap;

/// <summary>
/// Lightweight localization service. Loads JSON translation files from
/// Resources/Lang/{code}.json and provides string lookups via GetString().
/// Falls back to English for any missing key.
/// </summary>
public sealed class LocalizationService
{
    private static readonly string LangDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Resources", "Lang");

    private readonly Dictionary<string, string> _strings = new();
    private string _currentCode;

    /// <summary>Supported language codes.</summary>
    public static readonly (string Code, string Name)[] Languages =
    [
        ("en", "English"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("es", "Español"),
        ("ja", "日本語"),
    ];

    public string CurrentCode => _currentCode;

    /// <summary>Fired when the language changes so windows can refresh.</summary>
    public event Action? LanguageChanged;

    public LocalizationService(string languageCode = "en")
    {
        _currentCode = languageCode;
        LoadLanguage(languageCode);
    }

    public void SetLanguage(string code)
    {
        if (code == _currentCode) return;
        _currentCode = code;
        LoadLanguage(code);
        LanguageChanged?.Invoke();
    }

    private void LoadLanguage(string code)
    {
        _strings.Clear();

        // Always load English as base (fallback)
        LoadFile("en");

        // Load the target language on top (overrides)
        if (code != "en")
            LoadFile(code);
    }

    private void LoadFile(string code)
    {
        try
        {
            // Try app directory first (published), then repo source
            var paths = new[]
            {
                Path.Combine(LangDir, $"{code}.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Lang", $"{code}.json"),
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is null) continue;
                foreach (var (key, value) in dict)
                    _strings[key] = value;
                return;
            }
        }
        catch { /* best-effort — fall through to empty */ }
    }

    /// <summary>Get a localized string. Falls back to key if not found.</summary>
    public string GetString(string key) =>
        _strings.TryGetValue(key, out var value) ? value : key;

    /// <summary>Get a localized string with format arguments.</summary>
    public string Format(string key, params object[] args)
    {
        var fmt = GetString(key);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }

    // ── Loader helper for App.xaml.cs ─────────────────────────────────

    /// <summary>Create from a persisted language code (validates + falls back).</summary>
    public static LocalizationService FromCode(string? code)
    {
        if (code is not null && Languages.Any(l => l.Code == code))
            return new LocalizationService(code);
        return new LocalizationService("en");
    }
}
