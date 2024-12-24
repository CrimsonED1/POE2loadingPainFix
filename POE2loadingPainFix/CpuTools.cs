using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{
    public static class CpuTools
    {
        public static nint GetProcessorAffinity()
        {
            int cores = Environment.ProcessorCount / 4;
            int res = 0;
            for (int i = 0; i < cores; i++)
            {
                int cur = 0xF << i * 4;
                res = res | cur;
            }
            return (IntPtr)res;
        }

        public static nint GetProcessorAffinity(IEnumerable<bool> target)
        {
            bool[] targetA = target.ToArray();


            int cores = Environment.ProcessorCount / 4;
            int res = 0;
            int currentCore = 0;
            int currentCore_pu = 0;
            int[] resCores = new int[cores];
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                if (currentCore_pu > 3)
                {
                    currentCore++;
                    currentCore_pu = 0;
                }
                //#############
                Debugging.Step();
                if (i < targetA.Length)
                {
                    bool isset = targetA[i];
                    int b = 0;
                    if (isset)
                    {
                        if (currentCore_pu == 0)
                            b = 0x0001;
                        if (currentCore_pu == 1)
                            b = 0x0002;
                        if (currentCore_pu == 2)
                            b = 0x0004;
                        if (currentCore_pu == 3)
                            b = 0x0008;
                    }
                    resCores[currentCore] = resCores[currentCore] | b;
                    Debugging.Step();
                }

                //#############
                currentCore_pu++;
            }
            Debugging.Step();

            res = resCores[0];
            for (int i = 1; i < cores; i++)
            {
                int cur = resCores[i] << i * 4;
                res = res | cur;
            }
            return (IntPtr)res;
        }
    }
}
