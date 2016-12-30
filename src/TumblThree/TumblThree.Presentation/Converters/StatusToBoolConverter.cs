using System;
using System.Globalization;
using System.Windows.Data;
using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value is bool && (bool)value;

            return boolValue ? string.Format(CultureInfo.CurrentCulture, Resources.Online) : string.Format(CultureInfo.CurrentCulture, Resources.Offline);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
