using System;
using System.Waf.Foundation;

using TumblThree.Domain.Models;

namespace TumblThree.Domain.Queue
{
    [Serializable]
    public class QueueListItem : Model
    {
        private string progress;

        public QueueListItem(IBlog blog)
        {
            Blog = blog;
        }

        public IBlog Blog { get; }

        public string Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }
    }
}
