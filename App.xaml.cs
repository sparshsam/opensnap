using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenSnap;

public partial class App : System.Windows.Application
{
    /// <summary>Singleton localization service, accessible from all windows.</summary>
    public static LocalizationService T { get; private set; } = new("en");

    private TrayService? _tray;
    private AppSettings? _settings;
    private MainWindow? _widget;
    private HotkeyService? _hotkeys;
    private UpdateService? _updater;
    private SettingsWindow? _settingsWindow;

    // Single-instance mutex
    private Mutex? _instanceMutex;

    // Fullscreen detection timer
    private System.Windows.Threading.DispatcherTimer? _fullscreenTimer;
    private bool _isWidgetHiddenByFullscreen;

    // External foreground window tracking (for active-window capture)
    private IntPtr _lastExternalHwnd;
    private System.Windows.Threading.DispatcherTimer? _foregroundTracker;

    protected override void OnStartup(StartupEventArgs e)
    {
        var _startupTimer = Stopwatch.StartNew();
        base.OnStartup(e);

        // Enforce single instance
        _instanceMutex = new Mutex(true, "OpenSnap-Instance-Mutex", out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running — bring it to the foreground
            var existingHwnd = FindWindow(null, "OpenSnap");
            if (existingHwnd != IntPtr.Zero)
            {
                SetForegroundWindow(existingHwnd);
                ShowWindow(existingHwnd, SW_RESTORE);
                FlashWindow(existingHwnd, true);
            }
            Current.Shutdown();
            return;
        }

        // Redirect unhandled exceptions to log file
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Current.DispatcherUnhandledException += OnDispatcherException;

        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _settings = AppSettings.Load();

        // Initialize localization from saved preference
        T = LocalizationService.FromCode(_settings.Language);
        T.LanguageChanged += OnLanguageChanged;

        if (_settings.LaunchAtStartup != StartupManager.IsRegisteredForStartup())
            StartupManager.SetStartup(_settings.LaunchAtStartup);

        _widget = new MainWindow(_settings);
        _widget.ApplySettings();
        _widget.Show();

        _tray = new TrayService();
        _tray.CaptureRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _tray.AreaCaptureRequested += () => DispatchCapture(CaptureMode.AreaSelection);
        _tray.ActiveWindowRequested += () => DispatchCapture(CaptureMode.ActiveWindow);
        _tray.CaptureOcrRequested += () => DispatchCapture(CaptureMode.CaptureOcr);
        _tray.OpenFolderRequested += OnOpenFolder;
        _tray.StartupToggleRequested += OnStartupToggle;
        _tray.OpenLastScreenshotRequested += OnOpenLastScreenshot;
        _tray.CopyFilePathRequested += OnCopyFilePath;
        _tray.RevealInExplorerRequested += OnRevealInExplorer;
        _tray.OpenHistoryItemRequested += OnOpenHistoryItem;
        _tray.PinHistoryItemRequested += OnPinHistoryItem;
        _tray.DeleteHistoryItemRequested += OnDeleteHistoryItem;
        _tray.ClearHistoryRequested += OnClearHistory;
        _tray.SearchHistoryRequested += OnSearchHistory;
        _tray.SettingsRequested += OnOpenSettings;
        _tray.CheckUpdateRequested += OnCheckUpdate;
        _tray.UpdateNotificationClicked += OnUpdateNotificationClicked;
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

        // Track external foreground window for active-window capture
        StartForegroundTracker();

        // Periodic tray health check (handles Explorer restart)
        StartTrayHealthCheck();

        _startupTimer.Stop();
        BenchmarkLog("startup", _startupTimer.ElapsedMilliseconds);
        BenchmarkLog("memory_startup", GC.GetTotalMemory(false) / 1024, "KB");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();

        // Release the single-instance mutex
        try { _instanceMutex?.ReleaseMutex(); _instanceMutex?.Dispose(); }
        catch { /* best-effort */ }

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
        var _captureTimer = Stopwatch.StartNew();
        try
        {
            BitmapSource? source = null;

            // For ActiveWindow, temporarily move the widget off-screen
            // so GetForegroundWindow() returns the window behind it.
            // We move rather than hide so any in-progress animations
            // (bounce, flash) survive the capture cycle.
            if (mode == CaptureMode.ActiveWindow)
            {
                // Use pre-tracked external foreground window
                var targetHwnd = _lastExternalHwnd;

                if (targetHwnd == IntPtr.Zero)
                {
                    source = CaptureService.CaptureFullScreen();
                }
                else
                {
                    // Try DWM extended bounds first
                    var dwmBounds = GetDwmBoundsForCapture(targetHwnd);
                    if (dwmBounds.HasValue)
                    {
                        var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
                        var clamped = System.Drawing.Rectangle.Intersect(
                            new System.Drawing.Rectangle(vs.Left, vs.Top, vs.Width, vs.Height),
                            dwmBounds.Value);
                        if (clamped.Width > 0 && clamped.Height > 0)
                            source = CaptureService.CaptureArea(clamped.Left, clamped.Top, clamped.Width, clamped.Height);
                        else
                            source = CaptureService.CaptureFullScreen();
                    }
                    else
                    {
                        // Fallback to GetWindowRect
                        if (CaptureService.GetWindowRect(targetHwnd, out int awLeft, out int awTop, out int awWidth, out int awHeight))
                        {
                            var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
                            var clamped = System.Drawing.Rectangle.Intersect(
                                new System.Drawing.Rectangle(vs.Left, vs.Top, vs.Width, vs.Height),
                                new System.Drawing.Rectangle(awLeft, awTop, awWidth, awHeight));
                            if (clamped.Width > 0 && clamped.Height > 0)
                                source = CaptureService.CaptureArea(clamped.Left, clamped.Top, clamped.Width, clamped.Height);
                            else
                                source = CaptureService.CaptureFullScreen();
                        }
                        else
                        {
                            source = CaptureService.CaptureFullScreen();
                        }
                    }
                }
            }
            else
            {
                source = mode switch
                {
                    CaptureMode.AreaSelection => await CaptureAreaAsync(),
                    _ => CaptureService.CaptureFullScreen(),
                };
            }

            // Deactivate area-selection toggle after the overlay closes
            if (mode == CaptureMode.AreaSelection)
                _widget?.ResetAreaToggle();

            if (source == null) return;

            // Advanced naming: resolve path + filename
            var baseFolder = AppSettings.EnsureFolder(_settings!.SavePath);
            var saveFolder = ScreenshotService.ResolveSavePath(baseFolder, _settings);
            Directory.CreateDirectory(saveFolder);

            int seq = _settings.UseSequentialNumbering ? _settings.SequentialCounter++ : 0;
            var fileName = ScreenshotService.GenerateFileName(
                _settings.FilenameTemplate, _settings.ProjectPrefix, seq);
            var fullPath = Path.Combine(saveFolder, fileName);

            ScreenshotService.SaveAsPng(source, fullPath);
            if (!_settings.SaveOnly)
                ScreenshotService.CopyToClipboard(source);

            // OCR mode: extract text
            string ocrText = "";
            if (mode == CaptureMode.CaptureOcr)
            {
                (ocrText, _) = await OcrService.CaptureOcrAsync(source);
            }

            // Play capture sound
            if (_settings.PlayCaptureSound)
                PlayShutterSound();

            // Update history
            _settings.ScreenshotHistory.Add(fullPath);
            if (_settings.ScreenshotHistory.Count > 20)
                _settings.ScreenshotHistory.RemoveAt(0);
            _settings.Save();

            _tray?.UpdateHistory(_settings.ScreenshotHistory, _settings.PinnedCaptures);
            _tray?.SetHistoryActionsEnabled(true);

            // Show quick-actions popup
            if (_settings.ShowQuickActions)
            {
                var popup = new CapturePopup(fullPath, ocrText, _settings);
                popup.Show();
            }
            else
            {
                _tray?.Notify("OpenSnap", $"Saved  \u2022  {fileName}");
            }

            _captureTimer.Stop();
            BenchmarkLog($"capture_{mode}", _captureTimer.ElapsedMilliseconds, fileName);
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

    private static async Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
    {
        // Pre-capture full desktop before showing overlay.
        // This preserves the exact visible window z-order.
        var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
        int vsLeft = vs.Left, vsTop = vs.Top, vsWidth = vs.Width, vsHeight = vs.Height;

        BitmapSource fullDesktop;
        try
        {
            fullDesktop = CaptureService.CaptureArea(vsLeft, vsTop, vsWidth, vsHeight);
        }
        catch
        {
            return null;
        }

        var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();
        var overlay = new AreaSelectorWindow();

        overlay.SelectionCompleted = rect =>
        {
            try
            {
                // Get DPI scale from overlay
                var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(overlay);
                double scaleX = dpi.DpiScaleX;
                double scaleY = dpi.DpiScaleY;

                // Convert DIP coordinates to physical pixels
                int dipX = (int)(overlay.Left + rect.X);
                int dipY = (int)(overlay.Top + rect.Y);
                int dipW = (int)rect.Width;
                int dipH = (int)rect.Height;

                int physX = (int)(dipX * scaleX);
                int physY = (int)(dipY * scaleY);
                int physW = (int)(dipW * scaleX);
                int physH = (int)(dipH * scaleY);

                if (physW > 5 && physH > 5)
                {
                    // Clamp to virtual screen bounds
                    physX = Math.Max(vsLeft, Math.Min(physX, vsLeft + vsWidth - 1));
                    physY = Math.Max(vsTop, Math.Min(physY, vsTop + vsHeight - 1));
                    physW = Math.Min(physW, vsLeft + vsWidth - physX);
                    physH = Math.Min(physH, vsTop + vsHeight - physY);

                    // Crop from pre-captured desktop bitmap
                    var cropped = new System.Windows.Media.Imaging.CroppedBitmap(
                        fullDesktop,
                        new System.Windows.Int32Rect(
                            physX - vsLeft, physY - vsTop,
                            physW, physH));

                    tcs.TrySetResult(cropped);
                }
                else
                {
                    tcs.TrySetResult(null);
                }

                overlay.Close();
            }
            catch (Exception ex)
            {
                LogException("CaptureArea", ex);
                // Fallback: direct capture
                try
                {
                    var fallback = CaptureService.CaptureArea(
                        (int)((overlay.Left + rect.X) * 1.0),
                        (int)((overlay.Top + rect.Y) * 1.0),
                        (int)rect.Width, (int)rect.Height);
                    tcs.TrySetResult(fallback);
                }
                catch
                {
                    overlay.Close();
                    tcs.TrySetResult(null);
                }
            }
        };

        overlay.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.TrySetResult(null);
        };

        overlay.Show();
        return await tcs.Task;
    }

    // ── Fullscreen auto-hide monitor ───────────────────────────────────

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bInvert);

    private const int SW_RESTORE = 9;

    // ── Benchmark logger ───────────────────────────────────────────────

    private static readonly string BenchmarkPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenSnap", "logs", "benchmark.csv");

    private static void BenchmarkLog(string label, long elapsedMs, string? detail = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(BenchmarkPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            bool header = !File.Exists(BenchmarkPath);
            using var w = new StreamWriter(BenchmarkPath, append: true);
            if (header)
                w.WriteLine("timestamp,label,elapsed_ms,detail");
            w.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{label},{elapsedMs},{detail ?? ""}");
        }
        catch { /* best-effort */ }
    }

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

    // ── External foreground window tracker ────────────────────────────

    /// <summary>
    /// Every 250ms, poll GetForegroundWindow and save it if it's not
    /// owned by OpenSnap. This gives us the last valid external window
    /// for active-window capture without having to guess after our UI
    /// has taken focus.
    /// </summary>
    private void StartForegroundTracker()
    {
        _foregroundTracker = new System.Windows.Threading.DispatcherTimer(
            TimeSpan.FromMilliseconds(250),
            System.Windows.Threading.DispatcherPriority.Background,
            (_, _) =>
            {
                try
                {
                    var hWnd = GetForegroundWindow();
                    if (hWnd != IntPtr.Zero && !CaptureService.IsOwnWindow(hWnd))
                    {
                        _lastExternalHwnd = hWnd;
                    }
                }
                catch { /* best-effort */ }
            }, Dispatcher);
    }

    // ── DWM extended frame bounds helper ─────────────────────────────

    /// <summary>Get DWM extended frame bounds for a window, or null.</summary>
    private static System.Drawing.Rectangle? GetDwmBoundsForCapture(IntPtr hWnd)
    {
        try
        {
            var rect = new RECT();
            int hr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS,
                ref rect, Marshal.SizeOf<RECT>());
            if (hr == 0 && rect.Right > rect.Left && rect.Bottom > rect.Top)
                return new System.Drawing.Rectangle(
                    rect.Left, rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top);
        }
        catch { }
        return null;
    }

    // DWM constants/imports (used by both foreground tracker and capture)
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
        ref RECT pvAttribute, int cbAttribute);

    // ── Exception logging ───────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        _settings!.Language = T.CurrentCode;
        _settings.Save();
        // Refresh UI strings on main windows
        _widget?.ApplyLanguage();
    }

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

    private void OnPinHistoryItem(int index)
    {
        if (_settings == null || index < 0 || index >= _settings.ScreenshotHistory.Count) return;
        var path = _settings.ScreenshotHistory[index];
        if (!_settings.PinnedCaptures.Contains(path))
        {
            _settings.PinnedCaptures.Add(path);
            _settings.Save();
            _tray?.UpdateHistory(_settings.ScreenshotHistory, _settings.PinnedCaptures);
        }
    }

    private void OnDeleteHistoryItem(int index)
    {
        if (_settings == null || index < 0 || index >= _settings.ScreenshotHistory.Count) return;
        _settings.ScreenshotHistory.RemoveAt(index);
        _settings.Save();
        _tray?.UpdateHistory(_settings.ScreenshotHistory, _settings.PinnedCaptures);
        _tray?.SetHistoryActionsEnabled(_settings.ScreenshotHistory.Count > 0);
    }

    private void OnClearHistory()
    {
        if (_settings == null) return;
        _settings.ScreenshotHistory.Clear();
        _settings.Save();
        _tray?.UpdateHistory(_settings.ScreenshotHistory, _settings.PinnedCaptures);
        _tray?.SetHistoryActionsEnabled(false);
    }

    private void OnSearchHistory()
    {
        var dialog = new SearchHistoryDialog(
            _settings?.ScreenshotHistory ?? new(),
            _settings?.PinnedCaptures);
        dialog.FileSelected += path =>
        {
            try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\""); }
            catch { }
        };
        dialog.Owner = _widget;
        dialog.ShowDialog();
    }

    private async void OnCheckUpdate()
    {
        if (_updater != null)
            await _updater.CheckAsync(silentIfUpToDate: false);
    }

    private async void OnUpdateNotificationClicked()
    {
        if (_updater != null)
            await _updater.DownloadAndInstallAsync();
    }

    // ── Settings window ───────────────────────────────────────────────

    private void OnOpenSettings()
    {
        if (_settings == null) return;

        try
        {
            // If the settings window is already open, activate it
            if (_settingsWindow != null)
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                    _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
                _settingsWindow.Focus();
                return;
            }

            _settingsWindow = new SettingsWindow(_settings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.VisualSettingsChanged += () => _widget?.ApplySettings();
            _settingsWindow.Show();

            // Re-register hotkeys when settings window closes
            _settingsWindow.Closed += (_, _) =>
            {
                _hotkeys?.Unregister();
                if (_hotkeys != null)
                {
                    _hotkeys.CaptureModifiers = (uint)_settings.HotkeyCaptureModifiers;
                    _hotkeys.CaptureKey = (uint)_settings.HotkeyCaptureKey;
                    _hotkeys.ActiveWindowModifiers = (uint)_settings.HotkeyActiveWinModifiers;
                    _hotkeys.ActiveWindowKey = (uint)_settings.HotkeyActiveWinKey;
                    _hotkeys.Register();
                }
                _widget?.ApplySettings();
            };
        }
        catch (Exception ex)
        {
            LogException("OpenSettings", ex);
            System.Windows.MessageBox.Show(
                $"Failed to open Settings:\n{ex.Message}",
                "OpenSnap — Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
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
