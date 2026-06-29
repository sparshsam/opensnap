# OpenSnap

A minimal, always-on-top desktop screenshot widget for Windows.

**Stack:** C# WPF on .NET 8, Windows-only desktop app
**Repo:** https://github.com/sparshsam/opensnap
**Latest tag:** v0.5.1

---

## Project structure

```
/home/spars/repos/opensnap/           # Git repo root
├── OpenShot.csproj                   # .NET 8 WPF + WinRT project
├── App.xaml / .cs                    # Entry point, capture dispatch, tray wiring
├── MainWindow.xaml / .cs             # Floating glass widget (80×36 pill)
├── AppSettings.cs                    # JSON settings → %APPDATA%\OpenSnap\
├── ScreenshotService.cs              # Capture, save, clipboard, filename template
├── CaptureService.cs                 # Active window + area capture + CaptureMode enum
├── OcrService.cs                     # Windows.Media.Ocr text extraction
├── HotkeyService.cs                  # Global hotkeys (RegisterHotKey Win32 API)
├── TrayService.cs                    # System tray icon + context menu
├── StartupManager.cs                 # Registry Run key management
├── AreaSelectorWindow.xaml / .cs     # Fullscreen drag-select overlay
├── SettingsWindow.xaml / .cs         # Settings dialog
├── Resources/app.ico                 # Application icon
├── Resources/capture.wav             # Camera shutter sound
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

# Output: bin/Release/net8.0-windows10.0.19041.0/OpenSnap.dll
# Published: release/OpenSnap.exe
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

- 80×36 px translucent glass capsule with drop shadow
- Camera icon centred as the visual mark
- Always-on-top, draggable via threshold-based click-vs-drag

## Input reference

| Input | Action |
|---|---|
| Left-click | Capture full screen |
| Right-click | Open mode context menu |
| Middle-click | Capture active window |
| Drag | Move widget |
| Win+Shift+S | Capture full screen (global hotkey) |
| Win+Shift+W | Capture active window (global hotkey) |

## Capture modes (right-click menu)

1. **Full screen** — all monitors, via `Graphics.CopyFromScreen`
2. **Active window** — foreground window via `GetForegroundWindow` + `GetWindowRect`
3. **Area selection** — fullscreen transparent overlay, drag to select
4. **Capture + OCR** — full screen + `Windows.Media.Ocr.OcrEngine` → clipboard
5. **Settings** — save path, always-on-top, startup, sound, filename template

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

## Release workflow

```bash
# Quick cycle (WSL dev → Windows build)
rsync -a . /mnt/c/Users/spars/repos/opensnap/ --exclude=.git --exclude=bin --exclude=obj
/mnt/c/Windows/System32/cmd.exe /c "C:\tmp\publish-opensnap.bat"
/mnt/c/Windows/System32/cmd.exe /c "powershell.exe -ExecutionPolicy Bypass -File C:\tmp\createshortcut.ps1"

# Commit & tag
git add -A
git commit -m "description"
git tag -f v0.x.x
git push origin main --tags
```

The batch file at `C:\tmp\publish-opensnap.bat` handles dotnet restore + publish.
The PowerShell script at `C:\tmp\createshortcut.ps1` updates the desktop `.lnk`.

## Release history

| Tag | Highlights |
|---|---|
| v0.1.1 | Initial build, basic pill, tray, settings |
| v0.1.2 | Camera centering, glass UI, Desktop save, framework-dependent publish |
| v0.2.0 | Capture modes, area selection, settings dialog |
| v0.4.0 | Global hotkeys, history, capture sound, filename templates |
| v0.5.0 | Windows OCR (Capture + OCR), renamed from OpenShot |
| v0.5.1 | Stabilisation, naming cleanup, README rewrite |
