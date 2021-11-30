using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    public class WeekData
    {
        public Dictionary<DayOfWeek, TimeData> TimeDataDaily { get; set; }
        public int WeekNumber;

        public WeekData(int weekNumber, int workItemId)
        {
            WeekNumber = weekNumber == 0 ? 1 : weekNumber;
            TimeDataDaily = new Dictionary<DayOfWeek, TimeData>();
        }
        public bool IsChanged
        {
            get
            {
                if (TimeDataDaily.Count == 0)
                    return false;
                foreach (var data in TimeDataDaily)
                {
                    if (data.Value.OriginalWork != data.Value.Work)
                        return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Добавление сведений из TimeEntry к учтённому времени
        /// </summary>
        /// <param name="workItemId">TFS WorkItem ID</param>
        /// <param name="work">Учтённые часы</param>
        /// <param name="te">Учитываемый Clokify TimeEntry</param>
        /// <param name="applyChanges"></param>
        public void AppendTimeEntry(TimeEntry te)
        {
            int workItemId = te.WorkItemId;
            if (workItemId == -1)
                throw new ArgumentException("AppendTimeEntry: попытка добавить TimeEntry, не привязанный к WorkItem.Id: ");
            DateTime start = ((DateTimeOffset)te.Start).DateTime;
            DateTime end = ((DateTimeOffset)te.End).DateTime;
            TimeSpan work = end.Subtract(start);
            var workDay = te.DayOfWeek;
            TimeData td;

            if (!TimeDataDaily.TryGetValue(workDay, out td))
            { 
                TimeDataDaily.Add(workDay, new TimeData(start.Date, work, workItemId, WeekNumber));
            } 
            else
            {
                TimeDataDaily[workDay].Work += work;
                TimeDataDaily[workDay].OriginalWork += work;
            }

            if (TimeDataDaily[workDay].TimeEntries == null)
            {
                TimeDataDaily[workDay].TimeEntries = new List<TimeEntry>();                
            }
            TimeDataDaily[workDay].TimeEntries.Add(te);
        }
        /// <summary>
        /// Удалить Time Entry с пересчётом времени
        /// </summary>
        /// <param name="timeEntryId"></param>
        /// <returns></returns>
        public TimeSpan RemoveTimeEntry(string timeEntryId)
        {
            TimeSpan removedWork = TimeSpan.Zero;
            foreach (var td in this.TimeDataDaily)
            {
                var w = td.Value;
                if (w.TimeEntries != null)
                    foreach (var t in w.TimeEntries)
                        if (t.Id == timeEntryId)
                        {
                            removedWork = t.WorkTime;
                            w.Work -= t.WorkTime;
                            if (w.Work < TimeSpan.Zero) w.Work = TimeSpan.Zero;
                            w.OriginalWork -= t.WorkTime;
                            if (w.OriginalWork < TimeSpan.Zero) w.OriginalWork = TimeSpan.Zero;
                            w.TimeEntries.Remove(t);
                            return removedWork;
                        }
            }
            return removedWork;
        }
        /// <summary>
        /// Получить текущие трудозатраты за неделю
        /// </summary>
        /// <returns>Трудозатраты, введённые в табеле</returns>
        public TimeSpan GetTotalWork()
        {
            TimeSpan retval = TimeSpan.Zero;
            foreach (var time in TimeDataDaily)
            {
                retval += time.Value.Work;
            }
            return retval;
        }
        /// <summary>
        /// Получить учтённые трудозатраты за неделю 
        /// </summary>
        /// <returns>Трудозатраты, учтённые в клоки</returns>
        public TimeSpan GetOriginalTotalWork()
        {
            TimeSpan retval = TimeSpan.Zero;
            foreach (var time in TimeDataDaily)
            {
                retval += time.Value.OriginalWork;
            }
            return retval;
        }
    }
}
