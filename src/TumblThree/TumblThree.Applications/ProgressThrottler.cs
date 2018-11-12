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
            var resetTimer = new Timer { Interval = interval };
            resetTimer.Elapsed += resetTimer_Elapsed;
            resetTimer.Start();

            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public void Report(T value)
        {
            if (!reportProgressAfterThrottling)
                return;
            _progress.Report(value);
            reportProgressAfterThrottling = false;
        }

        private void resetTimer_Elapsed(object sender, ElapsedEventArgs e) => reportProgressAfterThrottling = true;
    }
}
