using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace OpenSnap;

/// <summary>
/// The floating widget — glass capsule divided into three clickable sections:
///   Left (toggle)   → Area selection
///   Center (default)→ Full screen capture
///   Right           → Active window capture
/// Drag moves the widget; right-click opens the mode context menu.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private System.Windows.Point _mouseDownPos;
    private bool _isDragging;
    private const double DragThreshold = 6;
    private DateTime _lastClickTime = DateTime.MinValue;
    private const double DoubleClickThresholdMs = 500;

    // Toggle state for the area-selection left button
    private bool _areaToggleActive;

    // ── Events fired to App ─────────────────────────────────────────

    /// <summary>Fired for full-screen capture (center button, or menu).</summary>
    public event Action? CaptureRequested;

    /// <summary>Fired for area / active-window / OCR capture (menu or buttons).</summary>
    public event Action<CaptureMode>? CaptureModeRequested;

    public event Action? SettingsRequested;
    public event Action? MiddleClickRequested;

    /// <summary>Fired when the user picks Exit from the context menu.</summary>
    public event Action? ExitRequested;

    // Section boundaries within the 240 px pill
    private const double AreaSectionEnd = 80;
    private const double FullSectionEnd = 160;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        // Pre-assign ScaleTransforms so BounceSection can animate them
        // immediately without a null-to-instance transition.
        AreaSection.RenderTransform = new ScaleTransform(1.0, 1.0);
        FullSection.RenderTransform = new ScaleTransform(1.0, 1.0);
        WinSection.RenderTransform  = new ScaleTransform(1.0, 1.0);

        // React to per-monitor DPI changes (PerMonitorV2 manifest).
        // WPF auto-scales content; we re-clamp window position.
        DpiChanged += OnDpiChanged;
    }

    private void OnDpiChanged(object? sender, System.Windows.DpiChangedEventArgs e)
    {
        ClampToVisibleScreen();
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
                CaptureModeRequested?.Invoke(CaptureMode.ActiveWindow);
                break;
            case "AreaSelection":
                ToggleAreaMode();
                break;
            case "CaptureOcr":
                CaptureModeRequested?.Invoke(CaptureMode.CaptureOcr);
                break;
            case "Settings":
                SettingsRequested?.Invoke();
                break;
            case "Exit":
                ExitRequested?.Invoke();
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

    // ── Which section was hit? ────────────────────────────────────────

    private int GetSectionIndex(double x) =>
        x < AreaSectionEnd ? 0 :
        x < FullSectionEnd ? 1 : 2;

    // ── Drag-vs-click (preview events) ────────────────────────────────

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _mouseDownPos = e.GetPosition(this);
            _isDragging = false;
        }
        else if (e.MiddleButton == MouseButtonState.Pressed)
        {
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

        // Dispatch based on section — play visual feedback FIRST
        // so the animation renders before capture blocks the UI thread.
        var clickPos = e.GetPosition(this);
        switch (GetSectionIndex(clickPos.X))
        {
            case 0:
                BounceSection(AreaSection);
                ToggleAreaMode();
                break;
            case 1:
                BounceSection(FullSection);
                FlashFeedback();
                CaptureRequested?.Invoke();
                break;
            case 2:
                BounceSection(WinSection);
                FlashFeedback();
                CaptureModeRequested?.Invoke(CaptureMode.ActiveWindow);
                break;
        }
    }

    // ── Area selection toggle ──────────────────────────────────────────

    /// <summary>Toggles the area-selection mode on/off and fires the capture event.</summary>
    private void ToggleAreaMode()
    {
        _areaToggleActive = !_areaToggleActive;
        ApplyAreaVisual();

        if (_areaToggleActive)
            CaptureModeRequested?.Invoke(CaptureMode.AreaSelection);
    }

    /// <summary>
    /// Called by <see cref="App"/> after the area-selection overlay
    /// completes or is cancelled, to deactivate the toggle state.
    /// </summary>
    public void ResetAreaToggle()
    {
        if (!_areaToggleActive) return;
        _areaToggleActive = false;
        ApplyAreaVisual();
    }

    private void ApplyAreaVisual()
    {
        if (_areaToggleActive)
        {
            AreaSection.Background = (Brush)FindResource("ToggleActiveBg");
            AreaSection.BorderBrush = (Brush)FindResource("ToggleActiveBorder");
            AreaSection.BorderThickness = new Thickness(0, 0, 1, 0);
            AreaIconPath.Fill = (Brush)FindResource("IconToggleColor");
        }
        else
        {
            AreaSection.Background = Brushes.Transparent;
            AreaSection.BorderBrush = null;
            AreaSection.BorderThickness = new Thickness(0);
            AreaIconPath.Fill = (Brush)FindResource("IconColor");
        }
    }

    // ── Hover effects per section ─────────────────────────────────────

    private void OnAreaMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_areaToggleActive)
            AreaSection.Background = (Brush)FindResource("HoverBg");
        AreaIconPath.Fill = (Brush)FindResource("IconHoverColor");
    }

    private void OnAreaMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_areaToggleActive)
            ApplyAreaVisual();
        else
        {
            AreaSection.Background = Brushes.Transparent;
            AreaIconPath.Fill = (Brush)FindResource("IconColor");
        }
    }

    private void OnFullMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        FullSection.Background = (Brush)FindResource("HoverBg");
        FullIconPath.Fill = (Brush)FindResource("IconHoverColor");
    }

    private void OnFullMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        FullSection.Background = Brushes.Transparent;
        FullIconPath.Fill = (Brush)FindResource("IconColor");
    }

    private void OnWinMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        WinSection.Background = (Brush)FindResource("HoverBg");
        WinIconPath.Fill = (Brush)FindResource("IconHoverColor");
    }

    private void OnWinMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        WinSection.Background = Brushes.Transparent;
        WinIconPath.Fill = (Brush)FindResource("IconColor");
    }

    // ── Feedback ──────────────────────────────────────────────────────

    /// <summary>
    /// Spring-back bounce on the pressed section — scales down then
    /// overshoots past 1.0 with a BackEase curve before settling.
    /// Animates the ScaleTransform <b>instance</b> so the transform
    /// is scoped to the individual section, not the parent capsule.
    /// </summary>
    private void BounceSection(FrameworkElement element)
    {
        // Ensure a local ScaleTransform exists for this element
        if (element.RenderTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform(1.0, 1.0);
            element.RenderTransform = scale;
        }

        // Animate the ScaleTransform instance directly (not the element)
        // so ScaleXProperty resolves on the correct dependency object.
        var bounce = new DoubleAnimation
        {
            From = 0.85,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut },
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
    }

    /// <summary>Brief green border flash around the capsule after a capture.</summary>
    private void FlashFeedback()
    {
        var flash = new ColorAnimation
        {
            From = Color.FromRgb(0x00, 0x7A, 0x3F),
            To   = Color.FromRgb(0x00, 0xCC, 0x66),
            Duration = TimeSpan.FromMilliseconds(300),
        };

        var brush = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0x3F));
        RootCapsule.BorderBrush = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
    }
}
