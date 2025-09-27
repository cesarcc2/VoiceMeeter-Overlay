using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VMHud.Core.ViewModels;

public class BusViewModel : INotifyPropertyChanged
{
    public int Index { get; set; }
    private string _name = string.Empty;
    public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

    private double _gain;
    public double Gain { get => _gain; set { if (Math.Abs(_gain - value) > 0.0001) { _gain = value; OnPropertyChanged(); } } }

    private bool _isAdjustingGain;
    public bool IsAdjustingGain { get => _isAdjustingGain; set { if (_isAdjustingGain != value) { _isAdjustingGain = value; OnPropertyChanged(); } } }

    public DateTime SuppressUntilUtc { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
