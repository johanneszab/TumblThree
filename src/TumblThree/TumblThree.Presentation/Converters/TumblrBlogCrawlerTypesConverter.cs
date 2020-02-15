using System;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Domain.Models;

namespace TumblThree.Presentation.Converters
{
    public class TumblrBlogCrawlerTypesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;
            switch ((BlogTypes)value)
            {
                case BlogTypes.tumblr:
                    return TumblrBlogCrawlerTypes.TumblrAPI;
                case BlogTypes.tmblrpriv:
                    return TumblrBlogCrawlerTypes.TumblrSVC;
                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;
            switch ((TumblrBlogCrawlerTypes)value)
            {
                case TumblrBlogCrawlerTypes.TumblrAPI:
                    return BlogTypes.tumblr;
                case TumblrBlogCrawlerTypes.TumblrSVC:
                    return BlogTypes.tmblrpriv;
                default:
                    return value;
            }
        }
    }
}
