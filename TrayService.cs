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
    public event Action<int>? PinHistoryItemRequested;
    public event Action<int>? DeleteHistoryItemRequested;
    public event Action? ClearHistoryRequested;
    public event Action<string>? SearchHistoryRequested;
    public event Action? CheckUpdateRequested;
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

        var updateItem = new Forms.ToolStripMenuItem("Check for updates");
        updateItem.Click += (_, _) => CheckUpdateRequested?.Invoke();

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
            updateItem,
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
    public void UpdateHistory(List<string> history, List<string>? pinned = null, int maxDisplay = 5)
    {
        _historyItem.DropDownItems.Clear();

        // Pinned section
        if (pinned is { Count: > 0 })
        {
            foreach (var pinPath in pinned)
            {
                if (!File.Exists(pinPath)) continue;
                var pinName = Path.GetFileName(pinPath);
                var pinItem = new Forms.ToolStripMenuItem($"📌 {pinName}");
                pinItem.Click += (_, _) =>
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", $"\"{pinPath}\""); }
                    catch { }
                };
                _historyItem.DropDownItems.Add(pinItem);
            }
            _historyItem.DropDownItems.Add(new Forms.ToolStripSeparator());
        }

        // Recent captures
        if (history.Count == 0)
        {
            _historyItem.DropDownItems.Add(new Forms.ToolStripMenuItem("(none)") { Enabled = false });
            return;
        }

        int start = Math.Max(0, history.Count - maxDisplay);
        for (int i = start; i < history.Count; i++)
        {
            var path = history[i];
            var fileName = Path.GetFileName(path);
            var index = i;
            var item = new Forms.ToolStripMenuItem(fileName);
            item.Click += (_, _) => OpenHistoryItemRequested?.Invoke(index);

            // Pin action
            var pinSub = new Forms.ToolStripMenuItem("Pin to top");
            pinSub.Click += (_, _) => PinHistoryItemRequested?.Invoke(index);
            item.DropDownItems.Add(pinSub);

            // Delete action
            var delSub = new Forms.ToolStripMenuItem("Delete from history");
            delSub.Click += (_, _) => DeleteHistoryItemRequested?.Invoke(index);
            item.DropDownItems.Add(delSub);

            _historyItem.DropDownItems.Add(item);
        }

        _historyItem.DropDownItems.Add(new Forms.ToolStripSeparator());
        var clearItem = new Forms.ToolStripMenuItem("Clear history");
        clearItem.Click += (_, _) => ClearHistoryRequested?.Invoke();
        _historyItem.DropDownItems.Add(clearItem);
    }

    public void SetHistoryActionsEnabled(bool hasHistory)
    {
        _openLastItem.Enabled = hasHistory;
        _revealItem.Enabled = hasHistory;
        _copyPathItem.Enabled = hasHistory;
    }

    public bool IsVisible => _icon.Visible;
    public void Show() => _icon.Visible = true;
    public void Hide() => _icon.Visible = false;

    public void Notify(string title, string text)
    {
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);
    }

    /// <summary>Fired when the user clicks an update notification.</summary>
    public event Action? UpdateNotificationClicked;

    /// <summary>Show a balloon tip that opens a URL or triggers an action when clicked.</summary>
    public void NotifyWithLink(string title, string text, string url)
    {
        EventHandler handler = null!;
        handler = (_, _) =>
        {
            _icon.BalloonTipClicked -= handler;
            try
            {
                if (url == "checkupdate://")
                {
                    UpdateNotificationClicked?.Invoke();
                }
                else if (url.StartsWith("install://"))
                {
                    var path = url["install://".Length..];
                    if (File.Exists(path))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER",
                            UseShellExecute = true,
                        });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch { /* best-effort */ }
        };
        _icon.BalloonTipClicked += handler;
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);
    }

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
