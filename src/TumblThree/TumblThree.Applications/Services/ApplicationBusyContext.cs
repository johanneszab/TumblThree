using System;

namespace TumblThree.Applications.Services
{
    internal class ApplicationBusyContext : IDisposable
    {
        public Action<ApplicationBusyContext> DisposeCallback { get; set; }

        public void Dispose()
        {
            DisposeCallback(this);
        }
    }
}
