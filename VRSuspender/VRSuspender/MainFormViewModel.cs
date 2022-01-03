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
        private bool _isMonitoring;
        public MainFormViewModel()
        {
            _watchedProcess.Add("vrserver.exe");
            _suspendedProcess = new ObservableCollection<SuspendedProcess>();
            _suspendedProcess.Add(new SuspendedProcess("iCUE"));
            _suspendedProcess.Add(new SuspendedProcess("EK-Connect"));
            _suspendedProcess.Add(new SuspendedProcess("SamsungMagician", ProcessState.Unknown, "", ProcessAction.Kill));
            _suspendedProcess.Add(new SuspendedProcess("msedge"));
            _isMonitoring = false;

            foreach(SuspendedProcess sp in _suspendedProcess)
            {
                Process[] p = Process.GetProcessesByName(sp.Name);
                if(p.Length > 0)
                {

                    sp.Status = p[0].Threads[0].WaitReason == ThreadWaitReason.Suspended ? ProcessState.Suspended : ProcessState.Running;
                    sp.Path = p[0].MainModule.FileName;
                }
                else
                {
                    sp.Status = ProcessState.NotFound;
                }
            }
        }

        public void StartMonitoring()
        {
            startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
            startWatch.Start();

            stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
            stopWatch.Start();

            _isMonitoring = true;

            WriteToLog("Waiting for SteamVR to start...");
        }

        public void StopMonitoring()
        {
            stopWatch.Stop();
            startWatch.Stop();

            _isMonitoring = false;
            WriteToLog("Stopping monitoring of SteamVR.");
        }

        #region EVENTS
        void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Stopped.");            

            foreach (SuspendedProcess s in _suspendedProcess)
            {
                switch (s.Action)
                {
                    case ProcessAction.Suspend:
                        ResumeProcess(s);
                        break;
                    case ProcessAction.Kill:
                        RestartProcess(s);
                        break;
                    case ProcessAction.KeepRunning:
                        s.Status = ProcessState.Running;
                        break;
                    default:
                        break;

                }
                                            
            }

        }

        private void ResumeProcess(SuspendedProcess process)
        {
            
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                foreach (Process p2 in p)
                {
                    WriteToLog($"Resuming {p2.ProcessName}");
                    p2.Resume();
                    process.Status = ProcessState.Running;
                }
            }
        }

        private void RestartProcess(SuspendedProcess process)
        {
            if (string.IsNullOrEmpty(process.Path)) return;
            WriteToLog($"Starting {process.Name}");
            ProcessStartInfo info = new ProcessStartInfo(process.Path);
            info.WindowStyle = ProcessWindowStyle.Minimized;
            Process newProcess = Process.Start(info);
        }

        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");            
   
            foreach(SuspendedProcess s in _suspendedProcess)
            {    
                switch (s.Action)
                {
                    case ProcessAction.Suspend:
                        SuspendProcess(s);
                        break;
                    case ProcessAction.Close:
                        CloseProcess(s);
                        break;
                    case ProcessAction.Kill:
                        KillProcess(s);
                        break;
                    case ProcessAction.KeepRunning:
                        s.Status = ProcessState.Running;
                        break;
                    default:
                        break;
                }

            }
                     
        }
        private void KillProcess(SuspendedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                foreach (Process p2 in p)
                {
                    WriteToLog($"Terminating {p2.ProcessName}");
                    p2.Kill(true);
                    p2.Close();
                }
            }
        }

        private void SuspendProcess(SuspendedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                foreach (Process p2 in p)
                {
                    if (p2.HasExited) continue;
                    WriteToLog($"Suspending {p2.ProcessName}");
                    p2.Suspend();
                    process.Status = ProcessState.Suspended;
                }

            }

  
        }

        private void CloseProcess(SuspendedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                foreach (Process p2 in p)
                {
                    WriteToLog($"Closing {p2.ProcessName}");
                    p2.CloseMainWindow();
                    p2.Close();
                    process.Status = ProcessState.Stopped;
                }
            }
        }

        #region COMMANDS

        public ICommand EditCommand => new AsyncRelayCommand(param => EditSuspendedProcess(), param => CanEditSuspendedProcess());
        public ICommand DeleteCommand => new AsyncRelayCommand(param => DeleteSuspendedProcess(), param => CanDeleteSuspendedProcess());
        public ICommand StartMonitoringCommand => new RelayCommand(param => StartMonitoring(), param => _isMonitoring == false);
        public ICommand StopMonitoringCommand => new RelayCommand(param => StopMonitoring(), param => _isMonitoring == true);
        public ICommand ResumeProcessCommand => new RelayCommand(param => ResumeProcess(SelectedSuspendedProcess), param => CanResumeProcess());
        public ICommand SuspendProcessCommand => new RelayCommand(param => SuspendProcess(SelectedSuspendedProcess), param => CanSuspendedProcess());

        private bool CanSuspendedProcess()
        {
            return _selectedSuspendedProcess != null && _selectedSuspendedProcess.Status == ProcessState.Running;
        }

        private bool CanResumeProcess()
        {
            return _selectedSuspendedProcess != null && _selectedSuspendedProcess.Status == ProcessState.Suspended;
        }

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
