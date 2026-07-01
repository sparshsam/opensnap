using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows;

namespace OpenSnap;

/// <summary>
/// Checks the GitHub Releases API for a newer version of OpenSnap and
/// notifies the user. Supports one-click background download + install.
/// </summary>
public sealed class UpdateService
{
    private const string RepoApi = "https://api.github.com/repos/sparshsam/opensnap/releases/latest";
    private const string RepoListApi = "https://api.github.com/repos/sparshsam/opensnap/releases";
    private const string RepoDownloadBase = "https://github.com/sparshsam/opensnap/releases/download";
    private static readonly Version CurrentVersion;

    private readonly TrayService _tray;
    private GitHubRelease? _latestRelease;
    private string? _downloadedInstallerPath;

    static UpdateService()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = ver ?? new Version(0, 9, 0);
    }

    /// <summary>Log file for update diagnostics.</summary>
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenSnap", "logs", "update.log");

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch { /* best-effort */ }
    }

    public UpdateService(TrayService tray)
    {
        _tray = tray;
    }

    /// <summary>Latest known tag (for About dialog).</summary>
    public string? LatestTag => _latestRelease?.TagName;

    /// <summary>
    /// Check for a newer release. Runs on a background thread.
    /// </summary>
    public async Task CheckAsync(bool silentIfUpToDate = true)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenSnap/1.0");
            client.Timeout = TimeSpan.FromSeconds(10);

            Log("Checking for updates...");
            Log($"Current version: v{CurrentVersion}");

            // Try the releases/latest endpoint first
            GitHubRelease? release = null;
            try
            {
                release = await client.GetFromJsonAsync<GitHubRelease>(RepoApi);
                if (release?.TagName is not null)
                    Log($"Latest endpoint returned tag: {release.TagName}");
            }
            catch (Exception ex)
            {
                Log($"Latest endpoint failed: {ex.GetType().Name}: {ex.Message}");
            }

            // Fallback: fetch the full releases list and pick the first non-draft
            if (release?.TagName is null)
            {
                Log("Falling back to releases list endpoint...");
                try
                {
                    var allReleases = await client.GetFromJsonAsync<GitHubRelease[]>(RepoListApi);
                    release = allReleases?.FirstOrDefault(r =>
                        !string.IsNullOrEmpty(r.TagName) &&
                        r is { Draft: false, Prerelease: false });
                    if (release?.TagName is not null)
                        Log($"List endpoint returned first non-draft tag: {release.TagName}");
                }
                catch (Exception ex)
                {
                    Log($"List endpoint also failed: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (release is null || release.TagName is null)
            {
                Log("Update check: no release data available");
                if (!silentIfUpToDate)
                    await Notify("Could not check for updates. Check the update log or try again later.");
                return;
            }

            var tag = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tag, out var latest))
            {
                Log($"Update check: failed to parse version from tag '{release.TagName}'");
                if (!silentIfUpToDate)
                    await Notify($"Could not parse latest version from tag '{release.TagName}'.");
                return;
            }

            _latestRelease = release;
            Log($"Remote version: v{latest}, Local version: v{CurrentVersion}");

            if (latest > CurrentVersion)
            {
                Log($"Update available: v{latest}");
                await Notify($"v{latest} available — click to download & install",
                    isUpdate: true);
            }
            else if (latest == CurrentVersion)
            {
                Log("Already up to date");
                if (!silentIfUpToDate)
                    await Notify($"You're up to date — v{CurrentVersion} is the latest version.");
            }
            else
            {
                Log($"Local version v{CurrentVersion} is newer than remote v{latest}");
                if (!silentIfUpToDate)
                    await Notify($"You're running v{CurrentVersion}, which is newer than the latest release (v{latest}).");
            }
        }
        catch (Exception ex)
        {
            Log($"Update check exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            if (!silentIfUpToDate)
                await Notify($"Update check failed: {ex.GetType().Name} — {ex.Message}");
        }
    }

    /// <summary>
    /// Download the latest installer in the background and prompt to install.
    /// </summary>
    public async Task DownloadAndInstallAsync()
    {
        if (_latestRelease is null)
        {
            await Notify("No update available. Check again later.");
            return;
        }

        // Find the installer asset (OpenSnap-Setup-v*.exe)
        var asset = _latestRelease.Assets?
            .FirstOrDefault(a => a.Name is not null &&
                a.Name.StartsWith("OpenSnap-Setup-", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        if (asset?.BrowserDownloadUrl is null)
        {
            await Notify("Installer asset not found on GitHub.");
            return;
        }

        await Notify("Downloading update...");

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenSnap/1.0");
            client.Timeout = TimeSpan.FromMinutes(5);

            var tempDir = Path.Combine(Path.GetTempPath(), "OpenSnapUpdate");
            Directory.CreateDirectory(tempDir);
            var destPath = Path.Combine(tempDir, asset.Name);

            var data = await client.GetByteArrayAsync(asset.BrowserDownloadUrl);
            await File.WriteAllBytesAsync(destPath, data);

            _downloadedInstallerPath = destPath;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _tray.NotifyWithLink("OpenSnap — Update ready",
                    "Click to install the update",
                    "install://" + destPath);
            });
        }
        catch (Exception ex)
        {
            await Notify($"Download failed: {ex.Message}");
        }
    }

    /// <summary>Launch the downloaded installer and exit the app.</summary>
    public void LaunchInstaller()
    {
        if (_downloadedInstallerPath is null || !File.Exists(_downloadedInstallerPath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _downloadedInstallerPath,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER",
                UseShellExecute = true,
            });
        }
        catch { /* best-effort */ }
    }

    // ── Notifications ───────────────────────────────────────────────────

    private Task Notify(string message, bool isUpdate = false)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (isUpdate)
                _tray.NotifyWithLink("OpenSnap — Update", message, "checkupdate://");
            else
                _tray.Notify("OpenSnap — Update", message);
        }).Task;
    }

    // ── GitHub API response model ─────────────────────────────────────

    /// <summary>GitHub API returns snake_case — map explicitly with JsonPropertyName.</summary>
    private sealed class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("draft")]
        public bool Draft { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
