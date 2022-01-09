using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using VRSuspender.Utils;

namespace VRSuspender.EditProcessForm
{
    public class EditTrackedProcessFormViewModel : ValidatableBindableBase
    {
 
        private string _name;
        private uint _action;
        private string _path;

        public EditTrackedProcessFormViewModel()
        {
            _path = string.Empty;
        }
        
        public ICommand BrowseExecutableCommand => new RelayCommand(param => BrowseExecutable());

        public string Name 
        { 
            get => _name; 
            set => SetProperty(ref _name,value); 
        }

        public uint Action 
        { 
            get => _action;
            set => SetProperty(ref _action, value); 
        }
        public string Path 
        { 
            get => _path; 
            set => SetProperty(ref _path,value); 
        }

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
                Path = ofd.FileName;
                Name = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
            }

        }

        public void EditTrackedProcess(TrackedProcess process)
        {
            Name = process.Name;
            Path = process.Path;
            switch(process.Action)
            {
                case ProcessAction.Suspend:
                    Action = 0;
                    break;
                case ProcessAction.Kill:
                    Action = 1;
                    break;
                case ProcessAction.KeepRunning:
                    Action = 2;
                    break;
                default:
                    break;

            }
        }

        public TrackedProcess GetTrackedProcess()
        {
            TrackedProcess process = new()
            {
                Name = _name,
                Path = _path
            };
            switch (_action)
            {
                case 0:
                    process.Action = ProcessAction.Suspend;
                    break;
                case 1:
                    process.Action = ProcessAction.Kill;
                    break;
                case 2:
                    process.Action = ProcessAction.KeepRunning;
                    break;
                default:
                    break;

            }

            return process;

        }
    }
}
