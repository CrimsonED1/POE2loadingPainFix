﻿using LiveChartsCore.Defaults;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;

namespace POE2loadingPainFix
{
    public class TargetProcess : ICloneable
    {


        public string ImagePath { get; } = "";
        public string Drive { get; } = "";

        public string POE2_LogFile { get; } = "";

        public string AffinityCaption { get; private set; } = "";
        //public bool[] Affinity { get; } = new bool[] { true };
        public bool IsRunning { get; private set; }

        public int Threads { get; private set; }

        public int PID { get; private set; }

        public bool IsCpuLimited { get; private set; }

        public TargetProcess(Process process)
        {
            if (process.MainModule == null)
                throw new Exception($"MainModule not set");
            ImagePath = process.MainModule.FileName;

            var poe2Dir = Path.GetDirectoryName(ImagePath);
            var poe2Log = Path.Combine(poe2Dir!, @"logs\client.txt");
            POE2_LogFile = poe2Log;

            FileInfo f = new FileInfo(ImagePath);
            Drive = System.IO.Path.GetPathRoot(f.FullName).Substring(0,1);


            Update(process);
            //Affinity = process.ProcessorAffinity;            
        }

        internal void Update(Process? process)
        {
            if (process == null)
            {
                IsRunning = false;
                PID = -1;
                Threads = -1;
                return;
            }
            IsRunning = true;
            Threads = process.Threads.Count;
            PID = process.Id;

            var af = process.ProcessorAffinity;
            nint af_normal = CpuTools.GetProcessorAffinity();
            IsCpuLimited = af != af_normal;

            AffinityCaption = af.ToString("X");
        }

        public object Clone()
        {
            var res = this.MemberwiseClone();
            return res;
        }
    }
}
