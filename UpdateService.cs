using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows;

namespace OpenSnap;

/// <summary>
/// Checks the GitHub Releases API for a newer version of OpenSnap and
/// notifies the user via a system-tray balloon.
/// </summary>
public sealed class UpdateService
{
    private const string RepoApi = "https://api.github.com/repos/sparshsam/opensnap/releases/latest";
    private static readonly Version CurrentVersion;

    private readonly TrayService _tray;
    private string? _latestHtmlUrl;

    static UpdateService()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = ver ?? new Version(0, 7, 0);
    }

    public UpdateService(TrayService tray)
    {
        _tray = tray;
    }

    /// <summary>
    /// Check for a newer release. Runs on a background thread; UI updates
    /// are marshalled to the Dispatcher.
    /// </summary>
    public async Task CheckAsync(bool silentIfUpToDate = true)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenSnap/1.0");

            var release = await client.GetFromJsonAsync<GitHubRelease>(RepoApi);
            if (release is null || release.TagName is null)
            {
                if (!silentIfUpToDate) await Notify("Could not check for updates.");
                return;
            }

            // Parse tag like "v0.7.0" → 0.7.0
            var tag = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tag, out var latest))
            {
                if (!silentIfUpToDate) await Notify("Could not parse latest version.");
                return;
            }

            _latestHtmlUrl = release.HtmlUrl;

            if (latest > CurrentVersion)
            {
                await Notify(
                    $"v{latest} is available!  Click to download.",
                    isUpdate: true);
            }
            else if (!silentIfUpToDate)
            {
                await Notify($"v{CurrentVersion} is the latest version.");
            }
        }
        catch
        {
            if (!silentIfUpToDate)
                await Notify("Update check failed (no internet?).");
        }
    }

    private Task Notify(string message, bool isUpdate = false)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (isUpdate && _latestHtmlUrl is not null)
                _tray.NotifyWithLink("OpenSnap — Update", message, _latestHtmlUrl);
            else
                _tray.Notify("OpenSnap — Update", message);
        }).Task;
    }

    // ── GitHub API response model ─────────────────────────────────────

#pragma warning disable CS0649
    private sealed class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
    }
#pragma warning restore CS0649
}
