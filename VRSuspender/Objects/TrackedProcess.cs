using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;
using VRSuspender.Utils;
using VRSuspender.Utils.Validations;

namespace VRSuspender
{
    public enum ProcessState { Running, Suspended, Stopped, Unknown, NotFound };
    public enum ProcessAction { KeepRunning, Suspend, Kill };

    public class TrackedProcess : ValidatableBindableBase
    {


        private string _profileName;
        private string _processName;
        private ProcessState _status;
        private string _path;
        private ProcessAction _action;
        private ImageSource _icon;

        public TrackedProcess()
        {
            _profileName = string.Empty;
            _processName = string.Empty;
            _status = ProcessState.Unknown;
            _path = string.Empty;
            _action = ProcessAction.Suspend;
            Icon = null;
        }

        public TrackedProcess(string profilename) : this()
        {         
            ProfileName = profilename;
        }

        public TrackedProcess(string profilename, string processname): this(profilename)
        {
            ProcessName = processname;
        }

        public TrackedProcess(string profilename, string processname, ProcessState status) :this(profilename, processname)
        {
            Status = status;
        }

        public TrackedProcess(string profilename, string processname, ProcessState status, string path) : this(profilename, processname, status)
        {
            Path = path;
        }

        public TrackedProcess(string profilename, string processname, ProcessState status, string path, ProcessAction action) : this(profilename, processname, status, path)
        {
            Action = action;
        }

        public TrackedProcess(string profilename, ProcessAction action)
        {
            ProfileName = profilename;
            ProcessName = string.Empty;
            Action = action;
            Path = string.Empty;
            ProcessName = string.Empty;
        }

        public string ProfileName 
        { 
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }

        [JsonIgnore]
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

        [JsonConverter(typeof(ProcessActionConverter))]
        public ProcessAction Action
        { 
            get => _action; 
            set => SetProperty(ref _action, value); 
        }
        
        [JsonIgnore]
        public ImageSource Icon 
        { 
            get => _icon; 
            set => SetProperty(ref _icon,value); 
        }

        public string ProcessName 
        { 
            get => _processName; 
            set => SetProperty(ref _processName,value); 
        }
    }

    public class ProcessActionConverter : JsonConverter
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string enumString = (string)reader.Value;
            return Enum.Parse(typeof(ProcessAction), enumString, true);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ProcessAction action = (ProcessAction)value;
            switch (action)
            {
                case ProcessAction.Kill:
                    writer.WriteValue("Suspend");
                    break;
                case ProcessAction.KeepRunning:
                    writer.WriteValue("KeepRunning");
                    break;
                default:
                case ProcessAction.Suspend:
                    writer.WriteValue("Suspend");
                    break;

            }
        }
    }


}
