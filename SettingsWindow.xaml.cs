using System.Windows;
using Forms = System.Windows.Forms;

namespace OpenShot;

/// <summary>
/// Settings dialog — save path, always-on-top, launch at startup.
/// Changes are written to settings immediately.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _suppressToggle;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _suppressToggle = true;
        SavePathBox.Text = _settings.SavePath;
        AlwaysOnTopCheck.IsChecked = _settings.AlwaysOnTop;
        LaunchAtStartupCheck.IsChecked = _settings.LaunchAtStartup;
        _suppressToggle = false;
    }

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
        _settings.Save();

        StartupManager.SetStartup(_settings.LaunchAtStartup);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
