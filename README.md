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

## Installation

### Download
Download the latest installer from the [Releases](https://github.com/sparshsam/opensnap/releases) page:

```
OpenSnap-Setup-v0.6.0.exe
```

### Install
1. Run `OpenSnap-Setup-v0.6.0.exe`
2. Choose whether to create a desktop shortcut
3. Launch OpenSnap from the Start Menu or desktop shortcut

The app is installed to `%LOCALAPPDATA%\Programs\OpenSnap`.  
Settings are stored separately at `%APPDATA%\OpenSnap\settings.json` and are never touched during upgrades.

### Upgrade
The installer replaces all program files. Your settings (`%APPDATA%\OpenSnap\settings.json`) are preserved automatically — no migration step needed. Just run the new installer over the old installation.

### Uninstall
**Via Settings → Apps:**
1. Open **Settings → Apps → Installed apps**
2. Search for **OpenSnap**
3. Click **Uninstall**

**Via Start Menu:**
1. Open the Start Menu and find **OpenSnap**
2. Right-click → **Uninstall**

Running the installer again also gives the option to remove OpenSnap.

### Troubleshooting

| Problem | Fix |
|---------|-----|
| "Windows protected your PC" (SmartScreen) | Click **More info → Run anyway**. The installer is signed by the developer certificate. |
| App won't start | Ensure [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed. |
| Settings lost after upgrade | Settings are stored in `%APPDATA%\OpenSnap\` and are never deleted by the installer. If missing, check that directory still exists. |
| OCR not working | OCR uses Windows language packs. Go to **Settings → Time & Language → Language & region** and install your language's speech/OCR support. |

## Build

```cmd
cd C:\Users\spars\repos\opensnap
dotnet restore
dotnet build -c Release
```

## Publish & package

```cmd
dotnet publish -c Release -o release
```

Output: `release\OpenSnap.exe` (~170 KB, framework-dependent, instant launch).

### Build installer

```cmd
build-installer.bat
```

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php) installed at `C:\Program Files (x86)\Inno Setup 6`.

Output: `dist\OpenSnap-Setup-v0.6.0.exe` (~5.5 MB).

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
opensnap/                        # Git repository root
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
├── SettingsWindow               # Settings dialog (hotkeys, sound, template)
├── AboutDialog                  # About dialog (version, GitHub, license)
├── Resources/app.ico            # Application icon
├── Resources/capture.wav        # Shutter sound
├── setup.iss                    # Inno Setup installer script
├── build-installer.bat          # Build + installer packaging script
├── README.md
└── .gitignore
```



## QA checklist (v0.6.0)

- [ ] Installer runs and installs to `%LOCALAPPDATA%\Programs\OpenSnap`
- [ ] Start Menu shortcut created
- [ ] Desktop shortcut created (when selected)
- [ ] OpenSnap launches from Start Menu
- [ ] Uninstall removes all program files
- [ ] Settings preserved after reinstall (upgrade)
- [ ] Settings → About shows correct version and GitHub link
- [ ] Settings → Hotkey combos change saved and applied
- [ ] Play capture sound toggle works
- [ ] Full screen capture saves PNG to Desktop
- [ ] Active window capture captures only the focused window (not the widget)
- [ ] Area selection overlay captures the selected region
- [ ] Capture + OCR saves image AND copies extracted text

## Release history

| Tag | Date | Highlights |
|---|---|---|
| v0.1.1 | — | Initial build, basic pill widget, tray icon, settings |
| v0.1.2 | — | Camera centering, Desktop save, glass UI, rectangular backing fix |
| v0.2.0 | — | Capture modes (active window, area selection), settings dialog |
| v0.4.0 | — | Global hotkeys, middle-click, history, capture sound, filename templates |
| v0.5.0 | — | Windows OCR (Capture + OCR), renamed OpenShot → OpenSnap |
| **v0.5.1** | — | **Stabilisation: naming cleanup, README rewrite, packaging verified** |
| **v0.6.0** | — | **Installer & distribution: Inno Setup packaging, About dialog, hotkey config in settings, area selection fix, active window fix** |
