using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartHomeUI.Converters;

public sealed class BoolToTextOnOffConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? "ON" : "OFF";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
