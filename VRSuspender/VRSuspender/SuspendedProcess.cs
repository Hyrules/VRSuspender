using System;
using System.Collections.Generic;
using System.Text;
using VRSuspender.Utils;

namespace VRSuspender
{
    public class SuspendedProcess : ValidatableBindableBase
    {
        string _name = string.Empty;
        string _status = string.Empty;
        string _path = string.Empty;

        public SuspendedProcess()
        {

        }

        public SuspendedProcess(string name)
        {
            Name = name;
        }

        public SuspendedProcess(string name, string status)
        {
            Name = name;
            Status = Status;

        }

        public SuspendedProcess(string name, string status, string path)
        {
            Name = name;
            Status = Status;
            Path = path;
        }

        public string Name 
        { 
            get => _name; 
            set => SetProperty(ref _name,value) 
        }
        public string Status 
        { 
            get => _status; 
            set => SetProperty(ref _status,value); 
        }
        public string Path 
        { 
            get => _path;
            set => SetProperty(ref _path, value);

        }
    }
}
