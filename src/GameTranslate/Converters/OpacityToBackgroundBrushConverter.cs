using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GameTranslate.Converters;

public sealed class OpacityToBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var opacity = value is double doubleValue ? doubleValue : 1.0;
        var rgb = parameter as string;
        var alpha = (byte)Math.Clamp(opacity * 255, 0, 255);

        return rgb switch
        {
            "White" => new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255)),
            _ => new SolidColorBrush(Color.FromArgb(alpha, 247, 248, 250))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
