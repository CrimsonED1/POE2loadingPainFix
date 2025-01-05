namespace POE2loadingPainFix
{
    public class PoeThreadSharedContext
    {
        private static readonly Lazy<PoeThreadSharedContext> _Instance
           = new Lazy<PoeThreadSharedContext>(() => new PoeThreadSharedContext());
        public static PoeThreadSharedContext Instance => _Instance.Value;

        private TargetProcess? _TargetProcess = null;
        public TargetProcess? TargetProcess
        {
            get
            {
                if (_TargetProcess == null)
                    return null;
                return (TargetProcess)_TargetProcess.Clone();
            }
            set
            {
                if (value!=null)
                    _TargetProcess = (TargetProcess)value.Clone();

                else
                    _TargetProcess=null;
                
            }
        }

        //public LimitMode LimitMode { get; set; } = LimitMode.Off;
        //public bool IsNotResponding { get; set; } = false;
        //public bool IsTryRecovery { get; set; } = false;

        private Config _Config = new Config();
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

        public bool IsTryRecovery { get; internal set; }

        private PoeThreadSharedContext()
        {
            Config = new Config();
        }
    }
}
