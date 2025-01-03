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

        private bool[]? _InLimitAffinity_Inverted;
        public bool[] InLimitAffinity_Inverted
        {
            get
            {
                if(_InLimitAffinity_Inverted!=null) 
                    return _InLimitAffinity_Inverted;
                var inv = new bool[InLimitAffinity.Length];
                for (int i = 0; i < InLimitAffinity.Length; i++)
                {
                    inv[i] = !InLimitAffinity[i];
                }
                return inv;
            }
        }





        public double LimitToNormalDelaySecs { get; set; } = 1;
        public string LimitToNormalDelaySecsCaption => $"{LimitToNormalDelaySecs:N1} secs";
        public LimitKind LimitKind { get; set; } = LimitKind.ViaClientLog;

        public bool IsLimit_SetAffinity { get; set; } = true;
        public bool IsAutolimit_pulse_limit { get; set; } = false;

        public bool IsLimit_RemovePrioBurst { get; set; } = true;
        public bool IsLimit_PrioLower { get; set; } = true;

        public double Autolimit_pulse_High_Secs { get; set; } = 1.0;
        public double Autolimit_pulse_Low_Secs { get; set; } = 1.0;
        public string Autolimit_pulse_High_SecsCaption => $"{Autolimit_pulse_High_Secs:N1} secs";
        public string Autolimit_pulse_Low_SecsCaption => $"{Autolimit_pulse_Low_Secs:N1} secs";


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
                IsLimit_PrioLower = this.IsLimit_PrioLower,
                IsLimit_RemovePrioBurst = this.IsLimit_RemovePrioBurst,
                IsLimit_SetAffinity = this.IsLimit_SetAffinity,
                Autolimit_pulse_High_Secs = this.Autolimit_pulse_High_Secs,
                Autolimit_pulse_Low_Secs = this.Autolimit_pulse_Low_Secs,
            };
            return res;
            
        }
    }
}

