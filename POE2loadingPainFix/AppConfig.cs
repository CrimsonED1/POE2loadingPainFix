using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using PropertyChanged;

namespace POE2loadingPainFix
{
    public enum AppConfigKind
    {
        Easy,
        Expert
    }

    
    public partial class AppConfig : INotifyPropertyChanged
    {
        private AppConfigKind _AppConfigKind = AppConfigKind.Easy;
        public AppConfigKind AppConfigKind
        {
            get=>_AppConfigKind;
            set
            {
                if (_AppConfigKind == value) 
                    return;
                _AppConfigKind = value;

                if(_AppConfigKind== AppConfigKind.Easy)
                {
                    ThrottleConfig = new Config();
                    this.IsUpdateGraphs = false;
                    this.IsUpdateGraphsThreads = false;
                    
                    Init();
                }
                if(_AppConfigKind==AppConfigKind.Expert)
                {
                    this.IsUpdateGraphs = true;
                    this.IsUpdateGraphsThreads = true;
                }
                OnPropertyChanged(nameof(IsExpertMode));

            }
        }
        public Rect Window_Position { get; set; }
        public Config ThrottleConfig { get; set; }

        
        public bool IsGraphMain_Expanded { get; set; } = true;
        public bool IsGraphThreads_Expanded { get; set; }= true;

        public bool IsUpdateGraphs { get; set; } = true;
        public bool IsUpdateGraphsThreads { get; set; } = true;


        public Visibility IsExpertMode => AppConfigKind== AppConfigKind.Expert ? Visibility.Visible: Visibility.Collapsed;
        public string Version { get; set; } = "";

        [JsonIgnore]
        public CPUEntry[] InLimitAffinity { get; private set; } = new CPUEntry[0];


        [JsonConstructor]
        public AppConfig()
        {
            ThrottleConfig = new Config();

            Init();

        }

        private void Init()
        {
            var cpus = Environment.ProcessorCount;

            if (ThrottleConfig.InLimitAffinity.Length == cpus)
            {
                //load from config
                //InLimitAffinity = new AffinityEntry[cpus];

                CPUEntry[] cpulist = CpuTools.GetProcessors();
                InLimitAffinity = cpulist;
                for (int i = 0; i < cpulist.Length; i++)
                {
                    InLimitAffinity[i].IsSet = ThrottleConfig.InLimitAffinity[i];
                }
                Debugging.Step();
            }
            else
            {
                CPUEntry[] cpulist = CpuTools.GetProcessors_perCore(1);
                //DEFAULT
                InLimitAffinity = cpulist;
                ThrottleConfig.InLimitAffinity = InLimitAffinity.Select(x => x.IsSet).ToArray();
            }

            Debugging.Step();

            foreach (var entry in InLimitAffinity)
                entry.PropertyChanged += SaveOn_PropertyChanged;
        }

        private void SaveOn_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ThrottleConfig.InLimitAffinity = InLimitAffinity.Select(x => x.IsSet).ToArray();
            SaveAppConfig(this);
        }



        public void StoreWindowPosition(Window window)
        {
            Window_Position = new Rect(window.Left, window.Top, window.Width, window.Height);
            SaveAppConfig(this);
        }



        public static string AppConfigPath
        {
            get
            {
                string pathconfig;
#if DEBUG
                pathconfig = @"C:\Users\Karsten\OneDrive\Dokumente\Development\POE2loadingPainFix\bin_data\appconfig.json";
#else
            var commonpath = Environment.GetFolderPath(SpecialFolder.MyDocuments);
            pathconfig = System.IO.Path.Combine(commonpath, @"POE2loadingPainFix\appconfig.json");
            var dir = Path.GetDirectoryName(pathconfig);
            Directory.CreateDirectory(dir);
#endif
                return pathconfig;

            }
        }

        public static AppConfig DefaultConfig()
        {
            var res = new AppConfig();

            return res;
        }


        public static AppConfig LoadAppConfig(string appVersion)
        {
            AppConfig? res = null;
            try
            {
                string json = File.ReadAllText(AppConfigPath, Encoding.UTF8);
                res = JsonSerializer.Deserialize<AppConfig>(json);                
                res?.Init();
            }
            catch (Exception ex)
            {
                Debugging.Step();
            }

            if (res == null)
            {
                res = DefaultConfig();
                res.Version = appVersion;
            }

            if(res.Version=="")
            {
                //patch old files!
                res.ThrottleConfig.InLimitAffinity = new bool[1];
                res.Init();
            }

            if (res.Version != appVersion)
            {
                //patch old files!
                res.ThrottleConfig.InLimitAffinity = new bool[1];
                res.Init();
            }

            res.Version = appVersion;
            return res;

        }

        public static void SaveAppConfig(AppConfig appConfig)
        {
            
            string json = JsonSerializer.Serialize(appConfig);
            File.WriteAllText(AppConfigPath, json, Encoding.UTF8);
        }
    }
}
