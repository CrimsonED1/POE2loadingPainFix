using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{
    public class PoeThreadLimitThreads:PoeThread
    {
        public const string Counter_LimitedThreads = "LIMIT_THREADS_LIMITED";
        public const string Counter_ActiveThreads = "LIMIT_THREADS_ACTIVE";
        public const string Counter_TotalThreads = "LIMIT_THREADS_TOTAL";

        public override string Caption => "LTT";
        protected override void Thread_Execute(Process? poeProcess)
        {
            if (UsedTP == null || poeProcess==null)
                return;
            if (!UsedConfig.IsLimit_ViaThreads)
                return;

            
            Next_ThreadSleep_LimitOn = UsedConfig.LimitThreads_Run_MSecs;

            int limitedThreads = 0;
            int totalThreads = poeProcess.Threads.Count;
            switch (UsedTP.LimitMode)
            {
                case LimitMode.On:
                    if (UsedConfig.IsLimit_ViaThreads)
                    {
                        limitedThreads = ThreadLimit.ThrottleProcess(poeProcess, UsedConfig.LimitThreads_Pause_MSecs);
                    }
                    break;
                case LimitMode.Off:
                    //nothing to do here!
                    break;
                default:
                    throw new NotImplementedException();
            }

            var states = new List<System.Diagnostics.ThreadState>();
            foreach (ProcessThread thread in poeProcess.Threads)
            {
                states.Add(thread.ThreadState);
                var state = thread.ThreadState;
            }

            var grp = states.GroupBy(x=>x,(key,g)=>new { State = key, Count = g.Count() }).ToList();

#if DEBUG
            var caption = grp.Select(x => $"{x.State}={x.Count}").ToSingleString(", ");
            Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - Threads: {caption}");
#endif

            var cur_active = grp.FirstOrDefault(x => x.State == System.Diagnostics.ThreadState.Running);
            int active = 0;
            if (cur_active != null)
                active = cur_active.Count;

            AddMeasure(Counter_LimitedThreads, limitedThreads);
            AddMeasure(Counter_TotalThreads, totalThreads);
            AddMeasure(Counter_ActiveThreads, active);


        }

    }
}
