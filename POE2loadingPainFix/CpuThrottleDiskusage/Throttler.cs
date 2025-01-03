using System.Diagnostics;
using System.IO;
using System.Text;


namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public enum LimitMode
    {
        Off,
        On,
        On_Inverted,
        PartialOff
    }

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

        private Pulser Pulser;
        private bool Pulser_High = false;

        //########################################

        public Throttler(Config config)
        {
            Config = config;
            usedConfig = Config;
            Pulser = new Pulser(TimeSpan.FromSeconds(Config.Autolimit_pulse_High_Secs), TimeSpan.FromSeconds(Config.Autolimit_pulse_Low_Secs));
        }

        public void UpdateConfig(Config newConfig)
        {
            lock (SyncRoot)
            {
                Config = newConfig;
                Pulser = new Pulser(TimeSpan.FromSeconds(Config.Autolimit_pulse_High_Secs), TimeSpan.FromSeconds(Config.Autolimit_pulse_Low_Secs));
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
            if (!IsSimulateDisk)
                return orginalValue;
            if (simulateSW == null)
            {
                simulateSW = new Stopwatch();
                simulateSW.Restart();
            }
            if (simulateSW.Elapsed.TotalSeconds > 5)
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

        DateTime StartTime = DateTime.Now;
        TimeSpan? CycleTime;
        TargetProcess? _TP = null;
        Stopwatch swTimeoutGUI = new Stopwatch();
        Stopwatch? swLimitToNormalDelaySW = null;

        Config usedConfig;

        List<MeasureEntry> measures = new List<MeasureEntry>(10000);
        List<LimitedEntry> limits = new List<LimitedEntry>(10000);
        

        DateTime DT_LastMeasure = DateTime.Now;

        private bool IsGuiTaskActive = false;


        private void DoGuiUpdate(Exception? exception = null)
        {
            int ThreadGuiUpdateMs = 300;

            if (swTimeoutGUI.Elapsed.TotalMilliseconds > ThreadGuiUpdateMs && GuiUpdate != null)
            {
                swTimeoutGUI.Restart();

                State state;
                if (exception != null)
                {
                    state = new State(_TP)
                    {
                        CycleTime = this.CycleTime,
                        LastError = exception,
                    };
                }
                else
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



                    state = new State(_TP)
                    {
                        CycleTime = this.CycleTime,
                        MeasureEntries = measures.ToArray(),
                        PfcException = ExceptionPFC,
                        LimitEntries = limits.ToArray(),
                    };
                    measures.Clear();
                    limits.Clear();

                    if (timetoResetLimit != null)
                    {
                        state.LimitCaption += $"Reset Limit in: {timetoResetLimit.Value.TotalSeconds:n1} sec";
                    }

                    if(_TP!=null && _TP.LogFile_InitialRun)
                    {
                        state.LimitCaption = "Startup, waiting for logfile changes";
                    }
                    


                }

                if (!IsGuiTaskActive)
                {
                    IsGuiTaskActive = true;
                    Task.Run(() =>
                    {
                        GuiUpdate(this, state);
                        IsGuiTaskActive = false;
                    });
                }
            }

        }


        DateTime DT_Cylcle = DateTime.Now;
        DateTime DT_LastCylce = DateTime.Now.AddMinutes(-1);
        
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
                    DT_Cylcle = DateTime.Now;
                    Thread_Execute_Sub();
                    DT_LastCylce = DT_Cylcle;
                }
                catch (Exception ex)
                {
                    DoGuiUpdate(ex);


                }
                finally
                {
                    sw.Stop();
                    CycleTime = sw.Elapsed;
#if DEBUG
                    if (CycleTime > TimeSpan.FromMilliseconds(150))
                        Trace.WriteLine($"CycleTime High! {CycleTime.Value.TotalMilliseconds}");
#endif
                }

                //Slow when POE2 started, fast when its not!
                if(_TP!=null)
                    Thread.Sleep(100);
                else
                    Thread.Sleep(1);

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





        static string ReadTail(string filename, uint targetBytes)
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

        private Exception? ExceptionPFC = null;
        private void Thread_Execute_PFC()
        {
            try
            {
                Thread_Execute_InitCounters();

                

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




                if (DT_Cylcle != DT_LastMeasure) //keep only unique entries!
                {
                    measures.Add(new MeasureEntry(DT_Cylcle, diskTime, ioReadMBS, cpuusage));
                    
                }
            }
            catch (Exception ex)
            {
                ExceptionPFC = ex;
            }
        }

        private ProcessPriorityClass Limited_PriorityClass = ProcessPriorityClass.BelowNormal;

        private void SetLimit(Process process, LimitMode limited)
        {
            if (_TP == null)
                return;


            switch (limited)
            {
                case LimitMode.On:
                case LimitMode.On_Inverted:
                    _TP.IsLimitedByApp = true;

                    if (usedConfig.IsLimit_SetAffinity)
                    {
                        if (limited == LimitMode.On)
                        {
                            nint af_limited = CpuTools.GetProcessorAffinity(usedConfig.InLimitAffinity);

                            if (process.ProcessorAffinity != af_limited)
                                process.ProcessorAffinity = af_limited;
                        }
                        else //INVERTED
                        {
                            nint af_limited = CpuTools.GetProcessorAffinity(usedConfig.InLimitAffinity_Inverted);

                            if (process.ProcessorAffinity != af_limited)
                                process.ProcessorAffinity = af_limited;

                        }
                    }
                    if (usedConfig.IsLimit_RemovePrioBurst)
                    {
                        var cur_prio = process.PriorityBoostEnabled;
                        if (cur_prio != false)
                        {
                            process.PriorityBoostEnabled = false;
                        }
                    }

                    if (usedConfig.IsLimit_PrioLower)
                    {
                        var cur_prio = process.PriorityClass;
                        if (cur_prio != Limited_PriorityClass)
                        {
                            process.PriorityClass = Limited_PriorityClass;
                        }
                    }

                    break;                
                case LimitMode.Off:
                    _TP.IsLimitedByApp = false;

                    if (usedConfig.IsLimit_SetAffinity)
                    {
                        nint af_normal = CpuTools.GetProcessorAffinity();
                        if (process.ProcessorAffinity != _TP.Orginal_Affinity)
                        {
                            process.ProcessorAffinity = _TP.Orginal_Affinity;
                        }
                    }

                    if (usedConfig.IsLimit_RemovePrioBurst)
                    {
                        var cur_prio = process.PriorityBoostEnabled;

                        bool usedValue = _TP.Orginal_PriorityBoostEnabled;
                        if (cur_prio != usedValue)
                        {
                            process.PriorityBoostEnabled = usedValue;
                        }
                    }

                    if (usedConfig.IsLimit_PrioLower)
                    {
                        ProcessPriorityClass usedValue = _TP.Orginal_PriortyClass;
                        var cur_prio = process.PriorityClass;
                        if (cur_prio != usedValue)
                        {
                            process.PriorityClass = usedValue;
                        }
                    }


                    break;
                case LimitMode.PartialOff:

                    _TP.IsLimitedByApp = true;

                    if (usedConfig.IsLimit_SetAffinity)
                    {
                        var usedValue = _TP.Orginal_Affinity;
                        var cur_aff = _TP.Current_Affinity;
                        if (cur_aff != usedValue)
                        {
                            process.ProcessorAffinity = usedValue;
                        }
                    }

                    if (usedConfig.IsLimit_RemovePrioBurst)
                    {
                        var cur_prio = _TP.Current_PriorityBoostEnabled;
                        bool usedValue = false;
                        if (cur_prio != usedValue)
                        {
                            process.PriorityBoostEnabled = usedValue;
                        }
                    }

                    if (usedConfig.IsLimit_PrioLower)
                    {
                        ProcessPriorityClass usedValue = this.Limited_PriorityClass;
                        var cur_prio = _TP.Current_PriortyClass; 
                        if (cur_prio != usedValue)
                        {
                            process.PriorityClass = usedValue;
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();

            }


        }

        private void Thread_Execute_Sub()
        {
            Pulser_High = Pulser.IsHigh;


            var processes = ProcessEx.GetProcessesByName(POE_ExeNames);
            if (processes.Length == 0)
            {
                Debugging.Step();
                ClearCounters();
                _TP = null;
                DoGuiUpdate();
                return;
            }
            //found
            var process = processes[0];



            
            

            //onetime Init!
            if (_TP == null)
            {
                _TP = new TargetProcess(process, StartTime);

                if(usedConfig.LimitKind== LimitKind.ViaClientLog)
                {
#if DEBUG
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - STARTUP!");
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - process.StartTime {process.StartTime.ToFullDT_German()}");
#endif
                    SetLimit(process, LimitMode.On);
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - LIMIT DONE!");

                    FileInfo fn = new FileInfo(_TP.POE2_LogFile);
                    _TP.LogFile_StartupLength = fn.Length;
                    _TP.LogFile_InitialRun = true;
                }
            }
            else
            {                
                _TP.Update(process);
            }
            Thread_Execute_PFC();

            var limitMode = LimitMode.PartialOff; //prio to PARTIAL OFF!!!, after startup...



            if (_TP!=null && process != null)
            {
                Debugging.Step();
                switch (usedConfig.LimitKind)
                {
                    case LimitKind.AlwaysOff:
                        SetLimit(process, LimitMode.Off);
                        break;
                    case LimitKind.AlwaysOn:
                        SetLimit(process, LimitMode.On);
                        break;
                    case LimitKind.ViaClientLog:

                        var sResetLimit = "[SHADER] Delay: ON";
                        var sSetLimit1 = "Got Instance Details from login server";
                        var sSetLimit2 = "[SHADER] Delay: OFF";


                        try
                        {

                            if (_TP.LogFile_InitialRun)
                            {
                                //waiting log changed...
                                FileInfo fn = new FileInfo(_TP.POE2_LogFile);
                                if (fn.Length != _TP.LogFile_StartupLength || !_TP.IsStartedAfterFix)
                                {
                                    _TP.LogFile_InitialRun = false;
#if DEBUG
                                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} Startup, Logfile changed!");
#endif
                                    if (_TP.IsStartedAfterFix)
                                    {
                                        SetLimit(process, LimitMode.PartialOff);
                                    }
                                    else
                                    {
                                        SetLimit(process, LimitMode.Off);
                                    }


                                }
                                else
                                {

                                    //POE2, has not added log...
#if DEBUG
                                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} Startup, waiting for new lines in logfile...");
#endif
                                }
                            }
                            else
                            {
                                //normal
                                string fileContents = ReadTail(_TP.POE2_LogFile, 20000);



                                _TP.Pos_ResetLimit = fileContents.LastIndexOf(sResetLimit);
                                _TP.Pos_SetLimit1 = fileContents.LastIndexOf(sSetLimit1);
                                _TP.Pos_SetLimit2 = fileContents.LastIndexOf(sSetLimit2);

                                int[] allConditions = [_TP.Pos_ResetLimit, _TP.Pos_SetLimit1, _TP.Pos_SetLimit2];
                                bool allUnset = allConditions.All(x => x == -1);

                                if (!allUnset)
                                {

                                    if (_TP.Pos_ResetLimit > _TP.Pos_SetLimit1 && _TP.Pos_ResetLimit > _TP.Pos_SetLimit2)
                                    {
                                        //not limiting...
                                        limitMode = LimitMode.Off;
                                    }
                                    else
                                    {
                                        //one of both...
                                        if (_TP.Pos_SetLimit1 > _TP.Pos_ResetLimit)
                                            limitMode = LimitMode.On;
                                        if (_TP.Pos_SetLimit2 > _TP.Pos_ResetLimit)
                                            limitMode = LimitMode.On;
                                    }
                                    Debugging.Step();

                                    //Pulse
                                    if (usedConfig.IsAutolimit_pulse_limit)
                                    {
                                        if (limitMode == LimitMode.On)
                                        {
                                            if (Pulser_High)
                                                limitMode = LimitMode.On_Inverted;
                                        }
                                    }//pulse
                                }//! all unset...

                            }//
                        }
                        catch
                        (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                            Debugging.Step();
                        }

                        //Trace.WriteLine($"condition_value: {condition_value}");


                        switch (limitMode)
                        {
                            case LimitMode.On:
                                swLimitToNormalDelaySW = null;
                                SetLimit(process, LimitMode.On);
                                Debugging.Step();
                                break;
                            case LimitMode.On_Inverted:
                                swLimitToNormalDelaySW = null;
                                SetLimit(process, LimitMode.On_Inverted);
                                Debugging.Step();
                                break;
                            case LimitMode.PartialOff:
                                SetLimit(process, LimitMode.PartialOff);
                                swLimitToNormalDelaySW = null;
                                break;
                            case LimitMode.Off:
                                if (swLimitToNormalDelaySW == null)
                                {
                                    swLimitToNormalDelaySW = new Stopwatch();
                                    swLimitToNormalDelaySW.Start();
                                }

                                if (swLimitToNormalDelaySW != null && swLimitToNormalDelaySW.Elapsed.TotalSeconds >= usedConfig.LimitToNormalDelaySecs)
                                {
                                    //will be called multiple times!!!
                                    SetLimit(process, LimitMode.Off);
#if DEBUG
                                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} ResetLimit");
#endif
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    default:
                        throw new ThrottlerCriticalException($"N/A {usedConfig.LimitKind}");
                }//switch kind

                //_TP.Update(process);

                if (DT_Cylcle != DT_LastCylce) //keep only unique entries!
                {
                    bool notresponding = true;
                    if (_TP != null)
                        notresponding = _TP.IsNotResponding;

                    limits.Add(new LimitedEntry(DT_Cylcle, _TP!.Current_Affinity_Percent, _TP.IsLimitedByApp,notresponding));

                }

            }//process!=null





            Debugging.Step();



            DoGuiUpdate();

        }

    }
}
