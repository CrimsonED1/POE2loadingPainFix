#define RECOVERY2
using System.Diagnostics;
using System.IO;
using System.Text;


namespace POE2loadingPainFix
{
    public enum LimitMode
    {
        Off,
        On,
    }



    public class PoeThrottler : PoeThread
    {
        TargetProcess? _TP = null;
        Stopwatch swTimeoutGUI = new Stopwatch();
        Stopwatch? swLimitToNormalDelaySW = null;



        List<MeasureEntry> measures = new List<MeasureEntry>(10000);
        List<LimitedEntry> limits = new List<LimitedEntry>(10000);


        DateTime DT_LastMeasure = DateTime.Now;

        private bool IsGuiTaskActive = false;


#if RECOVERY
        PoeRecovery? Recovery;
#endif
        PerformanceCounter? Disk_Time_Counter;
        PerformanceCounter? Process_IO_ReadBytesPerSecCounter;
        PerformanceCounter? CPU_Total_Counter;
        public event EventHandler<State>? GuiUpdate;


        //########################################

        public PoeThrottler() : base()
        {
            this.OnThreadException += PoeThrottler_OnThreadException;
            
        }

        private void PoeThrottler_OnThreadException(object? sender, Exception ex)
        {
            DoGuiUpdate(ex);
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

        public override void Start()
        {
            swTimeoutGUI.Start();

#if RECOVERY
            if (Recovery != null)
            {
                Recovery?.Stop();
                Recovery = null;
            }
#endif

            base.Start();

#if RECOVERY
            Recovery = new PoeRecovery();
            Recovery.OnThreadException += Recovery_OnThreadException;
            Recovery.Start();
#endif

        }

        private void Recovery_OnThreadException(object? sender, Exception ex)
        {
            DoGuiUpdate(ex);
        }

        public override void Stop()
        {
            base.Stop();
#if RECOVERY
            if (Recovery != null)
            {
                Recovery.Stop();
                Recovery = null;
            }
#endif
            ClearCounters();
        }


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
                        CycleTime = CycleTime,
                        LastError = exception,
                    };
                }
                else
                {


                    //update GUI
                    TimeSpan? timetoResetLimit = null;
                    if (swLimitToNormalDelaySW != null)
                    {
                        var diff = UsedConfig.LimitToNormalDelaySecs - swLimitToNormalDelaySW.Elapsed.TotalSeconds;
                        if (diff > 0)
                        {
                            timetoResetLimit = TimeSpan.FromSeconds(diff);
                        }
                    }



                    state = new State(_TP)
                    {
                        CycleTime = CycleTime,
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

                    if (_TP != null && _TP.LogFile_InitialRun)
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

        private void ThreadMain_Execute_InitCounters()
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
            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
        private void ThreadMain_Execute_PFC()
        {
            try
            {
                ThreadMain_Execute_InitCounters();



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


        private void SetLimit(Process process, LimitMode limited)
        {
            if (_TP == null)
                return;

#if RECOVERY
            bool isrecovery = PoeThreadSharedContext.Instance.IsTryRecovery;

            if (isrecovery)
            {
                return;
            }
#endif

            switch (limited)
            {
                case LimitMode.On:
                    _TP.IsLimitedByApp = true;

                    if (UsedConfig.IsLimit_ViaThreads)
                    {
                        ThreadLimit.ThrottleProcess(process);
                    }
                    else
                    {
                        if (UsedConfig.IsLimit_SetAffinity)
                        {
                            if (limited == LimitMode.On)
                            {
                                nint af_limited = CpuTools.GetProcessorAffinity(UsedConfig.InLimitAffinity);

                                if (process.ProcessorAffinity != af_limited)
                                    process.ProcessorAffinity = af_limited;
                            }

                        }
                        if (UsedConfig.IsLimit_RemovePrioBurst)
                        {
                            var cur_prio = process.PriorityBoostEnabled;
                            if (cur_prio != false)
                            {
                                process.PriorityBoostEnabled = false;
                            }
                        }
                    }
                    

                    break;
                case LimitMode.Off:
                    _TP.IsLimitedByApp = false;

                    if (UsedConfig.IsLimit_ViaThreads)
                    {
                        //nothing to do here!
                    }
                    else
                    {

                        if (UsedConfig.IsLimit_SetAffinity)
                        {
                            nint af_normal = CpuTools.GetProcessorAffinity();
                            if (process.ProcessorAffinity != _TP.Orginal_Affinity)
                            {
                                process.ProcessorAffinity = _TP.Orginal_Affinity;
                            }
                        }

                        if (UsedConfig.IsLimit_RemovePrioBurst)
                        {
                            var cur_prio = process.PriorityBoostEnabled;

                            bool usedValue = _TP.Orginal_PriorityBoostEnabled;
                            if (cur_prio != usedValue)
                            {
                                process.PriorityBoostEnabled = usedValue;
                            }
                        }
                    }

                    
                    break;
                default:
                    throw new NotImplementedException();

            }


        }

        protected override void Thread_Execute()
        {
  
            Process? process = PoeTools.GetPOE();
            if (process == null)
            {
                Debugging.Step();
                ClearCounters();
                _TP = null;
                DoGuiUpdate();
                return;
            }

            //found POE...



            //onetime Init!
            if (_TP == null)
            {
                _TP = new TargetProcess(process, StartTime);

                if (UsedConfig.LimitKind == LimitKind.ViaClientLog)
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
            ThreadMain_Execute_PFC();


            if (_TP != null && process != null)
            {
                var limitMode = LimitMode.Off;
                if (_TP.IsStartedAfterFix)
                    limitMode = LimitMode.On;

                Debugging.Step();
                switch (UsedConfig.LimitKind)
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
                                        SetLimit(process, LimitMode.On);
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
                            case LimitMode.Off:
                                if (swLimitToNormalDelaySW == null)
                                {
                                    swLimitToNormalDelaySW = new Stopwatch();
                                    swLimitToNormalDelaySW.Start();
                                }

                                if (swLimitToNormalDelaySW != null && swLimitToNormalDelaySW.Elapsed.TotalSeconds >= UsedConfig.LimitToNormalDelaySecs)
                                {
                                    //will be called multiple times!!!
                                    SetLimit(process, LimitMode.Off);
#if DEBUG2
                                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} ResetLimit");
#endif
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    default:
                        throw new ThrottlerCriticalException($"N/A {UsedConfig.LimitKind}");
                }//switch kind

                //_TP.Update(process);

                if (DT_Cylcle != DT_LastCylce) //keep only unique entries!
                {
                    bool isNotResponding = PoeThreadSharedContext.Instance.IsNotResponding;
                    bool isTryRecovery = PoeThreadSharedContext.Instance.IsTryRecovery;

                    limits.Add(new LimitedEntry(DT_Cylcle, _TP!.Current_Affinity_Percent, _TP.IsLimitedByApp, isNotResponding, isTryRecovery));

                }

            }//process!=null





            Debugging.Step();



            DoGuiUpdate();

        }

    }
}
