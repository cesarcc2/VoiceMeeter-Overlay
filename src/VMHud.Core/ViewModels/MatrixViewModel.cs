using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VMHud.Core.Models;
using VMHud.Core.Diagnostics;

namespace VMHud.Core.ViewModels;

public class MatrixViewModel : INotifyPropertyChanged
{
    public IReadOnlyList<string> Rows { get; } = new[] { "A1", "A2", "A3", "A4", "A5", "B1", "B2", "B3" };

    public ObservableCollection<StripViewModel> Strips { get; } = new();
    public ObservableCollection<BusViewModel> Buses { get; } = new();

    private BackendStatus _status = BackendStatus.Connecting;
    public BackendStatus Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }
    }

    private IReadOnlyList<string> _busNames = new[] { "A1", "A2", "A3", "A4", "A5", "B1", "B2", "B3" };
    public IReadOnlyList<string> BusNames
    {
        get => _busNames;
        private set { _busNames = value; OnPropertyChanged(); }
    }

    private bool _showVolumes;
    public bool ShowVolumes
    {
        get => _showVolumes;
        set { if (_showVolumes != value) { _showVolumes = value; OnPropertyChanged(); } }
    }

    public string StatusText => Status switch
    {
        BackendStatus.Connected => "Connected",
        BackendStatus.Connecting => "Connecting…",
        BackendStatus.Simulated => "Simulated",
        BackendStatus.Disconnected => "Disconnected",
        _ => "Unknown"
    };

    public void Update(MatrixState state)
    {
        SyncStrips(state.Strips, state.PhysicalInputCount, state.StripGains);
        BusNames = state.BusNames;
        SyncBuses(state.BusNames, state.BusGains);
    }

    private void SyncStrips(IReadOnlyList<InputStrip> modelStrips, int physicalCount, IReadOnlyList<float> gains)
    {
        // Adjust count
        while (Strips.Count < modelStrips.Count)
            Strips.Add(new StripViewModel());
        while (Strips.Count > modelStrips.Count)
            Strips.RemoveAt(Strips.Count - 1);

        // Update in place to minimize UI churn
        for (int i = 0; i < modelStrips.Count; i++)
        {
            var vm = Strips[i];
            var m = modelStrips[i];
            vm.Id = m.Id;
            vm.Name = m.Name;
            vm.IsPhysical = i < physicalCount;
            if (m.Outputs.Length >= 8)
            {
                vm.A1 = m.Outputs[0];
                vm.A2 = m.Outputs[1];
                vm.A3 = m.Outputs[2];
                vm.A4 = m.Outputs[3];
                vm.A5 = m.Outputs[4];
                vm.B1 = m.Outputs[5];
                vm.B2 = m.Outputs[6];
                vm.B3 = m.Outputs[7];
            }
            if (gains.Count > i)
            {
                var now = DateTime.UtcNow;
                if (vm.IsAdjustingVolume || now < vm.SuppressUntilUtc)
                {
                    Log.Info($"Skip apply strip gain (adjust/suppress): i={i} vm={vm.Volume:F2} backend={gains[i]:F2}");
                }
                else if (Math.Abs(vm.Volume - gains[i]) > 0.5)
                {
                    Log.Info($"Apply strip gain from backend: i={i} new={gains[i]:F2}");
                    vm.Volume = gains[i];
                }
            }
        }
    }

    private void SyncBuses(IReadOnlyList<string> names, IReadOnlyList<float> gains)
    {
        var count = System.Math.Min(names.Count, gains.Count);
        while (Buses.Count < count) Buses.Add(new BusViewModel());
        while (Buses.Count > count) Buses.RemoveAt(Buses.Count - 1);
        for (int i = 0; i < count; i++)
        {
            var b = Buses[i];
            b.Index = i;
            b.Name = names[i];
            var nowb = DateTime.UtcNow;
            if (b.IsAdjustingGain || nowb < b.SuppressUntilUtc)
            {
                Log.Info($"Skip apply bus gain (adjust/suppress): i={i} vm={b.Gain:F2} backend={gains[i]:F2}");
            }
            else if (Math.Abs(b.Gain - gains[i]) > 0.5)
            {
                Log.Info($"Apply bus gain from backend: i={i} new={gains[i]:F2}");
                b.Gain = gains[i];
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
