using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartHomeUI.Converters;

public sealed class IconKeyToGlyphConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        // If it looks like an image path, don't try to render glyph text
        if (LooksLikeImagePath(s)) return string.Empty;
        // Expecting hex like "E95F" -> return corresponding char string
        if (int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
        {
            return char.ConvertFromUtf32(code);
        }
        return s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

    private static bool LooksLikeImagePath(string s)
    {
        return s.Contains('/', StringComparison.Ordinal)
               || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
               || s.Contains("\\", StringComparison.Ordinal);
    }
}
