using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace OpenSnap;

/// <summary>
/// Extracts text from a screenshot using the built-in Windows OCR engine
/// (Windows 10+). No external dependencies or network calls.
/// </summary>
public static class OcrService
{
    /// <summary>
    /// Recognize text from a <see cref="BitmapSource"/>.
    /// Returns the extracted text, or an empty string on failure.
    /// </summary>
    public static async Task<string> ExtractTextAsync(BitmapSource source)
    {
        try
        {
            // Encode BitmapSource to PNG in-memory
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            // Convert to WinRT stream
            using var randomStream = ms.AsRandomAccessStream();

            // Decode to SoftwareBitmap
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // Run OCR
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine == null)
                return string.Empty;

            var result = await ocrEngine.RecognizeAsync(softwareBitmap);
            return result?.Text ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Recognizes text and copies it to the clipboard.
    /// Returns true if text was found and copied.
    /// </summary>
    public static async Task<(string Text, bool Copied)> CaptureOcrAsync(BitmapSource source)
    {
        var text = await ExtractTextAsync(source);

        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, false);

        try
        {
            System.Windows.Clipboard.SetText(text);
            return (text, true);
        }
        catch
        {
            return (text, false);
        }
    }
}
