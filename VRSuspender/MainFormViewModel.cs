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
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VRSuspender.EditProcessForm;
using Newtonsoft.Json;
using System.Windows.Data;
using Microsoft.Win32;

namespace VRSuspender
{
    public class MainFormViewModel : ValidatableBindableBase
    {
        private ObservableCollection<string> _log = new();
        private readonly ManagementEventWatcher startWatch = new(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private readonly ManagementEventWatcher stopWatch = new(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private readonly ObservableCollection<string> _listWatchedProcess = new();
        private ObservableCollection<TrackedProcess> _listTrackedProcess;
        private TrackedProcess _selectedTrackedProcess;
        private bool _isMonitoring;
        private uint _selectedFilterIndex = 0;
        private bool _startWithWindows;
        private bool _startMonitoringOnStartup;
        private uint _startState;
        private CollectionView _view;
        private bool _vrRunning = false;

        public MainFormViewModel()
        {
            StartMonitoringOnStartup = Properties.Settings.Default.StartMonitorOnStartup;
            StartState = Properties.Settings.Default.StartState;
            StartWithWindows = Properties.Settings.Default.StartWithWindows;

            _listWatchedProcess.Add("vrserver.exe");
            _listTrackedProcess = new ObservableCollection<TrackedProcess>();
            LoadTrackedProcessProfiles();
            IsMonitoring = false;
            RefreshProcess();


        }
       
        public void SetCollectionViewItemSource(System.Collections.IEnumerable source)
        {
            _view = (CollectionView)CollectionViewSource.GetDefaultView(source);
            _view.Filter = FilterMainViewProcessState;

        }

        public void StartMonitoring()
        {
            startWatch.EventArrived += new EventArrivedEventHandler(StartWatch_EventArrived);
            startWatch.Start();

            stopWatch.EventArrived += new EventArrivedEventHandler(StopWatch_EventArrived);
            stopWatch.Start();

            IsMonitoring = true;
            WriteToLog("Waiting for SteamVR to start...");
        }

        public void StopMonitoring()
        {
            stopWatch.Stop();
            startWatch.Stop();

            IsMonitoring = false;
            WriteToLog("Stopped monitoring of SteamVR.");
        }



        #region EVENTS

        void StartWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");
            ApplyStartVRActionToProcess();
        }

        public void ApplyStartVRActionToProcess()
        {
            if (VrRunning) return;
            RefreshProcess();
            foreach (TrackedProcess s in _listTrackedProcess)
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

            VrRunning = true;
        }

        void StopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Stopped.");
            ApplyStopVRActionToProcess();

        }
        public void ApplyStopVRActionToProcess()
        {
            if(!VrRunning) return;
            RefreshProcess();
            foreach (TrackedProcess s in _listTrackedProcess)
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
            VrRunning = false;
        }
        #endregion

        private void ResumeProcess(TrackedProcess process)
        {
            
            Process[] p = Process.GetProcessesByName(process.Name);
            // DO NOT RESUME A PROCESS THAT IS RUNNING, STOPPED OR NOT FOUND
            if(process.Status == ProcessState.Running || process.Status == ProcessState.Stopped || process.Status == ProcessState.NotFound)
            {
                WriteToLog($"Process {process.Name} is already {process.Status}. Cannot resume.");
                return;
            }
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
            // IF FOR SOME REASON THERE IS NO PROCESS PATH DON'T TRY TO RESTART THE PROCESS
            if (string.IsNullOrEmpty(process.Path))
            {
                WriteToLog($"Process {process.Name} has no path. Cannot restart.");
                return;
            }
            // DO NOT RESTART A PROCESS THAT IS ALREADY RUNNING OR IS SUSPENDED
            if (process.Status == ProcessState.Running || process.Status == ProcessState.Suspended)
            {
                WriteToLog($"Process {process.Name} is already {process.Status}. Will not restart the process.");
                return;
            }
            WriteToLog($"Starting {process.Name}");
            ProcessStartInfo info = new(process.Path)
            {
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(info);
        }

        private void LoadTrackedProcessProfiles()
        {

            string profilePath = AppDomain.CurrentDomain.BaseDirectory + "Profiles";
            if (Directory.Exists(profilePath))
            {
                string[] listProfiles = Directory.GetFiles(profilePath, "*.vrs");
                foreach(string profile in listProfiles)
                {
                    StreamReader sr = new(profile);
                    string json = sr.ReadToEnd();

                    ListTrackedProcess.Add(JsonConvert.DeserializeObject<TrackedProcess>(json));

                }
            }
        }

        private static void RefreshProcess(TrackedProcess process)
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
                    process.Icon = new BitmapImage(new Uri("/Resources/qm.png"));
                }

                if(p[0].Threads[0].ThreadState == ThreadState.Wait)
                {
                    if(p[0].Threads[0].WaitReason == ThreadWaitReason.Suspended)
                    {
                        process.Status = ProcessState.Suspended;
                    }
                    else
                    {
                        process.Status = ProcessState.Running;
                    }
                }
                else
                {
                    process.Status = ProcessState.Running;
                }
                process.Path = p[0].MainModule.FileName;
            }
            else
            {
                if(!string.IsNullOrEmpty(process.Path) && File.Exists(process.Path))
                {
                    process.Icon = Icon.ExtractAssociatedIcon(process.Path).ToImageSource();
                }
                else
                {
                    process.Icon = new BitmapImage(new Uri("/Resources/qm.png"));
                    process.Status = ProcessState.NotFound;

                }
            }
    
        }

        private void RefreshProcess()
        {
            foreach (TrackedProcess process in _listTrackedProcess)
            {
                RefreshProcess(process);
            }
        }



        private void KillProcess(TrackedProcess process, bool noprompt = true)
        {
            // SILENT KILL OR NOT WHEN USING COMMAND
            if(!noprompt)
                if (MessageBox.Show($"Are you sure you want to kill {process.Name} ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
            // DONT KILL IF STOPPED OR NOT FOUND
            if (process.Status == ProcessState.NotFound || process.Status == ProcessState.Stopped)
            {
                WriteToLog($"Process {process.Name} is already {process.Status}. Process will not be killed.");
                return;

            }
            Process[] p = Process.GetProcessesByName(process.Name);
            if (p.Length > 0)
            {
                foreach (Process p2 in p)
                {
                    WriteToLog($"Terminating {p2.ProcessName}");
                    p2.Kill();
                    p2.Close();
                }
            }
        }

        private void SuspendProcess(TrackedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.Name);
            // DONT SUSPEND PROCESS IF ALREADY SUSPENDED STOPPED OR NOT FOUND
            if (process.Status == ProcessState.Suspended || process.Status == ProcessState.Stopped || process.Status == ProcessState.NotFound)
            {
                WriteToLog($"Process {process.Name} is {process.Status}. Process will not be suspended.");
                return;
            }
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

        public ICommand EditCommand => new RelayCommand(param => EditProcess(), param => CanEditSuspendedProcess());
        public ICommand DeleteProcessCommand => new RelayCommand(param => DeleteSuspendedProcess(SelectedTrackedProcess), param => CanDeleteSuspendedProcess());
        public ICommand StartMonitoringCommand => new RelayCommand(param => StartMonitoring(), param => CanStartMonitor());
        public ICommand StopMonitoringCommand => new RelayCommand(param => StopMonitoring(), param => CanStopMonitor());
        public ICommand ResumeProcessCommand => new RelayCommand(param => ResumeProcess(SelectedTrackedProcess), param => CanResumeProcess());
        public ICommand SuspendProcessCommand => new RelayCommand(param => SuspendProcess(SelectedTrackedProcess), param => CanSuspendedProcess());
        public ICommand KillProcessCommand => new RelayCommand(param => KillProcess(SelectedTrackedProcess, false), param => CanKillProcess());
        public ICommand RefreshProcessCommand => new RelayCommand(param => RefreshProcess(SelectedTrackedProcess), param => CanRefreshProcess());
        public ICommand RefreshAllProcessCommand => new RelayCommand(param => RefreshProcess());
        public ICommand EditProcessCommand => new RelayCommand(param => EditProcess(), param => CanEditProcess());
        public ICommand AddProcessCommand => new RelayCommand(param => AddProces());
        public ICommand SaveSettingsCommand => new RelayCommand(param => SaveSettings());
        public ICommand FilterMainViewCommand => new RelayCommand(param => RefreshFilter());
        public ICommand ShowAboutWindowCommand => new RelayCommand(param => ShowAboutWindow());

        private void ShowAboutWindow()
        {
            
        }

        private void RefreshFilter()
        {
            CollectionViewSource.GetDefaultView(_view).Refresh();
        }

        public static ICommand NotificationIconDoubleClickCommand => new RelayCommand(param => Application.Current.MainWindow.Show());

        private bool FilterMainViewProcessState(object process)
        {
            if (SelectedFilterIndex == 0)
                return true;
            else
            {
                ProcessState state = ProcessState.Running;
                switch (SelectedFilterIndex)
                {
                    case 1:
                        state = ProcessState.Suspended;
                        break;
                    case 2:
                        state = ProcessState.Running;
                        break;
                    case 3:
                        state = ProcessState.Stopped;
                        break;

                }
                return (process as TrackedProcess).Status == state;
            }
        }

        private void SaveSettings()
        {
           
            Properties.Settings.Default.StartState = StartState;
            Properties.Settings.Default.StartWithWindows = StartWithWindows;
            SetStartWithWindows(StartWithWindows);
            Properties.Settings.Default.StartMonitorOnStartup = StartMonitoringOnStartup;
            Properties.Settings.Default.Save();

        }

        private void SetStartWithWindows(bool start)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (start)
            {            
                if (rkApp.GetValue("VRSuspender") == null)
                {
                    WriteToLog("VRSuspender will start with windows.");
                    string exe = $"\"{Environment.ProcessPath}\"";
                    rkApp.SetValue("VRSuspender", exe);
                }
                else
                {
                    WriteToLog("VR Suspender is already set to start with Windows.");
                }

            }
            else
            {
                WriteToLog("VRSuspender has been removed from Windows startup applications.");
                rkApp.DeleteValue("VRSuspender", false);
            }

        }

        private void AddProces()
        {
            EditTrackedProcessForm ProcessEditor = new()
            {
                Owner = Application.Current.MainWindow
            };
            if (ProcessEditor.ShowDialog() == true)
            {
                TrackedProcess ntp = ProcessEditor.GetTrackedProcess();
                RefreshProcess(ntp);
                ListTrackedProcess.Add(ntp);
            }
        }

        private bool CanStartMonitor()
        {
            return IsMonitoring == false;
        }

        private bool CanStopMonitor()
        {
            return IsMonitoring == true;
        }
        private bool CanEditProcess()
        {
            return SelectedTrackedProcess != null; 
        }

        private void EditProcess()
        {
            EditTrackedProcessForm ProcessEditor = new(_selectedTrackedProcess)
            {
                Owner = Application.Current.MainWindow
            };
            if (ProcessEditor.ShowDialog() == true)
            {
                TrackedProcess process = ProcessEditor.GetTrackedProcess();
                SelectedTrackedProcess.Name = process.Name;
                SelectedTrackedProcess.Action = process.Action;
                SelectedTrackedProcess.Path = process.Path;
            }
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

        private void DeleteSuspendedProcess(TrackedProcess process)
        {
            // PREVENT USER FROM REMOVING A SUSPENDED PROCESS. THIS WOULD LEAVE THE PROCESS IN SUSPENDED WITHOUT A WAY TO RESUME IT.
            if (process.Status == ProcessState.Suspended) 
            {
                MessageBox.Show($"Process {process.Name} is suspended. Please resume the process before removing it.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WriteToLog($"Process {process.Name} is suspended. Please resume the process before removing it.");
                return;
            };
            ListTrackedProcess.Remove(SelectedTrackedProcess);
        }

        private bool CanEditSuspendedProcess()
        {
            return SelectedTrackedProcess != null;
        }

        #endregion

        private void WriteToLog(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Log.Insert(0,$"[{DateTime.Now}] - {message}.")));
        }

        #region PROPERTIES

        public ObservableCollection<string> Log { get => _log; private set => SetProperty(ref _log, value); }
        public ObservableCollection<TrackedProcess> ListTrackedProcess { get => _listTrackedProcess; set => SetProperty(ref _listTrackedProcess,value); }
        public int SuspendedProcessCount { get => _listTrackedProcess.Count; }
        public TrackedProcess SelectedTrackedProcess { get => _selectedTrackedProcess; set => SetProperty(ref _selectedTrackedProcess,value);  }
        public bool StartWithWindows { get => _startWithWindows; set => SetProperty(ref _startWithWindows,value); }
        public bool StartMonitoringOnStartup { get => _startMonitoringOnStartup; set => SetProperty(ref _startMonitoringOnStartup,value); }
        public uint SelectedFilterIndex { get => _selectedFilterIndex; set => SetProperty(ref _selectedFilterIndex,value); }
        public bool IsMonitoring { get => _isMonitoring; set => SetProperty(ref _isMonitoring,value); }
        public uint StartState { get => _startState; set => SetProperty(ref _startState,value); }
        public bool VrRunning { get => _vrRunning; private set => SetProperty(ref _vrRunning,value); }
        #endregion
    }


}
