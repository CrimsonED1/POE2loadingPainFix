using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PropertyChanged;

namespace POE2loadingPainFix
{
    /// <summary>
    /// Interaktionslogik für ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window,INotifyPropertyChanged
    {
        public ExceptionWindow(Exception exception, string additionals="")
        {
            InitializeComponent();
            
            var lines = new List<string>();
            lines.Add($"CPU: {Environment.ProcessorCount}");
            lines.Add($"Version: {Environment.Version}");
            lines.Add($"IsPrivilegedProcess: {Environment.IsPrivilegedProcess}");
            lines.Add($"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
            lines.Add($"Is64BitProcess: {Environment.Is64BitProcess}");
            lines.Add($"OSVersion: {Environment.OSVersion}");
            lines.Add($"---------------------");
            lines.Add($"{additionals}");
            lines.Add($"---------------------");
            lines.Add($"{exception}");
            lines.Add($"---------------------");

            var devPath = @"C:\Users\Karsten\OneDrive\Dokumente\Development\";
            var txt = lines.ToSingleString(Environment.NewLine);
            var txtclean = txt.Replace(devPath, @".\");

            txtMain.Text = txtclean;
        }

        private void btClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtMain.Text);
        }
    }
}
