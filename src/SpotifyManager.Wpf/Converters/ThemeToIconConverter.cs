using System.Globalization;
using System.Windows.Data;

namespace SpotifyManager.Wpf.Converters;

public class ThemeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string theme)
        {
            return theme == "Light" ? "☀️" : "🌙";
        }
        return "☀️";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}