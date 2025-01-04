using System.Diagnostics;

namespace POE2loadingPainFix
{
    public class PoeThreadPFC : PoeThread
    {
        public const string Counter_CpuUsage = "CPUUSAGE";
        public const string Counter_DiskUsage = "DISKUSAGE";
        public const string Counter_IORead = "IOREAD";


        PerformanceCounter? Disk_Time_Counter;
        PerformanceCounter? Process_IO_ReadBytesPerSecCounter;
        PerformanceCounter? CPU_Total_Counter;
        Exception? ExceptionPFC = null;

        private void ClearCounters()
        {
            Disk_Time_Counter?.Dispose();
            Disk_Time_Counter = null;
            CPU_Total_Counter?.Dispose();
            CPU_Total_Counter = null;
            Process_IO_ReadBytesPerSecCounter?.Dispose();
            Process_IO_ReadBytesPerSecCounter = null;

        }

        private void InitCounters()
        {
            if (UsedTP == null)
                return;

            if (Process_IO_ReadBytesPerSecCounter == null)
            {
#if DEBUG2
                PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
                Debugging.Step();
                var counters = cat.GetCounters(ExeName);
                foreach (var c in counters)
                {
                    Trace.WriteLine($"{c.CounterName} / {c.CounterHelp}");
                }
                Debugging.Step();
#endif
                //IO Read Bytes/sec&

                Process_IO_ReadBytesPerSecCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", $"{UsedTP!.ExeName_NoExtension}", true);
                Debugging.Step();
            }


            if (Disk_Time_Counter == null)
            {
                PerformanceCounterCategory cat = new PerformanceCounterCategory("PhysicalDisk");
                var instances_PhysicalDisk = cat.GetInstanceNames();
                string? found_DiskCounter = instances_PhysicalDisk.FirstOrDefault(x => x.Contains(UsedTP!.Drive.ToUpper()));
                if (found_DiskCounter == null)
                    throw new ThrottlerCriticalException($"PerfCounter: PhysicalDisk / Drive: {UsedTP!.Drive} not found! Instances: {instances_PhysicalDisk.ToSingleString("/")}");

#if DEBUG2
                Debugging.Step();
                var counters = cat.GetCounters(found_DiskCounter);
                foreach (var c in counters)
                {
                    Trace.WriteLine($"{c.CounterName} / {c.CounterHelp}");
                }
#endif
                //% Disk Read Time
                //% Disk Time
                Disk_Time_Counter = new PerformanceCounter("PhysicalDisk", "% Disk Read Time", $"{found_DiskCounter}", true);
            }

            if (CPU_Total_Counter == null)
            {
#if DEBUG2
                PerformanceCounterCategory cat = new PerformanceCounterCategory("Processor");
                var instances = cat.GetInstanceNames();//_Total
                Debugging.Step();
#endif
                CPU_Total_Counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                Debugging.Step();
            }

        }


        protected override void Thread_Execute(Process? poeProcess)
        {
            if (UsedTP == null)
            {
                ClearCounters();
                return;
            }


            InitCounters();



            double ioReadMBS = 0;

            float ioRead = Process_IO_ReadBytesPerSecCounter!.NextValue();
            if (ioRead > 0)
            {
                ioReadMBS = ioRead / 1000d / 1000d; //bytes => MB
                                                    //Trace.WriteLine($"{ioReadMBS:N2}");
            }
            if (ioReadMBS > 50)
                ioReadMBS = 50d;


            float diskTime = Disk_Time_Counter!.NextValue();
            if (diskTime > 100)
                diskTime = 100;

            float cpuusage = CPU_Total_Counter!.NextValue();
            if (cpuusage > 100)
                cpuusage = 100;

            if (ioReadMBS > 0)
                Debugging.Step();




            ThreadState.AddMeasure(Counter_CpuUsage, cpuusage);
            ThreadState.AddMeasure(Counter_DiskUsage, diskTime);
            ThreadState.AddMeasure(Counter_IORead, ioReadMBS);

        }

        public override void Stop()
        {
            base.Stop();
            ClearCounters();
        }

    }
}
