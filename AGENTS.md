# OpenSnap — Agent Coordination Context

**Project:** C# WPF screenshot widget for Windows (.NET 8)
**GitHub:** https://github.com/sparshsam/opensnap
**Latest tag:** v1.0.1
**Landing page:** https://snap.kovina.org

## Current version — v1.0.1

v1.0.1 replaces v1.0.0 with proper MSIX visual assets and self-contained
packaging. The MSIX (`OpenSnap-1.0.1.msix`, 74.88 MB) is on the user's Desktop
ready for Partner Center upload.

## Visual assets architecture

Two permanent 1024×1024 PNG masters (do NOT modify):
- `opensnap_dark_mode.png` — light icon on black bg (stored in OneDrive)
- `opensnap_light_mode.png` — dark icon on light bg (stored in OneDrive)

Generated assets pipeline:
- Script: `generate_assets.py` in the OneDrive assets folder
- Output: `WindowsAssets/` with 4 themed sets (Dark, Light, Composite, CompositeLight)
- Each set: 66 files (11 logo types × 6 scale variants)
- Repo copy: `Assets/` contains the composite (transparent-bg) set, used for MSIX

## Build note

The `SaveOnlyCheck` build error on `SettingsWindow.xaml.cs` is caused by stale
`obj/` artifacts. Fix: `dotnet clean` before `dotnet build`.

## Key file references

| File | Purpose |
|---|---|
| `Assets/*.png` | 66 pre-generated MSIX store assets |
| `Package.appxmanifest` | Manifest with DefaultTile + SplashScreen refs |
| `package-msix.ps1` | MSIX packaging (uses pre-generated Assets/) |
| `Resources/app.ico` | 6-size app icon (16×16–256×256, from master) |

## Release checklist

1. Submit `OpenSnap-1.0.1.msix` to Microsoft Partner Center
2. Wait for certification
3. Promote to available in Store
4. Upload Inno Setup installer to GitHub Release
5. Update landing page with Store badge/link
