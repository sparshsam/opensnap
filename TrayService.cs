using System.IO;
using Forms = System.Windows.Forms;

namespace OpenSnap;

/// <summary>
/// Manages the system-tray icon with a right-click context menu,
/// including screenshot history submenu.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _startupItem;
    private readonly Forms.ToolStripMenuItem _historyItem;
    private readonly Forms.ToolStripMenuItem _openLastItem;
    private readonly Forms.ToolStripMenuItem _copyPathItem;
    private readonly Forms.ToolStripMenuItem _revealItem;

    public event Action? CaptureRequested;
    public event Action? OpenFolderRequested;
    public event Action? ChangeFolderRequested;
    public event Action<bool>? StartupToggleRequested;
    public event Action? OpenLastScreenshotRequested;
    public event Action? CopyFilePathRequested;
    public event Action? RevealInExplorerRequested;
    public event Action<int>? OpenHistoryItemRequested;
    public event Action? QuitRequested;

    public TrayService()
    {
        _icon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "OpenSnap — Screenshot widget",
            Visible = false,
        };

        _menu = new Forms.ContextMenuStrip();

        var captureItem = new Forms.ToolStripMenuItem("Capture");
        captureItem.Click += (_, _) => CaptureRequested?.Invoke();

        var openItem = new Forms.ToolStripMenuItem("Open Desktop folder");
        openItem.Click += (_, _) => OpenFolderRequested?.Invoke();

        var changeItem = new Forms.ToolStripMenuItem("Change save folder…");
        changeItem.Click += (_, _) => ChangeFolderRequested?.Invoke();

        // History submenu
        _historyItem = new Forms.ToolStripMenuItem("Recent screenshots");
        _openLastItem = new Forms.ToolStripMenuItem("Open last screenshot");
        _openLastItem.Click += (_, _) => OpenLastScreenshotRequested?.Invoke();
        _copyPathItem = new Forms.ToolStripMenuItem("Copy file path");
        _copyPathItem.Click += (_, _) => CopyFilePathRequested?.Invoke();
        _revealItem = new Forms.ToolStripMenuItem("Reveal in Explorer");
        _revealItem.Click += (_, _) => RevealInExplorerRequested?.Invoke();

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
            _openLastItem,
            _revealItem,
            _copyPathItem,
            _historyItem,
            new Forms.ToolStripSeparator(),
            _startupItem,
            new Forms.ToolStripSeparator(),
            quitItem,
        });

        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick += (_, _) => CaptureRequested?.Invoke();
    }

    public void SetStartupChecked(bool enabled)
    {
        _startupItem.Checked = enabled;
    }

    /// <summary>Update the recent-screenshots submenu with the latest entries.</summary>
    public void UpdateHistory(List<string> history, int maxDisplay = 5)
    {
        _historyItem.DropDownItems.Clear();

        if (history.Count == 0)
        {
            _historyItem.DropDownItems.Add(new Forms.ToolStripMenuItem("(none)") { Enabled = false });
            return;
        }

        int start = Math.Max(0, history.Count - maxDisplay);
        for (int i = start; i < history.Count; i++)
        {
            var fileName = Path.GetFileName(history[i]);
            var index = i; // capture for closure
            var item = new Forms.ToolStripMenuItem(fileName);
            item.Click += (_, _) => OpenHistoryItemRequested?.Invoke(index);
            _historyItem.DropDownItems.Add(item);
        }
    }

    public void SetHistoryActionsEnabled(bool hasHistory)
    {
        _openLastItem.Enabled = hasHistory;
        _revealItem.Enabled = hasHistory;
        _copyPathItem.Enabled = hasHistory;
    }

    public void Show() => _icon.Visible = true;
    public void Hide() => _icon.Visible = false;

    public void Notify(string title, string text)
    {
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);
    }

    /// <summary>Show a balloon tip that opens a URL when clicked.</summary>
    public void NotifyWithLink(string title, string text, string url)
    {
        // Wire a one-shot click handler that opens the URL
        BalloonTipClicked handler = null!;
        handler = (_, _) =>
        {
            _icon.BalloonTipClicked -= handler;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* best-effort */ }
        };
        _icon.BalloonTipClicked += handler;
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);
    }

    private delegate void BalloonTipClicked(object? sender, EventArgs e);

    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("OpenSnap.Resources.app.ico");
            if (stream is not null)
                return new System.Drawing.Icon(stream);
        }
        catch { }
        return System.Drawing.Icon.ExtractAssociatedIcon(
            System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
