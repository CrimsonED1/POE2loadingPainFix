namespace POE2loadingPainFix
{
    public struct DtValue
    {
        public DtValue(DateTime dT, double value)
        {
            DT = dT;
            Value = value;
        }

        public DateTime DT { get; }
        public double Value { get; }
    }
}
