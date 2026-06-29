using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenSnap;

/// <summary>
/// The floating widget — glass capsule. Left-click captures full screen;
/// right-click opens a quick-mode menu; middle-click captures active window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private System.Windows.Point _mouseDownPos;
    private bool _isDragging;
    private const double DragThreshold = 6;
    private DateTime _lastClickTime = DateTime.MinValue;
    private const double DoubleClickThresholdMs = 500;

    public event Action? CaptureRequested;
    public event Action<CaptureMode>? CaptureModeRequested;
    public event Action? SettingsRequested;
    public event Action? MiddleClickRequested;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ClampToVisibleScreen();
    }

    // ── Right-click context menu ──────────────────────────────────────

    private void OnPreviewRightClick(object sender, MouseButtonEventArgs e)
    {
        CaptureMenu.IsOpen = true;
        e.Handled = true;
    }

    private void OnMenuCapture(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not string tag)
            return;

        switch (tag)
        {
            case "FullScreen":
                CaptureRequested?.Invoke();
                break;
            case "ActiveWindow":
                CaptureModeRequested?.Invoke(OpenSnap.CaptureMode.ActiveWindow);
                break;
            case "AreaSelection":
                CaptureModeRequested?.Invoke(OpenSnap.CaptureMode.AreaSelection);
                break;
            case "CaptureOcr":
                CaptureModeRequested?.Invoke(OpenSnap.CaptureMode.CaptureOcr);
                break;
            case "Settings":
                SettingsRequested?.Invoke();
                break;
        }
    }

    // ── Screen clamping ───────────────────────────────────────────────

    private void ClampToVisibleScreen()
    {
        double x = _settings.WindowLeft >= 0 ? _settings.WindowLeft : 100;
        double y = _settings.WindowTop >= 0 ? _settings.WindowTop : 100;

        var workArea = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        const int margin = 40;

        x = Math.Clamp(x, workArea.Left - Width + margin, workArea.Right - margin);
        y = Math.Clamp(y, workArea.Top - margin, workArea.Bottom - margin);

        Left = x;
        Top = y;
    }

    // ── Drag-vs-click (left button) ───────────────────────────────────

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _mouseDownPos = e.GetPosition(this);
            _isDragging = false;
        }
        else if (e.MiddleButton == MouseButtonState.Pressed)
        {
            // Middle-click: capture active window
            e.Handled = true;
            MiddleClickRequested?.Invoke();
        }
    }

    private void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

        var pos = e.GetPosition(this);
        var dx = pos.X - _mouseDownPos.X;
        var dy = pos.Y - _mouseDownPos.Y;

        if (Math.Sqrt(dx * dx + dy * dy) > DragThreshold)
        {
            _isDragging = true;
            DragMove();
        }
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;

        // Suppress second click of a double-click
        var now = DateTime.UtcNow;
        if ((now - _lastClickTime).TotalMilliseconds < DoubleClickThresholdMs)
        {
            _lastClickTime = now;
            return;
        }
        _lastClickTime = now;

        CaptureRequested?.Invoke();
        FlashFeedback();
    }

    // ── Feedback ──────────────────────────────────────────────────────

    private void FlashFeedback()
    {
        var flash = new ColorAnimation
        {
            From = System.Windows.Media.Color.FromRgb(0x66, 0xBB, 0xFF),
            To   = System.Windows.Media.Color.FromRgb(0x30, 0xFF, 0xFF),
            Duration = TimeSpan.FromMilliseconds(300),
        };

        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0xBB, 0xFF));
        RootCapsule.BorderBrush = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
    }
}
