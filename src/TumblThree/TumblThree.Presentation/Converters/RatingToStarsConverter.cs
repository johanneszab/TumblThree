using System;
using System.Globalization;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rating = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (rating >= 99)
            {
                return 5;
            }
            else if (rating >= 75)
            {
                return 4;
            }
            else if (rating >= 50)
            {
                return 3;
            }
            else if (rating >= 25)
            {
                return 2;
            }
            else if (rating >= 1)
            {
                return 1;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int stars = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (stars == 5)
            {
                return 99;
            }
            else if (stars == 4)
            {
                return 75;
            }
            else if (stars == 3)
            {
                return 50;
            }
            else if (stars == 2)
            {
                return 25;
            }
            else if (stars == 1)
            {
                return 1;
            }
            return 0;
        }
    }
}
