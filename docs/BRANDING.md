# OpenSnap — Branding

> Product-specific branding for OpenSnap within the Kovina ecosystem and Open Product Family.
>
> This document describes **only** what is unique to OpenSnap. For ecosystem and family brand standards, refer to the canonical Kovina sources.

---

## Branding Hierarchy

```
KOVINA          Parent ecosystem     → kovina.org/standards/KOVINA_MANIFESTO.md
  ↓
OPEN            Product family       → kovina.org/standards/BRAND_GUIDELINES.md
  ↓
Snap            Individual product   → this document
```

OpenSnap exists at **Level 3** in this hierarchy. It inherits all Kovina ecosystem philosophy and all Open Product Family brand rules.

---

## Relationship to Kovina

- Kovina is the parent ecosystem. All Kovina standards apply.
- Kovina branding is never displayed inside the application UI.
- The Kovina reference belongs in README footers, about pages, and the About dialog's description area.
- OpenPalette is the canonical reference for the Open Product Family branding implementation.

---

## Relationship to the Open Product Family

- OpenSnap uses the **OPEN + Snap** stacked lockup in its About dialog header and website.
- **OPEN has no icon.** It is typography only — no symbol, no badge, no monogram.
- The application icon (camera motif) belongs exclusively to OpenSnap (the product), not to the Open family.
- Do not merge the icon into the typography lockup.
- Do not create a combined logo mark.

---

## Application Icon

- The icon is a camera motif representing **OpenSnap**, not OPEN, not Kovina.
- Never place the icon inside the typography lockup.
- The icon sits visually separate from the typography in the header lockup.
- Maintain equal padding. Keep optical balance. Do not resize disproportionately.
- Do not recolor, redraw, add gradients, shadows, containers, or outlines.

**Master files:**
- Dark: `assets/branding/logo-white.svg` (and `.png`)
- Light: `assets/branding/logo-on-black.png`
- Icon: `Resources/app.ico` (6-size .ico from master)

---

## Header Lockup (About Dialog)

```
[icon]  OPEN
        Snap
```

- Icon on the left, text stacked on the right.
- Gap between icon and wordmark: `10px`.
- OPEN and Snap share a single layout container in the About dialog header.
- Icon size: `28×28`.

### Where the lockup is used

- **About Dialog** — primary location. Shows the stacked lockup with camera icon.
- **Landing page** (`docs/landing/index.html`) — header navigates to `#home`.
- **README** — textual reference only (no lockup image needed).
- **GitHub** — the repository name is `opensnap`; the description references "screenshot widget for Windows."

### Where "OpenSnap" (full name) is used

- **Window title** — `MainWindow.xaml` Title="OpenSnap"
- **System tray tooltip** — `TrayService.cs` Text = "OpenSnap — Screenshot widget"
- **Application code** — `namespace OpenSnap`, class names, documentation
- **README title** — "OpenSnap"
- **Landing page** — page title `<title>`, feature headings, body references
- **GitHub** — repository name, description

The full "OpenSnap" name remains unchanged everywhere. Only the header lockup uses the stacked OPEN / Snap format.

---

## Typography Hierarchy

| Role | Font | Weight | Tracking | Case |
|------|------|--------|----------|------|
| OPEN (family) | Segoe UI (Inter-equivalent) | Bold (700) | 0.06em | Uppercase |
| Snap (product) | Segoe UI (Inter-equivalent) | Medium (500) | 0em | Title Case |

OPEN is intentionally quieter than the product name — it reads as a category label, not part of the product name.

---

## Color Usage

| Token | Value | Usage |
|-------|-------|-------|
| Accent | `#4A9EFF` | Interactive elements, update status |
| Text primary | `#F0F0F0` / `#1a1a1a` | Snap wordmark |
| Text muted | `#888888` | OPEN wordmark, version text |
| Background dark | `#0d0d0d` | App dark theme |
| Background light | `#f9f9f9` | App light theme |

---

## Branding Rules

1. **OPEN never receives an icon** — no symbol, badge, monogram, or mark.
2. The application icon belongs only to OpenSnap — it is not the Open family mark.
3. Do not merge the icon into the typography.
4. Do not create a combined logo mark.
5. Preserve the stacked lockup layout — OPEN above Snap.
6. Never collapse the lockup to a single line except at extremely narrow widths.
7. The full name "OpenSnap" remains in window titles, tray, code, README, and landing page meta references.
8. Never redesign branding without explicit instruction.
9. Always follow Kovina standards first, then product-specific rules.
10. OpenPalette is the canonical reference for Open Product Family branding implementation.

---

## References

- [Kovina Manifesto](https://github.com/sparshsam/kovina/standards/KOVINA_MANIFESTO.md) — ecosystem philosophy
- [Kovina Brand Guidelines](https://github.com/sparshsam/kovina/standards/BRAND_GUIDELINES.md) — parent brand rules
- [Open Product Family Brand Guidelines](https://github.com/sparshsam/kovina/assets/branding/open/BRAND_GUIDELINES.md) — family brand rules
- [OpenPalette BRANDING.md](https://github.com/sparshsam/openpalette/docs/BRANDING.md) — canonical reference implementation
- [Kovina Design Playbook](https://github.com/sparshsam/kovina/standards/DESIGN_PLAYBOOK.md) — design conventions
