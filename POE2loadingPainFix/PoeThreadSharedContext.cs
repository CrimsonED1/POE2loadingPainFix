namespace POE2loadingPainFix
{
    public class PoeThreadSharedContext
    {
        private static readonly Lazy<PoeThreadSharedContext> _Instance
           = new Lazy<PoeThreadSharedContext>(() => new PoeThreadSharedContext());
        public static PoeThreadSharedContext Instance => _Instance.Value;


        public LimitMode LimitMode { get; set; } = LimitMode.Off;
        public bool IsNotResponding { get; set; } = false;
        public bool IsTryRecovery { get; set; } = false;

        private Config _Config;
        public Config Config
        {
            get
            {
                return (Config)_Config.Clone();
            }
            set
            {
                _Config = value;
            }
        }
        private PoeThreadSharedContext()
        {
            Config = new Config();
        }
    }
}
