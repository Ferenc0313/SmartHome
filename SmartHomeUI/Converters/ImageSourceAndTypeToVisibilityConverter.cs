using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartHomeUI.Converters;

public sealed class ImageSourceAndTypeToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var imageSource = values.Length > 0 ? values[0] : null;
        var type = values.Length > 1 ? values[1] as string : null;

        if (type is not null && type.Equals("SmartDoor", StringComparison.OrdinalIgnoreCase))
        {
            return Visibility.Collapsed;
        }

        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var hasImage = imageSource is not null;
        var show = invert ? !hasImage : hasImage;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

