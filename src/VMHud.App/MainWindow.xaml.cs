using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using VMHud.Core.Models;
using VMHud.Core.ViewModels;
using System.Windows.Media;
using VMHud.Core.Contracts;
using VMHud.Core.Diagnostics;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace VMHud.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private IntPtr _hwnd;
    private bool _clickThroughEnabled = false; // force initial SetClickThrough(true) to apply
    private bool _autoHideOnRelease = false; // if overlay was visible on press, hide on release
    private bool _prevHoldV = false;
    private bool _prevHoldX = false;
    private bool _ctrlDown, _altDown, _vDown, _xDown;
    private LowLevelKeyboardProc? _hookProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private double _scale = 1.0;
    public IMatrixController? MatrixController { get; set; }
    private bool _dragCandidate = false;
    private bool _suppressNextTileClick = false;
    private System.Windows.Point _mouseDownPoint;
    private readonly Dictionary<int, float> _pendingStripGains = new();
    private readonly Dictionary<int, float> _pendingBusGains = new();
    private bool _autoHideEnabled;
    private TimeSpan _autoHideTimeout = TimeSpan.FromSeconds(2);
    private DateTime _lastInteractionUtc = DateTime.UtcNow;
    private DispatcherTimer? _autoHideTimer;
    private CancellationTokenSource? _gainCts;
    private Task? _gainTask;

    public bool IsClickThrough => _clickThroughEnabled;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Apply extended window styles (always clickable)
        _hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED; // keep layered always
        SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);
        SetClickThrough(false); // overlay is always clickable
        VMHud.Core.Diagnostics.Log.Info("MainWindow initialized; click-through enabled");
        // Constrain to work area to avoid off-screen growth; wrapping prevents scrollbars
        var wa = SystemParameters.WorkArea;
        MaxWidth = Math.Max(200, wa.Width - 32);
        MaxHeight = Math.Max(120, wa.Height - 32);

        // Install low-level keyboard hook for responsive hold detection
        _hookProc = HookCallback;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, IntPtr.Zero, 0);
        VMHud.Core.Diagnostics.Log.Info($"Keyboard hook installed: {_hookHandle != IntPtr.Zero}");

        // Load and apply settings (position, opacity, scale)
        var s = Settings.Load();
        if (s is not null)
        {
            Left = s.X;
            Top = s.Y;
            if (s.Opacity is >= 0.2 and <= 1.0) Opacity = s.Opacity;
            if (s.Scale is >= 0.5 and <= 3.0) ApplyScale(s.Scale); else ApplyScale(1.0);
            if (DataContext is MatrixViewModel mvm)
            {
                mvm.ShowVolumes = s.ShowVolumes;
                mvm.AutoHideEnabled = s.AutoHide;
            }
            _autoHideEnabled = s.AutoHide;
        }

        // Start realtime gain worker (latest value wins)
        _gainCts = new CancellationTokenSource();
        _gainTask = Task.Run(() => GainWorker(_gainCts.Token));

        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _autoHideTimer.Tick += (_, __) => CheckAutoHide();
        _autoHideTimer.Start();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        try { PersistSettings(); } catch { /* ignore */ }
        if (_hookHandle != IntPtr.Zero) UnhookWindowsHookEx(_hookHandle);
        try { _gainCts?.Cancel(); _gainTask?.Wait(100); } catch { }
        try { _autoHideTimer?.Stop(); } catch { }
        VMHud.Core.Diagnostics.Log.Info("MainWindow closing");
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RegisterInteraction();
        try { DragMove(); PersistSettings(); } catch { }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RegisterInteraction();
        // Do not start a window drag if the press began over an interactive control
        if (e.OriginalSource is DependencyObject origin && IsOverInteractiveElement(origin))
        {
            _dragCandidate = false;
            return;
        }
        _dragCandidate = true;
        _suppressNextTileClick = false;
        _mouseDownPoint = e.GetPosition(this);
    }

    private void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        RegisterInteraction();
        if (!_dragCandidate) return;
        if (e.LeftButton != MouseButtonState.Pressed) { _dragCandidate = false; return; }
        var p = e.GetPosition(this);
        var dx = Math.Abs(p.X - _mouseDownPoint.X);
        var dy = Math.Abs(p.Y - _mouseDownPoint.Y);
        if (dx > 4 || dy > 4)
        {
            try { DragMove(); PersistSettings(); } catch { }
            _dragCandidate = false;
            _suppressNextTileClick = true;
        }
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RegisterInteraction();
        _dragCandidate = false;
    }

    private void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            HideOverlay();
        }
        else
        {
            ShowOverlay();
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (data.vkCode != 0)
            {
                bool isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
                switch (data.vkCode)
                {
                    case VK_LCONTROL:
                    case VK_RCONTROL:
                        _ctrlDown = isDown; break;
                    case VK_LMENU: // Alt
                    case VK_RMENU:
                        _altDown = isDown; break;
                    case VK_V:
                        _vDown = isDown; break;
                    case VK_X:
                        _xDown = isDown; break;
                }
                UpdateHoldStates();
            }
        }
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void UpdateHoldStates()
    {
        bool holdV = _ctrlDown && _altDown && _vDown;
        bool holdX = _ctrlDown && _altDown && _xDown;

        if (holdV && !_prevHoldV)
        {
            // Press behavior
            bool wasVisible = Visibility == Visibility.Visible;
            if (!wasVisible)
            {
                ShowOverlay();
            }
            _autoHideOnRelease = wasVisible; // if visible before press, hide on release
        }
        else if (!holdV && _prevHoldV)
        {
            // Release behavior
            if (_autoHideOnRelease)
            {
                HideOverlay();
            }
            _autoHideOnRelease = false;
        }

        // No click-through toggling; overlay is always clickable

        // Exit hotkey (Ctrl+Alt+X) triggers on press
        if (holdX && !_prevHoldX)
        {
            System.Windows.Application.Current.Shutdown();
        }

        _prevHoldV = holdV;
        _prevHoldX = holdX;
    }

    private void SetClickThrough(bool enable)
    {
        if (_hwnd == IntPtr.Zero) return;
        // Always clickable: clear transparent flag, record state as false
        _clickThroughEnabled = false;
        var style = GetWindowLong(_hwnd, GWL_EXSTYLE);
        style &= ~WS_EX_TRANSPARENT;
        SetWindowLong(_hwnd, GWL_EXSTYLE, style);
        UpdateInteractionVisual(isInteractive: true);
    }

    private void ShowOverlay()
    {
        Show();
        Activate();
        RegisterInteraction();
        _autoHideTimer?.Start();
    }

    private void HideOverlay()
    {
        Hide();
        // Keep timer running; it will check state next time shown
    }

    private void RegisterInteraction()
    {
        _lastInteractionUtc = DateTime.UtcNow;
    }

    private void CheckAutoHide()
    {
        if (!_autoHideEnabled) return;
        if (Visibility != Visibility.Visible) return;
        if (DateTime.UtcNow - _lastInteractionUtc < _autoHideTimeout) return;
        HideOverlay();
    }

    private void UpdateInteractionVisual(bool isInteractive)
    {
        // Access the border created in XAML (RootBorder)
        if (FindName("RootBorder") is System.Windows.Controls.Border b)
        {
            if (isInteractive)
            {
                b.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3D, 0xD3, 0x8E));
                b.BorderThickness = new Thickness(2);
            }
            else
            {
                b.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
                b.BorderThickness = new Thickness(1);
            }
        }
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LMENU = 0xA4; // Alt
    private const int VK_RMENU = 0xA5;
    private const int VK_V = 0x56;
    private const int VK_X = 0x58;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    

    public void ApplyOpacity(double value)
    {
        if (value < 0.2) value = 0.2; if (value > 1.0) value = 1.0;
        Opacity = value;
        PersistSettings();
    }

    private void Tile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RegisterInteraction();
        // Always allow tile clicks
        if (MatrixController is null) return;
        if (sender is not System.Windows.Controls.Border cell) return;
        if (DataContext is not MatrixViewModel mvm) return;
        if (cell.DataContext is not StripViewModel svm) return;
        if (!int.TryParse(cell.Uid, out var busIndex)) return;
        if (_suppressNextTileClick) { _suppressNextTileClick = false; return; }

        // Determine current value and toggle
        bool current = busIndex switch
        {
            0 => svm.A1,
            1 => svm.A2,
            2 => svm.A3,
            3 => svm.A4,
            4 => svm.A5,
            5 => svm.B1,
            6 => svm.B2,
            7 => svm.B3,
            _ => false
        };
        var newVal = !current;

        // Optimistic UI update
        switch (busIndex)
        {
            case 0: svm.A1 = newVal; break;
            case 1: svm.A2 = newVal; break;
            case 2: svm.A3 = newVal; break;
            case 3: svm.A4 = newVal; break;
            case 4: svm.A5 = newVal; break;
            case 5: svm.B1 = newVal; break;
            case 6: svm.B2 = newVal; break;
            case 7: svm.B3 = newVal; break;
        }

        // Apply via controller; backend will push confirmed state shortly
        try { MatrixController.SetRoute(svm.Id, busIndex, newVal); }
        catch { /* swallow */ }
    }

    private void StripVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MatrixController is null) return;
        if (sender is not System.Windows.Controls.Slider s) return;
        if (s.DataContext is not StripViewModel svm) return;
        RegisterInteraction();
        Log.Info($"UI StripVolume ValueChanged: strip={svm.Id} value={e.NewValue:F2} adjusting={svm.IsAdjustingVolume}");
        if (!svm.IsAdjustingVolume) return; // send only while user is adjusting
        _pendingStripGains[svm.Id] = (float)e.NewValue;
    }

    private void BusVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MatrixController is null) return;
        if (sender is not System.Windows.Controls.Slider s) return;
        if (s.DataContext is not BusViewModel bvm) return;
        RegisterInteraction();
        Log.Info($"UI BusVolume ValueChanged: bus={bvm.Index} value={e.NewValue:F2} adjusting={bvm.IsAdjustingGain}");
        if (!bvm.IsAdjustingGain) return; // send only while user is adjusting
        _pendingBusGains[bvm.Index] = (float)e.NewValue;
    }

    private void StripVolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Slider s && s.DataContext is StripViewModel svm)
        {
            RegisterInteraction();
            if (e.ClickCount == 2)
            {
                svm.Volume = 0;
                if (MatrixController is not null) { try { MatrixController.SetStripGain(svm.Id, 0f); } catch { } }
                e.Handled = true;
                return;
            }
            svm.IsAdjustingVolume = true;
            Log.Info($"UI StripVolume DragStarted: strip={svm.Id} current={svm.Volume:F2}");
        }
    }

    private void StripVolumeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Slider s && s.DataContext is StripViewModel svm)
        {
            svm.IsAdjustingVolume = false;
            RegisterInteraction();
            Log.Info($"UI StripVolume DragCompleted: strip={svm.Id} final={svm.Volume:F2}");
            // Send final value immediately
            if (MatrixController is not null)
            {
                try { MatrixController.SetStripGain(svm.Id, (float)svm.Volume); } catch { }
            }
            _pendingStripGains.Remove(svm.Id);
            // Suppress backend snapback for a short grace window
            svm.SuppressUntilUtc = DateTime.UtcNow.AddMilliseconds(250);
        }
    }

    private void BusVolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Slider s && s.DataContext is BusViewModel bvm)
        {
            RegisterInteraction();
            if (e.ClickCount == 2)
            {
                bvm.Gain = 0;
                if (MatrixController is not null) { try { MatrixController.SetBusGain(bvm.Index, 0f); } catch { } }
                e.Handled = true;
                return;
            }
            bvm.IsAdjustingGain = true;
            Log.Info($"UI BusVolume DragStarted: bus={bvm.Index} current={bvm.Gain:F2}");
        }
    }

    private void BusVolumeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Slider s && s.DataContext is BusViewModel bvm)
        {
            bvm.IsAdjustingGain = false;
            RegisterInteraction();
            Log.Info($"UI BusVolume DragCompleted: bus={bvm.Index} final={bvm.Gain:F2}");
            if (MatrixController is not null)
            {
                try { MatrixController.SetBusGain(bvm.Index, (float)bvm.Gain); } catch { }
            }
            _pendingBusGains.Remove(bvm.Index);
            bvm.SuppressUntilUtc = DateTime.UtcNow.AddMilliseconds(250);
        }
    }

    private void FlushPendingGains()
    {
        if (MatrixController is null) return;
        Dictionary<int, float> strips, buses;
        lock (_pendingStripGains) strips = new Dictionary<int, float>(_pendingStripGains);
        lock (_pendingBusGains) buses = new Dictionary<int, float>(_pendingBusGains);
        foreach (var kv in strips)
        {
            try { MatrixController.SetStripGain(kv.Key, kv.Value); } catch { }
        }
        foreach (var kv in buses)
        {
            try { MatrixController.SetBusGain(kv.Key, kv.Value); } catch { }
        }
        lock (_pendingStripGains) _pendingStripGains.Clear();
        lock (_pendingBusGains) _pendingBusGains.Clear();
    }

    private void GainWorker(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                FlushPendingGains();
                Thread.Sleep(8); // ~120 Hz update cadence
            }
        }
        catch { }
    }

    private bool IsGloballyInteractive() => true;

    private static bool IsOverInteractiveElement(DependencyObject el)
    {
        // Treat sliders and routing tiles (Border with numeric Uid) as interactive
        while (el != null)
        {
            if (el is System.Windows.Controls.Slider) return true;
            if (el is System.Windows.Controls.Border b && int.TryParse(b.Uid, out _)) return true;
            el = VisualTreeHelper.GetParent(el);
        }
        return false;
    }

    public void ApplyScale(double scale)
    {
        if (scale < 0.5) scale = 0.5; if (scale > 3.0) scale = 3.0;
        _scale = scale;
        if (FindName("RootBorder") is System.Windows.Controls.Border b)
        {
            b.LayoutTransform = new System.Windows.Media.ScaleTransform(_scale, _scale);
        }
        PersistSettings();
        // Re-apply constraints in case the content size changed notably
        var wa = SystemParameters.WorkArea;
        MaxWidth = Math.Max(200, wa.Width - 32);
        MaxHeight = Math.Max(120, wa.Height - 32);
    }

    public void PersistSettings()
    {
        var vm = DataContext as MatrixViewModel;
        var showVolumes = vm?.ShowVolumes ?? false;
        var autoHide = vm?.AutoHideEnabled ?? _autoHideEnabled;
        var autoHideSeconds = vm?.AutoHideSeconds ?? (int)_autoHideTimeout.TotalSeconds;
        try { Settings.Save(new Settings(Left, Top, Opacity, _scale, showVolumes, autoHide, autoHideSeconds)); } catch { }
    }

    public void SetShowVolumes(bool on)
    {
        if (DataContext is MatrixViewModel) PersistSettings();
    }

    public void SetAutoHideEnabled(bool on)
    {
        _autoHideEnabled = on;
        if (on)
        {
            _autoHideTimer?.Start();
            RegisterInteraction();
        }
        else
        {
            _autoHideTimer?.Stop();
        }
        PersistSettings();
    }

    public void SetAutoHideSeconds(int seconds)
    {
        if (seconds < 1) seconds = 1;
        if (seconds > 60) seconds = 60;
        _autoHideTimeout = TimeSpan.FromSeconds(seconds);
        RegisterInteraction();
        PersistSettings();
    }

    private sealed record Settings(double X, double Y, double Opacity, double Scale = 1.0, bool ShowVolumes = false, bool AutoHide = false, int AutoHideSeconds = 2)
    {
        public static string Path => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VMHud",
            "config.json");

        public static Settings? Load()
        {
            try
            {
                if (!File.Exists(Path)) return null;
                var json = File.ReadAllText(Path);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            catch { return null; }
        }

        public static void Save(Settings s)
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path, json);
        }
    }
}
