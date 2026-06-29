using System.IO;
using System.Windows;

namespace OpenShot;

/// <summary>
/// Application entry point. Owns settings, screenshot logic, tray lifecycle,
/// startup registration, and the new capture-mode dispatch.
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

        _settings = AppSettings.Load();

        if (_settings.LaunchAtStartup != StartupManager.IsRegisteredForStartup())
            StartupManager.SetStartup(_settings.LaunchAtStartup);

        _widget = new MainWindow(_settings);
        _widget.Topmost = _settings.AlwaysOnTop;
        _widget.Show();

        _tray = new TrayService();
        _tray.CaptureRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.ChangeFolderRequested += OnChangeFolder;
        _tray.StartupToggleRequested += OnStartupToggle;
        _tray.QuitRequested += OnQuit;
        _tray.SetStartupChecked(_settings.LaunchAtStartup);
        _tray.Show();

        // Widget left-click = full screen
        _widget.CaptureRequested += () => DispatchCapture(CaptureMode.FullScreen);
        // Widget right-click menu
        _widget.CaptureModeRequested += DispatchCapture;
        _widget.SettingsRequested += OnOpenSettings;
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

    // ── Capture dispatch ─────────────────────────────────────────────

    private async void DispatchCapture(CaptureMode mode)
    {
        try
        {
            var source = mode switch
            {
                CaptureMode.ActiveWindow => CaptureService.CaptureActiveWindow(),
                CaptureMode.AreaSelection => await CaptureAreaAsync(),
                _ => ScreenshotService.CaptureDesktop(),
            };

            if (source == null) return; // user cancelled area selection

            var folder = AppSettings.EnsureFolder(_settings!.SavePath);
            var fileName = ScreenshotService.GenerateFileName();
            var fullPath = Path.Combine(folder, fileName);
            ScreenshotService.SaveAsPng(source, fullPath);
            ScreenshotService.CopyToClipboard(source);

            _tray?.Notify("OpenShot", $"Saved  \u2022  {fileName}");
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

    /// <summary>Opens the area-selection overlay. Returns null if cancelled.</summary>
    private static Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
    {
        var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();

        var overlay = new AreaSelectorWindow();
        overlay.SelectionCompleted = rect =>
        {
            var source = CaptureService.CaptureArea(
                (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            tcs.TrySetResult(source);
        };

        overlay.Closed += (_, _) =>
        {
            tcs.TrySetResult(null);
        };

        overlay.Show();
        return tcs.Task;
    }

    // ── Settings window ───────────────────────────────────────────────

    private void OnOpenSettings()
    {
        if (_settings == null) return;
        var win = new SettingsWindow(_settings);
        win.Owner = _widget;
        win.ShowDialog();

        // Re-read settings after dialog closes
        if (_widget != null)
            _widget.Topmost = _settings.AlwaysOnTop;
    }

    // ── Shared handlers ───────────────────────────────────────────────

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
