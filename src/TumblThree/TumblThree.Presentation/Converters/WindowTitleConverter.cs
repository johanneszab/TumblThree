using System;
using System.Globalization;
using System.Linq;
using System.Waf.Foundation;
using System.Windows.Data;

using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Converters
{
    public class WindowTitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //var stringList = values.OfType<string>().Where(x => !string.IsNullOrEmpty(x)).ToArray();
            //return string.Join(" - ", stringList);

            ReadOnlyObservableList<QueueListItem> crawlingQueuelistItem = (ReadOnlyObservableList<QueueListItem>)values[0];
            if (crawlingQueuelistItem.Count() == 0)
            {
                return values[1];
            }

            string blogStringArray = string.Join(" - ", crawlingQueuelistItem.Select(x => x.Blog.Name));
            return values[1] + " - " + blogStringArray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
