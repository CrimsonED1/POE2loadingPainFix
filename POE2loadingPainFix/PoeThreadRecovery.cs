using System.Diagnostics;

namespace POE2loadingPainFix
{
    public class PoeThreadRecovery : PoeThread
    {
        public const string Counter_NotResponding = "NOTRESPONDING";

        public PoeThreadRecovery() : base()
        {
        }


        

        private DateTime LastResponseDT = DateTime.Now;

        private bool LastResponseValue = false;

        private Stopwatch? Recovery_Restart_DelayTime;
        private Stopwatch? Recovery_MaximumTime;

       
        protected override void Thread_Execute(Process? poeProcess)
        {

            if (poeProcess == null || UsedTP == null)
            {
                Debugging.Step();
                return;
            }
            Next_ThreadSleep_LimitOn = 2;


            bool responding = poeProcess.Responding;
            bool IsRealTimeSet = false;
            var cur_prio = poeProcess.PriorityClass;



#if DEBUG
            if (responding != LastResponseValue)
            {

                if (responding)
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


                //set prio
                if (cur_prio == ProcessPriorityClass.RealTime)
                {
                    poeProcess.PriorityClass = ProcessPriorityClass.Normal;
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
                    if (Recovery_Restart_DelayTime != null)
                        Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - Recovery_Restart_DelayTime: {Recovery_Restart_DelayTime}");
#endif

                    if (Recovery_Restart_DelayTime == null || Recovery_Restart_DelayTime.Elapsed.TotalSeconds > 5)
                    {
                        Recovery_Restart_DelayTime = Stopwatch.StartNew();
                        Recovery_MaximumTime = Stopwatch.StartNew();
                        if (poeProcess.PriorityClass != ProcessPriorityClass.RealTime)
                        {

#if DEBUG
                            Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - NO RESPONSE SET REALTIME");
#endif
                            poeProcess.PriorityBoostEnabled = true;
                            poeProcess.PriorityClass = ProcessPriorityClass.RealTime;
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

                    if (Recovery_MaximumTime != null && Recovery_MaximumTime.Elapsed.TotalSeconds > 5)
                    {
#if DEBUG
                        Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - FALLBACK! Set to realtime not working!");
#endif
                        //FALLBACK, POE doesnt go to normal state!
                        var usedValue = ProcessPriorityClass.Normal;
                        poeProcess.PriorityClass = usedValue;
                        Recovery_MaximumTime = null;
                        IsRealTimeSet = false;

                        Recovery_Restart_DelayTime = Stopwatch.StartNew();

                    }
                }

            }

            LastResponseValue = responding;

            ThreadState.AddMeasure(Counter_NotResponding, !responding ? 1 : 0);


        }//exec

    }
}
