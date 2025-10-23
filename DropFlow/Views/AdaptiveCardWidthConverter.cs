using System.Globalization;
using System.Windows.Data;

namespace DropFlow.Views;

public class AdaptiveCardWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is double containerWidth)
        {
            // marge totale estimée et minimum 280px
            double cardWidth = Math.Max(280, (containerWidth - 80) / 4);
            return cardWidth;
        }
        return 280;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}