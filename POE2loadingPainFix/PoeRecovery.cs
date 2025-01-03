using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace POE2loadingPainFix
{
    public class PoeRecovery : PoeThread
    {
        public PoeRecovery() : base()
        {
        }

        public override void Start()
        {
            ThreadSleep = 10;
            base.Start();   
        }        

        private ProcessPriorityClass Limited_PriorityClass = ProcessPriorityClass.BelowNormal;

        private DateTime LastResponseDT = DateTime.Now;

        private bool LastResponseValue = false;

        private Stopwatch? Recovery_Restart_DelayTime;
        private Stopwatch? Recovery_MaximumTime;

        private ProcessPriorityClass GetNormal_PriorityClass()
        {
            var limitmode = PoeThreadSharedContext.Instance.LimitMode;
            ProcessPriorityClass usedValue = ProcessPriorityClass.Normal;
            switch (limitmode)
            {
                case LimitMode.Off:
                    if (UsedConfig.IsLimit_PrioLower)
                    {
                        usedValue = ProcessPriorityClass.Normal;
                    }
                    break;
                case LimitMode.On:
                    if (UsedConfig.IsLimit_PrioLower)
                    {
                        usedValue = Limited_PriorityClass;
                    }
                    break;
                default:
                    throw new NotImplementedException();



            }

            return usedValue;
        }

        protected override void Thread_Execute()
        {

            Process? process = PoeTools.GetPOE();
            if (process == null)
            {
                Debugging.Step();
                return;
            }

            bool responding = process.Responding;
            bool IsRealTimeSet = false;
            var cur_prio = process.PriorityClass;



#if DEBUG
            if (responding != LastResponseValue)
            {

                if(responding)
                {
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - RESPONDING Again!");
                }
                else
                {
                    var time = DateTime.Now - LastResponseDT;
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - NO RESPONSE (since {time.TotalSeconds:N1})");
                }
            }
#endif

            if (responding)
            {
                LastResponseDT = DateTime.Now;

                var usedValue = GetNormal_PriorityClass();

                //set prio
                if (cur_prio != usedValue)
                {
                    process.PriorityClass = usedValue;
                }
                PoeThreadSharedContext.Instance.IsTryRecovery = false;
                IsRealTimeSet = false;
                
                Recovery_MaximumTime = null;
            }
            else
            {
                //ELSE RECOVERY!!!
                PoeThreadSharedContext.Instance.IsTryRecovery = true;

                if (!IsRealTimeSet)
                {
#if DEBUG
                    if(Recovery_Restart_DelayTime!=null)
                        Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - Recovery_Restart_DelayTime: {Recovery_Restart_DelayTime}");
#endif

                    if (Recovery_Restart_DelayTime==null || Recovery_Restart_DelayTime.Elapsed.TotalSeconds > 5)
                    {
                        Recovery_Restart_DelayTime = Stopwatch.StartNew();
                        Recovery_MaximumTime = Stopwatch.StartNew();
                        if (process.PriorityClass != ProcessPriorityClass.RealTime)
                        {

#if DEBUG
                            Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - NO RESPONSE SET REALTIME");
#endif
                            process.PriorityBoostEnabled = true;
                            process.PriorityClass = ProcessPriorityClass.RealTime;
                            IsRealTimeSet = true;
#if DEBUG
                            Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - NO RESPONSE SET REALTIME, DONE");
#endif
                        }
                    }
                }
                else
                {
#if DEBUG
                    if (Recovery_MaximumTime != null)
                        Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - Recovery_MaximumTime: {Recovery_MaximumTime}");
#endif

                    if (Recovery_MaximumTime!=null && Recovery_MaximumTime.Elapsed.TotalSeconds>5)
                    {
#if DEBUG
                        Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - FALLBACK! Set to realtime not working!");
#endif
                        //FALLBACK, POE doesnt go to normal state!
                        var usedValue = GetNormal_PriorityClass();
                        process.PriorityClass = usedValue;
                        Recovery_MaximumTime = null;
                        IsRealTimeSet = false;

                        Recovery_Restart_DelayTime = Stopwatch.StartNew();

                    }
                }

            }

            LastResponseValue = responding;

            PoeThreadSharedContext.Instance.IsNotResponding = !responding;

        }//exec

    }
}
