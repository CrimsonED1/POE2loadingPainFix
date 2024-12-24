namespace POE2loadingPainFix.CpuThrottleDiskusage
{
    public class ThrottlerCriticalException : Exception
    {
        public ThrottlerCriticalException(string? message) : base(message)
        {
        }
    }
}
