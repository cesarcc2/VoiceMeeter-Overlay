# VMHud Architecture

This document describes the overall architecture of VMHud — a Windows-only WPF overlay that displays the Voicemeeter Potato output matrix (A1..A5, B1..B3) for each input strip.

## Goals
- Keep the overlay extremely lightweight and unobtrusive
- Reflect Voicemeeter state with minimal latency and CPU usage
- Be safe and robust when Voicemeeter is not running or reconnects
- Simple to build, package, and update

## Solution Layout

Monorepo with three projects and tests:
- `src/VMHud.App` (WPF UI, views, resources)
- `src/VMHud.Core` (models, viewmodels, configuration, hotkey service interfaces)
- `src/VMHud.Backend` (Voicemeeter interop and polling + glue to Core)
- `src/VMHud.Tests` (unit tests where feasible)

## Runtime Components

- Overlay Window (WPF)
  - Borderless, topmost, transparent, click-through by default
  - Binds to a `MatrixViewModel` that exposes a grid of booleans for A/B outputs per input strip

- Backend Poller
  - Connects to Voicemeeter Remote API
  - Polls strip -> bus routing (A1..A5, B1..B3) and publishes changes
  - Handles initial connect/disconnect/reconnect logic

- Hotkey Manager
  - Global hotkey (default Ctrl+Alt+V) to toggle overlay visibility
  - Optional "show while held" mode and auto-hide timeout

- Configuration
  - Simple JSON config stored per-user (e.g., `%AppData%/VMHud/config.json`)
  - Position, scale, colors, opacity, hotkey, behavior flags

## Data Flow

1) Backend connects to Voicemeeter and starts a low-interval polling loop (e.g., 30–100ms configurable)
2) It reads current routing state for each input strip and compares with last snapshot
3) On changes, it raises events or updates an observable state service in Core
4) ViewModels subscribe to these updates and raise property change notifications
5) Overlay window updates the grid through WPF bindings

## Concurrency and Threading

- Backend polling runs on a background thread or timer
- UI updates are marshalled onto the WPF UI thread (Dispatcher)
- State merges are designed to be idempotent and lightweight

## Failure and Resilience

- If Voicemeeter is not running, backend stays idle and retries periodically
- Reconnect on API errors with exponential backoff (bounded)
- Defensive parsing and bounds checking for strip counts and buses

## Model Overview

- `InputStrip` — id, name, type (physical/virtual), routing `A1..A5`, `B1..B3`
- `OutputBus` — enumerations for A1..A5, B1..B3
- `MatrixState` — snapshot of routing for all input strips
- `AppSettings` — window position, scale, theme, hotkey, timers

## UI Overview

- Grid layout: columns per input strip; rows: A1..A5 then B1..B3
- Cells show enabled/disabled via color and subtle animation on change
- Optional headers: strip names and bus labels
- Click-through by default; hold modifier to allow dragging or context menu

## External Dependencies

- .NET 8.0, WPF
- Voicemeeter Remote API (installed with Voicemeeter; not redistributed here)
- Optional: community C# wrapper or direct P/Invoke bindings

## Configuration Storage

- File: `%AppData%/VMHud/config.json`
- Layout example:
  ```json
  {
    "Position": { "X": 16, "Y": 16 },
    "Scale": 1.0,
    "Opacity": 0.9,
    "Hotkey": "Ctrl+Alt+V",
    "ShowWhileHeld": false,
    "AutoHideMs": 0
  }
  ```

## Security and Privacy

- No network calls; all local to machine
- No telemetry by default
- Only interacts with Voicemeeter via its official Remote API

## Testing Strategy

- Core models and viewmodels: pure unit tests
- Backend polling: mock API surface to simulate state changes and disconnects
- UI: light MVVM tests; manual verification for visuals

