using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenShot;

/// <summary>
/// The floating borderless widget window. Draggable, always-on-top pill.
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
        ClampToVisibleScreen();
    }

    /// <summary>
    /// Move the widget back on-screen if the saved position is off-screen
    /// (e.g., monitor was disconnected or resolution changed).
    /// </summary>
    private void ClampToVisibleScreen()
    {
        double x = _settings.WindowLeft >= 0 ? _settings.WindowLeft : 100;
        double y = _settings.WindowTop >= 0 ? _settings.WindowTop : 100;

        // Work area of the primary screen (excludes taskbar)
        var workArea = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;

        // Clamp so at least 40px of the widget is visible
        const int margin = 40;
        x = Math.Clamp(x, workArea.Left - Width + margin, workArea.Right - margin);
        y = Math.Clamp(y, workArea.Top - margin, workArea.Bottom - margin);

        Left = x;
        Top = y;
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

    // ── Toast feedback ────────────────────────────────────────────────

    /// <summary>
    /// Show a brief toast on the widget with the saved filename.
    /// Auto-hides after ~2 seconds.
    /// </summary>
    public void ShowCaptureToast(string fileName)
    {
        FeedbackLabel.Text = $"Saved  \u2022  {fileName}";
        FadeToast(1, 0.15);

        var timer = new System.Timers.Timer(2200) { AutoReset = false };
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

    /// <summary>Brief border flash on capture.</summary>
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
