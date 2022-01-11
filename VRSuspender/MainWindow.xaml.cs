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
using AdonisUI;
using AdonisUI.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace VRSuspender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        private readonly MainFormViewModel _mfvm;
        private bool _closingFromMenu;
        public MainWindow()
        {
            InitializeComponent();
            _mfvm = DataContext as MainFormViewModel;
            _mfvm.SetCollectionViewItemSource(lvTrackedProcess.ItemsSource);
            _closingFromMenu = false;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if(Properties.Settings.Default.StartMonitorOnStartup)
                _mfvm.StartMonitoring();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(Properties.Settings.Default.CloseToTray && !_closingFromMenu)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            if(_mfvm.VrRunning)
            {
                if (MessageBox.Show("VR is currently running. Are you sure you want to close VRSuspender ? (Process will be restored to their initial state)", "Warning", AdonisUI.Controls.MessageBoxButton.YesNo, AdonisUI.Controls.MessageBoxImage.Warning) == AdonisUI.Controls.MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    _mfvm.ApplyStopVRActionToProcess();
                }
            }
            
            if(_mfvm.IsMonitoring)
                _mfvm.StopMonitoring();
                                    
        }

        public void MnuNIQuit_Click(object sender, RoutedEventArgs e)
        {
            _closingFromMenu = true;
            Close();
        }

        private void AdonisWindow_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized)
            {
                if(Properties.Settings.Default.MinimizeToTray)
                {
                    this.Hide();
                }
            }
        }
    }
}
