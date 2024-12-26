using System.Diagnostics;

namespace POE2loadingPainFix
{
    public static class ProcessEx
    {
        public static Process[] GetProcessesByName(IEnumerable<string> processnames)
        {
            var res = new List<Process>(processnames.Count());
            foreach(string n in processnames)
            {
                res.AddRange(Process.GetProcessesByName(n));
            }
            return res.ToArray();
        }
    }
}

    
