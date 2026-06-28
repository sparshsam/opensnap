# OpenShot

A minimal, always-on-top desktop screenshot widget for Windows.

Click **Capture** to grab the full multi-monitor desktop, save it as PNG to a
configurable folder, and copy it to your clipboard — all in one click.

## Features

- Borderless floating widget — draggable anywhere on screen
- Always-on-top (configurable in settings)
- Full multi-monitor desktop capture
- Saves to `Pictures/Screenshots/OpenShot/` by default
- Auto-generates filenames: `screenshot-yyyy-MM-dd-HHmmss.png`
- Copies the captured image to clipboard automatically
- System tray icon with context menu:
  - Capture
  - Open screenshots folder
  - Change save folder…
  - Quit
- Settings persisted to `%APPDATA%/OpenShot/settings.json`
- Visual flash feedback on capture
- Error handling for missing folders, clipboard issues, capture failures

## Prerequisites

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
  (the **Desktop development with .NET** workload — Visual Studio or
  `dotnet workload install`)

## Build & Run

```cmd
:: From the repository root
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

Or open the project folder in Visual Studio and press **F5**.

## Usage

1. The widget appears as a small floating bar. Drag it by the title area to
   reposition.
2. Click **Capture** (or double-click the tray icon) to take a screenshot.
3. The image is saved to the configured folder and copied to your clipboard —
   paste it anywhere with Ctrl+V.
4. Right-click the tray icon to change the save folder, open the folder, or quit.

## Configuration

Settings are stored at:

```
%APPDATA%\OpenShot\settings.json
```

Fields: `SavePath`, `WindowLeft`, `WindowTop`, `AlwaysOnTop`.

## Project Structure

```
OpenShot/
├── OpenShot.csproj        — .NET 8 Windows WPF project
├── App.xaml / .cs         — Application entry point, lifecycle, tray wiring
├── MainWindow.xaml / .cs  — Floating widget window
├── AppSettings.cs         — JSON settings persistence
├── ScreenshotService.cs   — Capture, save, clipboard logic
├── TrayService.cs         — System tray icon + context menu
├── Resources/app.ico      — Application icon
├── README.md
└── .gitignore
```
