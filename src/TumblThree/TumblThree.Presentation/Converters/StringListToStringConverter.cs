using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Applications.Data;

namespace TumblThree.Presentation.Converters
{
    public class StringListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = ((IEnumerable<string>)value);
            return StringListConverter.ToString(list, GetSeparator(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = (string)value;
            return StringListConverter.FromString(text, GetSeparator(parameter));
        }

        private static string GetSeparator(object commandParameter)
        {
            if (ConverterHelper.IsParameterSet("ListSeparator", commandParameter))
            {
                return null;
            }
            return Environment.NewLine;
        }
    }
}
