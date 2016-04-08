using System;
using System.Waf.Foundation;
using TumblThree.Domain.Models;

namespace TumblThree.Domain.Queue
{
    [Serializable]
    public class QueueListItem : Model
    {
        public QueueListItem(IBlog blog)
        {
            Blog = blog;
        }

        public IBlog Blog { get; }
    }
}
