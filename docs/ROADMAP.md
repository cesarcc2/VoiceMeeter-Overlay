# Roadmap

High-level plan for VMHud features and improvements. Priorities may change based on feedback.

## Milestone 1 — MVP

- Overlay displays A1..A5 and B1..B3 for all input strips
- Global hotkey to toggle visibility (default Ctrl+Alt+V)
- Click-through overlay, always-on-top, transparent
- Basic JSON config (position, scale, opacity, hotkey)
- Resilient connect/reconnect to Voicemeeter

## Milestone 2 — Polish

- Show-while-held mode and auto-hide timer
- Headers: bus labels and strip names
- Color themes (light/dark, high-contrast)
- Subtle pulse animation on state change
- Configurable poll interval

## Milestone 3 — UX and Controls

- Temporary interaction mode (hold modifier to drag/move)
- Snap overlay to screen edges; multi-monitor support
- Simple settings UI panel
- Hotkey rebinding UI with collision detection

## Milestone 4 — Packaging

- Single-file Release builds
- Optional code signing
- Installer package (MSIX or MSI) and winget manifest

## Milestone 5 — Advanced

- Localization support (en-US baseline)
- Optional telemetry toggle (opt-in only) for crashes/perf
- Profiles per game/app (advanced scenario)

## Nice-to-haves

- Outline mode with very low visual footprint
- Per-strip filters (show only active strips)
- Visual scaling presets for stream overlays

