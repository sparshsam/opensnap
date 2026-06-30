# OpenSnap

A minimal, always-on-top desktop screenshot widget for Windows.

**Stack:** C# WPF on .NET 8, Windows-only desktop app
**Repo:** https://github.com/sparshsam/opensnap
**Latest tag:** v0.7.0

---

## Project structure

```
/home/spars/repos/opensnap/           # Git repo root
├── OpenShot.csproj                   # .NET 8 WPF + WinRT project
├── App.xaml / .cs                    # Entry point, capture dispatch, tray wiring
├── MainWindow.xaml / .cs             # Floating glass widget (240×36 triple-button pill)
├── AppSettings.cs                    # JSON settings → %APPDATA%\OpenSnap\
├── ScreenshotService.cs              # Capture, save, clipboard, filename template
├── CaptureService.cs                 # Active window + area capture + CaptureMode enum
├── OcrService.cs                     # Windows.Media.Ocr text extraction
├── HotkeyService.cs                  # Global hotkeys (RegisterHotKey Win32 API)
├── TrayService.cs                    # System tray icon + context menu
├── StartupManager.cs                 # Registry Run key management
├── AreaSelectorWindow.xaml / .cs     # Fullscreen drag-select overlay
├── SettingsWindow.xaml / .cs         # Settings dialog (hotkeys, sound, template)
├── AboutDialog.xaml / .cs            # About dialog (version, GitHub, license)
├── Resources/app.ico                 # Application icon
├── Resources/capture.wav             # Camera shutter sound
├── setup.iss                         # Inno Setup installer script
├── build-installer.bat               # Build + installer packaging script
├── RELEASE_NOTES_v0.6.0.md           # GitHub release notes
├── README.md
├── CLAUDE.md
├── AGENTS.md
└── .gitignore
```

## Build & publish

```bash
# Build
dotnet build -c Release

# Publish (framework-dependent, ~170 KB)
dotnet publish -c Release -o release

# Build installer (requires Inno Setup 6)
build-installer.bat

# Output: bin/Release/net8.0-windows10.0.19041.0/OpenSnap.dll
# Published: release/OpenSnap.exe
# Installer: dist/OpenSnap-Setup-v0.7.0.exe
```

The app targets `net8.0-windows10.0.19041.0` for access to `Windows.Media.Ocr`.

## Key conventions

- **Namespace:** `OpenSnap` (was `OpenShot`, renamed in v0.5.0)
- **Assembly name:** `OpenSnap`
- **Settings path:** `%APPDATA%\OpenSnap\settings.json`
- **Default save path:** `C:\Users\spars\Desktop`
- **Filenames:** `screenshot-{yyyy}-{MM}-{dd}-{HHmmss}.png`
- **Target framework:** `net8.0-windows10.0.19041.0`

## Widget design

- 240×36 px translucent glass capsule with drop shadow, divided into 3 sections
- Left (toggle): area selection — selection-box icon, blue glow when active
- Center (default): full screen — camera icon
- Right: active window — window icon
- Spring-back bounce animation (BackEase, 400ms) on button press
- Green border flash (#007a3f) after capture
- Hover glow per section, glass dividers between sections
- Always-on-top, draggable via threshold-based click-vs-drag

## Input reference (widget sections, 240px pill)

| Input | Action |
|---|---|
| Left-click — left third | Area selection (toggle, blue glow) |
| Left-click — center third | Full screen capture |
| Left-click — right third | Active window capture |
| Right-click | Open mode context menu |
| Middle-click | Capture active window |
| Drag | Move widget |
| Win+Shift+S | Capture full screen (global hotkey) |
| Win+Shift+W | Capture active window (global hotkey) |
| (configurable in Settings) | Hotkey modifier + key picker |

## Capture modes (right-click menu)

1. **Full screen** — all monitors, via `Graphics.CopyFromScreen`
2. **Active window** — foreground window via `GetForegroundWindow` + `GetWindowRect`
3. **Area selection** — fullscreen transparent overlay, drag to select
4. **Capture + OCR** — full screen + `Windows.Media.Ocr.OcrEngine` → clipboard
5. **Settings** — save path, always-on-top, startup, sound, filename template, hotkeys, About

## Settings dialog

Stored at `%APPDATA%\OpenSnap\settings.json`. Configurable fields:

| Field | Default | Description |
|---|---|---|
| `SavePath` | Desktop | Folder for screenshots |
| `AlwaysOnTop` | true | Widget stays above other windows |
| `LaunchAtStartup` | false | Auto-start with Windows |
| `PlayCaptureSound` | true | Play shutter sound on capture |
| `FilenameTemplate` | `screenshot-{yyyy}-{MM}-{dd}-{HHmmss}` | Template with `{yyyy}`, `{MM}`, `{dd}`, `{HH}`, `{mm}`, `{ss}`, `{HHmmss}` |
| `HotkeyCaptureModifiers` | `Win+Shift` (12) | Modifier flags for full screen hotkey |
| `HotkeyCaptureKey` | `S` (0x53) | Virtual key code for full screen hotkey |
| `HotkeyActiveWinModifiers` | `Win+Shift` (12) | Modifier flags for active window hotkey |
| `HotkeyActiveWinKey` | `W` (0x57) | Virtual key code for active window hotkey |
| `ScreenshotHistory` | [] | Last 20 saved file paths |

## System tray

- Capture / Open Desktop folder / Change save folder
- Recent screenshots submenu (last 5)
- Open last screenshot / Copy file path / Reveal in Explorer
- Launch at startup toggle (Registry `HKCU\...\Run\OpenSnap`)
- Quit

## C# build quirks

- `UseWindowsForms=true` causes ambiguous references (`Point`, `Color`,
  `Clipboard`, `MouseEventArgs`, `KeyEventArgs`, `BitmapFrame`,
  `BitmapDecoder`) — use fully qualified names in new code.
- WPF `Border` accepts only one child — wrap in `Grid` for multiple layers.
- `DragMove()` is blocking — use `PreviewMouseMove` with threshold for
  click-vs-drag detection.
- Embedded resource names follow `{RootNamespace}.{folder}.{file}` pattern.
- Setting `SelectedIndex` on an empty `ComboBox` in XAML throws
  `ArgumentException` — populate items in code-behind first.
- `SelectionChanged` fires during `InitializeComponent()` if `SelectedIndex` is
  set in XAML — guard handlers with a `_suppressToggle` flag.

## Release workflow

```bash
# Full release pipeline
rsync -a . /mnt/c/Users/spars/repos/opensnap/ --exclude=.git --exclude=bin --exclude=obj
/mnt/c/Windows/System32/cmd.exe /c "C:\tmp\publish-opensnap.bat"   # dotnet restore + publish
/mnt/c/Windows/System32/cmd.exe /c "C:\tmp\createshortcut.ps1"     # update desktop .lnk
/mnt/c/Program Files (x86)\Inno Setup 6\ISCC.exe setup.iss         # build installer

# Commit & tag
git add -A
git commit -m "description"
git tag -f v0.x.x
git push origin main --tags

# GitHub release (requires gh CLI)
gh release create v0.x.x --title "v0.x.x — Title" --notes-file RELEASE_NOTES.md
gh release upload v0.x.x dist/OpenSnap-Setup-v0.x.x.exe
```

## Release history

| Tag | Highlights |
|---|---|
| v0.1.1 | Initial build, basic pill, tray, settings |
| v0.1.2 | Camera centering, glass UI, Desktop save, framework-dependent publish |
| v0.2.0 | Capture modes, area selection, settings dialog |
| v0.4.0 | Global hotkeys, history, capture sound, filename templates |
| v0.5.0 | Windows OCR (Capture + OCR), renamed from OpenShot |
| v0.5.1 | Stabilisation, naming cleanup, README rewrite |
| v0.6.0 | Inno Setup installer, hotkey config in Settings, About dialog, area selection fix, active window fix, settings crash fix |
| v0.7.0 | Triple-button pill (240px), bounce animation, green flash, per-section hover + toggle, exit menu, v0.7 fix pass, docs |
