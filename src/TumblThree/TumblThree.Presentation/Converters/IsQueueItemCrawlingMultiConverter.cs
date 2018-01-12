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
            ReadOnlyObservableList<QueueListItem> crawlingQueuelistItem = (ReadOnlyObservableList<QueueListItem>)values[0];
            QueueListItem currentQueuelistItem = (QueueListItem)values[1];

            if (crawlingQueuelistItem.Contains(currentQueuelistItem))
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
