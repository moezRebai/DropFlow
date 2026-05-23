using System.Globalization;

namespace DropFlow.Mobile.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

public class EyeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "👁" : "🙈";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int status ? StatusColor(status) : Colors.Gray;

    private static Color StatusColor(int status) => status switch
    {
        0 => Color.FromArgb("#42A5F5"),
        1 => Color.FromArgb("#FFA726"),
        2 => Color.FromArgb("#AB47BC"),
        3 => Color.FromArgb("#66BB6A"),
        4 => Color.FromArgb("#BDBDBD"),
        _ => Colors.Gray
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int status ? StatusBackground(status) : Color.FromArgb("#F5F5F5");

    private static Color StatusBackground(int status) => status switch
    {
        0 => Color.FromArgb("#E3F2FD"),
        1 => Color.FromArgb("#FFF3E0"),
        2 => Color.FromArgb("#F3E5F5"),
        3 => Color.FromArgb("#E8F5E9"),
        4 => Color.FromArgb("#F5F5F5"),
        _ => Color.FromArgb("#F5F5F5")
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToCheckConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "✅" : "⬜";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal d ? $"{d:F2}€" : "0,00€";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TimeSpanFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
        if (value is string s && TimeSpan.TryParse(s, out var parsed)) return parsed.ToString(@"hh\:mm");
        return value?.ToString() ?? string.Empty;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.4;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
