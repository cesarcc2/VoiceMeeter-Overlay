using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VMHud.App;

public sealed class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var on = value is bool b && b;
        var group = parameter as string;
        var res = System.Windows.Application.Current.Resources;
        if (on)
        {
            return (group == "B") ? (System.Windows.Media.Brush)res["BBusOnBrush"] : (System.Windows.Media.Brush)res["ABusOnBrush"];
        }
        return (System.Windows.Media.Brush)res["OffTileBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
