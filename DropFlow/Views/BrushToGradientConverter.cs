using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DropFlow.Views;

public class BrushToGradientConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush solid) 
            return Brushes.Transparent;
        
        var color = solid.Color;
        var gradient = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };

        // début opaque
        gradient.GradientStops.Add(new GradientStop(color, 0));

        // fin légèrement transparente
        var faded = Color.FromArgb((byte)(color.A * 0.85), color.R, color.G, color.B);
        gradient.GradientStops.Add(new GradientStop(faded, 1));

        return gradient;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}