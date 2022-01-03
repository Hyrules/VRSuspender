using System;
using System.Collections.Generic;
using System.Text;
using VRSuspender.Utils;
using System.Management;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using System.Linq;
using System.IO;
using VRSuspender.Extensions;

namespace VRSuspender
{
    public class MainFormViewModel : ValidatableBindableBase
    {
        private ObservableCollection<string> _log = new ObservableCollection<string>();
        ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'vrmonitor.exe'"));
        ManagementEventWatcher stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ObservableCollection<string> _watchedProcess = new ObservableCollection<string>();
        private ObservableCollection<string> _suspendedProcess = new ObservableCollection<string>();

        public MainFormViewModel()
        {
            _watchedProcess.Add("vrserver.exe");
            _suspendedProcess.Add("iCUE");
            _suspendedProcess.Add("EK-Connect");
            _suspendedProcess.Add("SamsungMagician");
            _suspendedProcess.Add("msedge");
        }

        public void StartMonitoring()
        {
            startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
            startWatch.Start();

            stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
            stopWatch.Start();

            WriteToLog("Waiting for SteamVR to start...");
        }

        public void StopMonitoring()
        {
            stopWatch.Stop();
            startWatch.Stop();
        }

        #region EVENTS
        void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Stopped.");            

            foreach (string s in _suspendedProcess)
            {
                Process[] p = Process.GetProcessesByName(s);
                if (p.Length > 0)
                {
                    foreach(Process p2 in p)
                    {
                        p2.Resume();
                        WriteToLog($"Resuming {p[0].ProcessName}");
                    }
                }
            }

        }

        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");            
   
            foreach(string s in _suspendedProcess)
            {
                Process[] p = Process.GetProcessesByName(s);
                if(p.Length > 0)
                {
                    foreach (Process p2 in p)
                    {
                        p2.Suspend();
                        WriteToLog($"Suspending {p2.ProcessName}");
                    }

                }
            }
            
            
        }

        private void WriteToLog(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Log.Add($"[{DateTime.Now}] - {message}.")));
        }

        #endregion

        #region PROPERTIES

        public ObservableCollection<string> Log 
        { 
            get => _log;  
            private set => SetProperty(ref _log, value); 
        }

        public ObservableCollection<string> WatchedProcess 
        { 
            get => _watchedProcess; 
            set => SetProperty(ref _watchedProcess,value); 
        }
        public ObservableCollection<string> SupendedProcess 
        { 
            get => _suspendedProcess; 
            set => SetProperty(ref _suspendedProcess,value); 
        }

        #endregion
    }


}
