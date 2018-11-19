using System;
using System.Globalization;
using System.Waf.Foundation;
using System.Windows.Data;

using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Converters
{
    public class IsQueueItemCrawlingMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var crawlingQueuelistItem = (ReadOnlyObservableList<QueueListItem>)values[0];
            var currentQueuelistItem = (QueueListItem)values[1];

            return crawlingQueuelistItem.Contains(currentQueuelistItem);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
