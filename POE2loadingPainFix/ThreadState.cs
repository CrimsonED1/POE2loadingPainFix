namespace POE2loadingPainFix
{
    public class ThreadState
    {
        public ThreadState(Type threadType)
        {
            ThreadType = threadType;
        }

        public Type ThreadType { get; }
        public TimeSpan? CycleTime { get; set; }
        public Exception? Exception { get; set; }

        public Dictionary<string, List<DtValue>> Measures { get; } = new Dictionary<string, List<DtValue>>();


        public DateTime DT_Cylcle { get; set; } = DateTime.Now;
        public DateTime DT_LastCylce { get; set; } = DateTime.Now.AddMinutes(-1);


        public void AddMeasure(string measureName, double value)
        {
            if (DT_Cylcle == DT_LastCylce)
                return; //prevent adding dupes!

            List<DtValue> values;
            if (!Measures.TryGetValue(measureName, out values))
            {
                values = new List<DtValue>();
                Measures.Add(measureName, values);
            }

            values.Add(new DtValue(DT_Cylcle, value));



        }
    }
}
