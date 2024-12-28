namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public class MeasureEntry
    {
        public DateTime DT { get;  }
        public double DiskUsage { get; }
        public double IORead { get; }

        public double CpuUsage { get; }
        public MeasureEntry(DateTime dT, double diskUsage, double iORead,double cpuusage)
        {
            DT = dT;
            DiskUsage = diskUsage;
            IORead = iORead;
            CpuUsage = cpuusage;
        }

    }

    public class State
    {
        public TargetProcess? TargetProcess { get; }

        public MeasureEntry[] MeasureEntries { get; set; } = new MeasureEntry[0];

        public Exception? PfcException { get; set; } = null;

        public string CpuUsageCaption => MeasureEntries.Length > 0 ? $"{MeasureEntries.Last().CpuUsage:N1} %" : "N/A";
        public string DiskUsageCaption => MeasureEntries.Length>0 ? $"{MeasureEntries.Last().DiskUsage:N1} %" : "N/A";
        public string IOReadCaption => MeasureEntries.Length>0 ? $"{MeasureEntries.Last().IORead:N2} MB/s" : "N/A";

        public Exception? LastError { get; set; } 

        public string LimitCaption { get; set; } = "";


        public TimeSpan? CycleTime { get; set; }
        public string CycleTimeCaption => CycleTime!=null ? $"{CycleTime.Value.TotalMilliseconds:n0} ms" : "N/A";

        public State(TargetProcess? targetProcess)
        {
            if (targetProcess != null)
                TargetProcess = (TargetProcess)targetProcess.Clone();           
        }





    }
}
