using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Domain.Models;
using TumblThree.Presentation.Properties;

namespace TumblThree.Presentation.Converters
{
    public class BlogtypeToDisplayValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BlogTypes type = (BlogTypes)value;
            switch (type)
            {
                case BlogTypes.tumblr:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTumblr);
                case BlogTypes.tmblrpriv:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTumblrPrivate);
                case BlogTypes.tumblrsearch:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTumblrSearch);
                case BlogTypes.tumblrtagsearch:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTumblrTagSearch);
                case BlogTypes.tlb:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTumblrLikedBy);
                case BlogTypes.instagram:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeInstagram);
                case BlogTypes.twitter:
                    return string.Format(CultureInfo.CurrentCulture, Resources.BlogtypeTwitter);
                case BlogTypes.all:
                    return "";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
