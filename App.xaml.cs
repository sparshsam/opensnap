using System.IO;
using System.Windows;

namespace OpenShot;

/// <summary>
/// Application entry point. Owns settings, screenshot logic, tray lifecycle,
/// and startup registration.
/// </summary>
public partial class App : System.Windows.Application
{
    private TrayService? _tray;
    private AppSettings? _settings;
    private MainWindow? _widget;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Load persisted settings
        _settings = AppSettings.Load();

        // Sync startup registry to actual state (in case user manually deleted the key)
        if (_settings.LaunchAtStartup != StartupManager.IsRegisteredForStartup())
        {
            StartupManager.SetStartup(_settings.LaunchAtStartup);
        }

        // Create the floating widget window
        _widget = new MainWindow(_settings);
        _widget.Topmost = _settings.AlwaysOnTop;
        _widget.Show();

        // Tray icon
        _tray = new TrayService();
        _tray.CaptureRequested += OnCapture;
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.ChangeFolderRequested += OnChangeFolder;
        _tray.StartupToggleRequested += OnStartupToggle;
        _tray.QuitRequested += OnQuit;
        _tray.SetStartupChecked(_settings.LaunchAtStartup);
        _tray.Show();

        // Wire widget capture to same handler
        if (_widget != null)
            _widget.CaptureRequested += OnCapture;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_widget != null && _settings != null)
        {
            _settings.WindowLeft = _widget.Left;
            _settings.WindowTop = _widget.Top;
            _settings.AlwaysOnTop = _widget.Topmost;
            _settings.Save();
        }

        _tray?.Dispose();
        base.OnExit(e);
    }

    // ── Event handlers ────────────────────────────────────────────────

    private void OnCapture()
    {
        try
        {
            var bitmap = ScreenshotService.CaptureDesktop();

            // Save to disk
            var folder = AppSettings.EnsureFolder(_settings!.SavePath);
            var fileName = ScreenshotService.GenerateFileName();
            var fullPath = Path.Combine(folder, fileName);
            ScreenshotService.SaveAsPng(bitmap, fullPath);

            // Copy to clipboard (with retries)
            var clipboardOk = ScreenshotService.CopyToClipboard(bitmap);

            // Visual feedback
            _tray?.Notify("OpenShot", $"Saved  \u2022  {fileName}");
            _widget?.FlashFeedback();
            _widget?.ShowCaptureToast(fileName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Screenshot failed:\n{ex.Message}",
                "OpenShot — Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnOpenFolder()
    {
        if (_settings != null)
            ScreenshotService.OpenFolder(_settings.SavePath);
    }

    private void OnChangeFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select screenshot save folder",
            SelectedPath = _settings?.SavePath ?? AppSettings.GetDefaultSavePath(),
            ShowNewFolderButton = true,
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (_settings != null)
            {
                _settings.SavePath = dialog.SelectedPath;
                _settings.Save();
                _tray?.Notify("OpenShot", $"Save folder changed to {dialog.SelectedPath}");
            }
        }
    }

    private void OnStartupToggle(bool enabled)
    {
        StartupManager.SetStartup(enabled);
        if (_settings != null)
        {
            _settings.LaunchAtStartup = enabled;
            _settings.Save();
        }
    }

    private void OnQuit()
    {
        _tray?.Hide();
        _widget?.Close();
        Current.Shutdown();
    }
}
