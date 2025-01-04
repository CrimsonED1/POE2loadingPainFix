using System.Collections.Generic;

namespace POE2loadingPainFix
{

    public class State
    {
        public TargetProcess? TargetProcess { get; }
        public ThreadState[] ThreadStates { get; set; } = new ThreadState[0];

        public string ExceptionsCaption
        {
            get
            {
                if (ThreadStates.Length == 0)
                    return "";
                var res = ThreadStates.Where(x=>x.Exception!=null).Select(x => $"Thread: {x.ThreadType.Name} Exception: {x.Exception!.Message}").ToSingleString(Environment.NewLine);
                return res;
            }
        }

        public string CpuUsageCaption => $"{GetLastMeasureValue(PoeThreadPFC.Counter_CpuUsage):N0} %";
        public string DiskUsageCaption => $"{GetLastMeasureValue(PoeThreadPFC.Counter_DiskUsage):N0} %";
        public string IOReadCaption => $"{GetLastMeasureValue(PoeThreadPFC.Counter_IORead):N2} MB/s";
        public string ThreadsCaption => $"{GetLastMeasureValue(PoeThreadLimitThreads.Counter_DoneThreads)} / {GetLastMeasureValue(PoeThreadLimitThreads.Counter_TotalThreads)}"; 

        public string LimitCaption { get; set; } = "";

        public Dictionary<string, List<DtValue>> AllMeasures 
        {
            get
            {
                var res = new Dictionary<string, List<DtValue>>();
                foreach ( var ts in ThreadStates )
                {
                    foreach( var m in ts.Measures )
                    {
                        res.Add(m.Key,m.Value);
                    }
                    
                }
                return res;
            }
        }

        public List<DtValue> GetMeasureValues(string caption)
        {
            if (!AllMeasures.TryGetValue(caption, out var values))
                return new List<DtValue>();
            return values;
        }

        public double GetLastMeasureValue(string caption)
        {
            if(!AllMeasures.TryGetValue(caption, out var values))
                return 0;

            double res = 0;
            if (values.Count > 0)
            {
                res = values.Last().Value;
            }
            return res;
        }

        public string CycleTimeCaption
        {
            get
            {
                if (ThreadStates.Length == 0)
                    return "";
                var res = ThreadStates.Select(x => x.CycleTime!=null ?  $"{x.ThreadType.Name}={x.CycleTime.Value.TotalMilliseconds:n0} ms" : "N/A").ToSingleString();
                return res;
            }
        }

        public State(TargetProcess? targetProcess)
        {
            if (targetProcess != null)
                TargetProcess = (TargetProcess)targetProcess.Clone();
        }





    }
}
