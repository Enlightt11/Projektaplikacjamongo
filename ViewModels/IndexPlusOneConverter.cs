using System;
using System.Globalization;
using System.Windows.Data;

namespace projektaplikacjamongo.ViewModels
{
    /// <summary>
    /// Converts a 0-based AlternationIndex to a 1-based ranking number.
    /// </summary>
    public class IndexPlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
                return (index + 1).ToString() + ".";
            return "?.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
