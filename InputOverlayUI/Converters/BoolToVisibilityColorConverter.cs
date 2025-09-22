using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InputOverlayUI.Converters
{
    public class BoolToVisibilityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Green when visible
                    : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray when hidden
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}