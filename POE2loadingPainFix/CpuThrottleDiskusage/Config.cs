using PropertyChanged;
using System.ComponentModel;

namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public enum LimitKind
    {
        AlwaysOff = 0,
        AlwaysOn = 1,
        ViaDiskUsage=2,
        ViaIOBytesUsage = 3
    }

    public class Config:ICloneable,INotifyPropertyChanged
    {
        
        /// <summary>
        /// MB/s
        /// </summary>
        public double LimitProcessIORead { get; set; } = 3;

        /// <summary>
        /// percent
        /// </summary>
        public double LimitDiskUsage { get; set; } = 40;

        public string LimitDiskUsageCaption => $"{LimitDiskUsage:N1} %";

        public string LimitProcessIOReadCaption => $"{LimitProcessIORead:N1} MB/s";

        public double LimitStartHoldSecs { get; set; } = 0.2;
        public string LimitStartHoldSecsCaption => $"{LimitStartHoldSecs:N1} secs";

        public bool[] InLimitAffinity { get; set; } = new bool[] { true }; //min 1

        public int ThreadSleepMs { get; set; } = 10;
        public int ThreadGuiUpdateMs { get; set; } = 300;

        public double LimitToNormalDelaySecs { get; set; } = 9.5;
        public string LimitToNormalDelaySecsCaption => $"{LimitToNormalDelaySecs:N1} secs";
        public LimitKind LimitKind { get; set; } = LimitKind.ViaDiskUsage;



        public event PropertyChangedEventHandler? PropertyChanged;

        public object Clone()
        {
            //return this.MemberwiseClone();
            Config res = new Config()
            {
                InLimitAffinity = this.InLimitAffinity,
                LimitDiskUsage = this.LimitDiskUsage,
                LimitKind = this.LimitKind,
                LimitToNormalDelaySecs = this.LimitToNormalDelaySecs,
                ThreadGuiUpdateMs = this.ThreadGuiUpdateMs,
                ThreadSleepMs = this.ThreadSleepMs,
                LimitStartHoldSecs = this.LimitStartHoldSecs,
                LimitProcessIORead = this.LimitProcessIORead,
                
            };
            return res;
            
        }
    }
}
