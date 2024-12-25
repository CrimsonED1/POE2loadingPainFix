using System.ComponentModel;
using PropertyChanged;

namespace POE2loadingPainFix
{
    
    public class AffinityEntry : INotifyPropertyChanged
    {
        public AffinityEntry(int cPUID, bool isset)
        {
            CPUID = cPUID;
            IsSet = isset;
        }

        public int CPUID { get; }

        public bool IsSet { get; set; }
        public string Caption => $"Logical processor {CPUID}";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
