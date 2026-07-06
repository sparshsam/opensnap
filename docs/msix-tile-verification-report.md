# OpenSnap MSIX Tile Asset — Verification Against Microsoft Official Specs

**Date:** 2026-07-06  
**Sources:**
- [Tile and toast visual assets (Windows Runtime apps)](https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh781198(v=win.10)) — official pixel dimensions for each tile type
- [Construct your Windows app's icon](https://learn.microsoft.com/en-us/windows/apps/design/iconography/app-icon-construction) — required asset file listing including AppList.targetsize-*
- [App icons and logos](https://learn.microsoft.com/en-us/windows/uwp/design/style/app-icons-and-logos) — app icon requirements

---

## 1. Full Dimension Verification Table

**Microsoft sizes** calculated as `floor(base_size × scale_pct / 100)` per integer truncation convention.

| Asset | Microsoft Spec | Generated | Match |
|-------|---------------|-----------|-------|
| `Square44x44Logo.png` | 44×44 | 44×44 | ✅ |
| `Square44x44Logo.scale-125.png` | 55×55 | 55×55 | ✅ |
| `Square44x44Logo.scale-150.png` | 66×66 | 66×66 | ✅ |
| `Square44x44Logo.scale-200.png` | 88×88 | 88×88 | ✅ |
| `Square44x44Logo.scale-400.png` | 176×176 | 176×176 | ✅ |
| `Square71x71Logo.png` | 71×71 | 71×71 | ✅ |
| `Square71x71Logo.scale-125.png` | **88×88** | **89×89** | ❌ ceil vs floor |
| `Square71x71Logo.scale-150.png` | 106×106 | 106×106 | ✅ |
| `Square71x71Logo.scale-200.png` | 142×142 | 142×142 | ✅ |
| `Square71x71Logo.scale-400.png` | 284×284 | 284×284 | ✅ |
| `Square89x89Logo.png` | 89×89 | 89×89 | ✅ |
| `Square89x89Logo.scale-125.png` | 111×111 | 111×111 | ✅ |
| `Square89x89Logo.scale-150.png` | **133×133** | **134×134** | ❌ ceil vs floor |
| `Square89x89Logo.scale-200.png` | 178×178 | 178×178 | ✅ |
| `Square89x89Logo.scale-400.png` | 356×356 | 356×356 | ✅ |
| `Square107x107Logo.png` | 107×107 | 107×107 | ✅ |
| `Square107x107Logo.scale-125.png` | **133×133** | **134×134** | ❌ ceil vs floor |
| `Square107x107Logo.scale-150.png` | 160×160 | 160×160 | ✅ |
| `Square107x107Logo.scale-200.png` | 214×214 | 214×214 | ✅ |
| `Square107x107Logo.scale-400.png` | 428×428 | 428×428 | ✅ |
| `Square142x142Logo.png` | 142×142 | 142×142 | ✅ |
| `Square142x142Logo.scale-125.png` | **177×177** | **178×178** | ❌ ceil vs floor |
| `Square142x142Logo.scale-150.png` | 213×213 | 213×213 | ✅ |
| `Square142x142Logo.scale-200.png` | 284×284 | 284×284 | ✅ |
| `Square142x142Logo.scale-400.png` | 568×568 | 568×568 | ✅ |
| `Square150x150Logo.png` | 150×150 | 150×150 | ✅ |
| `Square150x150Logo.scale-125.png` | **187×187** | **188×188** | ❌ ceil vs floor |
| `Square150x150Logo.scale-150.png` | 225×225 | 225×225 | ✅ |
| `Square150x150Logo.scale-200.png` | 300×300 | 300×300 | ✅ |
| `Square150x150Logo.scale-400.png` | 600×600 | 600×600 | ✅ |
| `Square284x284Logo.png` | 284×284 | 284×284 | ✅ |
| `Square284x284Logo.scale-125.png` | 355×355 | 355×355 | ✅ |
| `Square284x284Logo.scale-150.png` | 426×426 | 426×426 | ✅ |
| `Square284x284Logo.scale-200.png` | 568×568 | 568×568 | ✅ |
| `Square284x284Logo.scale-400.png` | 1136×1136 | 1136×1136 | ✅ |
| `Square310x310Logo.png` | 310×310 | 310×310 | ✅ |
| `Square310x310Logo.scale-125.png` | **387×387** | **388×388** | ❌ ceil vs floor |
| `Square310x310Logo.scale-150.png` | 465×465 | 465×465 | ✅ |
| `Square310x310Logo.scale-200.png` | 620×620 | 620×620 | ✅ |
| `Square310x310Logo.scale-400.png` | 1240×1240 | 1240×1240 | ✅ |
| `Wide310x150Logo.png` | 310×150 | 310×150 | ✅ |
| `Wide310x150Logo.scale-125.png` | **387×187** | **388×188** | ❌ ceil vs floor |
| `Wide310x150Logo.scale-150.png` | 465×225 | 465×225 | ✅ |
| `Wide310x150Logo.scale-200.png` | 620×300 | 620×300 | ✅ |
| `Wide310x150Logo.scale-400.png` | 1240×600 | 1240×600 | ✅ |
| `StoreLogo.png` | 50×50 | 50×50 | ✅ |
| `StoreLogo.scale-125.png` | 62×62 | 62×62 | ✅ |
| `StoreLogo.scale-150.png` | 75×75 | 75×75 | ✅ |
| `StoreLogo.scale-200.png` | 100×100 | 100×100 | ✅ |
| `StoreLogo.scale-400.png` | 200×200 | 200×200 | ✅ |
| `SplashScreen.png` | **620×300** | 620×300 | ✅ |
| `SplashScreen.scale-125.png` | 775×375 | 775×375 | ✅ |
| `SplashScreen.scale-150.png` | 930×450 | 930×450 | ✅ |
| `SplashScreen.scale-200.png` | 1240×600 | 1240×600 | ✅ |
| `SplashScreen.scale-400.png` | 2480×1200 | 2480×1200 | ✅ |

**Result:** 7 mismatches — all at scale-125 (6) and scale-150 (1). Each is 1 pixel oversized due to `ceil()` vs `floor()` rounding.

---

## 2. The Rounding Issue

The 7 mismatches all follow the same pattern:

| Asset | Base | ×1.25 | Floor | Ceil | Generated |
|-------|------|-------|-------|------|-----------|
| Square71x71Logo | 71 | 88.75 | 88 | **89** | 89 ❌ |
| Square89x89Logo | 89 | — | — | — | (scale-150 issue) |
| Square107x107Logo | 107 | 133.75 | 133 | **134** | 134 ❌ |
| Square142x142Logo | 142 | 177.5 | 177 | **178** | 178 ❌ |
| Square150x150Logo | 150 | 187.5 | 187 | **188** | 188 ❌ |
| Square310x310Logo | 310 | 387.5 | 387 | **388** | 388 ❌ |
| Wide310x150Logo | 310×150 | 387.5×187.5 | 387×187 | **388×188** | 388×188 ❌ |
| Square89x89Logo | 89 | ×1.5=133.5 | 133 | **134** | 134 ❌ |

The generator uses `ceil()` (rounding up) for fractional sizes. Microsoft's integer truncation convention uses `floor()`.

---

## 3. The SplashScreen: NOT Square — Correct at 620×300

My earlier assumption was wrong. Multiple Microsoft sources confirm:

| Source | SplashScreen Size |
|--------|-------------------|
| [Tile and toast visual assets](https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh781198(v=win.10)) | 620×300 |
| [Tip of the Day #5 - Tiles & Logos](https://learn.microsoft.com/en-gb/archive/blogs/ikivanc/tip-of-the-day-5-tiles-logos) | "SplashScreen: 620×300 px" |
| [Image generation tutorial](https://learn.microsoft.com/en-gb/archive/blogs/windows_app_studio_news/image-generation-tutorial-for-app-creation-publication) | "Splash Screen: 620×300" |

The SplashScreen is a **wide banner** (620 wide × 300 tall), not a square. All 6 splash files are at the correct aspect ratio and pass.

---

## 4. Missing TargetSize Assets (MOST LIKELY ROOT CAUSE OF WACK FAILURE)

Microsoft's [app-icon-construction](https://learn.microsoft.com/en-us/windows/apps/design/iconography/app-icon-construction) page lists these as **REQUIRED**:

```
AppList.targetsize-16.png     AppList.targetsize-20.png
AppList.targetsize-24.png     AppList.targetsize-30.png
AppList.targetsize-32.png     AppList.targetsize-36.png
AppList.targetsize-40.png     AppList.targetsize-48.png
AppList.targetsize-60.png     AppList.targetsize-64.png
AppList.targetsize-72.png     AppList.targetsize-80.png
AppList.targetsize-96.png     AppList.targetsize-256.png
```

And for dark/light theme (also **Required** per Microsoft):
```
AppList.targetsize-16_altform-unplated.png    (×14 dark)
AppList.targetsize-16_altform-lightunplated.png (×14 light)
```

**Current state: NONE of these exist in `Assets/`.**

**Why this matters:**
Without `targetsize` assets, Windows 11 selects an icon for the Start menu and taskbar by scaling from whatever IS available. On the Dell Inspiron 12-5280 (the WACK test device), which likely runs at 125–150% scaling, Windows may:

1. Fall back to the **Square44x44Logo** app list icon
2. Scale it up to fill the 48px or 64px slot needed at 150% scaling
3. This upscaling produces a **blurry tile**

The WACK rule 10.1.1.11 "On Device Tiles" checks the rendered tile on the test device. If Windows had to upscale a smaller image because the proper size wasn't available, the result is "blurry/low resolution."

---

## 5. Summary Table

| Category | Finding | WACK Impact |
|----------|---------|-------------|
| **SplashScreen** (all 6 scale variants) | ✅ 620×300 correct per Microsoft spec | None |
| **scale-100 base files** (all 11) | ✅ All match exact spec | None |
| **scale-200, scale-400** (all 22) | ✅ All match exact spec ×2, ×4 | None |
| **scale-125** (7 files) | ❌ 1px oversized (ceil vs floor) | Minimal — unlikely visible |
| **scale-150** (1 file, Square89x89Logo) | ❌ 1px oversized (ceil vs floor) | Minimal |
| **TargetSize assets** (42 files) | ❌ **ALL MISSING** | **HIGH — likely WACK failure cause** |

---

## 6. What to Fix

### Fix 1: Rounding — change `ceil()` to `floor()` in the generator

7 files need regeneration:
- Square71x71Logo.scale-125.png → 88×88 (was 89×89)
- Square89x89Logo.scale-150.png → 133×133 (was 134×134)
- Square107x107Logo.scale-125.png → 133×133 (was 134×134)
- Square142x142Logo.scale-125.png → 177×177 (was 178×178)
- Square150x150Logo.scale-125.png → 187×187 (was 188×188)
- Square310x310Logo.scale-125.png → 387×387 (was 388×388)
- Wide310x150Logo.scale-125.png → 387×187 (was 388×188)

### Fix 2 (Critical): Generate AppList.targetsize-* assets

Generate all 42 targetsize files from the 1024×1024 master:
- 14 default: `AppList.targetsize-{16,20,24,30,32,36,40,48,60,64,72,80,96,256}.png`
- 14 dark: `AppList.targetsize-{n}_altform-unplated.png`
- 14 light: `AppList.targetsize-{n}_altform-lightunplated.png`

These are pixel-exact fixed sizes (no scaling — each file is exactly the specified pixel dimension).
