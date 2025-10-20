using System;
using System.ComponentModel;
using System.Linq;

namespace SmartRefillWasm.Models
{
    public class MachineRefillSetting : INotifyPropertyChanged
    {
        private string _machineName = "";
        public string MachineName
        {
            get => _machineName;
            set
            {
                _machineName = value;
                OnPropertyChanged();
            }
        }

        private DateTime _refillDate = DateTime.Today;
        public DateTime RefillDate
        {
            get => _refillDate;
            set
            {
                _refillDate = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _refillTime = new TimeSpan(9, 0, 0);
        public TimeSpan RefillTime
        {
            get => _refillTime;
            set
            {
                if (_refillTime != value)
                {
                    _refillTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RefillTimeAsTimeOnly));
                }
            }
        }

        public TimeOnly RefillTimeAsTimeOnly
        {
            get => TimeOnly.FromTimeSpan(RefillTime);
            set => RefillTime = value.ToTimeSpan();
        }
        
        public DateTime GetFullDateTime()
        {
            return RefillDate.Date + RefillTime;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 