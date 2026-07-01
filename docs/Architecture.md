# Architecture

## Overview

OpenSnap is a Windows Presentation Foundation (WPF) application targeting .NET 8 with Windows 10 SDK. It uses WinRT interop for `Windows.Media.Ocr` and WinForms for the system tray icon and folder browser.

## Layers

### Widget (MainWindow)
The floating glass capsule with three clickable sections. Uses WPF for rendering, Win32 interop for global hotkeys and foreground window detection. Drag-move via `PreviewMouseMove` with threshold-based click detection.

### Capture Pipeline
```
User clicks section → Bounce animation → CaptureRequested event
    → App.cs dispatches CaptureService / ScreenshotService
    → BitmapSource → SaveAsPng + CopyToClipboard
    → CapturePopup (optional quick actions)
    → TrayService.UpdateHistory
```

### Services

| Service | Responsibility |
|---------|---------------|
| `ScreenshotService` | Full desktop capture, PNG save, clipboard copy, filename generation |
| `CaptureService` | Active window capture, area capture, P/Invoke for `GetForegroundWindow` |
| `OcrService` | `Windows.Media.Ocr.OcrEngine` wrapper |
| `HotkeyService` | `RegisterHotKey` Win32 API management |
| `TrayService` | `NotifyIcon` with context menu, history submenu |
| `UpdateService` | GitHub Releases API check, installer download |
| `LocalizationService` | JSON-based string lookup, 5 languages |

### Settings
Persisted as JSON at `%APPDATA%\OpenSnap\settings.json`. Written immediately on change via `AppSettings.Save()`.

## Threading
- UI thread: WPF, WinForms tray, all capture operations (GDI+ requires STA)
- Background: HTTP calls (update check, download)
- `async void` for event handlers (fire-and-forget with exception handling)

## Packaging
- **Microsoft Store**: Primary distribution channel. Signed MSIX with automatic updates.
- **Inno Setup** (`setup.iss`): Per-user or per-machine install, silent support
- **MSIX** (`package-msix.ps1`): Standalone sideloading package (unsigned), requires Windows SDK MakeAppx.exe
