using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartHomeUI.Converters;

/// <summary>
/// Converts a bool to custom text. ConverterParameter can be "TrueText|FalseText".
/// </summary>
public sealed class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        var param = parameter as string;
        if (!string.IsNullOrWhiteSpace(param))
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return flag ? parts[0] : parts[1];
            }
        }
        return flag ? "On" : "Off";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
