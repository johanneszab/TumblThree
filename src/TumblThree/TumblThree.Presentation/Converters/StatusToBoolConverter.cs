using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Waf.Foundation;
using System.Windows;
using System.Windows.Data;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value is bool && (bool)value;

            return boolValue ? "Online" : "Offline";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
