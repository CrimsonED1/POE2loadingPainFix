namespace POE2loadingPainFix
{
    public class LimitedEntry
    {
        public DateTime DT { get; }
        public bool Limited { get; }
        public double AffinityPercent { get; }

        public bool NotResponding { get; }
        public bool IsTryRecovery { get; }

        public LimitedEntry(DateTime dT, double affinity_percent, bool limited, bool notResponding,bool isTryRecovery)
        {
            DT = dT;
            Limited = limited;
            AffinityPercent = affinity_percent;
            NotResponding = notResponding;
            IsTryRecovery = isTryRecovery;
        }

    }

    public class MeasureEntry
    {
        public DateTime DT { get; }
        public double DiskUsage { get; }
        public double IORead { get; }


        public double CpuUsage { get; }
        public MeasureEntry(DateTime dT, double diskUsage, double iORead, double cpuusage)
        {
            DT = dT;
            DiskUsage = diskUsage;
            IORead = iORead;
            CpuUsage = cpuusage;
        }

    }
}
