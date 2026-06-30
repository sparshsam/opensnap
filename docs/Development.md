# Development

## Prerequisites

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (for installer builds)

## Build

```cmd
cd C:\Users\spars\repos\opensnap
dotnet restore
dotnet build -c Release
```

Output: `bin\Release\net8.0-windows10.0.19041.0\OpenSnap.dll`

## Run

```cmd
dotnet run -c Release
```

Double-click `OpenSnap.lnk` on the desktop after first build.

## Project structure

```
opensnap/
├── OpenShot.csproj              # .NET 8 WPF + WinRT project
├── App.xaml / .cs               # Entry point, capture dispatch, tray wiring
├── MainWindow.xaml / .cs        # Floating glass widget
├── AppSettings.cs               # JSON settings persistence
├── CapturePopup.xaml / .cs      # Post-capture quick actions popup
├── ScreenshotService.cs         # Capture, save, clipboard, filename template
├── CaptureService.cs            # Active window + area capture
├── OcrService.cs                # Windows OCR text extraction
├── UpdateService.cs             # GitHub release checker + downloader
├── LocalizationService.cs       # JSON-based language system
├── HotkeyService.cs             # Global hotkeys (RegisterHotKey)
├── TrayService.cs               # System tray icon + context menu
├── StartupManager.cs            # Registry Run key management
├── AreaSelectorWindow           # Fullscreen drag-select overlay
├── SettingsWindow               # Settings dialog
├── AboutDialog                  # About / changelog / diagnostics
├── Resources/app.ico            # Application icon
├── Resources/capture.wav        # Shutter sound
├── Resources/Lang/              # JSON translation files
├── assets/                      # Repository branding assets
├── docs/                        # Developer documentation
├── setup.iss                    # Inno Setup installer script
├── build-installer.bat          # Build + installer packaging
├── package-msix.ps1             # MSIX package builder
└── create-desktop-shortcut.ps1  # Desktop .lnk generator
```

## Key conventions

- **Namespace:** `OpenSnap`
- **Assembly name:** `OpenSnap`
- **Target framework:** `net8.0-windows10.0.19041.0`
- **Settings path:** `%APPDATA%\OpenSnap\settings.json`
- **Default save path:** Desktop

### C# build quirks

`UseWindowsForms=true` causes ambiguous references (`Point`, `Color`, `Clipboard`, `MouseEventArgs`, `KeyEventArgs`, `BitmapFrame`, `BitmapDecoder`) — use fully qualified names in new code.

WPF `Border` accepts only one child — wrap in `Grid` for multiple layers.

`DragMove()` is blocking — use `PreviewMouseMove` with threshold for click-vs-drag detection.

Embedded resource names follow `{RootNamespace}.{folder}.{file}` pattern.
