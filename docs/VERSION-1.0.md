# VMHud v1.0.0 — Release Notes

Date: 2025-09-27

## Highlights
- Always‑on‑top WPF overlay for Voicemeeter Potato (works with Banana/Standard)
- Real‑time routing matrix: per‑strip tiles for A1..A5 and B1..B3 with live labels
- Global hotkey: Ctrl+Alt+V to show; Ctrl+Alt+X to exit
- Always clickable overlay; drag window from header or empty area
- Theme support: presets and color pickers (background, border, A/B on colors, text)
- Volume control:
  - Per‑input sliders (−60..+12 dB) under each strip
  - Per‑bus sliders on the right side
  - Double‑click a slider to reset to 0.0 dB
  - Real‑time gain sending with background worker and post‑drag suppression
- System tray
  - Show/Hide overlay, Open Voicemeeter, Open Logs, Settings, Exit
- Settings window
  - Resizable + scrollable
  - Live apply: opacity, scale, theme, show/hide volume sliders, start with Windows
- Resilient backend
  - Auto‑detect edition → dynamic strip/bus counts
  - Uses dirty flag; dedups snapshots; graceful reconnect with backoff
  - 32/64‑bit interop handling (VoicemeeterRemote.dll / VoicemeeterRemote64.dll)
- Logging
  - Rolling log under `%LocalAppData%/VMHud/logs` for diagnostics

## Known Limitations
- Volume gains are smoothed to avoid snapbacks; extreme jitter from external automation may still appear delayed.
- Hotkeys are fixed (Ctrl+Alt+V/X) — no UI to remap yet.
- No installer/winget yet; distribution is via published folder.
- No localization; English only.

## Compatibility
- Windows 10/11, .NET 8, Voicemeeter installed (Potato recommended)
- Ensure the Voicemeeter Remote DLLs are available via default install path or PATH

## Build & Run
- See `docs/BUILD.md` for detailed steps.

---

# Next Steps (Shortlist)

## Stability & Performance
- Add setting to choose volume behavior: real‑time during drag vs send‑on‑release
- Tune gain worker cadence based on system capability (e.g., 60–200 Hz) and add optional coalescing
- Clamp incoming backend gains to Voicemeeter quantization to eliminate minor bounce

## UX & Controls
- Slider polish: snap to 0 when near ±0.2 dB; Ctrl‑snap while dragging
- Colorize thumbs by context (e.g., A vs B) via theme brushes
- Optional “drag from header only” toggle in Settings
- Add compact mode (smaller tiles, tighter spacing) and max columns setting

## Settings & Hotkeys
- Hotkey remap UI for Show/Hide and Exit (with conflict detection)
- Import/Export theme JSON and “Reset Theme” button
- Persist “real‑time vs release” volume mode and cadence

## Packaging & Docs
- Publish profiles for single‑file win‑x64 and optional MSIX/MSI + winget manifest
- Optional code signing
- README: screenshots/GIF, troubleshooting (DLL/bitness), and performance tips

## Backend Enhancements
- Probe and adapt to actual available strips/buses at runtime (parameter probing)
- Surface diagnostics (update rate, dedup ratio, set latency) in logs for field triage

---

If you want, I can start by adding the hotkey remap UI and the volume behavior toggle, or jump to packaging (MSIX/winget) for distribution.
