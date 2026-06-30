using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;

namespace OpenSnap;

public partial class AboutDialog : Window
{
    private readonly AppSettings _settings;

    public AboutDialog(AppSettings? settings = null)
    {
        _settings = settings ?? new AppSettings();
        InitializeComponent();

        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = ver != null
            ? $"Version {ver.Major}.{ver.Minor}.{ver.Build}"
            : "Version 0.9.0";

        LoadChangelog();
        LoadDiagnostics();
    }

    // ── Changelog ─────────────────────────────────────────────────────

    private void LoadChangelog()
    {
        try
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            // Load CHANGELOG.md from app directory, then from repo root
            var paths = new[]
            {
                Path.Combine(dir, "CHANGELOG.md"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md"),
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    ChangelogBox.Text = File.ReadAllText(path);
                    return;
                }
            }

            ChangelogBox.Text = "Release notes not found locally.\nSee GitHub for changelog.";
        }
        catch
        {
            ChangelogBox.Text = "Could not load changelog.";
        }
    }

    // ── Diagnostics ───────────────────────────────────────────────────

    private void LoadDiagnostics()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            var ver = Assembly.GetExecutingAssembly().GetName().Version;

            sb.AppendLine("OpenSnap Diagnostics");
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Version:       {ver?.ToString() ?? "?"}");
            sb.AppendLine($"Framework:     {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            sb.AppendLine($"OS:            {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            sb.AppendLine($"Process:       {Environment.ProcessPath}");
            sb.AppendLine($"Working set:   {Environment.WorkingSet / 1024 / 1024} MB");
            sb.AppendLine($"AppData:       {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenSnap")}");
            sb.AppendLine($"Save path:     {_settings.SavePath}");
            sb.AppendLine($"History count: {_settings.ScreenshotHistory.Count}");
            sb.AppendLine($"Pinned count:  {_settings.PinnedCaptures.Count}");
            sb.AppendLine($"Always on top: {_settings.AlwaysOnTop}");
            sb.AppendLine($"Auto-hide:     {_settings.AutoHideFullscreen}");
            sb.AppendLine($"Edge snap:     {_settings.EdgeSnapEnabled}");
            sb.AppendLine($"Opacity:       {_settings.Opacity:P0}");
            sb.AppendLine($"Sound:         {_settings.PlayCaptureSound}");
            sb.AppendLine($"Seq numbering: {_settings.UseSequentialNumbering}");

            // Logs
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenSnap", "logs");
            if (Directory.Exists(logDir))
            {
                var logFiles = Directory.GetFiles(logDir, "*.log");
                sb.AppendLine($"Log files:     {logFiles.Length}");
                if (logFiles.Length > 0)
                {
                    var latest = logFiles.OrderByDescending(File.GetLastWriteTime).First();
                    var size = new FileInfo(latest).Length;
                    sb.AppendLine($"Latest log:    {Path.GetFileName(latest)} ({size / 1024} KB)");
                }
            }
            else
            {
                sb.AppendLine($"Logs:          (none)");
            }

            DiagBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            DiagBox.Text = $"Error loading diagnostics: {ex.Message}";
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────

    private void OnGitHubLink(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true }); }
        catch { }
        e.Handled = true;
    }

    private void OnGitHubReleases(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/sparshsam/opensnap/releases") { UseShellExecute = true }); }
        catch { }
    }

    private void OnCopyDiagnostics(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetText(DiagBox.Text); }
        catch { }
    }

    private void OnExportLogs(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenSnap", "logs");
            if (!Directory.Exists(logDir))
            {
                System.Windows.MessageBox.Show("No logs found.", "Export logs", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"OpenSnap-logs-{DateTime.Now:yyyy-MM-dd-HHmmss}.zip");

            // Simple: copy all log files to a temp dir and open it
            var exportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"OpenSnap-logs-{DateTime.Now:yyyy-MM-dd-HHmmss}");
            Directory.CreateDirectory(exportDir);
            foreach (var f in Directory.GetFiles(logDir, "*.log"))
            {
                File.Copy(f, Path.Combine(exportDir, Path.GetFileName(f)));
            }
            Process.Start("explorer.exe", exportDir);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Export logs", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
