using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenShot;

/// <summary>
/// The floating borderless widget window. Draggable by its title area.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;

    public event Action? CaptureRequested;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    // ── Window-level drag ─────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_settings.WindowLeft >= 0 && _settings.WindowTop >= 0)
        {
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    // ── Capture button ─────────────────────────────────────────────────

    private void OnCaptureClick(object sender, RoutedEventArgs e)
    {
        CaptureRequested?.Invoke();
    }

    // ── Visual feedback: brief border flash ───────────────────────────

    public void FlashFeedback()
    {
        var flash = new ColorAnimation
        {
            From = System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xFF),
            To   = System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55),
            Duration = TimeSpan.FromMilliseconds(400),
        };

        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xFF));
        var border = (System.Windows.Controls.Border)Content;
        border.BorderBrush = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
    }
}
