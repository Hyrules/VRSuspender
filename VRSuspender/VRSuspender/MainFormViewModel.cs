﻿using System;
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
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VRSuspender.EditProcessForm;

namespace VRSuspender
{
    public class MainFormViewModel : ValidatableBindableBase
    {
        private ObservableCollection<string> _log = new ObservableCollection<string>();
        private ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ManagementEventWatcher stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ObservableCollection<string> _watchedProcess = new ObservableCollection<string>();
        private ObservableCollection<TrackedProcess> _trackedProcess;
        private TrackedProcess _selectedTrackedProcess;
        private bool _isMonitoring;
        public MainFormViewModel()
        {
            _watchedProcess.Add("vrserver.exe");
            _trackedProcess = new ObservableCollection<TrackedProcess>
            {
                new TrackedProcess("iCUE"),
                new TrackedProcess("EK-Connect"),
                new TrackedProcess("SamsungMagician"),
                new TrackedProcess("msedge")
            };
            _isMonitoring = false;
            RefreshProcess();
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
            WriteToLog("Stopped monitoring of SteamVR.");
        }

        #region EVENTS
        void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Stopped.");            

            foreach (TrackedProcess s in _trackedProcess)
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

        private void ResumeProcess(TrackedProcess process)
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

        private void RestartProcess(TrackedProcess process)
        {
            if (string.IsNullOrEmpty(process.Path)) return;
            WriteToLog($"Starting {process.Name}");
            ProcessStartInfo info = new ProcessStartInfo(process.Path);
            info.WindowStyle = ProcessWindowStyle.Minimized;
            Process newProcess = Process.Start(info);
        }

        private void RefreshProcess(TrackedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                try
                {
                    process.Icon = Icon.ExtractAssociatedIcon(p[0].MainModule.FileName).ToImageSource();
                }
                catch (Exception)
                {
                    process.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/qm.png"));
                }
                process.Status = p[0].Threads[0].WaitReason == ThreadWaitReason.Suspended ? ProcessState.Suspended : ProcessState.Running;
                process.Path = p[0].MainModule.FileName;
            }
            else
            {
                process.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/del.png"));
                process.Status = ProcessState.NotFound;
            }
        }

        private void RefreshProcess()
        {
            foreach (TrackedProcess process in _trackedProcess)
            {
                RefreshProcess(process);
            }
        }


        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");            
   
            foreach(TrackedProcess s in _trackedProcess)
            {    
                switch (s.Action)
                {
                    case ProcessAction.Suspend:
                        SuspendProcess(s);
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
        private void KillProcess(TrackedProcess process)
        {
            if (MessageBox.Show($"Are you sure you want to kill {process.Name} ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
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

        private void SuspendProcess(TrackedProcess process)
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

        #region COMMANDS

        public ICommand EditCommand => new AsyncRelayCommand(param => EditSuspendedProcess(), param => CanEditSuspendedProcess());
        public ICommand DeleteCommand => new AsyncRelayCommand(param => DeleteSuspendedProcess(), param => CanDeleteSuspendedProcess());
        public ICommand StartMonitoringCommand => new RelayCommand(param => StartMonitoring(), param => _isMonitoring == false);
        public ICommand StopMonitoringCommand => new RelayCommand(param => StopMonitoring(), param => _isMonitoring == true);
        public ICommand ResumeProcessCommand => new RelayCommand(param => ResumeProcess(SelectedTrackedProcess), param => CanResumeProcess());
        public ICommand SuspendProcessCommand => new RelayCommand(param => SuspendProcess(SelectedTrackedProcess), param => CanSuspendedProcess());
        public ICommand KillProcessCommand => new RelayCommand(param => KillProcess(SelectedTrackedProcess), param => CanKillProcess());
        public ICommand RefreshProcessCommand => new RelayCommand(param => RefreshProcess(SelectedTrackedProcess), param => CanRefreshProcess());
        public ICommand RefreshAllProcessCommand => new RelayCommand(param => RefreshProcess());
        public ICommand EditProcessCommand => new RelayCommand(param => EditProcess(), param => CanEditProcess());

        private bool CanEditProcess()
        {
            return SelectedTrackedProcess != null; 
        }

        private void EditProcess()
        {
            EditTrackedProcessForm ProcessEditor = new EditTrackedProcessForm();
            ProcessEditor.ShowDialog();
        }

        private bool CanRefreshProcess()
        {
            return _selectedTrackedProcess != null;
        }

        private bool CanKillProcess()
        {
            return _selectedTrackedProcess != null && (_selectedTrackedProcess.Status == ProcessState.Running || _selectedTrackedProcess.Status == ProcessState.Suspended);
        }

        private bool CanSuspendedProcess()
        {
            return _selectedTrackedProcess != null && _selectedTrackedProcess.Status == ProcessState.Running;
        }

        private bool CanResumeProcess()
        {
            return _selectedTrackedProcess != null && _selectedTrackedProcess.Status == ProcessState.Suspended;
        }

        private bool CanDeleteSuspendedProcess()
        {
            return SelectedTrackedProcess != null;
        }

        private Task DeleteSuspendedProcess()
        {
            return null;
        }

        private bool CanEditSuspendedProcess()
        {
            return SelectedTrackedProcess != null;
        }

        private Task EditSuspendedProcess()
        {
            return null;
        }

        #endregion

        private void WriteToLog(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Log.Insert(0,$"[{DateTime.Now}] - {message}.")));
        }

        #endregion

        #region PROPERTIES

        public ObservableCollection<string> Log 
        { 
            get => _log;  
            private set => SetProperty(ref _log, value); 
        }


        public ObservableCollection<TrackedProcess> TrackedProcess 
        { 
            get => _trackedProcess; 
            set => SetProperty(ref _trackedProcess,value); 
        }

        public int SuspendedProcessCount
        {
            get => _trackedProcess.Count;
        }
        public TrackedProcess SelectedTrackedProcess 
        { 
            get => _selectedTrackedProcess; 
            set => SetProperty(ref _selectedTrackedProcess,value); 
        }
        #endregion
    }


}
