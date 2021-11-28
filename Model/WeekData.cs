using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    public class WeekData
    {
        public TimeData[] TimeData { get; set; }
        public int WeekNumber;

        public WeekData(int weekNumber, int workItemId)
        {
            WeekNumber = weekNumber == 0 ? 1 : weekNumber;
            TimeData = new TimeData[7];
            for (int i = 0; i < 7; i++)
            {
                TimeData[i] = new TimeData(i, TimeSpan.Zero, workItemId, weekNumber);
            }
        }
        public bool IsChanged
        {
            get
            {
                foreach (var data in TimeData)
                {
                    if (data == null)
                        return false;
                    if (data.OriginalWork != data.Work)
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
        public void AppendTimeEntry(int workItemId, TimeSpan work, TimeEntry te)
        {
            int workDay = te.DayOfWeek;

            if (TimeData[workDay] == null)
            { 
                TimeData[workDay] = new TimeData(workDay, work, workItemId, WeekNumber);
            } else
            {
                TimeData[workDay].Work += work;
                TimeData[workDay].OriginalWork += work;
            }

            if (TimeData[workDay].TimeEntries == null)
            {
                TimeData[workDay].TimeEntries = new List<TimeEntry>();                
            }
            TimeData[workDay].TimeEntries.Add(te);
        }
        /// <summary>
        /// Удалить Time Entry с пересчётом времени
        /// </summary>
        /// <param name="timeEntryId"></param>
        /// <returns></returns>
        public TimeSpan RemoveTimeEntry(string timeEntryId)
        {
            TimeSpan removedWork = TimeSpan.Zero;
            foreach (var w in this.TimeData)
            {
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
            foreach (var time in TimeData)
            {
                retval += time.Work;
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
            foreach (var time in TimeData)
            {
                retval += time.OriginalWork;
            }
            return retval;
        }
    }
}
