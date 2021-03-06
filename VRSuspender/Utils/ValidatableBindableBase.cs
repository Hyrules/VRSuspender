using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using VRSuspender.Utils.Annotations;

namespace VRSuspender.Utils
{
    public class ValidatableBindableBase : IDataErrorInfo, IChangeTracking, INotifyPropertyChanged
    {
        private ValidationContext _validationContext { get; set; }
        private bool _isChanged;

        protected ValidatableBindableBase()
        {
            _isChanged = false;
            this._validationContext = new ValidationContext(this);
        }

        [Browsable(false)]
        public string this[string propertyName]
        {
            get
            {
                var prop = GetType().GetProperty(propertyName);
                var validationMap = prop.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>();
                List<string> errormessages = new List<string>();
                foreach (var v in validationMap)
                {
                    try
                    {
                        v.Validate(prop.GetValue(this, null), _validationContext);
                    }
                    catch (Exception)
                    {
                        errormessages.Add((string)v.GetType().GetProperty("ErrorMessageString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(v));
                    }

                }

                return errormessages.Count == 0 ? null : string.Join(",", errormessages);
            }
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        [Browsable(false)]
        public string Error { get; internal set; }

        [Browsable(false)]
        public bool IsChanged
        {
            get => _isChanged;
            internal set => _isChanged = value;
        }

        public virtual void AcceptChanges()
        {
            this.IsChanged = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            IsChanged = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
