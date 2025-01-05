namespace POE2loadingPainFix
{
    public class ThreadState
    {
        public ThreadState(Type threadType,string caption)
        {
            ThreadType = threadType;
            Caption = caption;
        }

        public string Caption { get; }

        public Type ThreadType { get; }
        public TimeSpan? CycleTime { get; set; }
        public Exception? Exception { get; set; }

        public Dictionary<string, List<DtValue>> Measures { get; } = new Dictionary<string, List<DtValue>>();


 


    }

    public static class ThreadStateExtensions
    {

        public static void MoveDataFrom(this ThreadState targetState, ThreadState sourceState)
        {
            targetState.CycleTime = sourceState.CycleTime;
            targetState.Exception = sourceState.Exception;

            if (sourceState.Measures.Count > 0)
            {
                foreach (var kv in sourceState.Measures)
                {
                    List<DtValue> values;
                    if (!targetState.Measures.TryGetValue(kv.Key, out values))
                    {
                        values = new List<DtValue>();
                        targetState.Measures.Add(kv.Key, values);
                    }
                    values.AddRange(kv.Value);
                }

                sourceState.Measures.Clear();
            }


        }
    }
}
