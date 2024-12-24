using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace POE2loadingPainFix.CpuThrottleDiskusage
{

    public class Throttler
    {
        object SyncRoot = new object();
        Thread? Thread;
        bool Terminate = false;
        Config Config;
        PerformanceCounter? Disk_Time_Counter;
        PerformanceCounter? Process_IO_ReadBytesPerSecCounter;
        string ExeName { get; }
        public event EventHandler<State>? GuiUpdate;

        //########################################

        public Throttler(string exeName, Config config)
        {
            ExeName = exeName;
            Config = config;
        }

        public void UpdateConfig(Config newConfig)
        {
            lock(SyncRoot)
            {
                Config = newConfig;
            }
        }


        public void Start()
        {
            Stop();
            if (Thread != null)
            {
                Terminate = true;
                Thread.Join();
            }

            Terminate = false;
            Thread = new Thread(new ParameterizedThreadStart(Thread_Execute));
            Thread.IsBackground = true;
            Thread.Priority = ThreadPriority.AboveNormal;
            Thread.Start();

        }

        private void ClearCounters()
        {
            Disk_Time_Counter?.Dispose();
            Disk_Time_Counter = null;
            Process_IO_ReadBytesPerSecCounter?.Dispose();
            Process_IO_ReadBytesPerSecCounter = null;

        }

        public void Stop()
        {
            Terminate = true;
            if (Thread != null)
            {
                Thread = null;
                ClearCounters();
            }
        }

#if DEBUG
        bool IsSimulateDisk = true;
        bool IsSimulateHigh = true;
        Stopwatch? simulateSW;
        double SimulateDiskUsage(double orginalValue)
        {
            if(!IsSimulateDisk)
                return orginalValue;
            if (simulateSW == null)
            {
                simulateSW = new Stopwatch();
                simulateSW.Restart();
            }
            if(simulateSW.Elapsed.TotalSeconds>5)
            {
                IsSimulateHigh = !IsSimulateHigh;
                simulateSW.Restart();
            }

            if (IsSimulateHigh)
                return 55.55;
            else
                return 1.11;


        }
#endif



        private TimeSpan? CycleTime;
        TargetProcess? _TP = null;
        Stopwatch swTimeoutGUI = new Stopwatch();
        Stopwatch? swLimitToNormalDelaySW = null;
        Stopwatch? swLimitStartDelay = null;
        Config usedConfig;

        List<MeasureEntry> measures = new List<MeasureEntry>(10000);

        DateTime DT_LastMeasure = DateTime.Now;

        private void Thread_Execute(object? sender)
        {


            //try
            //{
            //    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            //    foreach (ManagementObject disk in searcher.Get())
            //    {
            //        Trace.WriteLine($"Disk: {disk["Model"]}");
            //        try
            //        {
            //            Trace.WriteLine($"Max Transfer Rate: {disk["MaxTransferRate"]} bytes per second");
            //        }
            //        catch
            //        {
            //            Trace.WriteLine($"Max Transfer Rate: Not found!");
            //        }
                    
            //        foreach(var p in disk.Properties)
            //        {
            //            Trace.WriteLine($"{p.Name}={p.Value}");
            //        }
            //    }
            //    Debugging.Step();
            //}
            //catch (ManagementException e)
            //{
            //    Trace.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            //}



            var sw = new Stopwatch();
            swTimeoutGUI.Start();




            while (!Terminate)
            {
                sw.Restart();
                lock (SyncRoot)
                {
                    usedConfig = Config;
                }

                try
                {
                    Thread_Execute_Sub();
                }
                catch (Exception ex)
                {
                    if(ex  is ThrottlerCriticalException)
                    {
                        Terminate = true;

                        var state = new State(_TP)
                        {
                            CycleTime = this.CycleTime,
                            Error = ex.Message
                        };
                        GuiUpdate?.Invoke(this, state);
                    }

                    Debugging.Step();
                    Trace.WriteLine(ex.Message);

                }
                finally 
                { 
                    sw.Stop(); 
                    CycleTime = sw.Elapsed;
                }

                Thread.Sleep(usedConfig.ThreadSleepMs);

            }//while

            Debugging.Step();
        }//exec

        private void Thread_Execute_InitCounters(Process process)
        {

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

                Process_IO_ReadBytesPerSecCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", $"{ExeName}", true);
                Debugging.Step();
            }


            if (Disk_Time_Counter == null)
            {
                PerformanceCounterCategory cat = new PerformanceCounterCategory("PhysicalDisk");
                var instances_PhysicalDisk = cat.GetInstanceNames();
                string? found_DiskCounter = instances_PhysicalDisk.FirstOrDefault(x => x.Contains(_TP!.Drive.ToUpper()));
                if (found_DiskCounter == null)
                    throw new ThrottlerCriticalException($"PerfCounter: PhysicalDisk / Drive: {_TP!.Drive} not found! Instances: {instances_PhysicalDisk.ToSingleString("/")}");

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
        }

        private void Thread_Execute_Sub()
        {
            var processes = Process.GetProcessesByName(ExeName);
            if (processes.Length == 0)
            {
                Debugging.Step();
                ClearCounters();
                var state = new State(null)
                {
                    CycleTime = this.CycleTime,
                };

                GuiUpdate?.Invoke(this, state);
                return;
            }
            //found
            var process = processes[0];

            //onetime Init!
            if (_TP == null)
                _TP = new TargetProcess(process);
            else
                _TP.Update(process);

            Thread_Execute_InitCounters(process);


            float ioRead = Process_IO_ReadBytesPerSecCounter!.NextValue();
            double ioReadMBS = 0;
            if (ioRead > 0)
            {
                ioReadMBS = ioRead / 1000d /1000d ; //bytes => MB
                Trace.WriteLine($"{ioReadMBS:N2}");
            }
            if (ioReadMBS > 50)
                ioReadMBS = 50d;


            float diskTime = Disk_Time_Counter!.NextValue();
            if (diskTime > 100)
                diskTime = 100;
            DateTime dtMeasure = DateTime.Now;



            if (process != null)
            {
                nint af_cur = process.ProcessorAffinity;
                nint af_normal = CpuTools.GetProcessorAffinity();
                nint af_limited = CpuTools.GetProcessorAffinity(usedConfig.InLimitAffinity);
                Debugging.Step();
                switch (usedConfig.LimitKind)
                {
                    case LimitKind.AlwaysOff:
                        if (process.ProcessorAffinity != af_normal)
                        {
                            process.ProcessorAffinity = af_normal;
                        }
                        break;
                    case LimitKind.AlwaysOn:
                        if (process.ProcessorAffinity != af_limited)
                        {
                            process.ProcessorAffinity = af_limited;
                        }
                        break;
                    case LimitKind.ViaDiskUsage:
                    case LimitKind.ViaIOBytesUsage:

                        double condition;
                        double condition_value;
                        //Conditions...
                        switch (usedConfig.LimitKind)
                        {
                            case LimitKind.ViaDiskUsage:
                                condition = usedConfig.LimitDiskUsage;
                                condition_value = diskTime;
#if DEBUGSIMU
                    diskTimeCondtions = SimulateDiskUsage(diskTimeCondtions);
#endif

                                break;
                            //##############################
                            case LimitKind.ViaIOBytesUsage:
                                condition = usedConfig.LimitProcessIORead;
                                condition_value = ioReadMBS;

                                break;
                            //##############################
                            default: 
                                throw new NotImplementedException();
                        }



                        if (condition_value >= condition)
                        {
                            if (swLimitStartDelay == null)
                            {
                                swLimitStartDelay = new Stopwatch();
                                swLimitStartDelay.Start();
                            }

                            swLimitToNormalDelaySW = null;


                            if (swLimitStartDelay.Elapsed.TotalSeconds >= usedConfig.LimitStartHoldSecs)
                            {
                                if (process.ProcessorAffinity != af_limited)
                                    process.ProcessorAffinity = af_limited;
                            }

                            Debugging.Step();
                        }
                        else if (process.ProcessorAffinity == af_limited)
                        {
                            swLimitStartDelay = null;

                            if (swLimitToNormalDelaySW == null)
                            {
                                swLimitToNormalDelaySW = new Stopwatch();
                                swLimitToNormalDelaySW.Start();
                            }

                            if (swLimitToNormalDelaySW.Elapsed.TotalSeconds >= usedConfig.LimitToNormalDelaySecs)
                            {
                                process.ProcessorAffinity = af_normal;
                                swLimitToNormalDelaySW = null;
                            }
                        }


                        break;
                    default:
                        throw new ThrottlerCriticalException($"N/A {usedConfig.LimitKind}");
                }

                _TP.Update(process);
            }



            Debugging.Step();

            if(ioReadMBS>0)
                Debugging.Step();

            if (dtMeasure != DT_LastMeasure) //keep only unique entries!
            {
                measures.Add(new MeasureEntry(dtMeasure, diskTime, ioReadMBS));
                DT_LastMeasure = dtMeasure;
            }

            if (swTimeoutGUI.Elapsed.TotalMilliseconds > usedConfig.ThreadGuiUpdateMs)
            {
                //update GUI
                TimeSpan? timetoResetLimit = null;
                if (swLimitToNormalDelaySW != null)
                {
                    var diff = usedConfig.LimitToNormalDelaySecs - swLimitToNormalDelaySW.Elapsed.TotalSeconds;
                    if (diff > 0)
                    {
                        timetoResetLimit = TimeSpan.FromSeconds(diff);
                    }
                }

                TimeSpan? timetoStartLimit = null;
                if (swLimitStartDelay != null)
                {
                    var diff = usedConfig.LimitStartHoldSecs - swLimitStartDelay.Elapsed.TotalSeconds;
                    if (diff > 0)
                    {
                        timetoStartLimit = TimeSpan.FromSeconds(diff);
                    }
                }

                var state = new State(_TP)
                {
                    CycleTime = this.CycleTime,
                    MeasureEntries = measures.ToArray()
                };
                measures.Clear();

                if (timetoResetLimit != null)
                {
                    state.LimitCaption += $"Reset Limit in: {timetoResetLimit.Value.TotalSeconds:n1} sec";
                }

                if (timetoStartLimit != null)
                {
                    state.LimitCaption += $"Start Limit in: {timetoStartLimit.Value.TotalSeconds:n1} sec";
                }


                GuiUpdate?.Invoke(this, state);
                swTimeoutGUI.Restart();
            }

        }

    }
}
