using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace POE2loadingPainFix
{
    public static class PoeTools
    {
        public static readonly string[] POE_ExeNames = 
            [
#if DEBUG
            "DummyPOE",
#endif
            "PathOfExileSteam", 
            "PathOfExile",
            ];

        public static Process? GetPOE()
        {
            var processes = ProcessEx.GetProcessesByName(POE_ExeNames);
            if (processes.Length == 0)
            {
                return null;
            }
            //found
            var process = processes[0];
            return process;
        }
    }
}
