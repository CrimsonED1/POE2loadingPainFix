using LiveChartsCore.Defaults;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;

namespace POE2loadingPainFix
{
    public class TargetProcess : ICloneable
    {

        public string ExeName_NoExtension=> Path.GetFileNameWithoutExtension(ImagePath);

        public string ImagePath { get; } = "";
        public string Drive { get; } = "";

        public string POE2_LogFile { get; } = "";

        public string AffinityCaption { get; private set; } = "";

        public nint Normal_Affinity { get; private set; }
        public nint Current_Affinity { get; private set; }
        public bool Current_PriorityBoostEnabled { get; private set; }
        public ProcessPriorityClass Current_PriortyClass { get; private set; }

        public DateTime? StartTime { get; set; } = null;

        public bool IsStartedAfterFix { get; }

        public int Threads { get; private set; }

        public int PID { get; private set; }

        public LimitMode LimitMode { get; set; } = LimitMode.Off;
        public nint Orginal_Affinity { get; }

        public ProcessPriorityClass Orginal_PriortyClass { get; }

        public bool Orginal_PriorityBoostEnabled { get; }

        public bool LogFile_InitialRun { get; set; } = true;
        public long LogFile_StartupLength { get; set; } = 0;
        public int Pos_ResetLimit { get; set; } = 0;
        public int Pos_SetLimit1 { get; set; } = 0;
        public int Pos_SetLimit2 { get; set; } = 0;
        public double Current_Affinity_Percent { get; private set; }

        public TargetProcess(Process process, DateTime startTimeApp)
        {
            if (process.MainModule == null)
                throw new Exception($"MainModule not set");
            ImagePath = process.MainModule.FileName;

            var poe2Dir = Path.GetDirectoryName(ImagePath);
            var poe2Log = Path.Combine(poe2Dir!, @"logs\client.txt");
            POE2_LogFile = poe2Log;

            FileInfo f = new FileInfo(ImagePath);
            Drive = System.IO.Path.GetPathRoot(f.FullName).Substring(0,1);

            nint af_normal = CpuTools.GetProcessorAffinity();

            //only if poe2 is started after this app
            if (process.StartTime > startTimeApp)
            {
                Orginal_PriorityBoostEnabled = process.PriorityBoostEnabled;
                Orginal_PriortyClass = process.PriorityClass;
                Orginal_Affinity = process.ProcessorAffinity;
                IsStartedAfterFix = true;
            }
            else
            {
                Orginal_PriortyClass = ProcessPriorityClass.Normal;
                Orginal_PriorityBoostEnabled = true;                
                Orginal_Affinity = af_normal;
                IsStartedAfterFix = false;
            }

            Normal_Affinity = af_normal;


            StartTime = process.StartTime;

            //if (StartTime < startTimeApp)
            //{
            //    throw new ThrottlerCriticalException($"POE2 must be started after this App!");
            //}

            Update(process);
            //Affinity = process.ProcessorAffinity;            
        }

        internal void Update(Process process)
        {
            Threads = process.Threads.Count;
            PID = process.Id;

            Current_PriorityBoostEnabled = process.PriorityBoostEnabled;
            Current_PriortyClass = process.PriorityClass;

            Current_Affinity = process.ProcessorAffinity;
            

            if (Current_Affinity > 0)
            {
                int currentCount = CpuTools.GetProcessorsCount_FromAffinity(Current_Affinity);
                int normalCount = CpuTools.GetProcessorsCount_FromAffinity(Normal_Affinity);
                double dC = currentCount;
                double dN = normalCount;
                Current_Affinity_Percent = dC / dN * 100d;
            }

            AffinityCaption = Current_Affinity.ToString("X");
        }

        public object Clone()
        {
            var res = this.MemberwiseClone();
            return res;
        }
    }
}

