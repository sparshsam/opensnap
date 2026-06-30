# OpenSnap

C# WPF screenshot widget for Windows. `net8.0-windows10.0.19041.0`.

**GitHub:** https://github.com/sparshsam/opensnap
**Latest tag:** v1.0.0
**Stable branch:** `stable`

---

## Current state — v1.0.0 release ready

The app is feature-complete for v1.0.0. The MSIX (`OpenSnap-0.9.5.msix`) is on the desktop
ready for Partner Center upload. The Inno Setup installer is on the GitHub Release page.

### What's shipped this session

| Version | Highlights |
|---|---|
| **v0.7.0** | 240px triple-button pill, bounce animation, green flash (#007a3f), edge snapping, opacity slider, auto-hide on fullscreen, DPI PerMonitorV2, multi-monitor area selection fix, exception logging |
| **v0.8.0** | Post-capture quick actions popup (Copy/OCR/Paint/Reveal), project prefix `{prefix}`, sequential numbering `{seq}`, date subfolders, history pin/delete/clear, custom app path |
| **v0.9.0** | Per-user/per-machine installer, silent install (/VERYSILENT), background update download + one-click install, changelog viewer, diagnostics page, log export, MSIX packaging config |
| **v0.9.5** | Keyboard nav (arrows + Enter), screen reader labels (AutomationProperties), High Contrast mode, larger icons toggle, 5-language localization (EN/FR/DE/ES/JA), app icon + theme-aware README logo |
| **v1.0.0 prep** | Issue templates, SECURITY.md, CODE_OF_CONDUCT.md, FAQ, troubleshooting guide, stable branch, premium README |

### Remaining for v1.0.0 (needs Windows runtime)

- [ ] Run QA checklist (docs/Testing.md) — no known crashes
- [ ] Verify installer, uninstaller, startup, OCR, clipboard, multi-monitor, DPI
- [ ] Tag v1.0.0 and create GitHub release
- [ ] Upload MSIX to Partner Center
- [ ] Add real screenshots to `assets/screenshots/`
- [ ] Upload Inno Setup installer to release

---

## Key files

| File | Purpose |
|---|---|
| `MainWindow.xaml/.cs` | 240×36 floating glass pill, 3 clickable sections |
| `App.xaml.cs` | Entry point, capture dispatch, fullscreen monitor, tray wiring |
| `ScreenshotService.cs` | Full desktop capture, PNG save, clipboard, filename generation |
| `CaptureService.cs` | Active window + area capture, P/Invoke |
| `OcrService.cs` | `Windows.Media.Ocr` wrapper |
| `UpdateService.cs` | GitHub release checker + installer download |
| `LocalizationService.cs` | JSON-based string lookup, 5 languages |
| `TrayService.cs` | System tray icon, context menu, history submenu, update notifications |
| `CapturePopup.xaml/.cs` | Post-capture quick actions popup (auto-closes 5s) |
| `AppSettings.cs` | JSON settings → `%APPDATA%\OpenSnap\settings.json` |
| `HotkeyService.cs` | Global hotkeys via RegisterHotKey Win32 API |
| `StartupManager.cs` | Registry Run key management |
| `AreaSelectorWindow.xaml/.cs` | Fullscreen drag-select overlay |
| `SettingsWindow.xaml/.cs` | Settings dialog (all options) |
| `AboutDialog.xaml/.cs` | About / Changelog / Diagnostics tabs |
| `setup.iss` | Inno Setup installer config |
| `package-msix.ps1` | MSIX package builder |
| `Resources/Lang/*.json` | Translation files (en/fr/de/es/ja) |
| `assets/branding/` | Logo SVGs/PNGs, brand assets |

## Settings fields

| Field | Default | Description |
|---|---|---|
| `SavePath` | Desktop | Screenshot folder |
| `Opacity` | 1.0 | Widget opacity (0.2–1.0) |
| `AlwaysOnTop` | true | Stay above other windows |
| `EdgeSnapEnabled` | true | Magnetic edge snapping |
| `AutoHideFullscreen` | false | Hide when fullscreen app is active |
| `ShowQuickActions` | true | Show popup after capture |
| `LargeIcons` | false | Larger section icons |
| `Language` | "en" | UI language (en/fr/de/es/ja) |
| `ProjectPrefix` | "" | `{prefix}` token value |
| `UseSequentialNumbering` | false | Auto-increment `{seq}` counter |
| `DateSubfolders` | false | Save to `yyyy/MM-yyyy/` |
| `CustomAppPath` | "" | Path to external image editor |
| `PinnedCaptures` | [] | Pinned screenshot paths |
| `ScreenshotHistory` | [] | Last 20 file paths |
| `PlayCaptureSound` | true | Shutter sound on capture |
| `FilenameTemplate` | `screenshot-{yyyy}-{MM}-{dd}-{HHmmss}` | Template with `{yyyy}`, `{MM}`, `{dd}`, `{HH}`, `{mm}`, `{ss}`, `{HHmmss}`, `{seq}`, `{prefix}` |

## Capture modes

| Method | Trigger |
|---|---|
| Full screen | Center button, Win+Shift+S, tray "Capture" |
| Active window | Right button, Win+Shift+W, middle-click |
| Area selection | Left button (toggle) |
| Capture + OCR | Right-click menu → Capture + OCR |

## Conventions

- `UseWindowsForms=true` → fully qualify `Brush`, `Color`, `MouseEventArgs`, `KeyEventArgs`, `Clipboard`, `MessageBox`, `SystemColors`, `DpiChangedEventArgs`
- WPF `Border` accepts one child → wrap in `Grid` for multiple layers
- `DragMove()` is blocking → use `PreviewMouseMove` with threshold
- Embedded resource names: `{RootNamespace}.{folder}.{file}`
- `BeginAnimation` on `ScaleTransform` must target the **instance**, not the element

## Build & publish

```cmd
dotnet build -c Release
dotnet publish -c Release -o release
build-installer.bat                    # Inno Setup
.\package-msix.ps1 -Version 0.9.5     # MSIX (requires Windows SDK)
```

## Release workflow

```bash
git tag v1.0.0 && git push origin v1.0.0
gh release create v1.0.0 --title "v1.0.0 — Stable Release" --notes-file CHANGELOG.md
gh release upload v1.0.0 dist/OpenSnap-Setup-v1.0.0.exe
```

Then upload MSIX to Partner Center.

## Docs

| File | Contents |
|---|---|
| `docs/Development.md` | Build, project structure, conventions |
| `docs/Architecture.md` | Layers, capture pipeline, services |
| `docs/Deployment.md` | Publish, installer, MSIX, release workflow |
| `docs/Testing.md` | Manual QA checklist |
| `docs/Contributing.md` | Contribution guidelines |
| `docs/FAQ.md` | Frequently asked questions |
| `docs/Troubleshooting.md` | Common issues and fixes |

## Repository structure

```
opensnap/
├── assets/branding/        # Logo SVGs, PNGs, brand assets
├── assets/hero/            # README hero graphic
├── assets/icons/           # App icons
├── assets/screenshots/     # Product screenshots (add as needed)
├── docs/                   # Developer documentation
├── .github/ISSUE_TEMPLATE/ # Bug report + feature request templates
├── Resources/Lang/         # JSON translation files
├── *.xaml / *.cs           # WPF app source
├── *.ps1                   # Build and packaging scripts
├── setup.iss               # Inno Setup installer config
├── README.md               # Premium landing page
├── CHANGELOG.md            # Full version history
├── CLAUDE.md               # AI project context (this file)
├── SECURITY.md             # Security policy
└── CODE_OF_CONDUCT.md      # Code of conduct
```
