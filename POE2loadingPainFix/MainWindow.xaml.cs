using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MahApps.Metro.Controls;
using POE2loadingPainFix.CpuThrottleDiskusage;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Navigation;
using System.Windows.Threading;


namespace POE2loadingPainFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public string Version = "0.7";

        /// <summary>
        /// https://stackoverflow.com/questions/54848286/performancecounter-physicaldisk-disk-time-wrong-value
        /// </summary>

        private Throttler _Throttler;

        public AppConfig AppConfig { get; private set; }

        public State? State { get; private set; }

        public string ShortException => LastException != null ? $"{LastException.GetType()}: {LastException.Message}" : "";
        public Exception? LastException { get; private set; } = null;

        public string PoeExes => Throttler.POE_ExeNames.ToSingleString("/");

        public Visibility VisWaitingExe => State == null || (State != null && State.TargetProcess == null) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisLoadingLevel => State != null && State.TargetProcess != null && State.TargetProcess.IsCpuLimited ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisNormal => State != null && State.TargetProcess != null && !State.TargetProcess.IsCpuLimited ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisError => ShortException!="" ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisPfcError => State != null && State.PfcException!=null ? Visibility.Visible : Visibility.Collapsed;
        public string ShortPfcException => State != null && State.PfcException != null ? State.PfcException.Message : "";


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
        private readonly List<DateTimePoint> _DiskValues = [];
        private readonly List<DateTimePoint> _CpuValues = [];
        private readonly List<DateTimePoint> _IOReadValues = [];
        private readonly List<DateTimePoint> _LimitedValues = [];

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


            AppConfig = AppConfig.LoadAppConfig();

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

            _Throttler = new Throttler(AppConfig.ThrottleConfig);
            _Throttler.GuiUpdate += _Throttler_GuiUpdate;

            Series = [
            new LineSeries<DateTimePoint>
            {
                Values = _DiskValues,
                Fill = null,
                Name="POE2-Disk (%)",
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometryFill = null,
                GeometryStroke = null
            },
            new LineSeries<DateTimePoint>
            {
                Values = _CpuValues,
                Fill = null,
                Name = "CPU (%)",
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 1,Style=SKPaintStyle.StrokeAndFill },
                GeometryFill = null,
                GeometryStroke = null
            },
            new LineSeries<DateTimePoint>
            {
                Values = _IOReadValues,
                Fill = null,
                Name = "I/O(MB/s)",
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                GeometryFill = null,
                GeometryStroke = null
            },
                        new LineSeries<DateTimePoint>
            {
                Values = _LimitedValues,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },

                Name="Limited",
                GeometryFill = null,
                GeometryStroke = null
            },


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

            if(State.LastError != null)
            {
                bool raiseChanged = false;
                if (LastException == null)
                {
                    LastException = State.LastError;
                    raiseChanged = true;
                }
                else if (LastException.Message != State.LastError.Message)
                {
                    //update exception...
                    LastException = State.LastError;
                    raiseChanged = true;

                }

                if (raiseChanged)
                {
                    OnPropertyChanged(nameof(ShortException));
                    OnPropertyChanged(nameof(LastException));
                    OnPropertyChanged(nameof(VisError));
                }
            }
            else
            {
                LastException = null;
            }


            lock (SyncRoot)
            {
                var cur = DateTime.Now;
                

                if (State.PfcException==null && State.MeasureEntries.Length > 0 && IsUpdateGraphs)
                {

                    _DiskValues.AddRange(State.MeasureEntries.Select(x => new DateTimePoint(x.DT, x.DiskUsage)));
                    _DiskValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                    _IOReadValues.AddRange(State.MeasureEntries.Select(x => new DateTimePoint(x.DT, x.IORead)));
                    _IOReadValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);


                    //smmothen cpu...
                    IEnumerable<DateTimePoint> cpuvals;
                    {
                        var values = State.MeasureEntries.Select(x => new DateTimePoint(x.DT, x.CpuUsage)).ToArray();
                        double[] values_d = values.Select(x => x.Value!.Value).ToArray();
                        DateTime dtmin = values.Min(x => x.DateTime);
                        DateTime dtmax = values.Max(x => x.DateTime);
                        TimeSpan diff = dtmax - dtmin;
                        var median = values_d.Median();

                        cpuvals = new[] { /*new DateTimePoint(dtmin, median),*/ new DateTimePoint(dtmax, median) };
                    }
                    _CpuValues.AddRange(cpuvals);
                    _CpuValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);



                } //measures..
                double limitValueDisk = 0;
                if (State.TargetProcess != null && State.TargetProcess.IsCpuLimited)
                {
                    limitValueDisk = 100;
                }

                _LimitedValues.Add(new DateTimePoint(cur, limitValueDisk));
                _LimitedValues.RemoveAll(x => (cur - x.DateTime).TotalSeconds > 30);

                // we need to update the separators every time we add a new point 
                _customAxis.CustomSeparators = GetSeparators();

            }


        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Config newConfig = (Config)AppConfig.ThrottleConfig.Clone();
            newConfig.InLimitAffinity = AppConfig.InLimitAffinity.Select(x => x.IsSet).ToArray();

            _Throttler?.UpdateConfig(newConfig);
            OnPropertyChanged(nameof(IsAlwaysOff));
            OnPropertyChanged(nameof(IsAlwaysOn));
            OnPropertyChanged(nameof(IsViaPoe2LogFile));
        }



        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _Throttler?.Stop();
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
            _Throttler.Start();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void btShowFullError_Click(object sender, RoutedEventArgs e)
        {
            if (LastException == null)
                return;
            var ex = LastException;

            string additionalinfos = "";
            try 
            {
                var lines = new List<string>();
                
                lines.Add($"CPU = {CpuCaption}");
                lines.Add($"LimitKind = {this.AppConfig.ThrottleConfig.LimitKind}");
                lines.Add($"InLimitAffinity = {this.AppConfig.ThrottleConfig.InLimitAffinity.Select(x=>x.ToString()).ToSingleString()}");
                lines.Add($"LimitToNormalDelaySecs = {this.AppConfig.ThrottleConfig.LimitToNormalDelaySecs}");
                additionalinfos = lines.ToSingleString(Environment.NewLine);
            }
            catch { }


            var w = new ExceptionWindow(ex, additionalinfos);
            w.ShowDialog();
        }
    }
}