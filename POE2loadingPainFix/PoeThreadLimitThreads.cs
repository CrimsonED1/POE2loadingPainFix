using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{
    public class PoeThreadLimitThreads:PoeThread
    {
        public const string Counter_DoneThreads = "LIMIT_THREADS_DONE";
        public const string Counter_TotalThreads = "LIMIT_THREADS_TOTAL";


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

            ThreadState.AddMeasure(Counter_DoneThreads, limitedThreads);
            ThreadState.AddMeasure(Counter_TotalThreads, totalThreads);


        }

    }
}
