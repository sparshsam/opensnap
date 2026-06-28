using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenShot;

/// <summary>
/// Handles full-desktop screenshot capture, PNG save, and clipboard copy.
/// </summary>
public static class ScreenshotService
{
    /// <summary>
    /// Capture the entire multi-monitor desktop as a <see cref="BitmapSource"/>.
    /// </summary>
    public static BitmapSource CaptureDesktop()
    {
        int left = SystemInformation.VirtualScreen.Left;
        int top = SystemInformation.VirtualScreen.Top;
        int width = SystemInformation.VirtualScreen.Width;
        int height = SystemInformation.VirtualScreen.Height;

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(left, top, 0, 0, bitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);
        }

        return BitmapToBitmapSource(bitmap);
    }

    /// <summary>Saves a <see cref="BitmapSource"/> as PNG to the specified path.</summary>
    public static void SaveAsPng(BitmapSource source, string filePath)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = File.OpenWrite(filePath);
        encoder.Save(stream);
    }

    /// <summary>
    /// Copies a <see cref="BitmapSource"/> to the Windows clipboard.
    /// Retries up to 3 times with a short delay if the clipboard is locked.
    /// </summary>
    public static bool CopyToClipboard(BitmapSource source)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                System.Windows.Clipboard.SetImage(source);
                return true;
            }
            catch (ExternalException)
            {
                // Clipboard locked by another process — retry after a short pause
                if (attempt < maxAttempts)
                    Thread.Sleep(100);
            }
        }

        return false;
    }

    /// <summary>Generates a timestamped filename: screenshot-yyyy-MM-dd-HHmmss.png</summary>
    public static string GenerateFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
        return $"screenshot-{timestamp}.png";
    }

    /// <summary>Opens the folder in File Explorer.</summary>
    public static void OpenFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
    }

    // ── Native P/Invoke ────────────────────────────────────────────────

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
            NativeMethods.DeleteObject(hBitmap);
        }
    }

    private static class NativeMethods
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
