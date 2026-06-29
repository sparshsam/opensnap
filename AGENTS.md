# OpenSnap — Project Status

**Version:** v0.6.0 (installer & distribution release)
**Status:** Active development, early stage
**Target:** Windows 10/11 desktop screenshot widget

---

## Current state

Functional MVP with core capture modes, OCR, global hotkeys, and a proper
Inno Setup installer. The widget is a floating 80×36 glass capsule that lives
always-on-top. Settings now include hotkey re-binding, capture sound toggle,
and filename template editing. An About dialog shows version info and a
GitHub link.

## What works

- Full screen multi-monitor capture
- Active window capture (foreground window via Win32 API, widget hidden before capture)
- Area selection (fullscreen transparent overlay with drag-select)
- Capture + OCR (Windows.Media.Ocr, text copied to clipboard)
- Global hotkeys (Win+Shift+S, Win+Shift+W via RegisterHotKey)
- System tray with history submenu, startup toggle, folder actions
- Screenshot history (last 20, persisted in settings.json)
- Customisable filename templates
- Capture shutter sound
- Settings dialog (save path, always-on-top, startup, sound, template, hotkeys)
- About dialog (version, GitHub link, license)
- Glass UI with drop shadow, draggable, position persists
- Off-screen position clamping
- Double-click suppression
- Framework-dependent publish (~170 KB exe)
- Inno Setup installer → Start Menu + optional desktop shortcut
- Installer preserves user settings on upgrade
- Clean uninstall via Settings → Apps
- GitHub release with installer artifact

## What needs attention

### Known rough edges
- **CLI:** Not published as a CLI tool / no command-line arguments yet
- **OCR:** No language selection — uses user profile languages
- **Scaling:** Widget dimensions fixed at 80×36 CSS pixels; may need DPI
  awareness verification
- **Multi-monitor:** Area selection overlay covers virtual screen; selection
  rect may have offset issues on non-primary monitors with different DPIs
- **First-launch flow:** No onboarding or tooltip on first run
- **Error handling:** OCR failures silently return empty string
- **Installer:** Not code-signed (SmartScreen warning on first run)

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

- Region/capture presets
- Image format options (JPEG, BMP)
- Upload to clipboard / cloud
- Custom overlay styling
- First-run tooltip / onboarding
- DPI awareness improvements
- Code signing for installer

## Architecture notes

The app uses WinRT interop via `net8.0-windows10.0.19041.0` targeting
pack for `Windows.Media.Ocr` and `Windows.Graphics.Imaging`. The WPF
frontend handles all UI; WinForms is pulled in for tray icon and
folder browser dialogs (`UseWindowsForms=true`).

The `UseWindowsForms=true` flag causes many ambiguous type references
(`Point`, `Color`, `Clipboard`, `MouseEventArgs`, `KeyEventArgs`,
`BitmapFrame`, `BitmapDecoder`). New code must use fully qualified names
for these types.

### Known WPF pitfalls (v0.6.0 additions)
- `SelectedIndex` on an empty `ComboBox` in XAML throws `ArgumentException`
  — populate items in code-behind, set selection in `OnLoaded` under
  `_suppressToggle`.
- `SelectionChanged` fires during `InitializeComponent()` if
  `SelectedIndex` is set in XAML — any handler accessing named elements
  declared later in the XAML tree will get `NullReferenceException`.

## Release process

1. Commit changes on WSL side (`/home/spars/repos/opensnap/`)
2. Sync to Windows: `rsync -a . /mnt/c/Users/spars/repos/opensnap/ --exclude=.git --exclude=bin --exclude=obj`
3. Publish via cmd.exe: `/mnt/c/Windows/System32/cmd.exe /c "C:\tmp\publish-opensnap.bat"`
4. Update shortcut via PowerShell: `/mnt/c/Windows/System32/cmd.exe /c "powershell.exe -ExecutionPolicy Bypass -File C:\tmp\createshortcut.ps1"`
5. Build installer: `/mnt/c/Program Files (x86)/Inno Setup 6/ISCC.exe setup.iss`
6. Tag and push: `git tag -f v0.x.x && git push origin main --tags`
7. Create GitHub release: `gh release create v0.x.x --title "v0.x.x — Title" --notes-file RELEASE_NOTES.md`
8. Upload artifact: `gh release upload v0.x.x dist/OpenSnap-Setup-v0.x.x.exe`

## v0.6.0 changelog

### Fixed
- Area selection now captures the selected region (callback `Close()` raced
  ahead of callback invocation, nulling the `TaskCompletionSource`)
- Active window capture grabs the correct foreground window instead of the
  widget itself (widget is hidden before `GetForegroundWindow()`)
- Settings no longer crashes on open (XAML `SelectedIndex="0"` on empty
  ComboBox + `SelectionChanged` handler accessed uninitialized named elements)
- Play capture sound toggle now exposed in Settings UI
- Hotkey modifier/key combo selection now functional in Settings

### Added
- Global hotkey configuration — modifier (Win/Ctrl/Alt/Shift combos) + key
  (A-Z, 0-9, F1-F12) picker for both full screen and active window
- Inno Setup installer (`setup.iss`) — installs to `%LOCALAPPDATA%\Programs\OpenSnap`
- `build-installer.bat` — one-command build + package pipeline
- About dialog — version number, GitHub link, MIT license
- Filename template editor in Settings
- `dist/` release artifact folder (gitignored)
- GitHub release workflow (gh CLI)

### Changed
- Version bumped from 0.5.1 → 0.6.0
- Repo folder renamed `openshot/` → `opensnap/`
- README updated with install/uninstall/upgrade/troubleshooting docs
- `CaptureService.CaptureActiveWindow()` uses `GetForegroundWindow()` directly
  (removed stale stored-handle mechanism from earlier failed attempt)
