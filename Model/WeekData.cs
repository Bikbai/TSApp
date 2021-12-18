using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    public class WeekData
    {
        public Dictionary<DayOfWeek, WorkItemTimeData> WorkDataDaily { get; set; }
        public int WeekNumber;
        public WeekData(int weekNumber, int workItemId)
        {
            WeekNumber = weekNumber == 0 ? 1 : weekNumber;
            WorkDataDaily = new Dictionary<DayOfWeek, WorkItemTimeData>();
            foreach(var d in Enum.GetValues(typeof(DayOfWeek)))
            {
                // инициализируем коллекцию как массив, пустыми значениями
                WorkDataDaily.Add((DayOfWeek)d, null);
            }
        }
        public bool IsChanged
        {
            get
            {
                if (WorkDataDaily.Count == 0)
                    return false;
                foreach (var data in WorkDataDaily)
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
        /// Добавление сведений из ClokifyEntry к учтённому времени
        /// </summary>
        /// <param name="te"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ApplyTimeData(ClokifyEntry te)
        {
            int workItemId = te.WorkItemId;
            if (workItemId == -1)
                throw new ArgumentException("AppendTimeEntry: попытка добавить TimeEntry, не привязанный к WorkItem.Id: ");
            DateTime start = ((DateTimeOffset)te.Start).DateTime;
            DateTime end = ((DateTimeOffset)te.End).DateTime;
            TimeSpan work = end.Subtract(start);
            var workDay = te.DayOfWeek;

            if (WorkDataDaily[workDay] == null)
            { 
                WorkDataDaily[workDay] = new WorkItemTimeData(start.Date, work, workItemId);
            } 
            else
            {
                WorkDataDaily[workDay].Work += work;
                WorkDataDaily[workDay].OriginalWork += work;
            }
        }
        /// <summary>
        /// Получить текущие трудозатраты за неделю
        /// </summary>
        /// <returns>Трудозатраты, введённые в табеле</returns>
        public TimeSpan GetTotalWork()
        {
            TimeSpan retval = TimeSpan.Zero;
            foreach (var time in WorkDataDaily)
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
            foreach (var time in WorkDataDaily)
            {
                if (time.Value == null) continue ;
                retval += time.Value.OriginalWork;
            }
            return retval;
        }
    }
}
