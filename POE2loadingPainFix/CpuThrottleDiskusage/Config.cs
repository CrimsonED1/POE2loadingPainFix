using PropertyChanged;
using System.ComponentModel;

namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public enum LimitKind
    {
        AlwaysOff = 0,
        AlwaysOn = 1,
        ViaClientLog = 4,
    }

    public class Config:ICloneable,INotifyPropertyChanged
    {
        
        public bool[] InLimitAffinity { get; set; } = new bool[] { true }; //min 1

        
        

        public double LimitToNormalDelaySecs { get; set; } = 4;
        public string LimitToNormalDelaySecsCaption => $"{LimitToNormalDelaySecs:N1} secs";
        public LimitKind LimitKind { get; set; } = LimitKind.ViaClientLog;

        public bool IsAutolimit_pulse_limit { get; set; } = true;

        

        public event PropertyChangedEventHandler? PropertyChanged;

        public object Clone()
        {
            //return this.MemberwiseClone();
            Config res = new Config()
            {
                InLimitAffinity = this.InLimitAffinity,
                LimitKind = this.LimitKind,
                LimitToNormalDelaySecs = this.LimitToNormalDelaySecs,
                IsAutolimit_pulse_limit = this.IsAutolimit_pulse_limit,
            };
            return res;
            
        }
    }
}

