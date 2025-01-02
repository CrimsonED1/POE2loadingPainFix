using System.Diagnostics;


namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public class Pulser
    {
        public TimeSpan TimeLow { get; }
        public TimeSpan TimeHigh { get; }
        private Stopwatch StopWatch { get; }

        public Pulser(TimeSpan pulseTimeHigh, TimeSpan pulseTimeLow)
        {
            TimeHigh = pulseTimeHigh;
            TimeLow = pulseTimeLow;
            StopWatch = new Stopwatch();
            StopWatch.Start();
        }

        private bool _IsHigh = false;
        public bool IsHigh
        {
            get
            {
                if(!_IsHigh)
                {
                    //low to high
                    if (StopWatch.Elapsed > TimeLow)
                    {
                        _IsHigh = true;
                        StopWatch.Restart();
                    }
                }
                else
                {
                    //high to low
                    if (StopWatch.Elapsed > TimeHigh)
                    {
                        _IsHigh = false;
                        StopWatch.Restart();
                    }

                }


                return _IsHigh;
            }
        }
    }
}
