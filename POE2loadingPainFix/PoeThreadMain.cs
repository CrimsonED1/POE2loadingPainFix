#define RECOVERY2
using System.Diagnostics;
using System.IO;
using System.Text;


namespace POE2loadingPainFix
{

    public class PoeThreadMain : PoeThread
    {
        public const string Counter_Limited = "LIMITED";


        Stopwatch swTimeoutGUI = new Stopwatch();
        Stopwatch? swLimitToNormalDelaySW = null;

        private bool IsGuiTaskActive = false;

        private int LimitDoneThreads = 0;


        Type[] Default_Threads = [
            typeof(PoeThreadPFC),
#if REOVERY
            typeof(PoeThreadRecovery),
#endif
        ];

        List<PoeThread> SubThreads = new List<PoeThread>();


        public event EventHandler<State>? GuiUpdate;


        //########################################

        public PoeThreadMain() : base()
        {

        }

        protected override void Thread_Execute_2()
        {
            DoGuiUpdate();
        }





        public override void Start()
        {
            swTimeoutGUI.Start();

            if (SubThreads.Count > 0)
            {
                foreach (var thread in SubThreads)
                {
                    thread.Stop();
                }
                SubThreads.Clear();
            }


            base.Start();


            if (Default_Threads.Length > 0)
            {
                foreach (var threadType in Default_Threads)
                {
                    PoeThread thread = (PoeThread)Activator.CreateInstance(threadType)!;
                    thread.Start();
                    SubThreads.Add(thread);
                }

            }
        }



        public override void Stop()
        {
            base.Stop();

            if (SubThreads.Count > 0)
            {
                foreach (var thread in SubThreads)
                {
                    thread.Stop();
                }
                SubThreads.Clear();
            }


        }


        private void DoGuiUpdate()
        {
            //collect thread data...
            var threadstates = new List<ThreadState>();
            foreach (var thread in SubThreads)
            {
                threadstates.Add(thread.TakeThreadState());
            }


            int ThreadGuiUpdateMs = 300;

            if (swTimeoutGUI.Elapsed.TotalMilliseconds > ThreadGuiUpdateMs && GuiUpdate != null)
            {
                swTimeoutGUI.Restart();

                State state = new State(UsedTP)
                {
                    ThreadStates = threadstates.ToArray(),

                };

                //foreach (var tsate in threadstates)
                //{
                //    if(tsate.)
                //}


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


                if (timetoResetLimit != null)
                {
                    state.LimitCaption += $"Reset Limit in: {timetoResetLimit.Value.TotalSeconds:n1} sec";
                }

                if (UsedTP != null && UsedTP.LogFile_InitialRun)
                {
                    state.LimitCaption = "Startup, waiting for logfile changes";
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




        private void SetLimit(Process process, LimitMode limited)
        {
//            if (UsedTP == null)
//                return;

//#if RECOVERY
//            bool isrecovery = PoeThreadSharedContext.Instance.IsTryRecovery;

//            if (isrecovery)
//            {
//                return;
//            }
//#endif

//            switch (limited)
//            {
//                case LimitMode.On:
//                    UsedTP.IsLimitedByApp = true;

//                    if (UsedConfig.IsLimit_ViaThreads)
//                    {
//                        LimitDoneThreads = ThreadLimit.ThrottleProcess(process, UsedConfig.LimitThreads_Pause_MSecs);
//                    }

//                    if (UsedConfig.IsLimit_SetAffinity)
//                    {
//                        if (limited == LimitMode.On)
//                        {
//                            nint af_limited = CpuTools.GetProcessorAffinity(UsedConfig.InLimitAffinity);

//                            if (process.ProcessorAffinity != af_limited)
//                                process.ProcessorAffinity = af_limited;
//                        }

//                    }
//                    if (UsedConfig.IsLimit_RemovePrioBurst)
//                    {
//                        var cur_prio = process.PriorityBoostEnabled;
//                        if (cur_prio != false)
//                        {
//                            process.PriorityBoostEnabled = false;
//                        }
//                    }


//                    break;
//                case LimitMode.Off:
//                    _TP.IsLimitedByApp = false;

//                    if (UsedConfig.IsLimit_ViaThreads)
//                    {
//                        //nothing to do here!
//                        LimitDoneThreads = 0;
//                    }

//                    if (UsedConfig.IsLimit_SetAffinity)
//                    {
//                        nint af_normal = CpuTools.GetProcessorAffinity();
//                        if (process.ProcessorAffinity != _TP.Orginal_Affinity)
//                        {
//                            process.ProcessorAffinity = _TP.Orginal_Affinity;
//                        }
//                    }

//                    if (UsedConfig.IsLimit_RemovePrioBurst)
//                    {
//                        var cur_prio = process.PriorityBoostEnabled;

//                        bool usedValue = _TP.Orginal_PriorityBoostEnabled;
//                        if (cur_prio != usedValue)
//                        {
//                            process.PriorityBoostEnabled = usedValue;
//                        }
//                    }



//                    break;
//                default:
//                    throw new NotImplementedException();

//            }


        }

        protected override void Thread_Execute(Process? poeProcess)
        {

            Process? process = PoeTools.GetPOE();
            if (process == null)
            {
                PoeThreadSharedContext.Instance.TargetProcess = null;
                return;
            }

            if (UsedTP != null)
            {
                if (process.Id != UsedTP.PID)
                {
                    PoeThreadSharedContext.Instance.TargetProcess = null;
                    return;
                }
            }


            //found POE...

            //onetime Init!
            if (UsedTP == null)
            {
                UsedTP = new TargetProcess(process, StartTime);

                if (UsedConfig.LimitKind == LimitKind.ViaClientLog)
                {
#if DEBUG
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - STARTUP!");
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - process.StartTime {process.StartTime.ToFullDT_German()}");
#endif
                    SetLimit(process, LimitMode.On);
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - LIMIT DONE!");

                    FileInfo fn = new FileInfo(UsedTP.POE2_LogFile);
                    UsedTP.LogFile_StartupLength = fn.Length;
                    UsedTP.LogFile_InitialRun = true;
                }
            }
            else
            {
                UsedTP.Update(process);
            }

            var limitMode = LimitMode.Off;

            if (UsedTP != null && process != null)
            {
                if (UsedTP.IsStartedAfterFix)
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

                            if (UsedTP.LogFile_InitialRun)
                            {
                                //waiting log changed...
                                FileInfo fn = new FileInfo(UsedTP.POE2_LogFile);
                                if (fn.Length != UsedTP.LogFile_StartupLength || !UsedTP.IsStartedAfterFix)
                                {
                                    UsedTP.LogFile_InitialRun = false;
#if DEBUG
                                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} Startup, Logfile changed!");
#endif
                                    if (UsedTP.IsStartedAfterFix)
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
                                string fileContents = ReadTail(UsedTP.POE2_LogFile, 20000);



                                UsedTP.Pos_ResetLimit = fileContents.LastIndexOf(sResetLimit);
                                UsedTP.Pos_SetLimit1 = fileContents.LastIndexOf(sSetLimit1);
                                UsedTP.Pos_SetLimit2 = fileContents.LastIndexOf(sSetLimit2);

                                int[] allConditions = [UsedTP.Pos_ResetLimit, UsedTP.Pos_SetLimit1, UsedTP.Pos_SetLimit2];
                                bool allUnset = allConditions.All(x => x == -1);

                                if (!allUnset)
                                {

                                    if (UsedTP.Pos_ResetLimit > UsedTP.Pos_SetLimit1 && UsedTP.Pos_ResetLimit > UsedTP.Pos_SetLimit2)
                                    {
                                        //not limiting...
                                        limitMode = LimitMode.Off;
                                    }
                                    else
                                    {
                                        //one of both...
                                        if (UsedTP.Pos_SetLimit1 > UsedTP.Pos_ResetLimit)
                                            limitMode = LimitMode.On;
                                        if (UsedTP.Pos_SetLimit2 > UsedTP.Pos_ResetLimit)
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


            }//process!=null


            Debugging.Step();


            ThreadState.AddMeasure(Counter_Limited, limitMode == LimitMode.On ? 1 : 0);
            PoeThreadSharedContext.Instance.LimitMode = limitMode;
            PoeThreadSharedContext.Instance.TargetProcess = UsedTP;


            DoGuiUpdate();

        }

    }
}
