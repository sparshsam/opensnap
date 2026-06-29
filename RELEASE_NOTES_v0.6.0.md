## What's new in v0.6.0

OpenSnap now installs like a proper Windows app with an Inno Setup installer, includes global hotkey configuration in Settings, fixes two capture bugs, and adds an About dialog.

### Installer & Distribution
- Inno Setup installer packaging (`dist\OpenSnap-Setup-v0.6.0.exe`)
- Installs to `%LOCALAPPDATA%\Programs\OpenSnap`
- Start Menu shortcut on install
- Optional desktop shortcut
- Clean uninstall via Settings → Apps or Start Menu
- User settings in `%APPDATA%\OpenSnap\` are never touched during upgrades
- `build-installer.bat` — one-command build + package pipeline

### Bug Fixes
- **Area selection** — selecting a region now actually captures that region (was returning null)
- **Active window capture** — now captures the correct foreground window instead of the widget itself (widget is temporarily hidden before capture)
- **Settings crash** — fixed XAML parsing crash when opening settings (empty ComboBox `SelectedIndex` and null-reference during `InitializeComponent`)

### Settings Enhancements
- Global hotkey configuration: modifier (Win/Ctrl/Alt/Shift combos) + key (A-Z, 0-9, F1-F12, etc.) for both full screen and active window
- Play capture sound toggle (was in settings model but not exposed in UI)
- Filename template editor with token reference
- About dialog with version number and GitHub link

### Chores
- Repo folder renamed `openshot/` → `opensnap/`
- Version bumped to 0.6.0
- `dist/` build output directory
- Updated README with install/uninstall/upgrade/troubleshooting docs
