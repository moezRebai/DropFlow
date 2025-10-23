using System.Globalization;
using System.Windows.Data;

namespace DropFlow.Views;

public class PageEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        if (values[0] is int current && values[1] is int page)
            return current == page;
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}