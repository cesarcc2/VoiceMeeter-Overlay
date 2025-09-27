# VMHud Technical Specification

This spec defines the behavior and internal contracts for VMHud, a WPF overlay that displays Voicemeeter Potato routing (A1..A5, B1..B3) for each input strip.

## Scope

- Windows 10/11 only
- .NET 8, WPF
- Reads state via Voicemeeter Remote API
- Non-invasive: read-only; no mixer control in v1

## Functional Requirements

- Overlay shows a matrix:
  - Columns: input strips in order reported by Voicemeeter
  - Rows: A1..A5 then B1..B3
  - Cell states: enabled = filled/bright; disabled = dim/outlined
- Global hotkey toggles overlay visibility (default: Ctrl+Alt+V)
- Optional: show-while-held (visible only while hotkey is pressed)
- Optional: auto-hide after inactivity delay (ms)
- Click-through by default; modifier or context action to re-enable hit testing
- Position and scale configurable and persisted
- Graceful behavior when Voicemeeter is not running (idle, no errors)

## Non-Functional Requirements

- Startup < 100ms after process start where feasible
- Idle CPU usage < ~1% on modern CPUs
- Low latency updates (< 100ms typical) without over-polling
- Robust reconnect logic; no crashes on API failures

## Integrations

- Voicemeeter Remote API (installed with Voicemeeter). Typical install paths include:
  - `C:\\Program Files (x86)\\VB\\Voicemeeter` (Potato)
  - The DLL `VoicemeeterRemote.dll` must be available at runtime via PATH or copied next to the executable (licensing considerations apply; not shipped in repo).

## Project Contracts

- Backend → Core:
  - `IMatrixStateProvider`
    - `MatrixState GetSnapshot()`
    - `IObservable<MatrixState> Updates { get; }`
    - `bool IsConnected { get; }`
  - `IBackendController`
    - `Task StartAsync(CancellationToken)`
    - `Task StopAsync()`

- Core → App (UI):
  - `MatrixViewModel`
    - `ObservableCollection<StripViewModel> Strips`
    - `IReadOnlyList<string> Rows => [A1..A5, B1..B3]`
  - `IHotkeyService`
    - `Register(Hotkey, Action onPressed, Action? onReleased)`
    - `Unregister(Hotkey)`

## Data Model

```csharp
public enum Bus { A1, A2, A3, A4, A5, B1, B2, B3 }

public sealed class InputStrip {
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool[] Outputs { get; } = new bool[8]; // A1..A5,B1..B3
}

public sealed class MatrixState {
    public IReadOnlyList<InputStrip> Strips { get; init; } = Array.Empty<InputStrip>();
    public DateTime TimestampUtc { get; init; }
}
```

## Polling Loop (Pseudocode)

```csharp
while (!ct.IsCancellationRequested) {
    var ok = api.TryConnect();
    if (!ok) { await Delay(Backoff()); continue; }

    var snapshot = ReadAll();
    if (!AreEqual(snapshot, last)) {
        last = snapshot;
        Updates.OnNext(snapshot);
    }

    await Task.Delay(interval, ct); // e.g., 50ms
}
```

`ReadAll()` reads strip count, names, and each routing flag. `AreEqual` should be O(n) shallow compare on booleans and strip names.

## UI Specification

- WPF window settings:
  - `WindowStyle=None`, `ResizeMode=NoResize`, `Topmost=True`
  - `AllowsTransparency=True`, `Background=Transparent`
  - Click-through: set `WS_EX_TRANSPARENT` and `WS_EX_LAYERED` via `WindowInteropHelper`; or toggle `IsHitTestVisible=false` on root panel

- Layout:
  - `ItemsControl` for columns bound to strips
  - Each column renders 8 cells for A/B buses
  - Visual changes on state flip: brief color pulse or fade

## Hotkey Registration

- Windows global hotkey via `RegisterHotKey(HWND, id, modifiers, vk)`
- Provide a fallback if registration fails (already in use): show message and allow rebind

## Configuration

`%AppData%/VMHud/config.json` example:

```json
{
  "Position": { "X": 16, "Y": 16 },
  "Scale": 1.0,
  "Opacity": 0.9,
  "Hotkey": "Ctrl+Alt+V",
  "ShowWhileHeld": false,
  "AutoHideMs": 0,
  "PollIntervalMs": 50
}
```

## Logging

- Minimal logging by default; debug mode writes to `%LocalAppData%/VMHud/logs/` with rolling files
- Log connect/disconnect, errors, and hotkey registration events

## Testing Notes

- Mock the Remote API to simulate routing changes and disconnects
- Verify VM updates dispatch on UI thread (Dispatcher)

