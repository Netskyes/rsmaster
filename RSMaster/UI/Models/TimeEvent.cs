using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace RSMaster.UI.Models
{
    using Data;

    public class TimeEvent : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int? day;
        private string script;
        private string beginningTime;
        private string endingTime;
        private int active;

        #endregion

        public TimeEvent()
        {
        }

        public TimeEvent(int accountId)
        {
            AccountId = accountId;
        }

        [PropUpdateIgnore]
        [PropInsertIgnore]
        public int Id { get; set; }

        [PropUpdateIgnore]
        public int AccountId { get; set; }

        public int Active
        {
            get => active;
            set
            {
                active = value;
                NotifyPropertyChanged("Active");
            }
        }

        public string Script
        {
            get => script;
            set
            {
                script = value;
                NotifyPropertyChanged("Script");
            }
        }

        public int? Day
        {
            get => day ?? 1;
            set
            {
                day = value;
                NotifyPropertyChanged("Day");
                NotifyPropertyChanged("DayName");
            }
        }

        public string BeginningTime
        {
            get => beginningTime;
            set
            {
                beginningTime = value;
                NotifyPropertyChanged("BeginningTime");
            }
        }

        public string EndingTime
        {
            get => endingTime;
            set
            {
                endingTime = value;
                NotifyPropertyChanged("EndingTime");
            }
        }

        [PropInsertIgnore, PropUpdateIgnore]
        public string DayName
        {
            get => day.HasValue ? SchedulerWindow.WeekDaysMap[day.Value] : "Unknown";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
