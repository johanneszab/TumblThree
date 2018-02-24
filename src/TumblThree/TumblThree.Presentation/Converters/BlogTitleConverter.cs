using System;
using System.Globalization;
using System.Linq;
using System.Waf.Foundation;
using System.Windows.Data;

using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Converters
{
    public class BlogTitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ReadOnlyObservableList<QueueListItem> crawlingQueuelistItem = (ReadOnlyObservableList<QueueListItem>)values[0];

            return string.Join(" - ", crawlingQueuelistItem.Select(x => x.Blog.Name).ToArray());
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
