# VMHud - Voicemeeter Output Matrix HUD (Windows/WPF)

A tiny, always-on-top overlay for Voicemeeter Potato that shows, at a glance, which outputs (A1..A5, B1..B3) are enabled for each input strip. Toggle it with a global hotkey and keep playing without opening the mixer UI.

> OS: Windows 10/11  
> UI: .NET 8 + WPF  
> Scope: Windows only (Voicemeeter is Windows-only)

## Features
- Overlay HUD in the top-left corner (position configurable)
- Click-through, borderless, always on top, transparent background
- Global hotkey (default: `Ctrl+Alt+V`) to show/hide
- Live sync via Voicemeeter Remote API with low overhead
- Shows a grid: columns = input strips, rows = A1..A5 | B1..B3
- Auto-hide timer and 'show while held' mode

## Repository Layout (expected)

/ (repo root)
- README.md
- CONTRIBUTING.md
- LICENSE (placeholder)
- .editorconfig
- .gitattributes
- src/
  - VMHud.App/           (WPF app)
  - VMHud.Backend/       (Voicemeeter interop + polling)
  - VMHud.Core/          (models, viewmodels, config)
  - VMHud.Tests/         (unit tests where feasible)
- docs/
  - ARCHITECTURE.md
  - TECH-SPEC.md
  - BUILD.md
  - RELEASE.md
  - ROADMAP.md

