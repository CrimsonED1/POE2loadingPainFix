using PropertyChanged;
using System.ComponentModel;

namespace POE2loadingPainFix
{
    public enum LimitKind
    {
        AlwaysOff = 0,
        AlwaysOn = 1,
        ViaClientLog = 4,
    }

    public class Config : ICloneable, INotifyPropertyChanged
    {

        public bool[] InLimitAffinity { get; set; } = new bool[] { true }; //min 1

        public int LimitThreads_Pause_MSecs { get; set; } = 10;
        public string LimitThreads_Pause_Caption => $"{LimitThreads_Pause_MSecs} MSecs";

        public int LimitThreads_Run_MSecs { get; set; } = 10;
        public string LimitThreads_Run_Caption => $"{LimitThreads_Run_MSecs} MSecs";


        public double LimitToNormalDelaySecs { get; set; } = 1;
        public string LimitToNormalDelaySecsCaption => $"{LimitToNormalDelaySecs:N1} secs";
        public LimitKind LimitKind { get; set; } = LimitKind.ViaClientLog;

        public bool IsLimit_SetAffinity { get; set; } = false;
        public bool IsLimit_RemovePrioBurst { get; set; } = false;
        public bool IsLimit_PrioLower { get; set; } = false;

        
        public bool IsLimit_ViaThreads { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public object Clone()
        {
            //return this.MemberwiseClone();
            Config res = new Config()
            {
                InLimitAffinity = InLimitAffinity,
                LimitKind = LimitKind,
                LimitToNormalDelaySecs = LimitToNormalDelaySecs,
                IsLimit_PrioLower = IsLimit_PrioLower,
                IsLimit_RemovePrioBurst = IsLimit_RemovePrioBurst,
                IsLimit_SetAffinity = IsLimit_SetAffinity,
                LimitThreads_Pause_MSecs = LimitThreads_Pause_MSecs,
                LimitThreads_Run_MSecs = LimitThreads_Run_MSecs,
                IsLimit_ViaThreads = IsLimit_ViaThreads,
            };
            return res;

        }
    }
}

