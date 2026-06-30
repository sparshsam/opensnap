# v0.7.0 — Triple-button pill

A major UX upgrade: the pill is now 3× wider and divided into three clickable
sections so you can choose your capture mode at a glance — no more right-click
menus for common actions.

## Added

- **Triple-button pill** — 240×36 px capsule with three sections:
  - Left: area selection (toggle — blue glow when active)
  - Center: full screen capture (default)
  - Right: active window capture
- **Spring-back bounce animation** on every button press (BackEase curve)
- **Green border flash** (#007a3f) confirmation on capture
- **Hover glow** per section with glass dividers
- **Exit** option in right-click context menu
- `create-desktop-shortcut.ps1` — one-command desktop shortcut generator

## Fixed

- Bounce animation now plays *before* the capture starts (was delayed by
  synchronous `Graphics.CopyFromScreen`)
- Active window capture no longer bounces the entire widget (animation is
  scoped to the individual section via `ScaleTransform` instance)
- `MouseEventArgs` / `Brush` / `Color` / `Brushes` ambiguity resolved with
  using aliases (`UseWindowsForms=true` quirk)

## Changed

- Pill expanded from 80×36 → 240×36
- Left-click now dispatches per-section instead of always full screen
- Full screen hotkeys (Win+Shift+S, Win+Shift+W) unchanged

## Notes

- Test project at `opensnap-test/` has the same UI for experimentation
- Installer not updated in this release — manual `dotnet build -c Release` to
  build, then run `setup.iss` through Inno Setup 6
