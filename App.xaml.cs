using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenSnap;

public partial class App : System.Windows.Application
{
    private TrayService? _tray;
    private AppSettings? _settings;
    private MainWindow? _widget;
    private HotkeyService? _hotkeys;
    private UpdateService? _updater;

    // Fullscreen detection timer
    private System.Windows.Threading.DispatcherTimer? _fullscreenTimer;
    private bool _isWidgetHiddenByFullscreen;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Redirect unhandled exceptions to log file
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Current.DispatcherUnhandledException += OnDispatcherException;

        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _settings = AppSettings.Load();

        if (_settings.LaunchAtStartup != StartupManager.IsRegisteredForStartup())
            StartupManager.SetStartup(_settings.LaunchAtStartup);

        _widget = new MainWindow(_settings);
        _widget.ApplySettings();
        _widget.Show();

        _tray = new TrayService();
        _tray.CaptureRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.ChangeFolderRequested += OnChangeFolder;
        _tray.StartupToggleRequested += OnStartupToggle;
        _tray.OpenLastScreenshotRequested += OnOpenLastScreenshot;
        _tray.CopyFilePathRequested += OnCopyFilePath;
        _tray.RevealInExplorerRequested += OnRevealInExplorer;
        _tray.OpenHistoryItemRequested += OnOpenHistoryItem;
        _tray.QuitRequested += OnQuit;
        _tray.SetStartupChecked(_settings.LaunchAtStartup);
        _tray.UpdateHistory(_settings.ScreenshotHistory);
        _tray.SetHistoryActionsEnabled(_settings.ScreenshotHistory.Count > 0);
        _tray.Show();

        _widget.CaptureRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _widget.CaptureModeRequested += DispatchCapture;
        _widget.SettingsRequested += OnOpenSettings;
        _widget.MiddleClickRequested += () => DispatchCapture(CaptureMode.ActiveWindow);
        _widget.ExitRequested += OnQuit;

        // Global hotkeys
        _hotkeys = new HotkeyService(_widget);
        _hotkeys.CaptureModifiers = (uint)_settings.HotkeyCaptureModifiers;
        _hotkeys.CaptureKey = (uint)_settings.HotkeyCaptureKey;
        _hotkeys.ActiveWindowModifiers = (uint)_settings.HotkeyActiveWinModifiers;
        _hotkeys.ActiveWindowKey = (uint)_settings.HotkeyActiveWinKey;
        _hotkeys.CaptureFullScreenRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _hotkeys.CaptureActiveWindowRequested += () => DispatchCapture(CaptureMode.ActiveWindow);
        _hotkeys.Register();

        // Background update check (silent if up-to-date).
        _updater = new UpdateService(_tray);
        _ = _updater.CheckAsync();

        // Auto-hide when fullscreen apps are active
        StartFullscreenMonitor();

        // Periodic tray health check (handles Explorer restart)
        StartTrayHealthCheck();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
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
            BitmapSource? source = null;

            // For ActiveWindow, temporarily move the widget off-screen
            // so GetForegroundWindow() returns the window behind it.
            // We move rather than hide so any in-progress animations
            // (bounce, flash) survive the capture cycle.
            if (mode == CaptureMode.ActiveWindow)
            {
                var restoreLeft = _widget!.Left;
                var restoreTop  = _widget.Top;
                var restoreTopmost = _widget.Topmost;

                _widget.Topmost = false;
                _widget.Left = -32000;
                _widget.Top = -32000;
                await Task.Delay(50);
                source = CaptureService.CaptureActiveWindow();
                _widget.Topmost = restoreTopmost;
                _widget.Left = restoreLeft;
                _widget.Top = restoreTop;
            }
            else
            {
                source = mode switch
                {
                    CaptureMode.AreaSelection => await CaptureAreaAsync(),
                    _ => ScreenshotService.CaptureDesktop(),
                };
            }

            // Deactivate area-selection toggle after the overlay closes
            if (mode == CaptureMode.AreaSelection)
                _widget?.ResetAreaToggle();

            if (source == null) return;

            var folder = AppSettings.EnsureFolder(_settings!.SavePath);
            var fileName = ScreenshotService.GenerateFileName(_settings.FilenameTemplate);
            var fullPath = Path.Combine(folder, fileName);

            ScreenshotService.SaveAsPng(source, fullPath);
            ScreenshotService.CopyToClipboard(source);

            // OCR mode: extract text and copy to clipboard too
            string ocrSuffix = "";
            if (mode == CaptureMode.CaptureOcr)
            {
                var (ocrText, ocrCopied) = await OcrService.CaptureOcrAsync(source);
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    ocrSuffix = ocrCopied ? "  + OCR copied" : "  + OCR done";
                }
            }

            // Play capture sound
            if (_settings.PlayCaptureSound)
                PlayShutterSound();

            // Update history
            _settings.ScreenshotHistory.Add(fullPath);
            if (_settings.ScreenshotHistory.Count > 20)
                _settings.ScreenshotHistory.RemoveAt(0);
            _settings.Save();

            _tray?.UpdateHistory(_settings.ScreenshotHistory);
            _tray?.SetHistoryActionsEnabled(true);
            _tray?.Notify("OpenSnap", $"Saved  \u2022  {fileName}{ocrSuffix}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Screenshot failed:\n{ex.Message}",
                "OpenSnap — Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private static Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
    {
        var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();
        var overlay = new AreaSelectorWindow();
        overlay.SelectionCompleted = rect =>
        {
            // Convert window-relative coords to screen-absolute coords
            // (the overlay spans the virtual desktop starting at
            //  VirtualScreenLeft/Top, and GetPosition() is
            //  window-content-relative).
            var source = CaptureService.CaptureArea(
                (int)(overlay.Left + rect.X),
                (int)(overlay.Top + rect.Y),
                (int)rect.Width, (int)rect.Height);
            tcs.TrySetResult(source);
        };
        overlay.Closed += (_, _) => tcs.TrySetResult(null);
        overlay.Show();
        return tcs.Task;
    }

    // ── Fullscreen auto-hide monitor ───────────────────────────────────

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    private void StartFullscreenMonitor()
    {
        _fullscreenTimer = new System.Windows.Threading.DispatcherTimer(
            TimeSpan.FromMilliseconds(800), System.Windows.Threading.DispatcherPriority.Background,
            OnFullscreenTick, Dispatcher);
    }

    private void OnFullscreenTick(object? sender, EventArgs e)
    {
        if (_widget == null || !_settings!.AutoHideFullscreen) return;

        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return;

        // Check if the foreground window IS our widget — never hide ourselves
        var widgetHwnd = new System.Windows.Interop.WindowInteropHelper(_widget).Handle;
        if (hWnd == widgetHwnd) return;

        if (!GetWindowRect(hWnd, out var rect)) return;

        int w = rect.Right - rect.Left;
        int h = rect.Bottom - rect.Top;

        // If the window spans the full virtual screen, it's fullscreen
        var virtW = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Width;
        var virtH = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height;

        bool isFullscreen = (w >= virtW && h >= virtH);

        if (isFullscreen && !_isWidgetHiddenByFullscreen)
        {
            _widget.SetWidgetVisible(false);
            _isWidgetHiddenByFullscreen = true;
        }
        else if (!isFullscreen && _isWidgetHiddenByFullscreen)
        {
            _widget.SetWidgetVisible(true);
            _isWidgetHiddenByFullscreen = false;
        }
    }

    // ── Tray health check (Explorer restart recovery) ──────────────────

    private void StartTrayHealthCheck()
    {
        var timer = new System.Windows.Threading.DispatcherTimer(
            TimeSpan.FromSeconds(5), System.Windows.Threading.DispatcherPriority.Background,
            (_, _) =>
            {
                if (_tray != null && !_tray.IsVisible)
                {
                    try { _tray.Show(); }
                    catch { /* best-effort recovery */ }
                }
            }, Dispatcher);
    }

    // ── Exception logging ───────────────────────────────────────────────

    private static void LogException(string context, Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenSnap", "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"error-{DateTime.Now:yyyy-MM-dd}.log");
            var msg = $"[{DateTime.Now:HH:mm:ss}] {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n";
            File.AppendAllText(path, msg);
        }
        catch { /* best-effort */ }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogException("Unhandled", ex);
    }

    private static void OnDispatcherException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher", e.Exception);
        e.Handled = true;
    }

    // ── Shutter sound ────────────────────────────────────────────────

    private void PlayShutterSound()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("OpenSnap.Resources.capture.wav");
            if (stream is not null)
            {
                using var player = new SoundPlayer(stream);
                player.Play();
            }
        }
        catch { /* best-effort */ }
    }

    // ── History actions ──────────────────────────────────────────────

    private void OnOpenLastScreenshot()
    {
        if (_settings == null || _settings.ScreenshotHistory.Count == 0) return;
        var path = _settings.ScreenshotHistory[^1];
        if (File.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
    }

    private void OnCopyFilePath()
    {
        if (_settings == null || _settings.ScreenshotHistory.Count == 0) return;
        var path = _settings.ScreenshotHistory[^1];
        try { System.Windows.Clipboard.SetText(path); }
        catch { /* best-effort */ }
    }

    private void OnRevealInExplorer()
    {
        if (_settings == null || _settings.ScreenshotHistory.Count == 0) return;
        ScreenshotService.RevealInExplorer(_settings.ScreenshotHistory[^1]);
    }

    private void OnOpenHistoryItem(int index)
    {
        if (_settings == null || index < 0 || index >= _settings.ScreenshotHistory.Count) return;
        var path = _settings.ScreenshotHistory[index];
        if (File.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
    }

    // ── Settings window ───────────────────────────────────────────────

    private void OnOpenSettings()
    {
        if (_settings == null) return;
        var win = new SettingsWindow(_settings);
        win.Owner = _widget;
        win.VisualSettingsChanged += () => _widget?.ApplySettings();
        win.ShowDialog();

        _widget?.ApplySettings();

        // Re-register hotkeys if settings changed
        _hotkeys?.Unregister();
        if (_hotkeys != null)
        {
            _hotkeys.CaptureModifiers = (uint)_settings.HotkeyCaptureModifiers;
            _hotkeys.CaptureKey = (uint)_settings.HotkeyCaptureKey;
            _hotkeys.ActiveWindowModifiers = (uint)_settings.HotkeyActiveWinModifiers;
            _hotkeys.ActiveWindowKey = (uint)_settings.HotkeyActiveWinKey;
            _hotkeys.Register();
        }
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
                _tray?.Notify("OpenSnap", $"Save folder changed to {dialog.SelectedPath}");
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
