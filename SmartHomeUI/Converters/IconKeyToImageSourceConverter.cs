using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SmartHomeUI.Converters;

public sealed class IconKeyToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!LooksLikeImagePath(s)) return null;

        try
        {
            var uri = BuildUri(s);
            return uri is null ? null : new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

    private static bool LooksLikeImagePath(string s)
    {
        // Accept common image paths or pack URIs
        return s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/", StringComparison.Ordinal)
               || s.StartsWith("\\", StringComparison.Ordinal)
               || s.StartsWith("Assets", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri? BuildUri(string s)
    {
        if (s.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(s, UriKind.Absolute);
        }

        // Relative resource inside the assembly -> build pack URI
        if (!s.Contains("://", StringComparison.Ordinal))
        {
            var path = s.TrimStart('/', '\\').Replace("\\", "/");
            // Explicitly include assembly name so resources resolve reliably
            return new Uri($"pack://application:,,,/SmartHomeUI;component/{path}", UriKind.Absolute);
        }

        // Fallback to absolute/relative URI as provided
        if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
        {
            return uri;
        }

        return null;
    }
}
