# Testing

OpenSnap has zero automated tests. All testing is manual.

## QA checklist

### Installation
- [ ] Inno Setup installer runs clean
- [ ] MSIX installs via `Add-AppxPackage` (when signed)
- [ ] Start Menu shortcut created
- [ ] Desktop shortcut created (when selected)
- [ ] App launches from Start Menu
- [ ] Uninstall removes all program files
- [ ] Settings preserved after reinstall (upgrade)

### Capture modes
- [ ] Full screen capture saves PNG to Desktop
- [ ] Active window captures only the focused window
- [ ] Area selection overlay captures selected region
- [ ] Image copied to clipboard after capture

### OCR
- [ ] Capture + OCR saves image AND extracts text
- [ ] OCR text copied to clipboard

### Widget
- [ ] All three sections respond to clicks
- [ ] Bounce animation plays on each section independently
- [ ] Green flash on capture
- [ ] Drag moves the widget
- [ ] Right-click opens context menu
- [ ] Middle-click captures active window
- [ ] Edge snapping works (when enabled)
- [ ] Opacity slider works
- [ ] Auto-hide on fullscreen apps (when enabled)

### Settings
- [ ] Always-on-top toggle works
- [ ] Launch at startup toggle works
- [ ] Sound toggle works
- [ ] Hotkey combos save and apply
- [ ] Filename template saves
- [ ] Language changes apply immediately
- [ ] Large icons toggle works

### Keyboard
- [ ] Left/Right arrows cycle between sections
- [ ] Enter/Space activates selected section

### Update
- [ ] "Check for updates" tray item fetches GitHub release
- [ ] About dialog shows correct version
- [ ] Diagnostics page shows system info
- [ ] Logs export works
