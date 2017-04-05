using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class IsLessThanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.First() == DependencyProperty.UnsetValue)
            {
                return DependencyProperty.UnsetValue;
            }

            double value = System.Convert.ToDouble(values[0]);
            double threshold = System.Convert.ToDouble(values[1]);

            return threshold < value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
