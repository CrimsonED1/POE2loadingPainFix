using System.ComponentModel;
using System.Windows;

namespace DummyPOE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WindowDummy : Window, INotifyPropertyChanged
    {
        string PathLog;
        string sResetLimit = "[SHADER] Delay: ON";
        string sSetLimit1 = "Got Instance Details from login server";
        string sSetLimit2 = "[SHADER] Delay: OFF";

        public string LevelState { get; set; } = "";
        public bool Terminate { get; private set; }

        private Thread Thread1;

        public WindowDummy()
        {
            InitializeComponent();

            var dir = System.IO.Path.GetDirectoryName(Environment.ProcessPath);

            PathLog = System.IO.Path.Combine(dir!, @"logs\client.txt");
            var dirLog = System.IO.Path.GetDirectoryName(PathLog);

            if (!System.IO.Directory.Exists(dirLog))
                System.IO.Directory.CreateDirectory(dirLog);
            if (!System.IO.File.Exists(PathLog))
                System.IO.File.WriteAllText(PathLog, "asdf");

            this.Closed += WindowDummy_Closed;

            //btLevelDone_Click(this, null);
            this.DataContext = this;

            Terminate = false;
            Thread1 = new Thread(new ParameterizedThreadStart(Thread_Execute));
            Thread1.IsBackground = true;
            Thread1.Start();
        }

        private void WindowDummy_Closed(object? sender, EventArgs e)
        {
            Terminate = true;
            Thread1.Join();
        }

        private void Thread_Execute(object? obj)
        {
            while (!Terminate)
            {
                for (int i = 0; i < 1000; i++)
                {
                    var d = Math.Pow(10, i);
                    var d2 = Math.PI * d;

                }
                //Thread.Sleep(10);
            }
        }

        private void btLevelStart_Click(object sender, RoutedEventArgs? e)
        {
            System.IO.File.AppendAllLines(PathLog, [$"{sSetLimit1}"]);
            LevelState = "Loading Level...";
        }

        private void btLevelDone_Click(object sender, RoutedEventArgs? e)
        {
            System.IO.File.AppendAllLines(PathLog, [$"{sResetLimit}"]);
            LevelState = "Loading Level...DONE";

        }
    }
}