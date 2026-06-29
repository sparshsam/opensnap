# OpenSnap — Project Status

**Version:** v0.6.0 (installer & distribution release)
**Status:** Active development, early stage
**Target:** Windows 10/11 desktop screenshot widget

---

## Current state

Functional MVP with core capture modes and OCR. The widget is a floating
80×36 glass capsule that lives always-on-top. Left-click captures full
screen. Right-click opens a menu for active window, area selection, and
Capture + OCR (Windows built-in text recognition).

## What works

- Full screen multi-monitor capture
- Active window capture (foreground window via Win32 API)
- Area selection (fullscreen transparent overlay with drag-select)
- Capture + OCR (Windows.Media.Ocr, text copied to clipboard)
- Global hotkeys (Win+Shift+S, Win+Shift+W via RegisterHotKey)
- System tray with history submenu, startup toggle, folder actions
- Screenshot history (last 20, persisted in settings.json)
- Customisable filename templates
- Capture shutter sound
- Settings dialog (save path, always-on-top, startup, sound, template)
- Glass UI with drop shadow, draggable, position persists
- Off-screen position clamping
- Double-click suppression
- Framework-dependent publish (~170 KB exe)

## What needs attention

### Fixed in v0.6.0
- Area selection now captures the selected region (callback-order bug)
- Active window capture grabs the correct window (widget is hidden before capture)
- Settings no longer crashes on open (XAML SelectedIndex + null-ref during InitializeComponent)
- Play capture sound toggle now exposed in UI
- Hotkey configuration available in Settings
- About dialog with version and GitHub link

### Known rough edges
- **CLI:** Not published as a CLI tool / no command-line arguments yet
- **Settings:** No hotkey re-binding UI in the settings dialog (hotkey values
  are stored in settings.json but the dialog doesn't have modifier dropdowns)
- **OCR:** No language selection — uses user profile languages
- **Scaling:** Widget dimensions fixed at 80×36 CSS pixels; may need DPI
  awareness verification
- **Multi-monitor:** Area selection overlay covers virtual screen; selection
  rect may have offset issues on non-primary monitors with different DPIs
- **First-launch flow:** No onboarding or tooltip on first run
- **Error handling:** OCR failures silently return empty string

### No test coverage
- Zero unit tests
- Zero integration tests
- Manual QA only (checklist in README)

### Build/deploy friction
- Must build on Windows (WPF + WinRT not available on Linux)
- Publish requires `cmd.exe` through WSL interop (UNC path workaround)
- No CI/CD pipeline
- Installer built with Inno Setup (`build-installer.bat`)

## Upcoming / planned

- Hotkey re-binding UI in settings
- Region/capture presets
- Image format options (JPEG, BMP)
- Upload to clipboard history
- Custom overlay styling
- First-run tooltip / onboarding
- DPI awareness improvements
- Installer packaging

## Architecture notes

The app uses WinRT interop via `net8.0-windows10.0.19041.0` targeting
pack for `Windows.Media.Ocr` and `Windows.Graphics.Imaging`. The WPF
frontend handles all UI; WinForms is pulled in for tray icon and
folder browser dialogs (`UseWindowsForms=true`).

The `UseWindowsForms=true` flag causes many ambiguous type references
(`Point`, `Color`, `Clipboard`, `MouseEventArgs`, `KeyEventArgs`,
`BitmapFrame`, `BitmapDecoder`). New code must use fully qualified names
for these types.

## Release process

1. Commit changes on WSL side (`/home/spars/repos/opensnap/`)
2. Sync to Windows: `rsync -a . /mnt/c/Users/spars/repos/opensnap/ --exclude=.git --exclude=bin --exclude=obj`
3. Publish via cmd.exe: `/mnt/c/Windows/System32/cmd.exe /c "C:\tmp\publish-opensnap.bat"`
4. Update shortcut via PowerShell: `/mnt/c/Windows/System32/cmd.exe /c "powershell.exe -ExecutionPolicy Bypass -File C:\tmp\createshortcut.ps1"`
5. Tag and push: `git tag -f v0.x.x && git push origin main --tags`
