using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSMaster
{
    using UI;
    using UI.Models;
    using Data;

    internal class ScheduleManager
    {
        private List<TimeEvent> timeEvents = new List<TimeEvent>();
        private readonly object timeEventsLock = new object();

        public ScheduleManager()
        {
            LoadTimeEvents();
        }

        public void Begin()
        {
            Task.Factory.StartNew(() => MonitorSchedule(), TaskCreationOptions.LongRunning);
        }

        private void LoadTimeEvents()
        {
            lock (timeEventsLock)
            {
                DataProvider.GetModels<TimeEvent>("schedule").Where
                    (x => x.Day.HasValue 
                    && !string.IsNullOrEmpty(x.BeginningTime) 
                    && !string.IsNullOrEmpty(x.EndingTime)
                    && x.Active == 1).ToList().ForEach(x => timeEvents.Add(x));
            }
        }

        private (int bHour, int bMinute, int eHour, int eMinute) ConvertBeginEndTime(string beginning, string ending)
        {
            var beginningTime = beginning.Split(':');
            var endingTime = ending.Split(':');

            int.TryParse(beginningTime[0],
                out int beginHour);
            int.TryParse(beginningTime[1],
                out int beginMinute);

            int.TryParse(endingTime[0], out int endingHour);
            int.TryParse(endingTime[1], out int endingMinute);

            return (beginHour, beginMinute, endingHour, endingMinute);
        }

        private void HandleTime(TimeEvent timeEvent)
        {
            if (timeEvent is null 
                || timeEvent.Active != 1)
                return;

            var dateTimeNow = DateTime.Now;

            var day = timeEvent.Day.Value;
            if (day != 7 && day != (int)dateTimeNow.DayOfWeek)
            {
                return;
            }

            var (bHour, bMinute, eHour, eMinute) = ConvertBeginEndTime(timeEvent.BeginningTime, timeEvent.EndingTime);
            var manager = MainWindow.AccountManager;
            var account = MainWindow.GetAccountsHandler().FirstOrDefault
                (x => x.Id == timeEvent.AccountId);
            if (account is null)
                return;

            if (dateTimeNow.Hour >= bHour 
                && dateTimeNow.Hour <= eHour
                && dateTimeNow.Minute >= bMinute 
                && dateTimeNow.Minute <= eMinute)
            {
                if (!manager.AccountsLaunching.Contains(account.Username) && !account.PID.HasValue)
                {
                    if (string.IsNullOrEmpty(timeEvent.Script))
                    {
                        account.Script = timeEvent.Script;
                    }

                    MainWindow.LaunchAccountHandler(account, true);
                }
            }
            else if (account.AutoLaunched && manager.IsRunning(account.Username))
            {
                Task.Run(() => manager.StopAccount(account));
            }
        }

        public void MonitorSchedule()
        {
            while (true)
            {
                lock (timeEventsLock)
                {
                    for (int i = 0; i < timeEvents.Count; i++)
                    {
                        HandleTime(timeEvents[i]);
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public void AddOrUpdateTimeEvent(TimeEvent timeEvent)
        {
            if (timeEvent is null 
                || !timeEvent.Day.HasValue 
                || string.IsNullOrEmpty(timeEvent.BeginningTime) 
                || string.IsNullOrEmpty(timeEvent.EndingTime))
            {
                return;
            }

            lock (timeEventsLock)
            {
                var teIndex = timeEvents.FindIndex(x => x.Id == timeEvent.Id);
                if (teIndex != -1)
                {
                    timeEvents[teIndex] = timeEvent;
                }
                else
                {
                    timeEvents.Add(timeEvent);
                }
            }
        }

        public void DeleteTimeEventById(int eventId)
        {
            lock (timeEventsLock)
            {
                var te = timeEvents.FirstOrDefault(x => x.Id == eventId);
                if (te != null)
                {
                    timeEvents.Remove(te);
                }
            }
        }
    }
}
