using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

using TumblThree.Domain.Models;

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
                    return "Tumblr";
                case BlogTypes.tumblrsearch:
                    return "Tumblr Search";
                case BlogTypes.all:
                    return "";
                case BlogTypes.instagram:
                    return "Instagram";
                case BlogTypes.tlb:
                    return "Tumblr Liked/By";
                case BlogTypes.tmblrpriv:
                    return "Tumblr Hidden";
                case BlogTypes.tumblrtagsearch:
                    return "Tumblr Tag Search";
                case BlogTypes.twitter:
                    return "Twitter";
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
