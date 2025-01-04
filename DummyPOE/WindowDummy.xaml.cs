using System.ComponentModel;
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

namespace DummyPOE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WindowDummy : Window,INotifyPropertyChanged
    {
        string PathLog;
        string sResetLimit = "[SHADER] Delay: ON";
        string sSetLimit1 = "Got Instance Details from login server";
        string sSetLimit2 = "[SHADER] Delay: OFF";

        public string LevelState { get; set; } = "";

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

            btLevelDone_Click(this, null);
            this.DataContext = this;
        }



        private void btLevelStart_Click(object sender, RoutedEventArgs? e)
        {
            System.IO.File.AppendAllLines(PathLog,[$"{sSetLimit1}"]);
            LevelState = "Loading Level...";
        }

        private void btLevelDone_Click(object sender, RoutedEventArgs? e)
        {
            System.IO.File.AppendAllLines(PathLog, [$"{sResetLimit}"]);
            LevelState = "Loading Level...DONE";

        }
    }
}