using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace OpenSnap;

/// <summary>
/// Settings dialog — save path, always-on-top, launch at startup,
/// capture sound, filename template, and global hotkey bindings.
/// Changes are written to settings immediately.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _suppressToggle;

    // Display-name → VK code mapping for the key combo boxes
    private static readonly (string Name, int Vk)[] KeyEntries =
    [
        ("A", 0x41), ("B", 0x42), ("C", 0x43), ("D", 0x44), ("E", 0x45),
        ("F", 0x46), ("G", 0x47), ("H", 0x48), ("I", 0x49), ("J", 0x4A),
        ("K", 0x4B), ("L", 0x4C), ("M", 0x4D), ("N", 0x4E), ("O", 0x4F),
        ("P", 0x50), ("Q", 0x51), ("R", 0x52), ("S", 0x53), ("T", 0x54),
        ("U", 0x55), ("V", 0x56), ("W", 0x57), ("X", 0x58), ("Y", 0x59),
        ("Z", 0x5A),
        ("0", 0x30), ("1", 0x31), ("2", 0x32), ("3", 0x33), ("4", 0x34),
        ("5", 0x35), ("6", 0x36), ("7", 0x37), ("8", 0x38), ("9", 0x39),
        ("F1", 0x70), ("F2", 0x71), ("F3", 0x72), ("F4", 0x73),
        ("F5", 0x74), ("F6", 0x75), ("F7", 0x76), ("F8", 0x77),
        ("F9", 0x78), ("F10", 0x79), ("F11", 0x7A), ("F12", 0x7B),
        ("Escape", 0x1B), ("Tab", 0x09), ("Space", 0x20),
        ("Enter", 0x0D), ("Backspace", 0x08),
        ("[ ]", 0xBA), (";", 0xBB), (",", 0xBC), ("-", 0xBD),
        (".", 0xBE), ("/", 0xBF), ("`", 0xC0),
    ];

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        PopulateKeyCombos();
        PopulateLanguageCombo();
    }

    private void PopulateLanguageCombo()
    {
        foreach (var (code, name) in LocalizationService.Languages)
        {
            LangCombo.Items.Add(new ComboBoxItem { Content = name, Tag = code });
        }
        // Select current language
        for (int i = 0; i < LangCombo.Items.Count; i++)
        {
            if (LangCombo.Items[i] is ComboBoxItem item && item.Tag is string tag && tag == _settings.Language)
            { LangCombo.SelectedIndex = i; break; }
        }
    }

    public event Action? VisualSettingsChanged;

    private void PopulateKeyCombos()
    {
        foreach (var (name, vk) in KeyEntries)
        {
            FullScreenKeyCombo.Items.Add(new ComboBoxItem { Content = name, Tag = vk });
            ActiveWinKeyCombo.Items.Add(new ComboBoxItem { Content = name, Tag = vk });
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _suppressToggle = true;

        // Existing fields
        SavePathBox.Text = _settings.SavePath;
        AlwaysOnTopCheck.IsChecked = _settings.AlwaysOnTop;
        LaunchAtStartupCheck.IsChecked = _settings.LaunchAtStartup;
        PlaySoundCheck.IsChecked = _settings.PlayCaptureSound;
        EdgeSnapCheck.IsChecked = _settings.EdgeSnapEnabled;
        AutoHideCheck.IsChecked = _settings.AutoHideFullscreen;

        // Opacity
        OpacitySlider.Value = _settings.Opacity;
        OpacityLabel.Text = $"{(int)(_settings.Opacity * 100)}%";

        // Filename template
        TemplateBox.Text = _settings.FilenameTemplate;

        // Naming
        PrefixBox.Text = _settings.ProjectPrefix;
        SeqNumberCheck.IsChecked = _settings.UseSequentialNumbering;
        DateFoldersCheck.IsChecked = _settings.DateSubfolders;
        LargeIconsCheck.IsChecked = _settings.LargeIcons;
        CustomAppBox.Text = _settings.CustomAppPath;
        QuickActionsCheck.IsChecked = _settings.ShowQuickActions;

        // Full screen hotkey
        SelectModCombo(FullScreenModCombo, _settings.HotkeyCaptureModifiers);
        SelectKeyCombo(FullScreenKeyCombo, _settings.HotkeyCaptureKey);

        // Active window hotkey
        SelectModCombo(ActiveWinModCombo, _settings.HotkeyActiveWinModifiers);
        SelectKeyCombo(ActiveWinKeyCombo, _settings.HotkeyActiveWinKey);

        _suppressToggle = false;
    }

    private static int FindModIndex(int modifierFlags)
    {
        return modifierFlags switch
        {
            8 => 0,   // Win
            2 => 1,   // Ctrl
            1 => 2,   // Alt
            4 => 3,   // Shift
            12 => 4,  // Win+Shift
            10 => 5,  // Win+Ctrl
            9 => 6,   // Win+Alt
            6 => 7,   // Ctrl+Shift
            3 => 8,   // Ctrl+Alt
            5 => 9,   // Alt+Shift
            _ => 4,   // default Win+Shift
        };
    }

    private static void SelectModCombo(System.Windows.Controls.ComboBox combo, int modifierFlags)
    {
        combo.SelectedIndex = FindModIndex(modifierFlags);
    }

    private static void SelectKeyCombo(System.Windows.Controls.ComboBox combo, int vkCode)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Tag is int tagVal && tagVal == vkCode)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        // Default: S (0x53)
        combo.SelectedIndex = 18; // 'S' is at index 18 in the key list
    }

    // ── Handlers ──────────────────────────────────────────────────────

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select screenshot save folder",
            SelectedPath = _settings.SavePath,
            ShowNewFolderButton = true,
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _settings.SavePath = dialog.SelectedPath;
            _settings.Save();
            SavePathBox.Text = dialog.SelectedPath;
        }
    }

    private void OnToggleChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle) return;

        _settings.AlwaysOnTop = AlwaysOnTopCheck.IsChecked ?? true;
        _settings.LaunchAtStartup = LaunchAtStartupCheck.IsChecked ?? false;
        _settings.PlayCaptureSound = PlaySoundCheck.IsChecked ?? true;
        _settings.EdgeSnapEnabled = EdgeSnapCheck.IsChecked ?? true;
        _settings.AutoHideFullscreen = AutoHideCheck.IsChecked ?? false;
        _settings.LargeIcons = LargeIconsCheck.IsChecked ?? false;
        _settings.UseSequentialNumbering = SeqNumberCheck.IsChecked ?? false;
        _settings.DateSubfolders = DateFoldersCheck.IsChecked ?? false;
        _settings.ShowQuickActions = QuickActionsCheck.IsChecked ?? true;
        _settings.Save();

        StartupManager.SetStartup(_settings.LaunchAtStartup);
        VisualSettingsChanged?.Invoke();
    }
    private void OnNamingChanged(object sender, EventArgs e)
    {
        if (_suppressToggle) return;
        _settings.ProjectPrefix = PrefixBox.Text;
        _settings.CustomAppPath = CustomAppBox.Text;
        _settings.Save();
    }
    private void OnBrowseApp(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Title = "Select image editor",
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            CustomAppBox.Text = dialog.FileName;
            _settings.CustomAppPath = dialog.FileName;
            _settings.Save();
        }
    }

    private void OnHotkeyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressToggle) return;

        // Read full screen hotkey
        int fsMod = GetModValue(FullScreenModCombo);
        int fsKey = GetKeyValue(FullScreenKeyCombo);

        // Read active window hotkey
        int awMod = GetModValue(ActiveWinModCombo);
        int awKey = GetKeyValue(ActiveWinKeyCombo);

        _settings.HotkeyCaptureModifiers = fsMod;
        _settings.HotkeyCaptureKey = fsKey;
        _settings.HotkeyActiveWinModifiers = awMod;
        _settings.HotkeyActiveWinKey = awKey;
        _settings.Save();
    }

    private void OnTemplateChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressToggle) return;

        _settings.FilenameTemplate = TemplateBox.Text;
        _settings.Save();
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressToggle) return;
        _settings.Opacity = Math.Round(e.NewValue, 2);
        OpacityLabel.Text = $"{(int)(_settings.Opacity * 100)}%";
        _settings.Save();
        VisualSettingsChanged?.Invoke();
    }

    private void OnLangChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressToggle) return;
        if (LangCombo.SelectedItem is ComboBoxItem item && item.Tag is string code)
        {
            App.T.SetLanguage(code);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog(_settings)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        dialog.ShowDialog();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static int GetModValue(System.Windows.Controls.ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
        {
            if (int.TryParse(tagStr, out int val))
                return val;
        }
        return 12; // Win+Shift default
    }

    private static int GetKeyValue(System.Windows.Controls.ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is int val)
            return val;
        return 0x53; // S default
    }
}
