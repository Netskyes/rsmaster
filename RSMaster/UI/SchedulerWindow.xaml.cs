using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RSMaster.UI
{
    using Models;
    using System.Text.RegularExpressions;
    using Utility;
    
    public partial class SchedulerWindow : MetroWindow
    {
        public TimeEvent TimeEventOpen { get; set; } = new TimeEvent();
        public ICollectionView TimeEventsList { get; set; }

        public static Dictionary<int, string> WeekDaysMap = new Dictionary<int, string>
        {
            { 1, "Monday" },
            { 2, "Tuesday" },
            { 3, "Wednesday" },
            { 4, "Thursday" },
            { 5, "Friday" },
            { 6, "Saturday" },
            { 7, "Sunday" },
            { 8, "Everyday" }
        };

        private MainWindow Host { get; set; }
        private AccountModel Account { get; set; }
        private ObservableCollection<TimeEvent> timeEventsList = new ObservableCollection<TimeEvent>();
        private bool eventsLoading;

        public SchedulerWindow(MainWindow host, AccountModel account)
        {
            InitializeComponent();

            Host = host;
            Host.Closed += Host_Closed;
            Account = account;
            Title = account.Username ?? account.Name;

            WeekDaysMap.Select(x => new PairValueModel(x.Key, x.Value)).ToList().ForEach(x => CmbBoxDayOfTheWeek.Items.Add(x));

            TimeEventsList = CollectionViewSource.GetDefaultView(timeEventsList);
            EventsList.DataContext = this;
            TimeEventSettings.DataContext = TimeEventOpen;

            LoadEvents();
        }

        private void LoadEvents()
        {
            Invoke(() =>
            {
                timeEventsList.Clear();
                DataProvider.GetModels<TimeEvent>("schedule").Where
                    (x => x.AccountId == Account.Id).ToList().ForEach(x => timeEventsList.Add(x));
            });
        }

        private void ButtonAddEvent_Click(object sender, RoutedEventArgs e)
        {
            int daysIndex = Invoke(() => (int)(CmbBoxDayOfTheWeek.SelectedItem as PairValueModel).Key);
            var timeEvent = new TimeEvent(Account.Id)
            {
                Day = daysIndex
            };

            if (DataProvider.SaveModel(timeEvent, "schedule"))
            {
                timeEvent = DataProvider.GetModels<TimeEvent>("schedule").FirstOrDefault();
                if (timeEvent != null)
                {
                    Invoke(() => timeEventsList.Add(timeEvent));
                }
            }
        }

        private void ButtonDeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            var item = Invoke(() => EventsList.SelectedItem) as TimeEvent;
            if (item != null)
            {
                if (DataProvider.DeleteModel(item.Id, "schedule"))
                {
                    Invoke(() =>
                    {
                        timeEventsList.Remove(item);
                        MainWindow.ScheduleManager.DeleteTimeEventById(item.Id);

                        Util.UpdateObjByProps(null, TimeEventOpen, true);
                    });
                }
            }
        }

        private void Host_Closed(object sender, EventArgs e) => Close();

        private void EventsList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            eventsLoading = true;
            if (Invoke(() => EventsList.SelectedItem) is TimeEvent item)
            {
                var timeEvent = DataProvider.GetModels<TimeEvent>("schedule").FirstOrDefault(x => x.Id == item.Id);
                if (timeEvent != null)
                {
                    Invoke(() => Util.UpdateObjByProps(item, TimeEventOpen, false));
                }
            }

            eventsLoading = false;
        }

        private void TimeEventDetailChange(object sender, TextChangedEventArgs e)
        {
            if (eventsLoading)
                return;

            var timeEvent = Invoke(() => TimeEventOpen);
            if (timeEvent != null)
            {
                if (DataProvider.UpdateModel(timeEvent, "schedule"))
                {
                    Invoke(() =>
                    {
                        var item = timeEventsList.FirstOrDefault(x => x.Id == timeEvent.Id);
                        if (item != null)
                        {
                            Invoke(() =>
                            {
                                MainWindow.ScheduleManager.AddOrUpdateTimeEvent(timeEvent);
                                Util.UpdateObjByProps(timeEvent, item, false);
                            });
                        }
                    });
                }
            }
        }

        private void DayOfTheWeekDetailChange(object sender, SelectionChangedEventArgs e)
            => TimeEventDetailChange(null, null);

        private void TimeFromDetailChange(object sender, RoutedEventArgs e)
        {
            var value = Invoke(() => TxtBoxEventTimeBeginning.Text);
            if (ValidateTime(value))
            {
                TimeEventDetailChange(null, null);
            }
        }

        private void TimeToDetailChange(object sender, RoutedEventArgs e)
        {
            var value = Invoke(() => TxtBoxEventTimeEnding.Text);
            if (ValidateTime(value))
            {
                TimeEventDetailChange(null, null);
            }
        }

        private void TimeEventActiveChange(object sender, RoutedEventArgs e)
        {
            var timeEvent = Invoke(() => EventsList.SelectedItem) as TimeEvent;
            if (timeEvent != null)
            {
                if (DataProvider.UpdateModel(timeEvent, "schedule"))
                {
                    Invoke(() =>
                    {
                        MainWindow.ScheduleManager.AddOrUpdateTimeEvent(timeEvent);
                        Util.UpdateObjByProps(timeEvent, TimeEventOpen, false);
                    });
                }
            }
        }

        #region Helpers

        private void Invoke(Action action) => Dispatcher.Invoke(action);

        private T Invoke<T>(Func<T> action)
        {
            T result = default(T);
            Dispatcher.Invoke(() => result = action.Invoke());
            return result;
        }

        private bool ValidateTime(string value)
        {
            var pattern = new Regex("^[0-9]{2}:[0-9]{2}$");
            if (value is null
                || !pattern.IsMatch(value))
                return false;

            var time = value.Split(':');
            if (time.Length > 1)
            {
                int.TryParse(time[0], out int hours);
                int.TryParse(time[1], out int minutes);

                return (hours >= 0 && hours <= 24) && (minutes >= 0 && minutes <= 60);
            }

            return false;
        }

        #endregion
    }
}
