using System;
using System.Timers;

namespace TumblThree.Applications
{
    public class ProgressThrottler<T> : IProgress<T>
    {
        private readonly IProgress<T> _progress;
        private bool reportProgressAfterThrottling = true;

        public ProgressThrottler(IProgress<T> progress, double interval)
        {
            var resettimer = new Timer { Interval = interval };
            resettimer.Elapsed += resettimer_Elapsed;
            resettimer.Start();

            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

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
