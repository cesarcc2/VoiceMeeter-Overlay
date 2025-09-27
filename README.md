# VMHud — Voicemeeter Output Matrix HUD (Windows/WPF)

Note: This project was created using ChatGPT Codex in agent mode (assistive coding workflow).

A tiny, always-on-top overlay for Voicemeeter Potato that shows, at a glance, which outputs (A1–A5, B1–B3) are enabled for each input strip. Toggle with a global hotkey and keep playing without opening the mixer UI.

- OS: Windows 10/11
- UI: .NET 8 + WPF
- Scope: Windows only (Voicemeeter is Windows-only)

## Features
- Overlay HUD in the top-left corner (position configurable)
- Click-through, borderless, always on top, transparent background
- Global hotkey (default: Ctrl+Alt+V) to show/hide
- Live sync via Voicemeeter Remote API with low overhead
- Grid view: columns = input strips; rows = A1..A5 | B1..B3
- Auto-hide timer and optional show-while-held mode

## Docs
- Architecture: docs/ARCHITECTURE.md
- Technical Spec: docs/TECH-SPEC.md
- Build: docs/BUILD.md
- Release: docs/RELEASE.md
- Roadmap: docs/ROADMAP.md

## Quickstart

Prereqs: .NET 8 SDK, Windows 10/11, Voicemeeter Potato installed.

- Build everything:
  - `dotnet build -c Debug`
- Run the app:
  - `dotnet run -c Debug --project src/VMHud.App`
- Publish single-file:
  - See docs/BUILD.md

If Voicemeeter is not running, the overlay stays idle. When Voicemeeter starts, the matrix updates automatically.

## Status
This repo currently contains a minimal project skeleton to align with the docs. The backend uses placeholders; functional integration with Voicemeeter will follow per the roadmap.

## License
TBD (placeholder)
