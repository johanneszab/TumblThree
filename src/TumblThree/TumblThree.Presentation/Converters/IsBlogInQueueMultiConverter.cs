using System;
using System.Globalization;
using System.Linq;
using System.Waf.Foundation;
using System.Windows.Data;

using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Converters
{
    public class IsBlogInQueueMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ReadOnlyObservableList<QueueListItem> crawlingQueuelistItem = (ReadOnlyObservableList<QueueListItem>)values[0];
            IBlog currentQueuelistItem = (IBlog)values[1];

            if (crawlingQueuelistItem.Any(item => item.Blog.Name.Equals(currentQueuelistItem.Name) && item.Blog.BlogType.Equals(currentQueuelistItem.BlogType)))
            {
                return true;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
