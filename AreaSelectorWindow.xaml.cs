using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace OpenSnap;

/// <summary>
/// Full-screen transparent overlay for dragging a rectangular selection.
/// Returns the selected screen region via the callback.
/// </summary>
public partial class AreaSelectorWindow : Window
{
    private System.Windows.Point _start;
    private bool _isSelecting;

    /// <summary>Called with screen-coordinate rect when selection completes.</summary>
    public Action<System.Windows.Rect>? SelectionCompleted { get; set; }

    public AreaSelectorWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Cover the entire virtual desktop (including monitors to the
        // left or above the primary display).
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectionCompleted = null; // cancel
            Close();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _start = e.GetPosition(this);
        _isSelecting = true;
        SelectionRect.Visibility = Visibility.Visible;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isSelecting) return;

        var pos = e.GetPosition(this);
        var x = Math.Min(_start.X, pos.X);
        var y = Math.Min(_start.Y, pos.Y);
        var w = Math.Abs(pos.X - _start.X);
        var h = Math.Abs(pos.Y - _start.Y);

        System.Windows.Controls.Canvas.SetLeft(SelectionRect, x);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;

        var pos = e.GetPosition(this);
        var x = (int)Math.Min(_start.X, pos.X);
        var y = (int)Math.Min(_start.Y, pos.Y);
        var w = (int)Math.Abs(pos.X - _start.X);
        var h = (int)Math.Abs(pos.Y - _start.Y);

        // Ignore tiny clicks
        if (w < 10 || h < 10)
        {
            Close();
            return;
        }

        var callback = SelectionCompleted;
        SelectionCompleted = null;
        // Invoke callback BEFORE Close() so the Closed event doesn't
        // race ahead and null out the TaskCompletionSource first.
        callback?.Invoke(new System.Windows.Rect(x, y, w, h));
        Close();
    }
}
