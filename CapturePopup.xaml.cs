using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OpenSnap;

/// <summary>
/// A small floating popup shown after each capture with quick-action buttons.
/// Auto-closes after 5 seconds unless the user interacts.
/// </summary>
public partial class CapturePopup : Window
{
    private readonly string _filePath;
    private readonly string? _ocrText;
    private readonly AppSettings _settings;
    private System.Windows.Threading.DispatcherTimer? _autoCloseTimer;

    /// <summary>Fired when the user clicks a quick action.</summary>
    public event Action<string>? ActionRequested;

    public CapturePopup(string filePath, string? ocrText, AppSettings settings)
    {
        _filePath = filePath;
        _ocrText = ocrText;
        _settings = settings;
        InitializeComponent();

        FileNameText.Text = Path.GetFileName(filePath);
        OcrBtn.IsEnabled = !string.IsNullOrEmpty(ocrText);
        CustomBtn.IsEnabled = !string.IsNullOrEmpty(_settings.CustomAppPath);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Position near the widget or bottom-right of primary screen
        var wa = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        Left = wa.Right - Width - 20;
        Top = wa.Bottom - Height - 20;

        // Auto-close after 5 seconds
        _autoCloseTimer = new System.Windows.Threading.DispatcherTimer(
            TimeSpan.FromSeconds(5),
            System.Windows.Threading.DispatcherPriority.Background,
            (_, _) => Close(),
            Dispatcher);
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Clicking anywhere dismisses early
        Close();
    }

    private void OnAction(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement btn || btn.Tag is not string action)
            return;

        _autoCloseTimer?.Stop();

        switch (action)
        {
            case "Copy":
                CopyToClipboard();
                break;
            case "Ocr":
                CopyOcrText();
                break;
            case "Paint":
                OpenIn("mspaint.exe");
                break;
            case "Reveal":
                ScreenshotService.RevealInExplorer(_filePath);
                break;
            case "Custom":
                OpenIn(_settings.CustomAppPath);
                break;
        }

        Close();
    }

    private void CopyToClipboard()
    {
        try
        {
            using var stream = File.OpenRead(_filePath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile,
                BitmapCacheOption.OnLoad);
            System.Windows.Clipboard.SetImage(decoder.Frames[0]);
        }
        catch { /* best-effort */ }
    }

    private void CopyOcrText()
    {
        if (!string.IsNullOrEmpty(_ocrText))
        {
            try { System.Windows.Clipboard.SetText(_ocrText); }
            catch { /* best-effort */ }
        }
    }

    private void OpenIn(string appPath)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = appPath,
                Arguments = $"\"{_filePath}\"",
                UseShellExecute = true,
            });
        }
        catch { /* best-effort */ }
    }
}
