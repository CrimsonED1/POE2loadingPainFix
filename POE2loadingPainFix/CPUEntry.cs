using System.ComponentModel;
using PropertyChanged;

namespace POE2loadingPainFix
{
    public class CPUEntry: INotifyPropertyChanged
    {
        public CPUEntry(int core, int core_Processor, int processor)
        {
            Core = core;
            Core_Processor = core_Processor;
            Processor = processor;            
        }

        public int Core { get; }
        public int Core_Processor { get; }
        public int Processor { get; }
        public bool IsSet { get; set; } = true;
        public string Caption => $"Core {Core} Core Processor {Core_Processor} Logical processor {Processor}";

        public event PropertyChangedEventHandler? PropertyChanged;

        public override string ToString()
        {
            return $"{Caption} IsSet: {IsSet}";
        }
    }
}
