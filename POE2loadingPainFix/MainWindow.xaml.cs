using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MahApps.Metro.Controls;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;


namespace POE2loadingPainFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public string Version = "0.8";

        /// <summary>
        /// https://stackoverflow.com/questions/54848286/performancecounter-physicaldisk-disk-time-wrong-value
        /// </summary>

        private PoeThreadMain _PoeThreadMain;

        public AppConfig AppConfig { get; private set; }

        public State? State { get; private set; }

        public Exception[]? LastExceptions { get; private set; } = null;
        public string LastExceptionsCaptions { get; private set; } = "";

        public string PoeExes => PoeTools.POE_ExeNames.ToSingleString("/");

        public Visibility VisWaitingExe => State == null || (State != null && State.TargetProcess == null) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisNormal => State != null && State.TargetProcess != null && State.TargetProcess.LimitMode==LimitMode.Off ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisLimited => State != null && State.TargetProcess != null && State.TargetProcess.LimitMode == LimitMode.On ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisNotResponding { get; private set; } = Visibility.Collapsed;


        public Visibility VisError => LastExceptionsCaptions != "" ? Visibility.Visible : Visibility.Collapsed;

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

        public bool IsViaPoe2LogFile
        {
            get => AppConfig.ThrottleConfig.LimitKind == LimitKind.ViaClientLog;
            set => AppConfig.ThrottleConfig.LimitKind = LimitKind.ViaClientLog;
        }
        public bool IsUpdateGraphs { get; set; } = true;

        public int CPUs { get; set; }

        public ObservableCollection<ISeries> Series { get; set; }
        private readonly List<DateTimePoint> _Values_Disk = [];
        private readonly List<DateTimePoint> _Values_Cpu = [];
        private readonly List<DateTimePoint> _Values_IORead = [];
        private readonly List<DateTimePoint> _Values_Limited = [];
        private readonly List<DateTimePoint> _Values_NotResponding = [];
        private readonly List<DateTimePoint> _Values_Affinity = [];

        private readonly DateTimeAxis _customAxis;

        public object SyncRoot { get; } = new object();

        public Axis[] XAxes { get; set; }


        public string CpuCaption { get; }

        private bool _IsInit = true;
        public MainWindow()
        {
            InitializeComponent();
            CPUs = Environment.ProcessorCount;
            this.Title = this.Title + $" Version: {Version} Processors: {CPUs}";


            AppConfig = AppConfig.LoadAppConfig(Version);

            CpuCaption = "";
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                var cpunames = new List<string>();
                foreach (ManagementObject obj in searcher.Get())
                {
                    cpunames.Add($"{obj["Name"]}");
                }
                CpuCaption = cpunames.ToSingleString();
            }
            catch { }



            AppConfig.ThrottleConfig.PropertyChanged += Config_PropertyChanged;
            PoeThreadSharedContext.Instance.Config = (Config)AppConfig.ThrottleConfig.Clone();

            _PoeThreadMain = new PoeThreadMain();
            _PoeThreadMain.GuiUpdate += PoeThreadMain_GuiUpdate;

            Series = [
            new LineSeries<DateTimePoint>
            {
                Values = _Values_Disk,
                Fill = null,
                Name="POE2-Disk (%)",
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometryFill = null,
                GeometryStroke = null
            },
            new LineSeries<DateTimePoint>
            {
                Values = _Values_Cpu,
                Fill = null,
                Name = "CPU (%)",
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3},
                GeometryFill = null,
                GeometryStroke = null
            },
            new LineSeries<DateTimePoint>
            {
                Values = _Values_IORead,
                Fill = null,
                Name = "I/O (MB/s)",
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                GeometryFill = null,
                GeometryStroke = null
            },
                        new LineSeries<DateTimePoint>
            {
                Values = _Values_Limited,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },

                Name="Limited (1/0)",
                GeometryFill = null,
                GeometryStroke = null
            },
                        new LineSeries<DateTimePoint>
            {
                Values = _Values_Affinity,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Yellow) { StrokeThickness = 3 },

                Name="Affinity (%)",
                GeometryFill = null,
                GeometryStroke = null
            },

            //            new LineSeries<DateTimePoint>
            //{
            //    Values = _Values_NotResponding,
            //    Fill = null,
            //    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 },

            //    Name="Not-Responding",
            //    GeometryFill = null,
            //    GeometryStroke = null
            //},



            ];

            _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            {
                CustomSeparators = GetSeparators(),
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
            };


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


        public SolidColorPaint LegendTextPaint { get; set; } =
                new SolidColorPaint
                {
                    Color = SKColors.White,
                    SKTypeface = SKTypeface.FromFamilyName("Courier New")
                };

        public SolidColorPaint LedgendBackgroundPaint { get; set; } =
            new SolidColorPaint(SKColors.Gray);

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
                        if (iStart >= 0 && iEnd > iStart)
                        {
                            var onlineversion = content.Substring(iStart2, iEnd - iStart2);
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



        private void PoeThreadMain_GuiUpdate(object? sender, State e)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(() => PoeThreadMain_GuiUpdate(sender, e));
                return;
            }
            State = e;
            OnPropertyChanged(nameof(VisWaitingExe));
            OnPropertyChanged(nameof(VisNormal));
            OnPropertyChanged(nameof(VisNotResponding));

            OnPropertyChanged(nameof(State));

            if (State.ExceptionsCaption != LastExceptionsCaptions)
            {
                bool raiseChanged = false;
                var cuurentEX = State.ThreadStates.Where(x => x.Exception != null).Select(x => x.Exception!).ToArray();
                LastExceptions = cuurentEX;
                LastExceptionsCaptions = State.ExceptionsCaption;
                if (raiseChanged)
                {
                    OnPropertyChanged(nameof(LastExceptionsCaptions));
                    OnPropertyChanged(nameof(VisError));
                }
            }
            else
            {
                LastExceptions = null;
                LastExceptionsCaptions = "";
            }


            lock (SyncRoot)
            {
                var cur = DateTime.Now;


                if (State.AllMeasures.Count > 0 && IsUpdateGraphs)
                {
                    var values_disk = State.GetMeasureValues(PoeThreadPFC.Counter_DiskUsage);
                    if (values_disk.Count > 0)
                        _Values_Disk.AddRange(values_disk.Select(x => new DateTimePoint(x.DT, x.Value)));
                    _Values_Disk.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                    var values_IO = State.GetMeasureValues(PoeThreadPFC.Counter_IORead);
                    if (values_IO.Count > 0)
                        _Values_IORead.AddRange(values_IO.Select(x => new DateTimePoint(x.DT, x.Value)));
                    _Values_IORead.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);




                    //smmothen cpu...
                    var medianCPU = new DateTimePoint[0];
                    {
                        var values_cpu = State.GetMeasureValues(PoeThreadPFC.Counter_CpuUsage);
                        if (values_cpu.Count > 0)
                        {
                            var values = values_cpu.Select(x => new DateTimePoint(x.DT, x.Value)).ToArray();
                            double[] values_d = values.Select(x => x.Value!.Value).ToArray();
                            DateTime dtmin = values.Min(x => x.DateTime);
                            DateTime dtmax = values.Max(x => x.DateTime);
                            TimeSpan diff = dtmax - dtmin;
                            var median = values_d.Median();

                            medianCPU = new[] { /*new DateTimePoint(dtmin, median),*/ new DateTimePoint(dtmax, median) };
                        }


                    }
                    _Values_Cpu.AddRange(medianCPU);
                    _Values_Cpu.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);



                    var values_AF = State.GetMeasureValues(PoeThreadAffinity.Counter_AffinityPercent);
                    if (values_AF.Count > 0)
                        _Values_Affinity.AddRange(values_AF.Select(x => new DateTimePoint(x.DT, x.Value)));
                    _Values_Affinity.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                    var values_LIM = State.GetMeasureValues(PoeThreadMain.Counter_Limited);
                    if (values_LIM.Count > 0)
                        _Values_Limited.AddRange(values_LIM.Select(x => new DateTimePoint(x.DT, x.Value > 0 ? 100 : 0)));
                    _Values_Limited.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);




                    var values_NR = State.GetMeasureValues(PoeThreadRecovery.Counter_NotResponding);
                    if (values_NR.Count > 0)
                        _Values_NotResponding.AddRange(values_NR.Select(x => new DateTimePoint(x.DT, x.Value > 0 ? 100 : 0)));
                    _Values_NotResponding.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);
                    if (_Values_NotResponding.Count > 0)
                    {
                        var lastNotResponding = _Values_NotResponding.Last();

                        var oldVisValue = VisNotResponding;
                        VisNotResponding = lastNotResponding.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
                        if (oldVisValue != VisNotResponding)
                            OnPropertyChanged(nameof(VisNotResponding));
                    }

                } //measures..


                // we need to update the separators every time we add a new point 
                _customAxis.CustomSeparators = GetSeparators();

            }


        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_IsInit)
                return;
            Config newConfig = (Config)AppConfig.ThrottleConfig.Clone();
            newConfig.InLimitAffinity = AppConfig.InLimitAffinity.Select(x => x.IsSet).ToArray();

            PoeThreadSharedContext.Instance.Config = newConfig;

            OnPropertyChanged(nameof(IsAlwaysOff));
            OnPropertyChanged(nameof(IsAlwaysOn));
            OnPropertyChanged(nameof(IsViaPoe2LogFile));

            AppConfig.SaveAppConfig(AppConfig);
        }



        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _PoeThreadMain?.Stop();
            Debugging.Step();
        }







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
            _PoeThreadMain.Start();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void btShowFullError_Click(object sender, RoutedEventArgs e)
        {
            if (LastExceptions == null)
                return;
            Exception[] exA = LastExceptions;

            string additionalinfos = "";
            try
            {
                var lines = new List<string>();

                lines.Add($"POE2loadingPainFix(Version) = {Version}");
                lines.Add($"CPU = {CpuCaption}");
                lines.Add($"LimitKind = {this.AppConfig.ThrottleConfig.LimitKind}");
                lines.Add($"InLimitAffinity = {this.AppConfig.ThrottleConfig.InLimitAffinity.Select(x => x.ToString()).ToSingleString()}");
                lines.Add($"LimitToNormalDelaySecs = {this.AppConfig.ThrottleConfig.LimitToNormalDelaySecs}");
                additionalinfos = lines.ToSingleString(Environment.NewLine);
            }
            catch { }


            var w = new ExceptionWindow(exA, additionalinfos);
            w.ShowDialog();
        }
    }
}