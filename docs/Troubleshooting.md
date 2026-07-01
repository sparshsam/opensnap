# Troubleshooting

## App won't start

Ensure [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed.

```
dotnet --list-runtimes
```

If .NET 8 is not listed, download and install the runtime, then restart.

## "Windows protected your PC" (SmartScreen)

If you're using the **Microsoft Store** version — this doesn't apply. The Store version is signed and trusted.  
If you're using the **standalone MSIX or EXE** installer: click **More info → Run anyway**. The standalone installer is not code-signed — this is expected. For a hassle-free experience, get OpenSnap from the [Microsoft Store](https://apps.microsoft.com/detail/9NV4G1F09L41).

## OCR not working

OCR requires a Windows language pack with OCR support:

1. Open **Settings → Time & Language → Language & region**
2. Add your language if not already installed
3. Click your language → **Options**
4. Download the **OCR** component

Restart OpenSnap after installing.

## Settings lost after upgrade

Settings are stored at `%APPDATA%\OpenSnap\settings.json`. The installer never touches this directory. If settings are missing, check that the file still exists — it may have been deleted by a third-party cleaner.

## Widget not visible

- Check your system tray (near the clock) — the widget icon lives there
- If auto-hide is enabled, close any fullscreen application
- Check that **Always on top** is enabled in Settings
- The widget may be off-screen — open `%APPDATA%\OpenSnap\settings.json` and reset `WindowLeft` / `WindowTop` to `-1`

## Capture saves a black image

This can happen with hardware-accelerated content (DRM video, some games). Try active window or area selection mode instead.

## Clipboard not working

If another application has the clipboard locked, OpenSnap retries automatically with exponential backoff (100ms → 300ms → 900ms). If it still fails, try again.

## Duplicate tray icons

If Windows Explorer crashes and restarts, the tray icon may appear duplicated. Right-click → Quit and relaunch OpenSnap to fix this.

## Logs

Check `%LOCALAPPDATA%\OpenSnap\logs\` for error logs. These can help diagnose crashes and issues. You can export them from **About → Diagnostics → Export logs**.
