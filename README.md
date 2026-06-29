# OpenSnap

A minimal, always-on-top desktop screenshot widget for Windows.

Click anywhere on the glass capsule to grab the full multi-monitor desktop,
save it as PNG to your Desktop, and copy it to your clipboard — all in one
click. Or right-click for capture modes including OCR text extraction.

## Features

- Compact glass capsule (~80 × 36 px), draggable anywhere
- Subtle drop shadow, translucent frosted-glass appearance
- Always-on-top
- Full multi-monitor desktop capture
- Active window capture (foreground window only)
- Area selection (fullscreen drag-select overlay)
- **Capture + OCR** — extracts text via built-in Windows OCR, copies to clipboard
- Saves to **Desktop** by default (configurable)
- Auto-generates filenames with customisable template
- Copies image to clipboard automatically (retries 3× if locked)
- Copies OCR text to clipboard automatically
- Global hotkeys: Win+Shift+S (full screen), Win+Shift+W (active window)
- Screenshot history (last 20) in tray menu
- Optional capture shutter sound
- System tray icon with context menu
- Launch at startup toggle
- Settings persist to `%APPDATA%\OpenSnap\settings.json`
- Startup position clamped to visible work area
- Error handling for missing folders, clipboard issues, capture failures

## Input reference

| Input | Action |
|---|---|
| **Left-click** | Capture full screen |
| **Right-click** | Open capture mode menu |
| **Middle-click** | Capture active window |
| **Double-click** | Suppressed (avoids double-save) |
| **Drag** | Move widget |
| **Win+Shift+S** | Capture full screen (global) |
| **Win+Shift+W** | Capture active window (global) |

Right-click menu: Full screen / Active window / Area selection / Capture + OCR / Settings

## Prerequisites

- Windows 10 / 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed

## Build

```cmd
cd C:\Users\spars\repos\openshot
dotnet restore
dotnet build -c Release
```

## Publish

```cmd
dotnet publish -c Release -o release
```

Output: `release\OpenSnap.exe` (~170 KB, framework-dependent, instant launch).

## Run

Double-click **OpenSnap.lnk** on the desktop, or run `dotnet run -c Release`.

## Startup behavior

When **Launch at startup** is toggled on (via the tray menu), OpenSnap
registers itself under:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\OpenSnap
```

The app starts automatically on Windows login. It lives in the system tray.

## Settings

Stored at `%APPDATA%\OpenSnap\settings.json`. Configurable fields:

| Field | Default | Description |
|---|---|---|
| `SavePath` | Desktop | Folder for screenshots |
| `AlwaysOnTop` | true | Widget stays above other windows |
| `LaunchAtStartup` | false | Auto-start with Windows |
| `PlayCaptureSound` | true | Play shutter sound on capture |
| `FilenameTemplate` | `screenshot-{yyyy}-{MM}-{dd}-{HHmmss}` | Template with `{yyyy}`, `{MM}`, `{dd}`, `{HH}`, `{mm}`, `{ss}`, `{HHmmss}` |
| `ScreenshotHistory` | [] | Last 20 saved file paths |

## Project structure

```
openshot/                        # Git repository root (named from before rename)
├── OpenSnap.csproj              # .NET 8 WPF + WinRT project
├── App.xaml / .cs               # Entry point, capture dispatch, tray wiring
├── MainWindow.xaml / .cs        # Floating glass widget
├── AppSettings.cs               # JSON settings persistence
├── ScreenshotService.cs         # Capture, save, clipboard, filename template
├── CaptureService.cs            # Active window + area capture
├── OcrService.cs                # Windows OCR text extraction
├── HotkeyService.cs             # Global hotkeys (RegisterHotKey)
├── TrayService.cs               # System tray icon + context menu
├── StartupManager.cs            # Registry Run key management
├── AreaSelectorWindow           # Fullscreen drag-select overlay
├── SettingsWindow               # Settings dialog
├── Resources/app.ico            # Application icon
├── Resources/capture.wav        # Shutter sound
├── README.md
└── .gitignore
```

> Note: The repo folder is still named `openshot` (from the original project
> name). This is cosmetic only — the app itself is fully branded as OpenSnap.
> Renaming the folder would break the git remote. All assembly names,
> namespaces, display strings, settings paths, and shortcuts use **OpenSnap**.

## QA checklist (v0.5.1)

- [ ] Full screen capture saves PNG to Desktop
- [ ] Active window capture captures only the focused window
- [ ] Area selection overlay appears, drag works, Escape cancels
- [ ] Capture + OCR saves image AND copies extracted text
- [ ] OCR text pastes correctly into Notepad / Word
- [ ] Clipboard image pastes into Paint / chat app
- [ ] Recent screenshots submenu shows last 5 entries
- [ ] Open last screenshot opens the file
- [ ] Copy file path copies full path to clipboard
- [ ] Reveal in Explorer opens folder with file selected
- [ ] Startup toggle writes HKCU registry key
- [ ] Widget reopens after login when startup is enabled
- [ ] No duplicate tray icons after multiple launches
- [ ] Double-click does not create duplicate saves
- [ ] Widget position persists across restart
- [ ] Off-screen widget resets to visible area
- [ ] Tray icon shows OpenSnap icon
- [ ] Published exe is named `OpenSnap.exe`
- [ ] Desktop shortcut is `OpenSnap.lnk` (no .bat file)
- [ ] Global hotkeys Win+Shift+S and Win+Shift+W work from any app

## Release history

| Tag | Date | Highlights |
|---|---|---|
| v0.1.1 | — | Initial build, basic pill widget, tray icon, settings |
| v0.1.2 | — | Camera centering, Desktop save, glass UI, rectangular backing fix |
| v0.2.0 | — | Capture modes (active window, area selection), settings dialog |
| v0.4.0 | — | Global hotkeys, middle-click, history, capture sound, filename templates |
| v0.5.0 | — | Windows OCR (Capture + OCR), renamed OpenShot → OpenSnap |
| **v0.5.1** | **today** | **Stabilisation: naming cleanup, README rewrite, packaging verified** |
