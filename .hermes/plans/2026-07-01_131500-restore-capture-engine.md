# Restore Capture Engine — Surgical Fix Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Restore OpenSnap v1.0.0 capture engine to working order by reverting all broken changes, then applying only surgical fixes for the three known bugs (area selection overlay, active window, stale file bytes).

**Architecture:** The capture engine has three layers: `ScreenshotService.CaptureDesktop()` (fullscreen), `CaptureService.CaptureActiveWindow()` (active window), and `CaptureService.CaptureArea()` (raw screen region). All use `System.Drawing.Bitmap` + `Graphics.CopyFromScreen` + `BitmapToBitmapSource` (via `GetHbitmap`). The v1.0.0 implementation of these worked correctly for fullscreen. Only three things need fixing: (1) area selection must close overlay before capture, (2) active window must hide widget before getting foreground, (3) `SaveAsPng` must truncate output file.

**Tech Stack:** .NET 8 WPF, System.Drawing.Common, WinForms

**Current broken state (12 files modified, all uncommitted):**
- `CaptureService.cs` — completely rewritten with `Format24bppRgb` (wrong pixel format), new `CaptureRegionPixels` (unnecessary), DWM bounds (buggy), removed `CaptureArea()` (broke area selection)
- `ScreenshotService.cs` — rewritten, removed `CloneBitmap` (maybe needed), delegates to `CaptureRegionPixels`
- `App.xaml.cs` — `DispatchCapture` rewritten with `_widget.Hide()/Show()` (crashes), `CaptureAreaAsync` uses dispatcher invoke (untested)
- `AreaSelectorWindow.xaml` — size label removed (good), but dashed border added (unnecessary change)
- `AreaSelectorWindow.xaml.cs` — size label removed (good)
- Plus prior fixes for Settings, Tray, UpdateService, About, Opacity, etc. that are correct.

---

## Task 1: Revert CaptureService.cs, ScreenshotService.cs to exact v1.0.0 originals

**Objective:** Restore the working capture engine to its exact v1.0.0 state. These files had NO bugs in v1.0.0 for fullscreen capture.

**Files:**
- Revert: `CaptureService.cs` — restore to `5a49889` (HEAD before docs commit) version
- Revert: `ScreenshotService.cs` — restore to `5a49889` version

**Step 1: Restore ScreenshotService.cs**

Run:
```bash
cd /home/spars/repos/opensnap
git checkout 5a49889 -- ScreenshotService.cs
```

Expected: ScreenshotService.cs restored to v1.0.0 with working `CaptureDesktop()`, `SaveAsPng(File.OpenWrite)`, `CopyToClipboard` with CloneBitmap, and all helper methods.

**Step 2: Restore CaptureService.cs**

Run:
```bash
cd /home/spars/repos/opensnap
git checkout 5a49889 -- CaptureService.cs
```

Expected: CaptureService.cs restored with `CaptureActiveWindow()` (CopyFromScreen-based), `CaptureArea()` (raw region capture), `FindValidForegroundWindow()`, `IsValidCaptureWindow()`, `BitmapToBitmapSource()`, `RECT` struct, `NativeMethods` class. No DWM, no `CaptureRegionPixels`, no `GetActiveWindowBounds`.

**Step 3: Verify compilation**

Run:
```bash
cd /home/spars/repos/opensnap && dotnet build -c Release 2>&1 | tail -5
```
Note: dotnet is not on WSL. Copy to test build dir and build with Windows dotnet. 

But first verify the git checkout worked:
```bash
cd /home/spars/repos/opensnap && git diff --cached ScreenshotService.cs | head -20
```

Expected: File restored to original — no staged/working tree changes for these files relative to base commit.

---

## Task 2: Restore App.xaml.cs DispatchCapture + CaptureAreaAsync to v1.0.0 originals

**Objective:** Revert the capture dispatch code in App.xaml.cs to the original v1.0.0 version that worked. The original `DispatchCapture` used `-32000` off-screen positioning (which worked for active window), `CaptureAreaAsync` captured with overlay (which was the known bug we'll fix separately).

**Files:**
- Modify: `App.xaml.cs` — restore DispatchCapture and CaptureAreaAsync methods

**Step 1: Restore the original DispatchCapture method**

The original v1.0.0 DispatchCapture (lines 142-260):

```csharp
private async void DispatchCapture(CaptureMode mode)
{
    var _captureTimer = Stopwatch.StartNew();
    try
    {
        BitmapSource? source = null;

        // For ActiveWindow, temporarily move the widget off-screen
        if (mode == CaptureMode.ActiveWindow)
        {
            var restoreLeft = _widget!.Left;
            var restoreTop  = _widget.Top;
            var restoreTopmost = _widget.Topmost;

            _widget.Topmost = false;
            _widget.Left = -32000;
            _widget.Top = -32000;
            await Task.Delay(50);
            source = CaptureService.CaptureActiveWindow();
            _widget.Topmost = restoreTopmost;
            _widget.Left = restoreLeft;
            _widget.Top = restoreTop;
        }
        else
        {
            source = mode switch
            {
                CaptureMode.AreaSelection => await CaptureAreaAsync(),
                _ => ScreenshotService.CaptureDesktop(),
            };
        }

        // Deactivate area-selection toggle after the overlay closes
        if (mode == CaptureMode.AreaSelection)
            _widget?.ResetAreaToggle();

        if (source == null) return;

        // Advanced naming: resolve path + filename
        var baseFolder = AppSettings.EnsureFolder(_settings!.SavePath);
        var saveFolder = ScreenshotService.ResolveSavePath(baseFolder, _settings);
        Directory.CreateDirectory(saveFolder);

        int seq = _settings.UseSequentialNumbering ? _settings.SequentialCounter++ : 0;
        var fileName = ScreenshotService.GenerateFileName(
            _settings.FilenameTemplate, _settings.ProjectPrefix, seq);
        var fullPath = Path.Combine(saveFolder, fileName);

        ScreenshotService.SaveAsPng(source, fullPath);
        if (!_settings.SaveOnly)
            ScreenshotService.CopyToClipboard(source);

        // OCR mode: extract text
        string ocrText = "";
        if (mode == CaptureMode.CaptureOcr)
        {
            (ocrText, _) = await OcrService.CaptureOcrAsync(source);
        }

        // Play capture sound
        if (_settings.PlayCaptureSound)
            PlayShutterSound();

        // Update history
        _settings.ScreenshotHistory.Add(fullPath);
        if (_settings.ScreenshotHistory.Count > 20)
            _settings.ScreenshotHistory.RemoveAt(0);
        _settings.Save();

        _tray?.UpdateHistory(_settings.ScreenshotHistory, _settings.PinnedCaptures);
        _tray?.SetHistoryActionsEnabled(true);

        // Show quick-actions popup
        if (_settings.ShowQuickActions)
        {
            var popup = new CapturePopup(fullPath, ocrText, _settings);
            popup.Show();
        }
        else
        {
            _tray?.Notify("OpenSnap", $"Saved  \u2022  {fileName}");
        }

        _captureTimer.Stop();
        BenchmarkLog($"capture_{mode}", _captureTimer.ElapsedMilliseconds, fileName);
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show(
            $"Screenshot failed:\n{ex.Message}",
            "OpenSnap — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
```

Restore this by extracting from git history:

```bash
cd /home/spars/repos/opensnap
# Extract the original dispatch section
git show 5a49889:App.xaml.cs | sed -n '142,260p' > /tmp/orig_dispatch.txt
```

Then use the content to replace the current DispatchCapture method boundaries.

The simplest approach is to use `patch` to replace the entire DispatchCapture block. The old string is the current broken DispatchCapture, the new string is the original v1.0.0 DispatchCapture.

**Step 2: Restore the original CaptureAreaAsync method**

Original v1.0.0:
```csharp
private static Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
{
    var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();
    var overlay = new AreaSelectorWindow();
    overlay.SelectionCompleted = rect =>
    {
        var source = CaptureService.CaptureArea(
            (int)(overlay.Left + rect.X),
            (int)(overlay.Top + rect.Y),
            (int)rect.Width, (int)rect.Height);
        tcs.TrySetResult(source);
    };
    overlay.Closed += (_, _) => tcs.TrySetResult(null);
    overlay.Show();
    return tcs.Task;
}
```

Same approach — restore using git content or manual patch.

**Step 3: Verify file integrity**

```bash
cd /home/spars/repos/opensnap && git diff App.xaml.cs | head -20
```

Expected: Diff should show only the non-capture changes (tray wiring, NativeMethods fix, Settings window, etc.) — no DispatchCapture or CaptureAreaAsync changes.

---

## Task 3: Apply surgical fix — area selection (close overlay before capture)

**Objective:** Fix area selection to not include the overlay in the saved image. The original v1.0.0 captured with the overlay visible. We must close it first, then capture.

**Files:**
- Modify: `App.xaml.cs` — CaptureAreaAsync method only

**Root cause:** The `SelectionCompleted` callback fires inside the `OnMouseUp` event handler. `Close()` is called after the callback. The overlay is still visible on screen during `CopyFromScreen`.

**Fix approach:** After computing the selection rect, close the overlay, then schedule the capture on the dispatcher at `ApplicationIdle` priority (higher than `Background` but after all input/rendering messages). Inside the dispatched action, wait 150ms for DWM composition, then capture.

The callback cannot use `await` (it's `Action<Rect>`, not `async`). But we can call `overlay.Dispatcher.InvokeAsync` with `ApplicationIdle` priority to schedule the capture after the close processes.

**Complete replacement for CaptureAreaAsync:**

```csharp
private static async Task<System.Windows.Media.Imaging.BitmapSource?> CaptureAreaAsync()
{
    var tcs = new TaskCompletionSource<System.Windows.Media.Imaging.BitmapSource?>();
    var overlay = new AreaSelectorWindow();

    overlay.SelectionCompleted = rect =>
    {
        try
        {
            // 1. Compute bounds in DIPs
            int dipX = (int)(overlay.Left + rect.X);
            int dipY = (int)(overlay.Top + rect.Y);
            int dipW = (int)rect.Width;
            int dipH = (int)rect.Height;

            // 2. Close overlay immediately so it leaves the screen
            overlay.Close();

            // 3. Schedule capture on dispatcher after close is processed
            _ = overlay.Dispatcher.InvokeAsync(() =>
            {
                // 4. Wait for DWM to remove overlay surface
                Task.Delay(150).Wait();
                
                // 5. Capture using original CaptureArea (physical pixels on PerMonitorV2)
                //    Note: overlay.Left/Top are 0 after Close(), but we saved them above
                var source = CaptureService.CaptureArea(dipX, dipY, dipW, dipH);
                tcs.TrySetResult(source);
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        catch (Exception ex)
        {
            LogException("CaptureArea", ex);
            tcs.TrySetResult(null);
        }
    };

    overlay.Closed += (_, _) =>
    {
        if (!tcs.Task.IsCompleted)
            tcs.TrySetResult(null);
    };

    overlay.Show();
    return await tcs.Task;
}
```

**Verification:** After building and testing, area selection saved image should contain only the selected screen content, with no overlay, no selection border, no dim background.

---

## Task 4: Apply surgical fix — active window (increase delay, ensure fullscreen fallback)

**Objective:** Fix active window capture which returns white images. The original v1.0.0 code moves the widget off-screen with `-32000` and waits 50ms. Increase to 150ms for reliability.

**Files:**
- Modify: `App.xaml.cs` — DispatchCapture method, active window section

**Root cause:** 50ms may not be enough for DWM to re-composite and for `GetForegroundWindow()` to return the correct window behind the widget. The foreground after moving the widget may still be the widget itself (which has no title and is filtered out by `IsValidCaptureWindow`), so `CaptureActiveWindow()` falls back to `CaptureDesktop()` — but on some systems the desktop capture at -32000 region returns white.

**Fix:** 
1. Increase delay from 50ms → 150ms
2. Do NOT rely on `FindValidForegroundWindow()` filtering the widget — instead, the widget is already at -32000 so the foreground should be whatever was behind it

Change in DispatchCapture:
```csharp
await Task.Delay(50);  // → await Task.Delay(150);
```

That's it — only change the delay.

**Verification:** Active window capture should capture the actual foreground window (e.g. a browser), not a white image.

---

## Task 5: Apply surgical fix — SaveAsPng file corruption (FileMode.Create)

**Objective:** Fix PNG files that have stale bytes from previous captures.

**Files:**
- Modify: `ScreenshotService.cs` — SaveAsPng method only

**Root cause:** `File.OpenWrite(path)` opens the file without truncating. If a new PNG is smaller than the old one at the same path, bytes from the old file remain at the end, corrupting the image.

**Fix:** Replace `File.OpenWrite` with `FileStream(FileMode.Create)`.

Replace:
```csharp
using var stream = File.OpenWrite(filePath);
```

With:
```csharp
using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
```

**Verification:** Repeated captures to the same path should produce clean, uncorrupted PNGs.

---

## Task 6: Verify and build test EXE

**Objective:** Build the fixed code and verify all features.

**Files:**
- Build: `/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-test/release/OpenSnap.exe`

**Step 1: Copy all modified files to test directory**

```bash
for f in App.xaml.cs CaptureService.cs ScreenshotService.cs; do
  cp /home/spars/repos/opensnap/$f "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-test/$f"
done
```

**Step 2: Build self-contained test EXE**

```bash
cd "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-test"
taskkill.exe /F /IM OpenSnap.exe 2>/dev/null || true
sleep 2
rm -rf build release
/mnt/c/Program\ Files/dotnet/dotnet.exe publish -c Release -r win-x64 --self-contained true -o build
cp -r build release
```

**Step 3: Verify version**

```bash
strings "/mnt/c/Users/spars/Desktop/OpenSnap-v1.0.1-test/release/OpenSnap.dll" | grep -E '^1\.0\.[0-9\.]+$'
```
Expected: `1.0.1.0`

**Step 4: Run and test on Windows:**
- Fullscreen capture → check saved PNG dimensions match screen resolution
- Active window capture → check correct window content, no white strips
- Area selection → drag a region, check only selected content in output
- Settings → opens without error
- Update check → says up to date or shows correct error
- OCR → captures text correctly

---

## Risks, Tradeoffs, and Open Questions

**Risk:** `Task.Delay(150).Wait()` inside the ApplicationIdle dispatcher callback blocks the UI thread briefly. But at ApplicationIdle priority, no other UI work is pending, so a 150ms block is acceptable during capture.

**Risk:** The original `CopyToClipboard` with `CloneBitmap` for large images (>5MP) re-encodes through PNG. This can slightly reduce quality for very large captures. The user complained about quality — but the fix for quality is to remove CloneBitmap, which causes clipboard memory pressure. For now, keep CloneBitmap to match v1.0.0 behavior. Quality complaint may be caused by other factors.

**Risk:** The `CaptureService.CaptureArea()` uses `(int)(overlay.Left + rect.X)` which truncates rather than rounds. On high-DPI displays with PerMonitorV2, DIPs need conversion to physical pixels. But the original code used this approach and area selection at least captured something (even if with overlay artifacts). For now, keep the original approach. DPI correctness is a separate fix.

**Open question:** Does the test system have Multi-monitor with different DPIs? If so, the simple `(int)` cast could cause off-by-one or shifted captures. This would need DPI correction.

**Open question:** Is the screen on 100% DPI? If so, DIPs = physical pixels and the area selection coordinates are exact.

---

## Summary of All Changes

| File | Change |
|---|---|
| `CaptureService.cs` | **Revert to v1.0.0 original** — contains working `CaptureActiveWindow()`, `CaptureArea()`, `FindValidForegroundWindow()`, `IsValidCaptureWindow()` |
| `ScreenshotService.cs` | **Revert to v1.0.0 original** — then surgically fix `File.OpenWrite` → `FileMode.Create` (Task 5) |
| `App.xaml.cs` | **Revert DispatchCapture + CaptureAreaAsync to v1.0.0** — then surgically fix active window delay (50→150ms) and area overlay close (Task 3, 4) |
| All other files | Keep current changes — TrayService, UpdateService, SettingsWindow, etc. are correct |
