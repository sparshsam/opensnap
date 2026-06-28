using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenShot;

/// <summary>
/// The floating borderless widget window — compact pill, always-on-top, draggable.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private const double CaptureZoneWidth = 32;  // matches XAML

    public event Action? CaptureRequested;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    // ── Window-level drag + click ─────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ClampToVisibleScreen();
    }

    /// <summary>Clamp position to the primary screen's working area.</summary>
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

    /// <summary>
    /// Distinguish capture-click (right side) from drag (everywhere else).
    /// </summary>
    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(this);

        if (pos.X >= Width - CaptureZoneWidth)
        {
            // Camera zone — capture
            e.Handled = true;
            CaptureRequested?.Invoke();
        }
        else
        {
            // Drag zone — start window drag
            DragMove();
        }
    }

    // ── Toast feedback ────────────────────────────────────────────────

    /// <summary>Show a brief filename toast, then auto-hide.</summary>
    public void ShowCaptureToast(string fileName)
    {
        FeedbackLabel.Text = $"\u2713  {fileName}";
        FadeToast(1, 0.12);

        var timer = new System.Timers.Timer(2000) { AutoReset = false };
        timer.Elapsed += (_, _) =>
        {
            Dispatcher.InvokeAsync(() => FadeToast(0, 0.15));
            timer.Dispose();
        };
        timer.Start();
    }

    private void FadeToast(double targetOpacity, double durationSeconds)
    {
        var anim = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = TimeSpan.FromSeconds(durationSeconds),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
        };
        FeedbackLabel.BeginAnimation(OpacityProperty, anim);
    }

    /// <summary>Brief border flash as secondary capture signal.</summary>
    public void FlashFeedback()
    {
        var flash = new ColorAnimation
        {
            From = System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xFF),
            To   = System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55),
            Duration = TimeSpan.FromMilliseconds(400),
        };

        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xFF));
        PillBorder.BorderBrush = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
    }
}
