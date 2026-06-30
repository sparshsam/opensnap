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

            var release = await client.GetFromJsonAsync<GitHubRelease>(RepoApi);
            if (release is null || release.TagName is null)
            {
                if (!silentIfUpToDate) await Notify("Could not check for updates.");
                return;
            }

            var tag = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tag, out var latest))
            {
                if (!silentIfUpToDate) await Notify("Could not parse latest version.");
                return;
            }

            _latestRelease = release;

            if (latest > CurrentVersion)
            {
                await Notify($"v{latest} available — click to download & install",
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

    private sealed class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}
