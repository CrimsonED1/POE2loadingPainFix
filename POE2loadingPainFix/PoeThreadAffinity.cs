using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{
    public class PoeThreadAffinity:PoeThread
    {
        public const string Counter_AffinityPercent = "AFFINITY%";
        public override string Caption => "AF";

        public const ProcessPriorityClass Limited_PriorityClass = ProcessPriorityClass.BelowNormal;

        private ProcessPriorityClass GetPriorityClass()
        {
            if (UsedTP == null)
            {
                return ProcessPriorityClass.Normal;
            }

            var limitmode = UsedTP.LimitMode;
            ProcessPriorityClass usedValue = ProcessPriorityClass.Normal;
            switch (limitmode)
            {
                case LimitMode.Off:
                    usedValue = UsedTP.Orginal_PriortyClass;
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



        protected override void Thread_Execute(Process? poeProcess)
        {
            if (UsedTP == null || poeProcess==null)
                return;

            AddMeasure(Counter_AffinityPercent, UsedTP.Current_Affinity_Percent);

            bool isrecovery = PoeThreadSharedContext.Instance.IsTryRecovery;
            if (isrecovery)
            {
                return;
            }

            //Restore default...
            //##########################################
            if (!UsedConfig.IsLimit_PrioLower)
            {
                var cur = poeProcess.PriorityClass;
                var usedValue = UsedTP.Orginal_PriortyClass;
                if (cur != usedValue)
                {
                    poeProcess.PriorityClass = usedValue;
                
                }
            }
            if (!UsedConfig.IsLimit_RemovePrioBurst)
            {
                var cur = poeProcess.PriorityBoostEnabled;
                var usedValue = UsedTP.Orginal_PriorityBoostEnabled;
                if (cur != usedValue)
                {
                    poeProcess.PriorityBoostEnabled = usedValue;

                }
            }
            if (!UsedConfig.IsLimit_SetAffinity)
            {
                var cur = poeProcess.ProcessorAffinity;
                var usedValue = UsedTP.Orginal_Affinity;
                if (cur != usedValue)
                {
                    poeProcess.ProcessorAffinity = usedValue;

                }
            }

            if (!UsedConfig.IsLimit_PrioLower && !UsedConfig.IsLimit_RemovePrioBurst && !UsedConfig.IsLimit_SetAffinity)
                return;

            //##########################################

            switch (UsedTP.LimitMode)
            {
                case LimitMode.On:
                    if (UsedConfig.IsLimit_SetAffinity)
                    {
                        nint af_limited = CpuTools.GetProcessorAffinity(UsedConfig.InLimitAffinity);

                        if (poeProcess.ProcessorAffinity != af_limited)
                            poeProcess.ProcessorAffinity = af_limited;
                    }

                    if (UsedConfig.IsLimit_RemovePrioBurst)
                    {
                        var cur_prio = poeProcess.PriorityBoostEnabled;
                        if (cur_prio != false)
                        {
                            poeProcess.PriorityBoostEnabled = false;
                        }
                    }

                    if (UsedConfig.IsLimit_PrioLower)
                    {
                        var cur_prio = poeProcess.PriorityClass;
                        var usedValue = GetPriorityClass();
                        if (cur_prio != usedValue)
                        {
                            poeProcess.PriorityClass = usedValue;
                        }
                    }

                    break;
                case LimitMode.Off:
                    

                    if (UsedConfig.IsLimit_SetAffinity)
                    {
                        nint af_normal = CpuTools.GetProcessorAffinity();
                        if (poeProcess.ProcessorAffinity != UsedTP.Orginal_Affinity)
                        {
                            poeProcess.ProcessorAffinity = UsedTP.Orginal_Affinity;
                        }
                    }

                    if (UsedConfig.IsLimit_RemovePrioBurst)
                    {
                        var cur_prio = poeProcess.PriorityBoostEnabled;

                        bool usedValue = UsedTP.Orginal_PriorityBoostEnabled;
                        if (cur_prio != usedValue)
                        {
                            poeProcess.PriorityBoostEnabled = usedValue;
                        }
                    }

                    if (UsedConfig.IsLimit_PrioLower)
                    {
                        var cur_prio = poeProcess.PriorityClass;
                        var usedValue = GetPriorityClass();
                        if (cur_prio != usedValue)
                        {
                            poeProcess.PriorityClass = usedValue;
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();

            }


        }

    }
}
