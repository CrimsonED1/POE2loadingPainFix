using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PropertyChanged;
using System.Windows.Interop;
using POE2loadingPainFix.CpuThrottleDiskusage;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using LiveChartsCore;
using System.Text.Json;
using System.IO;
using static System.Environment;
using System.Diagnostics;
using System.Net.Http;

namespace POE2loadingPainFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string Version = "0.1";

        /// <summary>
        /// https://stackoverflow.com/questions/54848286/performancecounter-physicaldisk-disk-time-wrong-value
        /// </summary>

        private Throttler _Throttler;

        public AppConfig AppConfig { get; private set; }

        public State? State { get; private set; }


        public Visibility VisWaitingExe => State == null || (State != null && State.TargetProcess == null) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisLoadingLevel => State != null && State.TargetProcess != null && State.TargetProcess.IsCpuLimited ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisNormal => State != null && State.TargetProcess != null && !State.TargetProcess.IsCpuLimited ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisError => State != null && State.Error != "" ? Visibility.Visible : Visibility.Collapsed;

        public bool IsAlwaysOn
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.AlwaysOn;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.AlwaysOn;
        }
        public bool IsAlwaysOff
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.AlwaysOff;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.AlwaysOff;
        }

        public bool IsViaDiskUsage
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.ViaDiskUsage;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.ViaDiskUsage;
        }

        public bool IsViaIOBytesUsage
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.ViaIOBytesUsage;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.ViaIOBytesUsage;
        }

        public bool IsViaPoe2LogFile
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.ViaClientLog;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.ViaClientLog;
        }
        public bool IsUpdateGraphs { get; set; } = true;

        public int CPUs { get; set; }

        public ObservableCollection<ISeries> Series_Disk { get; set; }
        public ObservableCollection<ISeries> Series_IORead { get; set; }
        private readonly List<DateTimePoint> _DiskValues = [];
        private readonly List<DateTimePoint> _IOReadValues = [];
        private readonly List<DateTimePoint> _LimitedValuesDisk = [];
        private readonly List<DateTimePoint> _LimitedValuesIO = [];

        private readonly DateTimeAxis _customAxis;

        public object SyncRoot { get; } = new object();

        public Axis[] XAxes { get; set; }

        private bool _IsInit = true;
        public MainWindow()
        {
            InitializeComponent();
            CPUs = Environment.ProcessorCount;
            this.Title = this.Title + $" Version: {Version} Processors: {CPUs}";


            AppConfig = AppConfig.LoadAppConfig();



            AppConfig.ThrottleConfig.PropertyChanged += Config_PropertyChanged;

            _Throttler = new Throttler(TargetExe, AppConfig.ThrottleConfig);
            _Throttler.GuiUpdate += _Throttler_GuiUpdate;


            Series_Disk = [
                        new LineSeries<DateTimePoint>
            {
                Values = _DiskValues,
                Fill = null,
                Name="usage(%)",

                GeometryFill = null,
                GeometryStroke = null
            },
                        new LineSeries<DateTimePoint>
            {
                Values = _LimitedValuesDisk,
                Fill = null,

                Name="limited",
                GeometryFill = null,
                GeometryStroke = null
            },];

            _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            {
                CustomSeparators = GetSeparators(),
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
            };

            Series_IORead = [
                        new LineSeries<DateTimePoint>
            {
                Values = _IOReadValues,
                Fill = null,
                Name="I/O(MB/s)",

                GeometryFill = null,
                GeometryStroke = null
            },
                        new LineSeries<DateTimePoint>
            {
                Values = _LimitedValuesIO,
                Fill = null,

                Name="limited",
                GeometryFill = null,
                GeometryStroke = null
            },];

            _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            {
                CustomSeparators = GetSeparators(),
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
            };


            XAxes = [_customAxis];



            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;

            this.DataContext = this;

            CheckNewVersion();
        }

        public Visibility VisNewVersionAvaible { get; private set; } = Visibility.Hidden;

        private void CheckNewVersion()
        {
            Task.Run(async () =>
            {
                try
                {
                    
                    var url = "https://raw.githubusercontent.com/CrimsonED1/POE2loadingPainFix/refs/heads/main/README.md";
                    using (HttpClient client = new HttpClient())
                    {
                        string content = await client.GetStringAsync(url);
                        var iStart = content.IndexOf("Current Version: ");
                        var iStart2 = iStart + "Current Version: ".Length;
                        var iEnd = content.IndexOf("\n", iStart2 + 1);
                        if (iStart>=0 && iEnd>iStart)
                        {
                            var onlineversion = content.Substring(iStart2,iEnd-iStart2);
                            if (onlineversion != Version)
                                VisNewVersionAvaible = Visibility.Visible;

                        }



                        Debugging.Step();
                    }
                }
                catch
                { }
            });

           
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (!_IsInit)
                AppConfig.StoreWindowPosition(this);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_IsInit)
                AppConfig.StoreWindowPosition(this);
        }

        private static double[] GetSeparators()
        {
            var now = DateTime.Now;

            return
            [
                now.AddSeconds(-30).Ticks,
                now.AddSeconds(-25).Ticks,
            now.AddSeconds(-20).Ticks,
            now.AddSeconds(-15).Ticks,
            now.AddSeconds(-10).Ticks,
            now.AddSeconds(-5).Ticks,
            now.Ticks
            ];
        }

        private static string Formatter(DateTime date)
        {
            var secsAgo = (DateTime.Now - date).TotalSeconds;

            return secsAgo < 1
                ? "now"
                : $"{secsAgo:N0}s";
        }



        private void _Throttler_GuiUpdate(object? sender, State e)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(() => _Throttler_GuiUpdate(sender, e));
                return;
            }
            State = e;
            OnPropertyChanged(nameof(VisWaitingExe));
            OnPropertyChanged(nameof(VisLoadingLevel));
            OnPropertyChanged(nameof(VisNormal));
            OnPropertyChanged(nameof(State));


            if (State.MeasureEntries.Length > 0 && IsUpdateGraphs)
            {
                lock (SyncRoot)
                {
                    var cur = DateTime.Now;
                    foreach (var entry in State.MeasureEntries)
                    {

                    }
                    _DiskValues.AddRange(State.MeasureEntries.Select(x => new DateTimePoint(x.DT, x.DiskUsage)));
                    _DiskValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                    _IOReadValues.AddRange(State.MeasureEntries.Select(x => new DateTimePoint(x.DT, x.IORead)));
                    _IOReadValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);


                    double limitValueDisk = 0;
                    double limitValueIO = 0;
                    if (State.TargetProcess != null && State.TargetProcess.IsCpuLimited)
                    {
                        limitValueDisk = 100;
                        limitValueIO = 10;
                    }

                    _LimitedValuesDisk.Add(new DateTimePoint(cur, limitValueDisk));
                    _LimitedValuesDisk.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);
                    _LimitedValuesIO.Add(new DateTimePoint(cur, limitValueIO));
                    _LimitedValuesIO.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                    // we need to update the separators every time we add a new point 
                    _customAxis.CustomSeparators = GetSeparators();
                }
            }


        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {


            Config newConfig = (Config)AppConfig.ThrottleConfig.Clone();
            newConfig.InLimitAffinity = AppConfig.InLimitAffinity.Select(x => x.IsSet).ToArray();

            _Throttler?.UpdateConfig(newConfig);
            OnPropertyChanged(nameof(IsAlwaysOff));
            OnPropertyChanged(nameof(IsAlwaysOn));
            OnPropertyChanged(nameof(IsViaDiskUsage));
        }



        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _Throttler?.Stop();
            Debugging.Step();
        }

#if DEBUG2
        private const string TargetExe = "dummypoe";
#else
        private const string TargetExe = "PathOfExileSteam";

#endif





        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_IsInit)
            {
                if (AppConfig.Window_Position.Left != 0 && AppConfig.Window_Position.Top != 0)
                {
                    this.Left = AppConfig.Window_Position.Left;
                    this.Top = AppConfig.Window_Position.Top;
                    this.Width = AppConfig.Window_Position.Width;
                    this.Height = AppConfig.Window_Position.Height;
                }
            }
            _IsInit = false;
            _Throttler.Start();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}