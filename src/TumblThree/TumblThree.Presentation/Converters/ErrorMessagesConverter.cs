using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    public sealed class ErrorMessagesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<Tuple<Exception, string>> errorMessages = values?.FirstOrDefault() as IEnumerable<Tuple<Exception, string>>;
            if (errorMessages != null)
            {
                string message = errorMessages.Any() ? errorMessages.Last().Item2 : "";
                return string.Format(CultureInfo.CurrentCulture, Resources.ErrorMessage, errorMessages.Count(), message);
            }
            return DependencyProperty.UnsetValue;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
