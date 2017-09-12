using System;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    public class TotalImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object totalImageCount = values[0];
            return string.Format(CultureInfo.CurrentCulture, Resources.NumberOfImages, totalImageCount);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
