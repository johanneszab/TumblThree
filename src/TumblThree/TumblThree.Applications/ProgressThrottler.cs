using System;
using System.Timers;

namespace TumblThree.Applications
{
    public class ProgressThrottler<T> : IProgress<T>
    {
        bool reportProgressAfterThrottling = true;

        public ProgressThrottler(IProgress<T> progress)
        {
            var resettimer = new Timer { Interval = 200 };
            resettimer.Elapsed += resettimer_Elapsed;
            resettimer.Start();

            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

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
