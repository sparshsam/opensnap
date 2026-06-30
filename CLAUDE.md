# OpenSnap

C# WPF screenshot widget for Windows. .NET 8, `net8.0-windows10.0.19041.0`.

## Key files

| File | Purpose |
|---|---|
| `MainWindow.xaml/.cs` | 240×36 floating glass pill, 3 sections |
| `App.xaml.cs` | Entry point, capture dispatch, tray wiring |
| `ScreenshotService.cs` | Full desktop capture, save, clipboard |
| `CaptureService.cs` | Active window + area capture |
| `OcrService.cs` | Windows.Media.Ocr wrapper |
| `UpdateService.cs` | GitHub release checker |
| `LocalizationService.cs` | JSON-based translations (5 languages) |
| `TrayService.cs` | System tray icon + history menu |
| `CapturePopup.xaml/.cs` | Post-capture quick actions popup |
| `AppSettings.cs` | JSON settings at `%APPDATA%\OpenSnap\` |
| `setup.iss` | Inno Setup installer config |
| `package-msix.ps1` | MSIX build script |
| `Resources/Lang/*.json` | Translation files (en/fr/de/es/ja) |

## Build

```cmd
dotnet build -c Release
```

## Conventions

- `UseWindowsForms=true` → fully qualify `Brush`, `Color`, `MouseEventArgs`, `KeyEventArgs`, `Clipboard`, `MessageBox`
- WPF `Border` accepts one child → wrap in `Grid`
- `DragMove()` blocking → use `PreviewMouseMove` with threshold
- Embedded resources: `OpenSnap.Resources.{file}`

## Docs

See `docs/` for development, architecture, deployment, testing, and contributing guides.
