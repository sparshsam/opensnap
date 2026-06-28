using System.Windows;
using Forms = System.Windows.Forms;

namespace OpenShot;

/// <summary>
/// Manages the system-tray icon with a right-click context menu.
/// (Capture / Open Folder / Change Folder / Quit)
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;

    public event Action? CaptureRequested;
    public event Action? OpenFolderRequested;
    public event Action? ChangeFolderRequested;
    public event Action? QuitRequested;

    public TrayService()
    {
        _icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location),
            Text = "OpenShot",
            Visible = false,
        };

        _menu = new Forms.ContextMenuStrip();

        var captureItem = new Forms.ToolStripMenuItem("Capture");
        captureItem.Click += (_, _) => CaptureRequested?.Invoke();

        var openItem = new Forms.ToolStripMenuItem("Open screenshots folder");
        openItem.Click += (_, _) => OpenFolderRequested?.Invoke();

        var changeItem = new Forms.ToolStripMenuItem("Change save folder…");
        changeItem.Click += (_, _) => ChangeFolderRequested?.Invoke();

        var quitItem = new Forms.ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => QuitRequested?.Invoke();

        _menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            captureItem,
            new Forms.ToolStripSeparator(),
            openItem,
            changeItem,
            new Forms.ToolStripSeparator(),
            quitItem,
        });

        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick += (_, _) => CaptureRequested?.Invoke();
    }

    public void Show()  => _icon.Visible = true;
    public void Hide()  => _icon.Visible = false;

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
