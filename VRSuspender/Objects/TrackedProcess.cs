using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;
using VRSuspender.Utils;

namespace VRSuspender
{
    public enum ProcessState { Running, Suspended, Stopped, Unknown, NotFound };
    public enum ProcessAction { KeepRunning, Suspend, Kill };

    public class TrackedProcess : ValidatableBindableBase
    {


        private string _name;
        private ProcessState _status;
        private string _path;
        private ProcessAction _action;
        private ImageSource _icon;

        public TrackedProcess()
        {
            _name = string.Empty;
            _status = ProcessState.Unknown;
            _path = string.Empty;
            _action = ProcessAction.Suspend;
            Icon = null;
        }

        public TrackedProcess(string name) : this()
        {         
            Name = name;
        }

        public TrackedProcess(string name, ProcessState status) :this(name)
        {            
            Status = status;
        }

        public TrackedProcess(string name, ProcessState status, string path) : this(name,status)
        {
            Path = path;
        }

        public TrackedProcess(string name, ProcessState status, string path, ProcessAction action) : this(name,status,path)
        {
            Action = action;
        }

        public TrackedProcess(string name, ProcessAction action)
        {
            Name = name;
            Action = action;
            Path = string.Empty;
        }

        public string Name 
        { 
            get => _name;
            set => SetProperty(ref _name, value);
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
