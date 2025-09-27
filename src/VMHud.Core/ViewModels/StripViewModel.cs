using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VMHud.Core.ViewModels;

public class StripViewModel : INotifyPropertyChanged
{
    public int Id { get; set; }
    private string _name = string.Empty;
    public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

    private bool _isPhysical;
    public bool IsPhysical { get => _isPhysical; set { if (_isPhysical != value) { _isPhysical = value; OnPropertyChanged(); } } }

    private double _volume;
    public double Volume { get => _volume; set { if (Math.Abs(_volume - value) > 0.0001) { _volume = value; OnPropertyChanged(); } } }

    private bool _isAdjustingVolume;
    public bool IsAdjustingVolume { get => _isAdjustingVolume; set { if (_isAdjustingVolume != value) { _isAdjustingVolume = value; OnPropertyChanged(); } } }

    public DateTime SuppressUntilUtc { get; set; }

    private bool _a1, _a2, _a3, _a4, _a5, _b1, _b2, _b3;
    public bool A1 { get => _a1; set { if (_a1 != value) { _a1 = value; OnPropertyChanged(); } } }
    public bool A2 { get => _a2; set { if (_a2 != value) { _a2 = value; OnPropertyChanged(); } } }
    public bool A3 { get => _a3; set { if (_a3 != value) { _a3 = value; OnPropertyChanged(); } } }
    public bool A4 { get => _a4; set { if (_a4 != value) { _a4 = value; OnPropertyChanged(); } } }
    public bool A5 { get => _a5; set { if (_a5 != value) { _a5 = value; OnPropertyChanged(); } } }
    public bool B1 { get => _b1; set { if (_b1 != value) { _b1 = value; OnPropertyChanged(); } } }
    public bool B2 { get => _b2; set { if (_b2 != value) { _b2 = value; OnPropertyChanged(); } } }
    public bool B3 { get => _b3; set { if (_b3 != value) { _b3 = value; OnPropertyChanged(); } } }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
