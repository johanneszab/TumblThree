using System.ComponentModel;
using System.Windows;

namespace System.Waf.Applications
{
    internal class PropertyChangedEventListener : IWeakEventListener
    {
        private readonly INotifyPropertyChanged source;
        private readonly PropertyChangedEventHandler handler;


        public PropertyChangedEventListener(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
        {
            if (source == null) { throw new ArgumentNullException("source"); }
            if (handler == null) { throw new ArgumentNullException("handler"); }
            this.source = source;
            this.handler = handler;
        }


        public INotifyPropertyChanged Source { get { return source; } }

        public PropertyChangedEventHandler Handler { get { return handler; } }


        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            handler(sender, (PropertyChangedEventArgs)e);
            return true;
        }
    }
}
