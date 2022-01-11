using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Input;
using VRSuspender.Utils;
using AdonisUI;
using AdonisUI.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;
using System.Windows.Controls;
using System.Windows;
using VRSuspender.Utils.Validations;
using VRSuspender.Objects;

namespace VRSuspender.EditProcessForm
{
    public class EditTrackedProcessFormViewModel : ValidatableBindableBase
    {
 
        private string _profileName;
        private string _processName;
        private ProcessAction _action;
        private string _path;
        private List<TrackedProcess> _listProfiles;
        private TrackedProcess _selectedProfile;

        public EditTrackedProcessFormViewModel()
        {
            _path = string.Empty;

            ListProfiles = ProfileDBManager.ListProfiles;
            TrackedProcess _customprocess = new() { ProfileName = "(Custom)", Action = ProcessAction.Suspend };
            _listProfiles.Insert(0,_customprocess);
            SelectedProfile = _customprocess;
        }

        public ICommand ProfileSelectionChangedCommand => new RelayCommand(param => ProfileSelectionChanged());
        public ICommand BrowseExecutableCommand => new RelayCommand(param => BrowseExecutable());
  
        private void ProfileSelectionChanged()
        {
            ProfileName = SelectedProfile.ProfileName;
            ProcessName = SelectedProfile.ProcessName;
            Action = SelectedProfile.Action;
            Path = SelectedProfile.Path;
        }

        [NotNullOrEmptyOrWhiteSpaceValidation(ErrorMessage = "This field cannot be empty, null or whitespaces.")]
        public string ProcessName 
        { 
            get => _processName; 
            set => SetProperty(ref _processName,value); 
        }

        public ProcessAction Action 
        { 
            get => _action;
            set => SetProperty(ref _action, value); 
        }
        [NotNullOrEmptyOrWhiteSpaceValidation(ErrorMessage = "This field cannot be empty, null or whitespaces.")]
        public string Path 
        { 
            get => _path; 
            set => SetProperty(ref _path,value); 
        }
        public List<TrackedProcess> ListProfiles { get => _listProfiles; set => SetProperty(ref _listProfiles,value); }
        public TrackedProcess SelectedProfile { get => _selectedProfile; set => SetProperty(ref _selectedProfile,value); }
        
        [NotNullOrEmptyOrWhiteSpaceValidation(ErrorMessage = "This field cannot be empty, null or whitespaces.")]
        public string ProfileName { get => _profileName; set => SetProperty(ref _profileName,value); }

        private void BrowseExecutable()
        {
            OpenFileDialog ofd = new()
            {
                Filter = "Executable file (*.exe)|*.exe"
            };
            if (Path != string.Empty)
            {
                ofd.FileName = System.IO.Path.GetFileName(Path);
                ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Path);
            }

            if(ofd.ShowDialog() == true)
            {
                if(ofd.FileName == Environment.ProcessPath)
                {
                    MessageBox.Show("This application cannot monitored itself.", "Error", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error);
                    return;
                }

                if(System.IO.Path.GetFileName(ofd.FileName) == "vrserver.exe")
                {
                    MessageBox.Show("This application cannot monitor SteamVR - vrserver.exe.", "Error", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error);
                    return;
                }

                Path = ofd.FileName;
                ProcessName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
            }

        }

        public void EditTrackedProcess(TrackedProcess process)
        {
            ProcessName = process.ProcessName;
            ProfileName = process.ProfileName;
            Path = process.Path;
            Action = process.Action;
        }

        public TrackedProcess GetTrackedProcess()
        {
            TrackedProcess process = new()
            {
                ProfileName = ProfileName,
                ProcessName = _processName,
                Path = _path,
                Action = _action
            };

            return process;

        }
    }
}
