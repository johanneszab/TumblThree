using System;
using System.Threading;
using System.Timers;

namespace TumblThree.Applications
{
    public class ProgressThrottler<T> : IProgress<T>
    {
        System.Timers.Timer resettimer;
        bool reportProgressAfterThrottling = true;

        public ProgressThrottler(IProgress<T> progress)
        {
            resettimer = new System.Timers.Timer();
            resettimer.Interval = 200;
            resettimer.Elapsed += resettimer_Elapsed;
            resettimer.Start();

            if (progress == null)
                throw new ArgumentNullException("progress");

            _progress = progress;
        }

        private readonly IProgress<T> _progress;

        public void Report(T value)
        {
            if (reportProgressAfterThrottling)
            {
                _progress.Report(value);
                reportProgressAfterThrottling = false;
            }
        }

        private void resettimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            reportProgressAfterThrottling = true;
        }
    }
}
