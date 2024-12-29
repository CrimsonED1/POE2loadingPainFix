using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Net.WebRequestMethods;


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
        PerformanceCounter? CPU_Total_Counter;
        public event EventHandler<State>? GuiUpdate;

        //########################################

        public Throttler(Config config)
        {
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
            CPU_Total_Counter?.Dispose(); 
            CPU_Total_Counter = null;
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

        public static readonly string[] POE_ExeNames = ["PathOfExileSteam", "PathOfExile"];

        private TimeSpan? CycleTime;
        TargetProcess? _TP = null;
        Stopwatch swTimeoutGUI = new Stopwatch();
        Stopwatch? swLimitToNormalDelaySW = null;
        
        Config usedConfig;

        List<MeasureEntry> measures = new List<MeasureEntry>(10000);

        DateTime DT_LastMeasure = DateTime.Now;

        private void Thread_Execute(object? sender)
        {

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
                    if (swTimeoutGUI.Elapsed.TotalMilliseconds > usedConfig.ThreadGuiUpdateMs)
                    {
                        swTimeoutGUI.Restart();

                        var state = new State(_TP)
                        {
                            CycleTime = this.CycleTime,
                            LastError = ex,
                        };
                        GuiUpdate?.Invoke(this, state);
                    }
                }
                finally 
                { 
                    sw.Stop(); 
                    CycleTime = sw.Elapsed;
                }

                Thread.Sleep(100);

            }//while

            Debugging.Step();
        }//exec

        private void Thread_Execute_InitCounters()
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

                Process_IO_ReadBytesPerSecCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", $"{_TP!.ExeName_NoExtension}", true);
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

            if(CPU_Total_Counter==null)
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


   


        static string ReadTail(string filename,uint targetBytes)
        {
            using (FileStream fs = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Seek 1024 bytes from the end of the file
                fs.Seek(-targetBytes, SeekOrigin.End);
                // read 1024 bytes
                byte[] bytes = new byte[targetBytes];
                fs.Read(bytes, 0, (int)targetBytes);
                // Convert bytes to string
                string s = Encoding.Default.GetString(bytes);
                // or string s = Encoding.UTF8.GetString(bytes);
                return s;
            }
        }

        private Exception? ExceptionPFC=null;
        private void Thread_Execute_PFC()
        {
            try
            {
                Thread_Execute_InitCounters();

                DateTime dtMeasure = DateTime.Now;

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
                               

                if (dtMeasure != DT_LastMeasure) //keep only unique entries!
                {
                    measures.Add(new MeasureEntry(dtMeasure, diskTime, ioReadMBS, cpuusage));
                    DT_LastMeasure = dtMeasure;
                }
            }
            catch (Exception ex)
            {
                ExceptionPFC = ex;
            }
        }


        private void Thread_Execute_Sub()
        {
            var processes = ProcessEx.GetProcessesByName(POE_ExeNames);
            if (processes.Length == 0)
            {
                Debugging.Step();
                ClearCounters();
                _TP = null;
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
            {                
                _TP.Update(process);
            }

            Thread_Execute_PFC();

            throw new NotImplementedException();


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
                    case LimitKind.ViaClientLog:


                        double condition;
                        double condition_value;
                        //Conditions...
                        switch (usedConfig.LimitKind)
                        {
                            case LimitKind.ViaClientLog:
                                var sResetLimit = "[SHADER] Delay: ON";
                                var sSetLimit1 = "Got Instance Details from login server";
                                var sSetLimit2 = "[SHADER] Delay: OFF";

                                condition_value = 0;
                                condition = 1;
                                try
                                {
                                    string fileContents = ReadTail(_TP.POE2_LogFile,20000);
                                    var iResetLimit = fileContents.LastIndexOf(sResetLimit);
                                    var iSetLimit1 = fileContents.LastIndexOf(sSetLimit1);
                                    var iSetLimit2 = fileContents.LastIndexOf(sSetLimit2);

                                    if(iResetLimit> iSetLimit1 && iResetLimit> iSetLimit2)
                                    {
                                        //not limiting...
                                        condition_value = 0;
                                    }
                                    else
                                    {
                                        //one of both...
                                        if(iSetLimit1> iResetLimit)
                                            condition_value = 1;
                                        if (iSetLimit2 > iResetLimit)
                                            condition_value = 1;
                                    }
                                    Debugging.Step();
                                    

                                }
                                catch
                                (Exception ex)
                                {
                                    Trace.WriteLine(ex.Message);
                                    Debugging.Step();
                                }
                                break;


                            //##############################
                            default: 
                                throw new NotImplementedException();
                        }


                        //Trace.WriteLine($"condition_value: {condition_value}");

                        if (condition_value >= condition)
                        {
                            swLimitToNormalDelaySW = null;
                            if (process.ProcessorAffinity != af_limited)
                                process.ProcessorAffinity = af_limited;
                            Debugging.Step();
                        }
                        else if (process.ProcessorAffinity == af_limited)
                        {
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

                

                var state = new State(_TP)
                {
                    CycleTime = this.CycleTime,
                    MeasureEntries = measures.ToArray(),
                    PfcException = ExceptionPFC,
                };
                measures.Clear();

                if (timetoResetLimit != null)
                {
                    state.LimitCaption += $"Reset Limit in: {timetoResetLimit.Value.TotalSeconds:n1} sec";
                }


                
                GuiUpdate?.Invoke(this, state);
                swTimeoutGUI.Restart();
            }

        }

    }
}
