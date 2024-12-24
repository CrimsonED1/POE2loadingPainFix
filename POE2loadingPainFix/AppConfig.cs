using POE2loadingPainFix.CpuThrottleDiskusage;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using static System.Environment;

namespace POE2loadingPainFix
{
    public class AppConfig : INotifyPropertyChanged
    {
        public Rect Window_Position { get; set; }
        public Config ThrottleConfig { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        [JsonIgnore]
        public AffinityEntry[] InLimitAffinity { get; private set; } = new AffinityEntry[0];


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
                for (int i = 0; i < cpus; i++)
                {
                    InLimitAffinity[i] = new AffinityEntry(i, ThrottleConfig.InLimitAffinity[i]);
                }
            }
            else
            {
                //DEFAULT
                var cpuHalf = cpus / 2;
                InLimitAffinity = new AffinityEntry[cpus];
                for (int i = 0; i < cpus; i++)
                {
                    if (i < cpuHalf)
                        InLimitAffinity[i] = new AffinityEntry(i, true);
                    else
                        InLimitAffinity[i] = new AffinityEntry(i, false);
                }
            }

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


        public static AppConfig LoadAppConfig()
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
                return DefaultConfig();
            else
                return res;

        }

        public static void SaveAppConfig(AppConfig appConfig)
        {
            
            string json = JsonSerializer.Serialize(appConfig);
            File.WriteAllText(AppConfigPath, json, Encoding.UTF8);
        }
    }
}
