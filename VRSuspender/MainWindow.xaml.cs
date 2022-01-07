using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using VRSuspender.Extensions;
using Hardcodet.Wpf;
using MahApps.Metro.Controls;

namespace VRSuspender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private MainFormViewModel _mfvm;
        public MainWindow()
        {
            InitializeComponent();
            _mfvm = DataContext as MainFormViewModel;
            _mfvm.SetCollectionViewItemSource(lvTrackedProcess.ItemsSource);

        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if(Properties.Settings.Default.StartMonitorOnStartup)
                _mfvm.StartMonitoring();
          
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_mfvm.IsMonitoring)
                _mfvm.StopMonitoring();
        }

 
        private void mnuNIQuit_Click(object sender, RoutedEventArgs e)
        {
            _mfvm.StopMonitoring();
            Close();
        }
    }
}
