using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenSnap;

/// <summary>
/// Capture modes available from the widget.
/// </summary>
public enum CaptureMode
{
    FullScreen,
    ActiveWindow,
    AreaSelection,
    CaptureOcr,
}

/// <summary>
/// Extends <see cref="ScreenshotService"/> with active-window and
/// area-selection capture. All methods return a <see cref="BitmapSource"/>
/// ready for save + clipboard.
/// </summary>
public static class CaptureService
{
    // ── Ex-style constants ─────────────────────────────────────────────

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // Window classes to skip for active-window capture
    private static readonly HashSet<string> IgnoredWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tooltip",
        "tooltips_class32",
        "tooltips_class32_tooltip",
        "NotifyIconOverflowWindow",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Windows.UI.Core.CoreWindow",
        "ApplicationFrameWindow",       // empty UWP frame
        "TaskListThumbnailWnd",         // taskbar thumbnail
    };

    /// <summary>
    /// Capture the currently focused (foreground) window only.
    /// Filters out tooltips, overlays, hidden windows, and non-target
    /// shell windows. Falls back to full desktop capture when no valid
    /// window is found.
    /// </summary>
    public static BitmapSource CaptureActiveWindow()
    {
        var hWnd = FindValidForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return ScreenshotService.CaptureDesktop();

        if (!NativeMethods.GetWindowRect(hWnd, out var rect))
            return ScreenshotService.CaptureDesktop();

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
            return ScreenshotService.CaptureDesktop();

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0,
                new System.Drawing.Size(width, height),
                System.Drawing.CopyPixelOperation.SourceCopy);
        }

        return BitmapToBitmapSource(bitmap);
    }

    /// <summary>
    /// Walk up from the foreground window to find the first valid
    /// parent window — skipping tooltips, overlays, and hidden windows.
    /// </summary>
    private static IntPtr FindValidForegroundWindow()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return IntPtr.Zero;

        // Try walking up to find an owning window that passes checks
        while (hWnd != IntPtr.Zero)
        {
            if (IsValidCaptureWindow(hWnd))
                return hWnd;

            hWnd = NativeMethods.GetParent(hWnd);
        }

        // Final attempt: enumerate from root if walking up failed
        hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return IntPtr.Zero;

        // One last check on the foreground window itself
        return IsValidCaptureWindow(hWnd) ? hWnd : IntPtr.Zero;
    }

    private static bool IsValidCaptureWindow(IntPtr hWnd)
    {
        // Must be visible
        if (!NativeMethods.IsWindowVisible(hWnd))
            return false;

        // Must have a non-empty title (skip utility windows)
        int len = NativeMethods.GetWindowTextLength(hWnd);
        if (len == 0)
            return false;

        // Must not be a tool window
        int exStyle = NativeMethods.GetWindowLong(hWnd, GWL_EXSTYLE);
        if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
            return false;

        // Must not be a no-activate window
        if ((exStyle & WS_EX_NOACTIVATE) != 0)
            return false;

        // Must not be an ignored window class
        var className = new StringBuilder(256);
        int classLen = NativeMethods.GetClassName(hWnd, className, className.Capacity);
        if (classLen > 0 && IgnoredWindowClasses.Contains(className.ToString()))
            return false;

        return true;
    }

    /// <summary>
    /// Capture a specific screen rectangle (screen coordinates).
    /// </summary>
    public static BitmapSource CaptureArea(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Area dimensions must be positive.");

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0,
                new System.Drawing.Size(width, height),
                System.Drawing.CopyPixelOperation.SourceCopy);
        }

        return BitmapToBitmapSource(bitmap);
    }

    // ── P/Invoke ─────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
    }

    // ── Shared helper ─────────────────────────────────────────────────

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    private static BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }
}
