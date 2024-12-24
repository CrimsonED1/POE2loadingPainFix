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
    }
}
