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

namespace VRSuspender
{
    public class MainFormViewModel : ValidatableBindableBase
    {
        private ObservableCollection<string> _log = new ObservableCollection<string>();
        private ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ManagementEventWatcher stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'vrmonitor.exe'"));
        private ObservableCollection<string> _listWatchedProcess = new ObservableCollection<string>();
        private ObservableCollection<TrackedProcess> _listTrackedProcess;
        private TrackedProcess _selectedTrackedProcess;
        private bool _isMonitoring;
        private uint _selectedFilterIndex = 0;
        private bool _startWithWindows;
        private bool _startMonitoringOnStartup;
        private uint _startState;
        private CollectionView _view;

        public MainFormViewModel()
        {
            StartMonitoringOnStartup = Properties.Settings.Default.StartMonitorOnStartup;
            StartState = Properties.Settings.Default.StartState;
            

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
            startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
            startWatch.Start();

            stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
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
        void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Stopped.");            

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

        private void LoadTrackedProcessProfiles()
        {

            string profilePath = AppDomain.CurrentDomain.BaseDirectory + "Profiles";
            if (Directory.Exists(profilePath))
            {
                string[] listProfiles = Directory.GetFiles(profilePath, "*.vrs");
                foreach(string profile in listProfiles)
                {
                    StreamReader sr = new StreamReader(profile);
                    string json = sr.ReadToEnd();

                    ListTrackedProcess.Add(JsonConvert.DeserializeObject<TrackedProcess>(json));

                }
            }
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
                if(!string.IsNullOrEmpty(process.Path) && File.Exists(process.Path))
                {
                    process.Icon = Icon.ExtractAssociatedIcon(process.Path).ToImageSource();
                }
                else
                {
                    process.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/qm.png"));
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


        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            WriteToLog(e.NewEvent.Properties["ProcessName"].Value + " Started.");            
   
            foreach(TrackedProcess s in _listTrackedProcess)
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

        public ICommand EditCommand => new RelayCommand(param => EditProcess(), param => CanEditSuspendedProcess());
        public ICommand DeleteCommand => new RelayCommand(param => DeleteSuspendedProcess(), param => CanDeleteSuspendedProcess());
        public ICommand StartMonitoringCommand => new RelayCommand(param => StartMonitoring(), param => CanStartMonitor());
        public ICommand StopMonitoringCommand => new RelayCommand(param => StopMonitoring(), param => CanStopMonitor());
        public ICommand ResumeProcessCommand => new RelayCommand(param => ResumeProcess(SelectedTrackedProcess), param => CanResumeProcess());
        public ICommand SuspendProcessCommand => new RelayCommand(param => SuspendProcess(SelectedTrackedProcess), param => CanSuspendedProcess());
        public ICommand KillProcessCommand => new RelayCommand(param => KillProcess(SelectedTrackedProcess), param => CanKillProcess());
        public ICommand RefreshProcessCommand => new RelayCommand(param => RefreshProcess(SelectedTrackedProcess), param => CanRefreshProcess());
        public ICommand RefreshAllProcessCommand => new RelayCommand(param => RefreshProcess());
        public ICommand EditProcessCommand => new RelayCommand(param => EditProcess(), param => CanEditProcess());
        public ICommand AddProcessCommand => new RelayCommand(param => AddProces());
        public ICommand SaveSettingsCommand => new RelayCommand(param => SaveSettings());
        public ICommand FilterMainViewCommand => new RelayCommand(param => RefreshFilter());

        private void RefreshFilter()
        {
            CollectionViewSource.GetDefaultView(_view).Refresh();
        }

        public ICommand NotificationIconDoubleClickCommand => new RelayCommand(param => Application.Current.MainWindow.Show());

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
            Properties.Settings.Default.StartMonitorOnStartup = StartMonitoringOnStartup;
            Properties.Settings.Default.Save();

        }

        private void AddProces()
        {
            EditTrackedProcessForm ProcessEditor = new EditTrackedProcessForm();
            ProcessEditor.Owner = Application.Current.MainWindow;
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
            EditTrackedProcessForm ProcessEditor = new EditTrackedProcessForm(_selectedTrackedProcess);
            ProcessEditor.Owner = Application.Current.MainWindow;
            if(ProcessEditor.ShowDialog() == true)
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

        private void DeleteSuspendedProcess()
        {
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

        #endregion

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
        #endregion
    }


}
