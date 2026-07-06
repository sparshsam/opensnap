#!/usr/bin/env python3
"""
OpenSnap MSIX Asset Generator v2
==================================
Regenerates ALL MSIX tile images and AppList.targetsize assets from
1024×1024 master PNGs using floor() rounding throughout.

Sources of truth:
  - docs/msix-tile-verification-report.md
  - https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh781198(v=win.10)
  - https://learn.microsoft.com/en-us/windows/apps/design/iconography/app-icon-construction

Masters (unmodified):
  Light: opensnap_light_mode.png
  Dark:  opensnap_dark_mode.png

Output:
  1. WindowsAssets/     — OneDrive intermediate (all generated assets)
  2. repo Assets/       — MSIX-ready copies in the repo
  3. Resources/app.ico  — Multi-resolution ICO

Usage: python3 scripts/regenerate-msix-assets.py
"""

import os, sys, math, struct, io, shutil
from PIL import Image

# ─── PATHS ───────────────────────────────────────────────────────────────────

ONEDRIVE = "/mnt/c/Users/spars/OneDrive/Kovina/Apps Stuff/opensnap"
LIGHT_MASTER = os.path.join(ONEDRIVE, "opensnap_light_mode.png")
DARK_MASTER  = os.path.join(ONEDRIVE, "opensnap_dark_mode.png")
WINDOWS_ASSETS = os.path.join(ONEDRIVE, "WindowsAssets")
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO_ASSETS = os.path.join(REPO_ROOT, "Assets")
REPO_RESOURCES = os.path.join(REPO_ROOT, "Resources")

# ─── TILE SPECIFICATIONS ─────────────────────────────────────────────────────

SCALES = [100, 125, 150, 200, 400]

TILES = [
    ("Square44x44Logo",     44,          True),
    ("Square71x71Logo",     71,          True),
    ("Square89x89Logo",     89,          True),
    ("Square107x107Logo",   107,         True),
    ("Square142x142Logo",   142,         True),
    ("Square150x150Logo",   150,         True),
    ("Square284x284Logo",   284,         True),
    ("Square310x310Logo",   310,         True),
    ("Wide310x150Logo",     (310, 150),  True),
    ("StoreLogo",           50,          True),
    ("SplashScreen",        620,         True),
]

SPLASH_HEIGHT = 300
TARGET_SIZES = [16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256]
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
BRAND_BG = (26, 4, 34)


def ms_size(base, scale_pct):
    if isinstance(base, tuple):
        return (base[0] * scale_pct // 100, base[1] * scale_pct // 100)
    if base == 620:
        w = base * scale_pct // 100
        h = SPLASH_HEIGHT * scale_pct // 100
        return (w, h)
    s = base * scale_pct // 100
    return (s, s)


def load_master(path, label):
    if not os.path.exists(path):
        print(f"  ERROR: {label} not found at {path}")
        sys.exit(1)
    img = Image.open(path)
    if img.size != (1024, 1024):
        print(f"  ERROR: {label} is {img.size[0]}x{img.size[1]}, expected 1024x1024")
        sys.exit(1)
    print(f"  {label}: {img.size[0]}x{img.size[1]} mode={img.mode}")
    return img


def render(master, w, h, label=""):
    if w <= 0 or h <= 0:
        raise ValueError(f"Invalid dimensions {w}x{h} for {label}")
    return master.resize((w, h), Image.LANCZOS)


def save_png(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, "PNG")


def verify_dim(path, expected_w, expected_h, label):
    if not os.path.exists(path):
        return f"  ERROR: {label}: FILE MISSING"
    img = Image.open(path)
    if img.size != (expected_w, expected_h):
        return f"  ERROR: {label}: got {img.size[0]}x{img.size[1]}, expected {expected_w}x{expected_h}"
    return None


def create_ico(png_buffers):
    count = len(png_buffers)
    header_size = 6 + count * 16
    offsets = []
    offset = header_size
    for buf in png_buffers:
        offsets.append(offset)
        offset += len(buf)
    out = bytearray()
    out += struct.pack("<HHH", 0, 1, count)
    for i, buf in enumerate(png_buffers):
        png_img = Image.open(io.BytesIO(buf))
        png_w, png_h = png_img.size
        entry_w = 0 if png_w >= 256 else png_w
        entry_h = 0 if png_h >= 256 else png_h
        out += struct.pack("<BBBBHHII", entry_w, entry_h, 0, 0, 1, 32, len(buf), offsets[i])
    for buf in png_buffers:
        out.extend(buf)
    return bytes(out)


def generate_ico(master, sizes, output_path):
    buffers = []
    for s in sizes:
        img = render(master, s, s, f"ico-{s}")
        buf = io.BytesIO()
        img.save(buf, "PNG")
        buffers.append(buf.getvalue())
    ico_data = create_ico(buffers)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "wb") as f:
        f.write(ico_data)
    print(f"  app.ico -> ({len(ico_data)} bytes, {len(sizes)} frames)")


def main():
    print("OpenSnap MSIX Asset Generator v2\n")

    print("-- 1. Loading masters --")
    light = load_master(LIGHT_MASTER, "Light master")
    dark = load_master(DARK_MASTER, "Dark master")
    total_files = 0
    errors = []

    print("\n-- 2. Tile images --")
    for name, base, has_dup in TILES:
        for scale_pct in SCALES:
            w, h = ms_size(base, scale_pct)
            fname = f"{name}.png" if scale_pct == 100 else f"{name}.scale-{scale_pct}.png"
            img = render(light, w, h, fname)
            save_png(img, os.path.join(WINDOWS_ASSETS, fname))
            total_files += 1
            if scale_pct == 100 and has_dup:
                save_png(img, os.path.join(WINDOWS_ASSETS, f"{name}.scale-100.png"))
                total_files += 1
    print(f"  Generated {total_files} tile image files")

    print("\n-- 3. SplashScreen --")
    for scale_pct in SCALES:
        w = 620 * scale_pct // 100
        h = SPLASH_HEIGHT * scale_pct // 100
        icon_size = max(44, min(w * 3 // 10, h * 3 // 10))
        icon_img = render(dark, icon_size, icon_size, f"splash@{scale_pct}")
        canvas = Image.new("RGB", (w, h), BRAND_BG)
        ix, iy = (w - icon_size) // 2, (h - icon_size) // 2
        canvas.paste(icon_img, (ix, iy))
        if scale_pct == 100:
            save_png(canvas, os.path.join(WINDOWS_ASSETS, "SplashScreen.png"))
            save_png(canvas, os.path.join(WINDOWS_ASSETS, "SplashScreen.scale-100.png"))
            total_files += 2
        else:
            save_png(canvas, os.path.join(WINDOWS_ASSETS, f"SplashScreen.scale-{scale_pct}.png"))
            total_files += 1
    print("  SplashScreen files generated")

    print("\n-- 4. AppList.targetsize assets --")
    for ts in TARGET_SIZES:
        save_png(render(light, ts, ts), os.path.join(WINDOWS_ASSETS, f"AppList.targetsize-{ts}.png"))
        save_png(render(dark, ts, ts), os.path.join(WINDOWS_ASSETS, f"AppList.targetsize-{ts}_altform-unplated.png"))
        save_png(render(light, ts, ts), os.path.join(WINDOWS_ASSETS, f"AppList.targetsize-{ts}_altform-lightunplated.png"))
    print(f"  Generated {len(TARGET_SIZES) * 3} targetsize files")

    print("\n-- 5. app.ico --")
    generate_ico(light, ICO_SIZES, os.path.join(WINDOWS_ASSETS, "app.ico"))

    print("\n-- 6. Verifying all dimensions --")
    for name, base, has_dup in TILES:
        for scale_pct in SCALES:
            ew, eh = ms_size(base, scale_pct)
            fname = f"{name}.png" if scale_pct == 100 else f"{name}.scale-{scale_pct}.png"
            err = verify_dim(os.path.join(WINDOWS_ASSETS, fname), ew, eh, fname)
            if err:
                print(f"  {err}"); errors.append(err)
            if scale_pct == 100 and has_dup:
                err = verify_dim(os.path.join(WINDOWS_ASSETS, f"{name}.scale-100.png"), ew, eh, f"{name}.scale-100.png")
                if err:
                    print(f"  {err}"); errors.append(err)
    for ts in TARGET_SIZES:
        for suffix in ["", "_altform-unplated", "_altform-lightunplated"]:
            fn = f"AppList.targetsize-{ts}{suffix}.png"
            err = verify_dim(os.path.join(WINDOWS_ASSETS, fn), ts, ts, fn)
            if err:
                print(f"  {err}"); errors.append(err)
    if not errors:
        print("  ALL DIMENSIONS VERIFIED -- 0 errors")

    print("\n-- 7. Copying to repo --")
    if os.path.exists(REPO_ASSETS):
        shutil.rmtree(REPO_ASSETS)
    os.makedirs(REPO_ASSETS, exist_ok=True)
    copied = 0
    for f in sorted(os.listdir(WINDOWS_ASSETS)):
        if f.endswith((".png", ".ico")):
            shutil.copy2(os.path.join(WINDOWS_ASSETS, f), os.path.join(REPO_ASSETS, f))
            copied += 1
    shutil.copy2(os.path.join(WINDOWS_ASSETS, "app.ico"), os.path.join(REPO_RESOURCES, "app.ico"))
    print(f"  Copied {copied} files to repo")

    print("\nSummary:")
    print(f"  Tile images:  {total_files}")
    print(f"  TargetSize:   {len(TARGET_SIZES) * 3}")
    print(f"  app.ico:      {len(ICO_SIZES)} frames")
    print(f"  Copied to:    {REPO_ASSETS}")

    if errors:
        print(f"\n  {len(errors)} ERROR(S) FOUND -- exiting")
        sys.exit(1)
    print("\n  ALL CHECKS PASSED -- assets ready for MSIX build\n")


if __name__ == "__main__":
    main()
