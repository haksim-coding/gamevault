using System.Globalization;
using System.Windows.Data;

namespace GameVault.Converters;

public class PlayTimeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not int minutes || minutes <= 0)
        {
            return "0 min";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;

        if (hours == 0)
        {
            return $"{remainingMinutes} min";
        }

        return remainingMinutes == 0
            ? $"{hours} h"
            : $"{hours} h {remainingMinutes} min";
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
