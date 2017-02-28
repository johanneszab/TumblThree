using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    public class CountByIntervalAwaitableConstraint : IAwaitableConstraint
    {
        private readonly int _Count;
        private readonly TimeSpan _TimeSpan;
        private readonly LimitedSizeStack<DateTime> _TimeStamps;
        private readonly SemaphoreSlim _Semafore = new SemaphoreSlim(1, 1);
        private readonly ITime _Time;

        public CountByIntervalAwaitableConstraint(int count, TimeSpan timeSpan)
        {
            if (count <= 0)
                throw new ArgumentException("count should be strictly positive", nameof(count));

            if (timeSpan.TotalMilliseconds <= 0)
                throw new ArgumentException("timeSpan should be strictly positive", nameof(timeSpan));

            _Count = count;
            _TimeSpan = timeSpan;
            _TimeStamps = new LimitedSizeStack<DateTime>(_Count);
            _Time = TimeSystem.StandardTime;
        }

        public async Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken)
        {
            await _Semafore.WaitAsync(cancellationToken);
            var count = 0;
            var now = _Time.GetNow();
            var target = now - _TimeSpan;
            LinkedListNode<DateTime> element = _TimeStamps.First, last = null;
            while ((element != null) && (element.Value > target))
            {
                last = element;
                element = element.Next;
                count++;
            }

            if (count < _Count)
                return new DisposeAction(OnEnded);

            Debug.Assert(element == null);
            Debug.Assert(last != null);
            var timetoWait = last.Value.Add(_TimeSpan) - now;
            try 
            {
                await _Time.GetDelay(timetoWait, cancellationToken);
            }
            catch (Exception) 
            {
                _Semafore.Release();
                throw;
            }           

            return new DisposeAction(OnEnded);
        }

        private void OnEnded() 
        {
            _TimeStamps.Push(_Time.GetNow());
            _Semafore.Release();
        }
    }
}
