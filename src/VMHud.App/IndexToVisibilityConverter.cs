using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VMHud.App;

public sealed class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var count = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            var index = System.Convert.ToInt32(parameter, CultureInfo.InvariantCulture);
            return count > index ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

