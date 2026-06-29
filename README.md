# OpenShot

A minimal, always-on-top desktop screenshot widget for Windows.

Click anywhere on the dark capsule to grab the full multi-monitor desktop, save it as PNG
to your Desktop, and copy it to your clipboard — all in one click.

## Features

- Compact dark capsule (~80 × 36 px), single unified action surface  
- Subtle drop shadow, clean capsule design  
- Always-on-top
- Full multi-monitor desktop capture
- Saves to **Desktop** by default (configurable)
- Auto-generates filenames: `screenshot-yyyy-MM-dd-HHmmss.png`
- Copies to clipboard automatically (retries 3× if locked)
- System tray icon (hidden in the taskbar overflow area) with context menu:
  - Capture
  - Open Desktop folder
  - Change save folder…
  - Launch at startup (toggle)
  - Quit
- Settings persisted to `%APPDATA%/OpenShot/settings.json`
- Startup position clamped to visible work area (handles monitor changes)
- Error handling for missing folders, clipboard issues, capture failures

## Prerequisites

- Windows 10 / 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or SDK installed on the system

## Build

```cmd
cd C:\Users\spars\repos\openshot
dotnet restore
dotnet build -c Release
```

## Publish (framework-dependent — fast, small)

```cmd
dotnet publish -c Release -o release
```

The output `release\OpenShot.exe` is ~170 KB and launches instantly. Requires
the .NET 8 runtime to be installed on the target machine (not bundled).

## Run

Double-click `OpenShot.lnk` on the desktop, or run:

```cmd
dotnet run -c Release

## Startup behavior

When **Launch at startup** is toggled on (via the tray menu), OpenShot
registers itself under:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\OpenShot
```

The app starts automatically on Windows login. It lives in the system tray —
the floating widget appears on screen; closing the widget keeps the app running
in the tray. Use the **Quit** tray item to fully exit.

## Test checklist

- [ ] Widget drags smoothly across the screen
- [ ] Widget stays on top of all other windows
- [ ] Click anywhere on the capsule → screenshot captured  
- [ ] Drag smoothly from anywhere on the capsule  
- [ ] Screenshot saved to Desktop as `screenshot-YYYY-MM-DD-HHmmss.png`
- [ ] Ctrl+V pastes the screenshot into an app (Paint, Word, chat)
- [ ] Tray icon visible in system tray overflow area
- [ ] Double-click tray icon → captures screenshot
- [ ] Tray menu → Capture works
- [ ] Tray menu → Open Desktop folder opens Explorer
- [ ] Tray menu → Change save folder persists across restarts
- [ ] Tray menu → Launch at startup toggle (check Regedit)
- [ ] Tray menu → Quit exits fully (no tray residue)
- [ ] Widget position persists across restart
- [ ] Widget clamped to visible area if monitor layout changed
- [ ] Default save path is C:\Users\spars\Desktop
- [ ] Widget renders correctly at 100%, 125%, and 150% Windows scaling
- [ ] Drop shadow renders correctly on light and dark desktop backgrounds
- [ ] App icon shows openshot camera icon in taskbar/tray/title bar
- [ ] Icon renders correctly on the widget at all scaling levels

## Project Structure

```
OpenShot/
├── OpenShot.csproj            — .NET 8 Windows WPF project
├── App.xaml / .cs             — Entry point, lifecycle, tray wiring
├── MainWindow.xaml / .cs      — Floating pill widget
├── AppSettings.cs             — JSON settings persistence
├── ScreenshotService.cs       — Capture, save, clipboard (with retry)
├── StartupManager.cs          — Registry Run key management
├── TrayService.cs             — System tray icon + context menu
├── Resources/app.ico          — Application icon
├── README.md
└── .gitignore
```
