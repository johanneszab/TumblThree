using System;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    public class ItemsCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var itemsCount = (int)value;
            if (itemsCount == 1)
            {
                return Resources.OneItem;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, Resources.NumberOfItems, itemsCount);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
