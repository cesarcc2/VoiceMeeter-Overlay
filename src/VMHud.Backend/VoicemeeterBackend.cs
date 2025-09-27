using System;
using System.Threading;
using VMHud.Core.Contracts;
using VMHud.Core.Models;

namespace VMHud.Backend;

public sealed class VoicemeeterBackend : IMatrixStateProvider, IBackendController, VMHud.Core.Contracts.IMatrixController
{
    private readonly VMHud.Backend.SimpleSubject<MatrixState> _updates = new VMHud.Backend.SimpleSubject<MatrixState>();
    private Timer? _timer;
    private volatile bool _connected;
    private VMHud.Core.Models.BackendStatus _status = VMHud.Core.Models.BackendStatus.Connecting;
    private int _inputCount = 0;
    private int _busCount = 8; // Potato default (A1..A5 + B1..B3)
    private MatrixState _current = new() { Strips = Array.Empty<InputStrip>() };
    private int _backoffMs = 250;
    private readonly int _maxBackoffMs = 5000;
    private DateTime _nextReconnect = DateTime.MinValue;
    private DateTime _lastFullRefresh = DateTime.MinValue;

    public MatrixState GetSnapshot() => _current;
    public IObservable<MatrixState> Updates => _updates;
    public bool IsConnected => _connected;
    public VMHud.Core.Models.BackendStatus Status => _status;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to locate DLL from default install directories
            VoicemeeterDllLocator.EnsureSearchPath();
            // Try to login and detect VM type
            _connected = TryLogin(out _inputCount, out _busCount);
            _status = _connected ? VMHud.Core.Models.BackendStatus.Connected : VMHud.Core.Models.BackendStatus.Connecting;
            _backoffMs = 250; _nextReconnect = DateTime.UtcNow;
            _timer = new Timer(_ => Tick(), null, 200, 50);
            VMHud.Core.Diagnostics.Log.Info($"VoicemeeterBackend Start: connected={_connected}, inputCount={_inputCount}");
        }
        catch (DllNotFoundException)
        {
            _connected = false;
            _status = VMHud.Core.Models.BackendStatus.Disconnected;
            VMHud.Core.Diagnostics.Log.Error("Voicemeeter DLL not found");
        }
        catch
        {
            _connected = false;
            _status = VMHud.Core.Models.BackendStatus.Disconnected;
            VMHud.Core.Diagnostics.Log.Error("VoicemeeterBackend Start failed");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _timer?.Dispose();
        _timer = null;
        if (_connected)
        {
            try { VoicemeeterInterop.VBVMR_Logout(); } catch { }
        }
        _connected = false;
        return Task.CompletedTask;
    }

    private static bool TryLogin(out int inputCount, out int busCount)
    {
        inputCount = 0;
        busCount = 8;
        // 0 = ok; <0 = not installed; >0 = retry later (engine starting)
        var rc = VoicemeeterInterop.VBVMR_Login();
        if (rc < 0) return false;
        // Even if rc>0, try getting type — it returns 0 when engine is ready
        if (VoicemeeterInterop.VBVMR_GetVoicemeeterType(out var type) != 0) return false;
        // Map type -> input strip count and bus count
        // 1: Voicemeeter (2 HW + 1 virtual = 3), 2: Banana (3 + 2 = 5), 3: Potato (5 + 3 = 8)
        inputCount = type switch { 1 => 3, 2 => 5, 3 => 8, _ => 8 };
        busCount   = type switch { 1 => 3, 2 => 5, 3 => 8, _ => 8 };
        return true;
    }

    private void Tick()
    {
        try
        {
            if (!_connected)
            {
                // Attempt reconnect with backoff
                if (DateTime.UtcNow < _nextReconnect) return;
                _connected = TryLogin(out _inputCount, out _busCount);
                if (!_connected)
                {
                    _status = VMHud.Core.Models.BackendStatus.Connecting;
                    _backoffMs = Math.Min(_backoffMs * 2, _maxBackoffMs);
                    _nextReconnect = DateTime.UtcNow.AddMilliseconds(_backoffMs);
                    return;
                }
                _status = VMHud.Core.Models.BackendStatus.Connected;
                _backoffMs = 250;
                _nextReconnect = DateTime.UtcNow;
                VMHud.Core.Diagnostics.Log.Info($"Voicemeeter reconnected. inputCount={_inputCount}");
            }

            // Throttle reads using dirty flag. Force refresh every 2s to keep labels updated.
            var now = DateTime.UtcNow;
            var dirty = false;
            try { dirty = VoicemeeterInterop.VBVMR_IsParametersDirty() != 0; } catch { dirty = true; }
            if (!dirty && (now - _lastFullRefresh).TotalMilliseconds < 2000)
            {
                return;
            }

            var strips = new InputStrip[_inputCount];
            var stripGains = new float[_inputCount];
            for (int i = 0; i < _inputCount; i++)
            {
                var strip = new InputStrip { Id = i, Name = GetStripName(i) };
                // A1..A5
                for (int a = 1; a <= 5; a++)
                {
                    strip.Outputs[a - 1] = GetFlag($"Strip[{i}].A{a}");
                }
                // B1..B3
                for (int b = 1; b <= 3; b++)
                {
                    strip.Outputs[5 + (b - 1)] = GetFlag($"Strip[{i}].B{b}");
                }
                if (VoicemeeterInterop.VBVMR_GetParameterFloat($"Strip[{i}].Gain", out var g) == 0) stripGains[i] = g;
                strips[i] = strip;
            }

            // Bus names sized to bus count; fetch labels with fallbacks
            var busNames = new string[_busCount];
            for (int b = 0; b < _busCount; b++) busNames[b] = GetBusName(b);
            // Bus gains
            var busGains = new float[_busCount];
            for (int b = 0; b < _busCount; b++)
            {
                if (VoicemeeterInterop.VBVMR_GetParameterFloat($"Bus[{b}].Gain", out var g) == 0) busGains[b] = g;
            }

            _lastFullRefresh = now;
            if (_current is not null && AreEqual(_current, strips, busNames, stripGains, busGains))
            {
                return;
            }
            var snapshot = new MatrixState { Strips = strips, TimestampUtc = now, BusNames = busNames, StripGains = stripGains, BusGains = busGains };
            _current = snapshot;
            _updates.OnNext(snapshot);
        }
        catch (DllNotFoundException)
        {
            _connected = false;
            _status = VMHud.Core.Models.BackendStatus.Disconnected;
            VMHud.Core.Diagnostics.Log.Error("Voicemeeter DLL not found during Tick");
        }
        catch
        {
            _connected = false;
            _status = VMHud.Core.Models.BackendStatus.Disconnected;
            VMHud.Core.Diagnostics.Log.Error("VoicemeeterBackend Tick error");
        }
    }

    private static bool GetFlag(string param)
    {
        try
        {
            if (VoicemeeterInterop.VBVMR_GetParameterFloat(param, out var f) == 0)
                return f >= 0.5f;
        }
        catch { }
        return false;
    }

    private static string GetStripName(int i)
    {
        try
        {
            if (VoicemeeterInterop.TryGetStringA($"Strip[{i}].Label", out var s) && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        catch { }
        return $"Strip {i + 1}";
    }

    private static string GetBusName(int index)
    {
        try
        {
            if (VoicemeeterInterop.TryGetStringA($"Bus[{index}].Label", out var s) && !string.IsNullOrWhiteSpace(s))
                return s;
        }
        catch { }
        // Fallback to A1..A5, B1..B3 based on index
        return index switch
        {
            0 => "A1",
            1 => "A2",
            2 => "A3",
            3 => "A4",
            4 => "A5",
            5 => "B1",
            6 => "B2",
            7 => "B3",
            _ => $"Bus {index + 1}"
        };
    }

    public void SetRoute(int stripIndex, int busIndex, bool enabled)
    {
        try
        {
            string param = busIndex switch
            {
                0 => $"Strip[{stripIndex}].A1",
                1 => $"Strip[{stripIndex}].A2",
                2 => $"Strip[{stripIndex}].A3",
                3 => $"Strip[{stripIndex}].A4",
                4 => $"Strip[{stripIndex}].A5",
                5 => $"Strip[{stripIndex}].B1",
                6 => $"Strip[{stripIndex}].B2",
                7 => $"Strip[{stripIndex}].B3",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(param)) return;
            VoicemeeterInterop.VBVMR_SetParameterFloat(param, enabled ? 1.0f : 0.0f);
        }
        catch { }
    }

    public void SetStripGain(int stripIndex, float gainDb)
    {
        try { VoicemeeterInterop.VBVMR_SetParameterFloat($"Strip[{stripIndex}].Gain", gainDb); } catch { }
    }

    public void SetBusGain(int busIndex, float gainDb)
    {
        try { VoicemeeterInterop.VBVMR_SetParameterFloat($"Bus[{busIndex}].Gain", gainDb); } catch { }
    }
    private static bool AreEqual(MatrixState prev, InputStrip[] nextStrips, string[] nextBusNames, float[] nextStripGains, float[] nextBusGains)
    {
        try
        {
            if (prev.BusNames.Count != nextBusNames.Length) return false;
            for (int i = 0; i < nextBusNames.Length; i++)
            {
                if (!string.Equals(prev.BusNames[i], nextBusNames[i], StringComparison.Ordinal)) return false;
            }
            if (prev.Strips.Count != nextStrips.Length) return false;
            for (int i = 0; i < nextStrips.Length; i++)
            {
                var a = prev.Strips[i];
                var b = nextStrips[i];
                if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal)) return false;
                if (a.Outputs.Length != b.Outputs.Length) return false;
                for (int j = 0; j < a.Outputs.Length; j++)
                {
                    if (a.Outputs[j] != b.Outputs[j]) return false;
                }
            }
            if (prev.StripGains.Count != nextStripGains.Length) return false;
            for (int i = 0; i < nextStripGains.Length; i++) if (prev.StripGains[i] != nextStripGains[i]) return false;
            if (prev.BusGains.Count != nextBusGains.Length) return false;
            for (int i = 0; i < nextBusGains.Length; i++) if (prev.BusGains[i] != nextBusGains[i]) return false;
            return true;
        }
        catch { return false; }
    }
}
