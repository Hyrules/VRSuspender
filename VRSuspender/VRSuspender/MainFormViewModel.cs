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
using System.Windows.Input;
using System.Threading.Tasks;

namespace VRSuspender
{
    public class MainFormViewModel : ValidatableBindableBase
    {
        private ObservableCollection<string> _log = new ObservableCollection<string>();
        private ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ManagementEventWatcher stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ObservableCollection<string> _watchedProcess = new ObservableCollection<string>();
        private ObservableCollection<SuspendedProcess> _suspendedProcess;
        private SuspendedProcess _selectedSuspendedProcess;

        public MainFormViewModel()
        {
            _watchedProcess.Add("vrserver.exe");
            _suspendedProcess = new ObservableCollection<SuspendedProcess>();
            _suspendedProcess.Add(new SuspendedProcess("iCUE"));
            _suspendedProcess.Add(new SuspendedProcess("EK-Connect"));
            _suspendedProcess.Add(new SuspendedProcess("SamsungMagician"));
            _suspendedProcess.Add(new SuspendedProcess("msedge"));
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

            foreach (SuspendedProcess s in _suspendedProcess)
            {
                Process[] p = Process.GetProcessesByName(s.Name);
                if (p.Length > 0)
                {
                    foreach(Process p2 in p)
                    {
                        p2.Resume();
                        s.Status = ProcessState.Running;

                        WriteToLog($"Resuming {p[0].ProcessName}");
                    }
                }
                else
                {
                    s.Status = ProcessState.NotFound;
                }
            }

        }

        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");            
   
            foreach(SuspendedProcess s in _suspendedProcess)
            {
                Process[] p = Process.GetProcessesByName(s.Name);
                if(p.Length > 0)
                {
                    foreach (Process p2 in p)
                    {
                        p2.Suspend();
                        s.Status = ProcessState.Suspended;
                        s.Path = p2.MainModule.FileName;
                        WriteToLog($"Suspending {p2.ProcessName}");
                    }

                }
                else
                {
                    s.Status = ProcessState.NotFound;
                }
            }
            
            
        }

        #region COMMANDS

        public ICommand EditCommand => new AsyncRelayCommand(param => EditSuspendedProcess(), param => CanEditSuspendedProcess());
        public ICommand DelteCommand => new AsyncRelayCommand(param => DeleteSuspendedProcess(), param => CanDeleteSuspendedProcess());

        private bool CanDeleteSuspendedProcess()
        {
            return SelectedSuspendedProcess != null;
        }

        private Task DeleteSuspendedProcess()
        {
            return null;
        }

        private bool CanEditSuspendedProcess()
        {
            return SelectedSuspendedProcess != null;
        }

        private Task EditSuspendedProcess()
        {
            return null;
        }

        #endregion

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
        public ObservableCollection<SuspendedProcess> SuspendedProcess 
        { 
            get => _suspendedProcess; 
            set => SetProperty(ref _suspendedProcess,value); 
        }

        public int SuspendedProcessCount
        {
            get => _suspendedProcess.Count;
        }
        public SuspendedProcess SelectedSuspendedProcess 
        { 
            get => _selectedSuspendedProcess; 
            set => SetProperty(ref _selectedSuspendedProcess,value); 
        }
        #endregion
    }


}
