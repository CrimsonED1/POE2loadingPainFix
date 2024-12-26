using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{

    public static class ExtensionTools
    {

        
        public static string ToSingleString<T>(this IEnumerable<T> items,string seperator=",")
        {
            var s = "";
            var a = items.ToArray();
            for(int i = 0;i< a.Length;i++)
            {
                
                if (i != a.Length - 1)
                    s += $"{a[i]}{seperator}";
                else
                    s += $"{a[i]}";
            }
            return s;

        }

        public static double[] NoiseReduction(this double[] src, int severity = 1)
        {
            var res = src;

            for (int i = 1; i < res.Length; i++)
            {
                //---------------------------------------------------------------avg
                var start = (i - severity > 0 ? i - severity : 0);
                var end = (i + severity < res.Length ? i + severity : res.Length);

                double sum = 0;

                for (int j = start; j < end; j++)
                {
                    sum += res[j];
                }

                var avg = sum / (end - start);
                //---------------------------------------------------------------
                res[i] = avg;

            }
            return res;
        }
    }
}

    
