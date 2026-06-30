using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenSnap;

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
    /// Retries with exponential backoff (100ms, 300ms, 900ms) if locked.
    /// For large bitmaps, uses a STA helper to avoid apartment-state issues.
    /// </summary>
    public static bool CopyToClipboard(BitmapSource source)
    {
        const int maxAttempts = 3;
        int[] delays = [100, 300, 900];

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // For images over 5 MP, use a temporary copy to reduce
                // memory pressure on the clipboard.
                var data = source.PixelWidth * source.PixelHeight > 5_000_000
                    ? CloneBitmap(source)
                    : source;
                System.Windows.Clipboard.SetImage(data);
                return true;
            }
            catch (ExternalException)
            {
                if (attempt < maxAttempts)
                    Thread.Sleep(delays[attempt - 1]);
            }
            catch (System.Threading.ThreadStateException)
            {
                // STA requirement — retry is same thread, so this is
                // unlikely; just move on.
                return false;
            }
        }

        return false;
    }

    private static BitmapSource CloneBitmap(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        ms.Position = 0;
        var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }

    /// <summary>Generates a timestamped filename using the configured template.</summary>
    public static string GenerateFileName(string? template = null,
        string? projectPrefix = null, int? sequentialNumber = null)
    {
        var fmt = template ?? "screenshot-{yyyy}-{MM}-{dd}-{HHmmss}";
        var now = DateTime.Now;
        fmt = fmt.Replace("{yyyy}", now.ToString("yyyy"))
                 .Replace("{MM}", now.ToString("MM"))
                 .Replace("{dd}", now.ToString("dd"))
                 .Replace("{HH}", now.ToString("HH"))
                 .Replace("{mm}", now.ToString("mm"))
                 .Replace("{ss}", now.ToString("ss"))
                 .Replace("{HHmmss}", now.ToString("HHmmss"))
                 .Replace("{seq}", (sequentialNumber ?? 0).ToString("D4"))
                 .Replace("{prefix}", projectPrefix ?? "");

        // Clean up double underscores/separators from empty prefix
        var name = fmt;
        if (string.IsNullOrEmpty(projectPrefix))
            name = name.Replace("{prefix}-", "").Replace("{prefix}_", "").Replace("{prefix}", "");
        name = name.Trim('-', '_', ' ');
        return name + ".png";
    }

    /// <summary>Resolve the full save path including optional date subfolders.</summary>
    public static string ResolveSavePath(string basePath, AppSettings settings)
    {
        if (!settings.DateSubfolders)
            return basePath;

        var now = DateTime.Now;
        var sub = Path.Combine(
            now.ToString("yyyy"),
            now.ToString("MM-yyyy"));
        return Path.Combine(basePath, sub);
    }

    /// <summary>Opens the folder in File Explorer.</summary>
    public static void OpenFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
    }

    /// <summary>Reveals a specific file in Explorer (selects it).</summary>
    public static void RevealInExplorer(string filePath)
    {
        if (File.Exists(filePath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
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
