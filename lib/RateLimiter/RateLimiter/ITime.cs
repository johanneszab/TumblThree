using System;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    public interface ITime
    {
        DateTime GetNow();

        Task GetDelay(TimeSpan timespan, CancellationToken cancellationToken);
    }
}
