# OpenSnap

C# WPF screenshot widget for Windows. `net8.0-windows10.0.19041.0`.

**GitHub:** https://github.com/sparshsam/opensnap
**Latest tag:** v1.0.1
**Stable branch:** `stable`

---

## Current state — v1.0.1 in certification pipeline

The app is feature-complete. v1.0.1 MSIX replaces the previous v1.0.0 submission
with proper high-quality visual assets and self-contained .NET runtime packaging.
Landing page at **snap.kovina.org**.

### What's shipped this session

| Version | Highlights |
|---|---|
| **v0.7.0** | 240px triple-button pill, bounce animation, green flash (#007a3f), edge snapping, opacity slider, auto-hide on fullscreen, DPI PerMonitorV2, multi-monitor area selection fix, exception logging |
| **v0.8.0** | Post-capture quick actions popup (Copy/OCR/Paint/Reveal), project prefix `{prefix}`, sequential numbering `{seq}`, date subfolders, history pin/delete/clear, custom app path |
| **v0.9.0** | Per-user/per-machine installer, silent install (/VERYSILENT), background update download + one-click install, changelog viewer, diagnostics page, log export, MSIX packaging config |
| **v0.9.5** | Keyboard nav (arrows + Enter), screen reader labels (AutomationProperties), High Contrast mode, larger icons toggle, 5-language localization (EN/FR/DE/ES/JA), app icon + theme-aware README logo |
| **v0.9.6** | Press animation, startup mutex, save-only mode, history search dialog, improved active-window detection |
| **v0.9.9** | Dead code cleanup, benchmark instrumentation, logging review, single-instance enforcement |
| **v1.0.0** | Stable release — GitHub release, annotated tag, signed MSIX in Partner Center, landing page at snap.kovina.org |
| **v1.0.1** | All MSIX visual assets regenerated from 1024×1024 masters (opensnap_dark_mode.png / opensnap_light_mode.png) using Pillow LANCZOS. 66 assets across 11 logo types × 6 scale variants (scale-100/125/150/200/400) plus SplashScreen and 6-size app.ico. Updated Package.appxmanifest with DefaultTile (Wide310x150, Square71x71, Square310x310) and SplashScreen. Built self-contained win-x64 MSIX (74.88 MB). |

### Next steps

- [ ] Submit OpenSnap-1.0.1.msix to Microsoft Partner Center
- [ ] Promote MSIX to available in Store
- [ ] Upload Inno Setup installer to GitHub Release
- [ ] Update landing page with Store badge/link

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
| `package-msix.ps1` | MSIX package builder (uses pre-generated Assets/) |
| `Assets/*.png` | 66 pre-generated MSIX store assets (11 logos × 6 scale variants) |
| `Resources/Lang/*.json` | Translation files (en/fr/de/es/ja) |
| `assets/branding/` | Brand assets (logos, screenshots) |

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
dotnet clean                       # Clean stale obj artifacts first
dotnet build -c Release
dotnet publish -c Release -o release                    # Framework-dependent
dotnet publish -c Release -r win-x64 --self-contained true -o msix-release   # Self-contained
# Package MSIX from self-contained output:
powershell -ExecutionPolicy Bypass -File C:\tmp\msix-pack.ps1 -Version 1.0.1
build-installer.bat                # Inno Setup
.\package-msix.ps1 -Version 1.0.1  # Legacy MSIX (uses release/)
```

## Visual assets

Masters (do NOT modify):
- `C:\Users\spars\OneDrive\Kovina\Apps Stuff\opensnap\opensnap_dark_mode.png` — light icon on black bg
- `C:\Users\spars\OneDrive\Kovina\Apps Stuff\opensnap\opensnap_light_mode.png` — dark icon on light bg

Regenerate all MSIX assets with:
```bash
python3 "C:\Users\spars\OneDrive\Kovina\Apps Stuff\opensnap\generate_assets.py"
```
Outputs 264 files (66 per theme × 4 themes) to `WindowsAssets/`. Copies composite
(transparent-bg, universal) set to repo `Assets/`.

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
├── Assets/                 # 66 pre-generated MSIX store assets (composite set)
├── assets/branding/        # Logo SVGs, PNGs, brand assets (README use)
├── assets/screenshots/     # Product screenshots (add as needed)
├── docs/                   # Developer documentation
├── docs/landing/           # Landing page (snap.kovina.org)
├── .github/ISSUE_TEMPLATE/ # Bug report + feature request templates
├── Resources/Lang/         # JSON translation files
├── Resources/app.ico       # App icon (6-size .ico from master)
├── *.xaml / *.cs           # WPF app source
├── *.ps1                   # Build and packaging scripts
├── setup.iss               # Inno Setup installer config
├── README.md               # GitHub README
├── CHANGELOG.md            # Full version history
├── CLAUDE.md               # AI project context (this file)
├── AGENTS.md               # Cross-agent coordination context
├── SECURITY.md             # Security policy
└── CODE_OF_CONDUCT.md      # Code of conduct
```
