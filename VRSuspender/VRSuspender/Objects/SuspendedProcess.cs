using System;
using System.Collections.Generic;
using System.Text;
using VRSuspender.Utils;

namespace VRSuspender
{
    public enum ProcessState { Running, Suspended, Stopped, Unknown, NotFound };
    public enum ProcessAction { KeepRunning, Suspend, Kill, Close };

    public class SuspendedProcess : ValidatableBindableBase
    {


        string _name;
        ProcessState _status;
        string _path;
        ProcessAction _action;

        public SuspendedProcess()
        {
            _name = string.Empty;
            _status = ProcessState.Unknown;
            _path = string.Empty;
            _action = ProcessAction.Suspend;
        }

        public SuspendedProcess(string name) : this()
        {         
            Name = name;
        }

        public SuspendedProcess(string name, ProcessState status) :this(name)
        {            
            Status = status;
        }

        public SuspendedProcess(string name, ProcessState status, string path) : this(name,status)
        {
            Path = path;
        }

        public SuspendedProcess(string name, ProcessState status, string path, ProcessAction action) : this(name,status,path)
        {
            Action = action;
        }


        public string Name 
        { 
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public ProcessState Status 
        { 
            get => _status; 
            set => SetProperty(ref _status,value); 
        }
        public string Path
        { 
            get => _path;
            set => SetProperty(ref _path, value);

        }
        public ProcessAction Action
        { 
            get => _action; 
            set => SetProperty(ref _action, value); 
        }
    }
}
