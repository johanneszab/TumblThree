using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TumblThree.Applications.Services;
using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    public class BlogQueueInfoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.First() == DependencyProperty.UnsetValue) { return DependencyProperty.UnsetValue; }

            var downloadedImages = values[0];
            var totalImages = values[1];

            return string.Format(CultureInfo.CurrentCulture, Resources.DownloadedImagesOf, downloadedImages, totalImages);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
