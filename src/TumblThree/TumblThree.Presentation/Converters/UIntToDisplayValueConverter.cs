using System;
using System.Globalization;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class UIntToDisplayValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint number = (uint)value;
            return number != 0 ? (object)number : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string displayValue = value as string;
            return string.IsNullOrEmpty(displayValue) ? 0 : uint.Parse(displayValue, CultureInfo.CurrentCulture);
        }
    }
}
