using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{
    public static class CpuTools
    {
        public static bool[] GetProcessors_FromAffinity(nint affinity)
        {
            var hex = affinity.ToString("X");
            
            var res = new List<bool>();
            for (int ihalfbyte = 0; ihalfbyte < hex.Length; ihalfbyte++)
            {                
                

                byte curHalfByte = Convert.ToByte(hex[ihalfbyte].ToString(),16);
                byte c1 = 0x0001;
                if ((curHalfByte & c1) != 0)
                    res.Add(true);
                else
                    res.Add(false);

                byte c2 = 0x0002;
                if ((curHalfByte & c2) != 0)
                    res.Add(true);
                else
                    res.Add(false);

                byte c3 = 0x0004;
                if ((curHalfByte & c3) != 0)
                    res.Add(true);
                else
                    res.Add(false);

                byte c4 = 0x0008;
                if ((curHalfByte & c4) != 0)
                    res.Add(true);
                else
                    res.Add(false);


                Debugging.Step();
            }
            return res.ToArray();
        }

        public static int GetProcessorsCount_FromAffinity(nint affinity)
        {
            bool[] subres = GetProcessors_FromAffinity(affinity);
            var allset = subres.Count(x => x == true);
            return allset;
        }

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

#if DEBUG
        public static int Debug_OverrideCPUs = Environment.ProcessorCount;
#endif

        public static nint GetProcessorAffinity(IEnumerable<bool> target)
        {
            bool[] targetA = target.ToArray();
            
            //you cannot remove all cpus!
            if (target.All(x => x == false))
                return 1;

            int cpus = Environment.ProcessorCount;
#if DEBUG 
            cpus = Debug_OverrideCPUs;
#endif

            {
                int cores = cpus / 4;

                // i dont know if this could be?
                if (cores < 0)
                    return 1;
            }


            int res = 0;
            int currentCore = 0;
            int currentCore_pu = 0;
            var resCores = new List<int>();
            resCores.Add(0);
            for (int i = 0; i < cpus; i++)
            {
                if (currentCore_pu > 3)
                {
                    currentCore++;
                    currentCore_pu = 0;
                    resCores.Add(0);
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
            for (int i = 1; i < resCores.Count; i++)
            {
                int cur = resCores[i] << i * 4;
                res = res | cur;
            }
            
            //must always return 1
            if (res == 0)
                res = 1;

            return (IntPtr)res;
        }
    }
}
