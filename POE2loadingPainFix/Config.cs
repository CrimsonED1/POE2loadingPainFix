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

        private bool[]? _InLimitAffinity_Inverted;
        public bool[] InLimitAffinity_Inverted
        {
            get
            {
                if (_InLimitAffinity_Inverted != null)
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
        public bool IsLimit_RemovePrioBurst { get; set; } = true;
        public bool IsLimit_PrioLower { get; set; } = true;

        public bool IsGuard_NotResponding { get; set; } = true;

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
                IsGuard_NotResponding = IsGuard_NotResponding,
            };
            return res;

        }
    }
}

