# FAQ

## Do I need .NET installed?
**For Microsoft Store users:** No — the Store package includes everything needed.  
**For MSIX / EXE users:** Yes, OpenSnap requires the .NET 8 Runtime. The installer does not bundle it. Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## How do I install OpenSnap?
The easiest way is from the **[Microsoft Store](https://apps.microsoft.com/detail/9NV4G1F09L41)** — one click, signed, and stays updated automatically.  
You can also download the [standalone MSIX (unsigned)](https://github.com/sparshsam/opensnap/releases/latest) for sideloading or the [EXE installer](https://github.com/sparshsam/opensnap/releases/latest) for Inno Setup installation.

## Does it upload my screenshots anywhere?
No. OpenSnap saves to your local Desktop and copies to your clipboard. No data leaves your computer.

## Does it work on multiple monitors?
Yes. Full screen captures span the entire virtual desktop. Area selection covers all monitors.

## Can I change where screenshots are saved?
Yes. Right-click the widget → Settings → Browse to choose any folder. You can also enable date-based subfolders.

## How do I take an area screenshot?
Click the left section of the pill (the selection-box icon). It toggles blue, then drag across the screen to select. Release to capture.

## How does OCR work?
Capture + OCR runs Windows built-in OCR engine. It works with any installed Windows language pack. Go to Settings → Time & Language to add OCR support for your language.

## Can I change the hotkeys?
Yes. Right-click → Settings → Global hotkeys. Choose modifier combinations and keys for full screen and active window capture.

## Why is the installer flagged by SmartScreen?
The **Microsoft Store** version is signed and won't trigger SmartScreen.  
The standalone MSIX and EXE installers are not code-signed. If you see "Windows protected your PC", click **More info → Run anyway**. For a hassle-free experience, get OpenSnap from the [Microsoft Store](https://apps.microsoft.com/detail/9NV4G1F09L41).

## How do I uninstall?
Go to Settings → Apps → Installed apps → OpenSnap → Uninstall. Or run the installer again and select Remove.

## How do I update?
**Microsoft Store version:** Updates are installed automatically through the Microsoft Store — no action needed.  
**Standalone version:** OpenSnap checks for updates on startup. You can also right-click the tray icon → Check for updates. The app will download and prompt you to install.
