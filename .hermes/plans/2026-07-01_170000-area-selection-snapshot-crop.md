# Area Selection Fix — Snapshot-Crop Approach

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Fix area selection to capture exactly the visible screen content the user saw, using a pre-capture snapshot to avoid both DPI scaling errors and desktop z-order artifacts after overlay removal.

**Architecture:** Instead of closing/hiding the overlay and then capturing (which triggers DWM to reveal the desktop underneath), capture the full screen **before** showing the overlay. When the user completes the selection, crop from that pre-captured bitmap using DPI-correct coordinates.

**Tech Stack:** .NET 8 WPF, `System.Windows.Media.Imaging`, `CroppedBitmap`, `CaptureService.CaptureArea()`

**Diagnostic evidence (from capture-debug.log, 150% display):**

```
[FullScreen] capturing (0,0 2880×1800)         ← correct physical pixels
[AreaSelection] selection dip=(843,46 1076×1108) dpi=1.50
[AreaSelection] captured=1076×1108              ← WRONG: used DIPs as physical pixels
```

**Root cause confirmed:** Two bugs combined:
1. **DPI mismatch** — Overlay mouse coordinates are in DIPs (843,46). `CaptureService.CaptureArea()` treats them as physical pixels. At 150% scaling, the actual region should be (1265,69 1614×1662). The captured region is shifted ~400px left and 23px down, landing on desktop wallpaper.
2. **Z-order disruption** — `overlay.Close()` removes the overlay from the DWM composition tree, and the windows behind may not re-composite before `CopyFromScreen` runs — capturing the revealed desktop/background instead of the original visible content.

---

## Task 1: Rewrite CaptureAreaAsync to use snapshot-crop

**Objective:** Capture the full desktop **before** showing the overlay, then crop from it on selection.

**Files:**
- Modify: `home/spars/repos/opensnap/App.xaml.cs` — `CaptureAreaAsync()` method only

**Complete replacement for CaptureAreaAsync:**

```csharp
private static async Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
{
    // Step 1: Capture the full desktop BEFORE showing the overlay.
    // This preserves the exact visible window z-order.
    var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
    int vsLeft = vs.Left, vsTop = vs.Top, vsWidth = vs.Width, vsHeight = vs.Height;
    CaptureLogger.Log("AreaSelection",
        $"pre-capturing full desktop ({vsLeft},{vsTop} {vsWidth}×{vsHeight})");

    BitmapSource fullDesktop;
    try
    {
        fullDesktop = CaptureService.CaptureArea(vsLeft, vsTop, vsWidth, vsHeight);
    }
    catch (Exception ex)
    {
        CaptureLogger.Log("AreaSelection", $"pre-capture failed: {ex.Message}");
        return null;
    }

    CaptureLogger.Log("AreaSelection",
        $"pre-captured={fullDesktop.PixelWidth}×{fullDesktop.PixelHeight}");

    // Step 2: Show overlay for user selection
    var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();
    var overlay = new AreaSelectorWindow();
    int savedX = 0, savedY = 0, savedW = 0, savedH = 0;
    bool selectionMade = false;

    overlay.SelectionCompleted = rect =>
    {
        try
        {
            // Get DPI scale from overlay before doing anything else
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(overlay);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            // Convert DIP coordinates to physical pixels
            int dipX = (int)(overlay.Left + rect.X);
            int dipY = (int)(overlay.Top + rect.Y);
            int dipW = (int)rect.Width;
            int dipH = (int)rect.Height;

            int physX = (int)(dipX * scaleX);
            int physY = (int)(dipY * scaleY);
            int physW = (int)(dipW * scaleX);
            int physH = (int)(dipH * scaleY);

            savedX = dipX;
            savedY = dipY;
            savedW = dipW;
            savedH = dipH;

            CaptureLogger.Log("AreaSelection",
                $"selection dip=({dipX},{dipY} {dipW}×{dipH}) " +
                $"pixel=({physX},{physY} {physW}×{physH}) " +
                $"scale={scaleX:F2}");

            if (physW > 5 && physH > 5)
            {
                // Step 3: Crop from the pre-captured full desktop bitmap.
                // Clamp to prevent out-of-bounds.
                physX = Math.Max(vsLeft, Math.Min(physX, vsLeft + vsWidth - 1));
                physY = Math.Max(vsTop, Math.Min(physY, vsTop + vsHeight - 1));
                physW = Math.Min(physW, vsLeft + vsWidth - physX);
                physH = Math.Min(physH, vsTop + vsHeight - physY);

                try
                {
                    var cropped = new System.Windows.Media.Imaging.CroppedBitmap(
                        fullDesktop,
                        new System.Windows.Int32Rect(
                            physX - vsLeft, physY - vsTop,
                            physW, physH));

                    CaptureLogger.Log("AreaSelection",
                        $"cropped={cropped.PixelWidth}×{cropped.PixelHeight}");
                    tcs.TrySetResult(cropped);
                }
                catch (Exception cropEx)
                {
                    CaptureLogger.Log("AreaSelection",
                        $"cropping error: {cropEx.Message}");
                    // Fallback: capture directly
                    try
                    {
                        var direct = CaptureService.CaptureArea(physX, physY, physW, physH);
                        tcs.TrySetResult(direct);
                    }
                    catch
                    {
                        tcs.TrySetResult(null);
                    }
                }
            }
            else
            {
                CaptureLogger.Log("AreaSelection", "too small, cancelled");
                tcs.TrySetResult(null);
            }

            // Close overlay after capture
            overlay.Close();
        }
        catch (Exception ex)
        {
            CaptureLogger.Log("AreaSelection", $"SelectionCompleted error: {ex.Message}");
            LogException("CaptureArea", ex);
            overlay.Close();
            tcs.TrySetResult(null);
        }
    };

    overlay.Closed += (_, _) =>
    {
        if (!tcs.Task.IsCompleted)
        {
            CaptureLogger.Log("AreaSelection", "Closed without selection (cancelled)");
            tcs.TrySetResult(null);
        }
    };

    overlay.Show();
    return await tcs.Task;
}
```

**Key design decisions:**
- `fullDesktop` is captured with `CaptureService.CaptureArea()` which uses `CopyFromScreen` with physical pixels — correct on any DPI
- `CroppedBitmap` wraps the source bitmap, no pixel data is copied (zero-cost crop)
- DIP→pixel conversion uses `VisualTreeHelper.GetDpi(overlay)` for per-monitor DPI accuracy
- Coordinates are saved in DIPs for logging but crop uses physical pixels
- `overlay.Close()` is called AFTER the crop (no z-order issue since we already have the bitmap)
- Fallback to direct capture if `CroppedBitmap` fails

---

## Task 2: Verify and build test EXE

**Objective:** Build the fixed code and verify area selection works correctly.

**Files:**
- Build: `C:\Users\spars\Desktop\OpenSnap-v1.0.1-debug\release\OpenSnap.exe`

**Step 1: Copy source and build**

```bash
cp /home/spars/repos/opensnap/App.xaml.cs "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-debug/App.xaml.cs"
taskkill.exe /F /IM OpenSnap.exe 2>/dev/null
cd "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-debug"
/mnt/c/Program\ Files/dotnet/dotnet.exe publish -c Release -r win-x64 --self-contained true -o release
```

**Step 2: Verify version**

```bash
strings "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-debug/release/OpenSnap.dll" | grep -E '^1\.0\.[0-9]+\.[0-9]+$'
```
Expected: `1.0.1.0`

**Step 3: Test on Windows**

1. Launch the EXE
2. Select a region with visible windows
3. Check saved image: should show exactly the selected windows, not wallpaper
4. Check `%LOCALAPPDATA%\OpenSnap\logs\capture-debug.log` for:
   - `pre-captured=` — should match full screen resolution (2880×1800)
   - `selection dip=` — DIP coordinates with dpi
   - `selection pixel=` — physical pixel equivalents
   - `cropped=` — should match selected physical dimensions

**Verification checklist:**
- [ ] Area selection captures exactly the selected region with visible window content
- [ ] No wallpaper/background artifacts
- [ ] Fullscreen capture still works (2880×1800)
- [ ] Active-window capture still works
- [ ] Settings still works
- [ ] Update check still works
- [ ] Debug log shows correct pre-capture dimensions

---

## Risks, Tradeoffs, and Open Questions

**Memory:** `fullDesktop` is a full-resolution bitmap. At 2880×1800 (5.1 MP, 32bpp) it's about 20 MB in memory. On high-end multi-monitor setups (e.g., 7680×4320 across 3 4K monitors = 33 MP, ~130 MB), this could be significant. The bitmap is released after the selection (goes out of scope). For OpenSnap's target usage (single user, one capture at a time), this is acceptable.

**CroppedBitmap quirks:** `CroppedBitmap` requires the source to have a valid pixel format and DPI. It doesn't copy pixel data — it just wraps the source with offset/size. This means the full-resolution bitmap stays in memory until the cropped result is consumed (saved/clipboard). `SaveAsPng` and `Clipboard.SetImage` create their own copies, so the memory is released after those calls.

**DPI accuracy:** The DPI scale is obtained from the overlay window via `VisualTreeHelper.GetDpi()`. On a per-monitor DPI system, this returns the DPI of the monitor where the overlay window is primarily displayed. If the user drags the selection across monitors with different DPIs, the scale might be wrong for parts of the selection. This is an edge case — most users have uniform DPI across monitors.

---

## Summary

| File | Change |
|---|---|
| `App.xaml.cs` | Replace `CaptureAreaAsync` with snapshot-crop approach. One method changed (~80 lines). |
| No other files | Fullscreen, active-window, settings, update — unchanged. |

Total: **1 file modified, ~80 lines changed** (replace one method).
