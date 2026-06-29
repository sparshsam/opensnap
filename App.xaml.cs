using System.IO;
using System.Media;
using System.Windows;

namespace OpenSnap;

public partial class App : System.Windows.Application
{
    private TrayService? _tray;
    private AppSettings? _settings;
    private MainWindow? _widget;
    private HotkeyService? _hotkeys;

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

        // Global hotkeys
        _hotkeys = new HotkeyService(_widget);
        _hotkeys.CaptureModifiers = (uint)_settings.HotkeyCaptureModifiers;
        _hotkeys.CaptureKey = (uint)_settings.HotkeyCaptureKey;
        _hotkeys.ActiveWindowModifiers = (uint)_settings.HotkeyActiveWinModifiers;
        _hotkeys.ActiveWindowKey = (uint)_settings.HotkeyActiveWinKey;
        _hotkeys.CaptureFullScreenRequested += () => DispatchCapture(CaptureMode.FullScreen);
        _hotkeys.CaptureActiveWindowRequested += () => DispatchCapture(CaptureMode.ActiveWindow);
        _hotkeys.Register();
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
            var source = mode switch
            {
                CaptureMode.ActiveWindow => CaptureService.CaptureActiveWindow(),
                CaptureMode.AreaSelection => await CaptureAreaAsync(),
                _ => ScreenshotService.CaptureDesktop(),
            };

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
            var source = CaptureService.CaptureArea(
                (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            tcs.TrySetResult(source);
        };
        overlay.Closed += (_, _) => tcs.TrySetResult(null);
        overlay.Show();
        return tcs.Task;
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
        win.ShowDialog();

        if (_widget != null)
            _widget.Topmost = _settings.AlwaysOnTop;

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
