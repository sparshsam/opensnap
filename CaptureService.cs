using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenShot;

/// <summary>
/// Capture modes available from the widget.
/// </summary>
public enum CaptureMode
{
    FullScreen,
    ActiveWindow,
    AreaSelection,
}

/// <summary>
/// Extends <see cref="ScreenshotService"/> with active-window and
/// area-selection capture. All methods return a <see cref="BitmapSource"/>
/// ready for save + clipboard.
/// </summary>
public static class CaptureService
{
    /// <summary>
    /// Capture the currently focused (foreground) window only.
    /// </summary>
    public static BitmapSource CaptureActiveWindow()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return ScreenshotService.CaptureDesktop(); // fallback

        if (!NativeMethods.GetWindowRect(hWnd, out var rect))
            return ScreenshotService.CaptureDesktop(); // fallback

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
