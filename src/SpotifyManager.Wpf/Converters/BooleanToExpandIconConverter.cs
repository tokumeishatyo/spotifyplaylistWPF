using System.Globalization;
using System.Windows.Data;

namespace SpotifyManager.Wpf.Converters;

public class BooleanToExpandIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "\uE70D" : "\uE70E"; // Down arrow : Right arrow
        }
        return "\uE70E"; // Default to right arrow
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}