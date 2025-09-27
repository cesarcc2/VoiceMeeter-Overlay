using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VMHud.Core.Models;

namespace VMHud.App;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value as BackendStatus? ?? (value is BackendStatus s ? s : BackendStatus.Disconnected);
        return status switch
        {
            BackendStatus.Connected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0xD3, 0x8E)), // green
            BackendStatus.Connecting => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xC1, 0x07)), // amber
            BackendStatus.Disconnected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x4B, 0x4B)), // red
            BackendStatus.Simulated => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7A, 0x86, 0xFF)), // blue-ish
            _ => new SolidColorBrush(System.Windows.Media.Colors.White)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
