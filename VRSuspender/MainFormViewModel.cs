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
using Windows.Storage;

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
        private bool _minimizeToTray;
        private bool _closeToTray;
        private CollectionView _view;
        private bool _vrRunning = false;

        public MainFormViewModel()
        {
            WriteToLog("Starting VRSuspender...");
            StartMonitoringOnStartup = Properties.Settings.Default.StartMonitorOnStartup;
            StartState = Properties.Settings.Default.StartState;
            StartWithWindows = Properties.Settings.Default.StartWithWindows;
            MinimizeToTray = Properties.Settings.Default.MinimizeToTray;
            CloseToTray = Properties.Settings.Default.CloseToTray;

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
            // CHECK IF STEAMVR IS RUNNING AND APPLY THE PROFILES
            Process[] p = Process.GetProcessesByName("vrserver");

            if (p.Length > 0)
            {
                WriteToLog($"Steam VR is already running. Applying user profiles.");
                ApplyStartVRActionToProcess();

            }
            else
            {
                WriteToLog("Waiting for SteamVR to start...");
            }

            startWatch.EventArrived += new EventArrivedEventHandler(StartWatch_EventArrived);
            startWatch.Start();

            stopWatch.EventArrived += new EventArrivedEventHandler(StopWatch_EventArrived);
            stopWatch.Start();

            IsMonitoring = true;
            
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
            
            Process[] p = Process.GetProcessesByName(process.ProcessName);
            // DO NOT RESUME A PROCESS THAT IS RUNNING, STOPPED OR NOT FOUND
            if(process.Status == ProcessState.Running || process.Status == ProcessState.Stopped || process.Status == ProcessState.NotFound)
            {
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) is already {process.Status}. Cannot resume.");
                return;
            }
            if (p.Length > 0)
            {
                int index = 1;
                foreach (Process p2 in p)
                {
                    WriteToLog($"Resuming {p2.ProcessName} [{index}/{p.Length}]");
                    p2.Resume();
                    process.Status = ProcessState.Running;
                    index++;
                }
            }
        }

        private void RestartProcess(TrackedProcess process)
        {
            // IF FOR SOME REASON THERE IS NO PROCESS PATH DON'T TRY TO RESTART THE PROCESS
            if (string.IsNullOrEmpty(process.Path))
            {
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) has no path. Cannot restart.");
                return;
            }
            // DO NOT RESTART A PROCESS THAT IS ALREADY RUNNING OR IS SUSPENDED
            if (process.Status == ProcessState.Running || process.Status == ProcessState.Suspended)
            {
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) is already {process.Status}. Will not restart the process.");
                return;
            }
            WriteToLog($"Starting {process.ProfileName } ({process.ProcessName})");
            ProcessStartInfo info = new(process.Path)
            {
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(info);
        }

        private void LoadTrackedProcessProfiles()
        {
            WriteToLog("Loading users profiles");
            ListTrackedProcess = JsonConvert.DeserializeObject<ObservableCollection<TrackedProcess>>(Properties.Settings.Default.UserProfiles);
            if(ListTrackedProcess == null)
            {
                ListTrackedProcess = new ObservableCollection<TrackedProcess>();
            }
        }

        private static void RefreshProcess(TrackedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.ProcessName);
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
                if (MessageBox.Show($"Are you sure you want to kill {process.ProfileName }({process.ProcessName}) ?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
            // DONT KILL IF STOPPED OR NOT FOUND
            if (process.Status == ProcessState.NotFound || process.Status == ProcessState.Stopped)
            {
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) is already {process.Status}. Process will not be killed.");
                return;

            }
            Process[] p = Process.GetProcessesByName(process.ProcessName);
            if (p.Length > 0)
            {
                int index = 1;
                foreach (Process p2 in p)
                {
                    WriteToLog($"Terminating {p2.ProcessName} [{index}/{p.Length}]");
                    p2.Kill();
                    p2.Close();
                    index++;
                }
            }
        }

        private void SuspendProcess(TrackedProcess process)
        {
            Process[] p = Process.GetProcessesByName(process.ProcessName);
            // DONT SUSPEND PROCESS IF ALREADY SUSPENDED STOPPED OR NOT FOUND
            if (process.Status == ProcessState.Suspended || process.Status == ProcessState.Stopped || process.Status == ProcessState.NotFound)
            {
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) is {process.Status}. Process will not be suspended.");
                return;
            }
            if (p.Length > 0)
            {
                int index = 1;
                foreach (Process p2 in p)
                {
                    if (p2.HasExited) continue;
                    
                    WriteToLog($"Suspending {p2.ProcessName} [{index}/{p.Length}]");
                    p2.Suspend();
                    process.Status = ProcessState.Suspended;
                    index++;
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
        public ICommand AddProcessCommand => new RelayCommand(param => AddProces(), param => CanAddProcess());

        public ICommand SaveSettingsCommand => new RelayCommand(param => SaveSettings());
        public ICommand FilterMainViewCommand => new RelayCommand(param => RefreshFilter());
        public static ICommand OpenVRSuspenderWebsiteCommand => new RelayCommand(param => OpenVRSuspenderWebsite());
        public ICommand StarWithWindowsCommand => new RelayCommand(param => SetStartWithWindows(StartWithWindows));


        private bool CanAddProcess()
        {
            return VrRunning == false;
        }


        private static void OpenVRSuspenderWebsite()
        {
            OpenBrowser("https://github.com/Hyrules/VRSuspender/");
        }

        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {              
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
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
            Properties.Settings.Default.StartMonitorOnStartup = StartMonitoringOnStartup;
            Properties.Settings.Default.MinimizeToTray = MinimizeToTray;
            Properties.Settings.Default.CloseToTray = MinimizeToTray;
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
            SaveSettings();
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
                SaveUserProcessList();
            }
        }

        private bool CanStartMonitor()
        {
            return IsMonitoring == false;
        }

        private bool CanStopMonitor()
        {
            return IsMonitoring == true && VrRunning == false;
        }
        private bool CanEditProcess()
        {
            return SelectedTrackedProcess != null && VrRunning == false; 
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
                SelectedTrackedProcess.ProcessName = process.ProcessName;
                SelectedTrackedProcess.ProfileName = process.ProfileName;
                SelectedTrackedProcess.Action = process.Action;
                SelectedTrackedProcess.Path = process.Path;
                SaveUserProcessList();
            }
        }

        private void SaveUserProcessList()
        {

            Properties.Settings.Default.UserProfiles = JsonConvert.SerializeObject(ListTrackedProcess);
            Properties.Settings.Default.Save();

            /*
             *             string appfolder = Environment.SpecialFolder.LocalApplicationData + "\\VRSuspender";
            if (Directory.Exists(appfolder))
            {
                File.WriteAllText(appfolder + "\\userprofiles.vrs", JsonConvert.SerializeObject(ListTrackedProcess));
            }*/
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
            return SelectedTrackedProcess != null && VrRunning == false;
        }

        private void DeleteSuspendedProcess(TrackedProcess process)
        {
            // PREVENT USER FROM REMOVING A SUSPENDED PROCESS. THIS WOULD LEAVE THE PROCESS IN SUSPENDED WITHOUT A WAY TO RESUME IT.
            if (process.Status == ProcessState.Suspended) 
            {
                MessageBox.Show($"Process {process.ProfileName } ({process.ProcessName}) is suspended. Please resume the process before removing it.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WriteToLog($"Process {process.ProfileName } ({process.ProcessName}) is suspended. Please resume the process before removing it.");
                return;
            };
            ListTrackedProcess.Remove(SelectedTrackedProcess);
            SaveUserProcessList();
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
        public bool MinimizeToTray { get => _minimizeToTray; set => SetProperty(ref _minimizeToTray,value); }
        public bool CloseToTray { get => _closeToTray; set => SetProperty(ref _closeToTray,value); }
        #endregion
    }


}
