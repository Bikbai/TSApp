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
            foreach(var d in Enum.GetValues(typeof(DayOfWeek)))
            {
                // инициализируем коллекцию как массив, пустыми значениями
                TimeDataDaily.Add((DayOfWeek)d, null);
            }
        }
        public bool IsChanged
        {
            get
            {
                if (TimeDataDaily.Count == 0)
                    return false;
                foreach (var data in TimeDataDaily)
                {
                    if (data.Value == null)
                        continue;
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
        public void AppendTimeEntry(ClokifyEntry te)
        {
            int workItemId = te.WorkItemId;
            if (workItemId == -1)
                throw new ArgumentException("AppendTimeEntry: попытка добавить TimeEntry, не привязанный к WorkItem.Id: ");
            DateTime start = ((DateTimeOffset)te.Start).DateTime;
            DateTime end = ((DateTimeOffset)te.End).DateTime;
            TimeSpan work = end.Subtract(start);
            var workDay = te.DayOfWeek;

            if (TimeDataDaily[workDay] == null)
            { 
                TimeDataDaily[workDay] = new TimeData(start.Date, work, workItemId);
            } 
            else
            {
                TimeDataDaily[workDay].Work += work;
                TimeDataDaily[workDay].OriginalWork += work;
            }

            if (TimeDataDaily[workDay].TimeEntries == null)
            {
                TimeDataDaily[workDay].TimeEntries = new List<ClokifyEntry>();                
            }
            TimeDataDaily[workDay].TimeEntries.Add(te);
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
                if (time.Value == null) continue;
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
                if (time.Value == null) continue ;
                retval += time.Value.OriginalWork;
            }
            return retval;
        }
    }
}
