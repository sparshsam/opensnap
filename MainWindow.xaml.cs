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

    // ── Press-animation state ──────────────────────────────────────────

    private static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan ReleaseDuration = TimeSpan.FromMilliseconds(100);
    private const double PressScale = 0.97;

    private readonly SolidColorBrush? _capsuleBg;
    private readonly DropShadowEffect? _capsuleShadow;
    private readonly Color _defaultBgColor;
    private readonly double _defaultShadowOpacity;

    // ── Events fired to App ─────────────────────────────────────────

    /// <summary>Fired for full-screen capture.</summary>
    public event Action? CaptureRequested;
    /// <summary>Fired for area / active-window / OCR capture.</summary>
    public event Action<CaptureMode>? CaptureModeRequested;
    public event Action? SettingsRequested;
    public event Action? MiddleClickRequested;
    /// <summary>Fired when the user picks Exit from the context menu.</summary>
    public event Action? ExitRequested;
    /// <summary>Fired when the widget finishes a drag (for snapping).</summary>
    public event Action? DragEnded;

    // Section boundaries within the 240 px pill
    private const double AreaSectionEnd = 80;
    private const double FullSectionEnd = 160;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        // Capsule-wide ScaleTransform for press animation (replaces per-section bounce)
        RootCapsule.RenderTransform = new ScaleTransform(1.0, 1.0);
        RootCapsule.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

        // Capture references for press animation
        _capsuleBg = RootCapsule.Background as SolidColorBrush;
        _capsuleShadow = RootCapsule.Effect as DropShadowEffect;
        _defaultBgColor = _capsuleBg?.Color ?? Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF);
        _defaultShadowOpacity = _capsuleShadow?.Opacity ?? 0.25;

        DpiChanged += OnDpiChanged;
    }

    // ── Public API for App.xaml.cs ─────────────────────────────────────

    /// <summary>Reapply visual settings (opacity, etc.) after settings dialog.</summary>
    public void ApplySettings()
    {
        Opacity = _settings.Opacity;
        Topmost = _settings.AlwaysOnTop;
    }

    /// <summary>Hide/show for fullscreen-auto-hide.</summary>
    public void SetWidgetVisible(bool visible)
    {
        if (visible && Visibility != Visibility.Visible)
            Show();
        else if (!visible && Visibility == Visibility.Visible)
            Hide();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ClampToVisibleScreen();
        Opacity = _settings.Opacity;
    }

    private void OnDpiChanged(object? sender, System.Windows.DpiChangedEventArgs e)
    {
        ClampToVisibleScreen();
    }

    /// <summary>Refresh UI strings when language changes.</summary>
    public void ApplyLanguage()
    {
        var t = App.T;
        AreaSection.ToolTip = t.GetString("pill.tooltip.area");
        FullSection.ToolTip = t.GetString("pill.tooltip.full");
        WinSection.ToolTip = t.GetString("pill.tooltip.win");

        // Context menu
        foreach (MenuItem item in CaptureMenu.Items)
        {
            item.Header = item.Tag switch
            {
                "FullScreen"    => t.GetString("menu.fullscreen"),
                "ActiveWindow"  => t.GetString("menu.activewindow"),
                "AreaSelection" => t.GetString("menu.areaselection"),
                "CaptureOcr"    => t.GetString("menu.captureocr"),
                "Settings"      => t.GetString("menu.settings"),
                "Exit"          => t.GetString("menu.exit"),
                _               => item.Header,
            };
        }
    }

    // ── Keyboard navigation ───────────────────────────────────────────

    private int _focusedSection;
    private readonly Border[] _sections = new Border[3];

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Initialize section references on first key press
        if (_sections[0] is null)
        { _sections[0] = AreaSection; _sections[1] = FullSection; _sections[2] = WinSection; }

        switch (e.Key)
        {
            case Key.Left:
                _focusedSection = (_focusedSection - 1 + 3) % 3;
                _sections[_focusedSection].Focus();
                e.Handled = true;
                break;
            case Key.Right:
                _focusedSection = (_focusedSection + 1) % 3;
                _sections[_focusedSection].Focus();
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                ActivateSection(_focusedSection);
                e.Handled = true;
                break;
        }
    }

    private void ActivateSection(int index)
    {
        // Press capsule, then release on next frame
        AnimatePress();
        _ = Dispatcher.BeginInvoke(() => AnimateRelease(),
            System.Windows.Threading.DispatcherPriority.Normal);

        switch (index)
        {
            case 0: ToggleAreaMode(); break;
            case 1: FlashFeedback(); CaptureRequested?.Invoke(); break;
            case 2: FlashFeedback(); CaptureModeRequested?.Invoke(CaptureMode.ActiveWindow); break;
        }
    }

    // ── High Contrast support ─────────────────────────────────────────

    private void ApplyHighContrastIfNeeded()
    {
        if (!SystemParameters.HighContrast) return;
        RootCapsule.Background = System.Windows.SystemColors.WindowBrush;
        RootCapsule.BorderBrush = System.Windows.SystemColors.WindowTextBrush;
    }

    // ── Right-click context menu ──────────────────────────────────────

    private void OnPreviewRightClick(object sender, MouseButtonEventArgs e)
    {
        CaptureMenu.IsOpen = true;
        e.Handled = true;
    }

    private void OnMenuCapture(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not string tag) return;

        switch (tag)
        {
            case "FullScreen":          CaptureRequested?.Invoke(); break;
            case "ActiveWindow":        CaptureModeRequested?.Invoke(CaptureMode.ActiveWindow); break;
            case "AreaSelection":       ToggleAreaMode(); break;
            case "CaptureOcr":          CaptureModeRequested?.Invoke(CaptureMode.CaptureOcr); break;
            case "Settings":            SettingsRequested?.Invoke(); break;
            case "Exit":                ExitRequested?.Invoke(); break;
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

    // ── Edge snapping ─────────────────────────────────────────────────

    /// <summary>After a drag finishes, snap to screen edges if enabled.</summary>
    private void ApplyEdgeSnap()
    {
        if (!_settings.EdgeSnapEnabled) return;

        var wa = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        int t = _settings.EdgeSnapThreshold;

        // Each edge independently
        if (Math.Abs(Left - wa.Left) < t) Left = wa.Left;
        else if (Math.Abs(Left + Width - wa.Right) < t) Left = wa.Right - Width;

        if (Math.Abs(Top - wa.Top) < t) Top = wa.Top;
        else if (Math.Abs(Top + Height - wa.Bottom) < t) Top = wa.Bottom - Height;
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
            AnimatePress();
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
        if (_isDragging)
        {
            _isDragging = false;
            RestoreCapsuleInstant();
            ApplyEdgeSnap();
            DragEnded?.Invoke();
            return;
        }

        // Suppress second click of a double-click
        var now = DateTime.UtcNow;
        if ((now - _lastClickTime).TotalMilliseconds < DoubleClickThresholdMs)
        {
            _lastClickTime = now;
            return;
        }
        _lastClickTime = now;

        // Release animation (smooth return from pressed state)
        AnimateRelease();

        // Dispatch based on section
        var clickPos = e.GetPosition(this);
        switch (GetSectionIndex(clickPos.X))
        {
            case 0: ToggleAreaMode(); break;
            case 1: FlashFeedback(); CaptureRequested?.Invoke(); break;
            case 2: FlashFeedback(); CaptureModeRequested?.Invoke(CaptureMode.ActiveWindow); break;
        }
    }

    // ── Area selection toggle ──────────────────────────────────────────

    private void ToggleAreaMode()
    {
        _areaToggleActive = !_areaToggleActive;
        ApplyAreaVisual();
        if (_areaToggleActive)
            CaptureModeRequested?.Invoke(CaptureMode.AreaSelection);
    }

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
        if (_areaToggleActive) ApplyAreaVisual();
        else { AreaSection.Background = Brushes.Transparent; AreaIconPath.Fill = (Brush)FindResource("IconColor"); }
    }

    private void OnFullMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    { FullSection.Background = (Brush)FindResource("HoverBg"); FullIconPath.Fill = (Brush)FindResource("IconHoverColor"); }

    private void OnFullMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    { FullSection.Background = Brushes.Transparent; FullIconPath.Fill = (Brush)FindResource("IconColor"); }

    private void OnWinMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    { WinSection.Background = (Brush)FindResource("HoverBg"); WinIconPath.Fill = (Brush)FindResource("IconHoverColor"); }

    private void OnWinMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    { WinSection.Background = Brushes.Transparent; WinIconPath.Fill = (Brush)FindResource("IconColor"); }

    // ── Feedback ──────────────────────────────────────────────────────

    /// <summary>
    /// Press animation — scale capsule to 97%, darken background, soften shadow.
    /// </summary>
    private void AnimatePress()
    {
        var scale = (ScaleTransform)RootCapsule.RenderTransform;
        var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        var scaleAnim = new DoubleAnimation(PressScale, PressDuration)
            { EasingFunction = easeOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        // Slightly darken the capsule background (#1A → #15)
        if (_capsuleBg != null)
        {
            var darker = Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF);
            var bgAnim = new ColorAnimation(darker, PressDuration)
                { EasingFunction = easeOut };
            _capsuleBg.BeginAnimation(SolidColorBrush.ColorProperty, bgAnim);
        }

        // Reduce shadow intensity
        if (_capsuleShadow != null)
        {
            var shadowAnim = new DoubleAnimation(0.08, PressDuration)
                { EasingFunction = easeOut };
            _capsuleShadow.BeginAnimation(DropShadowEffect.OpacityProperty, shadowAnim);
        }
    }

    /// <summary>
    /// Release animation — scale back to 100%, restore background and shadow.
    /// </summary>
    private void AnimateRelease()
    {
        var scale = (ScaleTransform)RootCapsule.RenderTransform;
        var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        var scaleAnim = new DoubleAnimation(1.0, ReleaseDuration)
            { EasingFunction = easeOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        if (_capsuleBg != null)
        {
            var bgAnim = new ColorAnimation(_defaultBgColor, ReleaseDuration)
                { EasingFunction = easeOut };
            _capsuleBg.BeginAnimation(SolidColorBrush.ColorProperty, bgAnim);
        }

        if (_capsuleShadow != null)
        {
            var shadowAnim = new DoubleAnimation(_defaultShadowOpacity, ReleaseDuration)
                { EasingFunction = easeOut };
            _capsuleShadow.BeginAnimation(DropShadowEffect.OpacityProperty, shadowAnim);
        }
    }

    /// <summary>Instantly restore capsule visuals (for drag-end).</summary>
    private void RestoreCapsuleInstant()
    {
        var scale = (ScaleTransform)RootCapsule.RenderTransform;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        scale.ScaleX = 1.0;
        scale.ScaleY = 1.0;

        if (_capsuleBg != null)
        {
            _capsuleBg.BeginAnimation(SolidColorBrush.ColorProperty, null);
            _capsuleBg.Color = _defaultBgColor;
        }

        if (_capsuleShadow != null)
        {
            _capsuleShadow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            _capsuleShadow.Opacity = _defaultShadowOpacity;
        }
    }

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
