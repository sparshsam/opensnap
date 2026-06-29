using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenShot;

/// <summary>
/// The floating widget — glass capsule. Click anywhere to capture;
/// drag anywhere to move. No rectangular backing — only the pill renders.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private System.Windows.Point _mouseDownPos;
    private bool _isDragging;
    private const double DragThreshold = 6;

    public event Action? CaptureRequested;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ClampToVisibleScreen();
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

    // ── Drag-vs-click ────────────────────────────────────────────────

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _mouseDownPos = e.GetPosition(this);
        _isDragging = false;
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
        if (!_isDragging)
        {
            CaptureRequested?.Invoke();
            FlashFeedback();
        }
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
