using Forms = System.Windows.Forms;

namespace OpenShot;

/// <summary>
/// Manages the system-tray icon with a right-click context menu.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _startupItem;

    public event Action? CaptureRequested;
    public event Action? OpenFolderRequested;
    public event Action? ChangeFolderRequested;
    public event Action<bool>? StartupToggleRequested;
    public event Action? QuitRequested;

    public TrayService()
    {
        _icon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "OpenShot — Screenshot widget",
            Visible = false,
        };

        _menu = new Forms.ContextMenuStrip();

        var captureItem = new Forms.ToolStripMenuItem("Capture");
        captureItem.Click += (_, _) => CaptureRequested?.Invoke();

        var openItem = new Forms.ToolStripMenuItem("Open Desktop folder");
        openItem.Click += (_, _) => OpenFolderRequested?.Invoke();

        var changeItem = new Forms.ToolStripMenuItem("Change save folder…");
        changeItem.Click += (_, _) => ChangeFolderRequested?.Invoke();

        _startupItem = new Forms.ToolStripMenuItem("Launch at startup");
        _startupItem.Click += (_, _) =>
        {
            _startupItem.Checked = !_startupItem.Checked;
            StartupToggleRequested?.Invoke(_startupItem.Checked);
        };

        var quitItem = new Forms.ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => QuitRequested?.Invoke();

        _menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            captureItem,
            new Forms.ToolStripSeparator(),
            openItem,
            changeItem,
            new Forms.ToolStripSeparator(),
            _startupItem,
            new Forms.ToolStripSeparator(),
            quitItem,
        });

        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick += (_, _) => CaptureRequested?.Invoke();
    }

    /// <summary>Sync the checked state of the startup toggle from settings.</summary>
    public void SetStartupChecked(bool enabled)
    {
        _startupItem.Checked = enabled;
    }

    public void Show() => _icon.Visible = true;
    public void Hide() => _icon.Visible = false;

    /// <summary>Load the tray icon from embedded resources (works in single-file publish).</summary>
    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("OpenShot.Resources.app.ico");
            if (stream is not null)
                return new System.Drawing.Icon(stream);
        }
        catch
        {
            // Fall through to default
        }
        return System.Drawing.Icon.ExtractAssociatedIcon(
            System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
    }

    /// <summary>Show a balloon tip notification.</summary>
    public void Notify(string title, string text)
    {
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
