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
        public void InitClokiWorkByDay(int workItemId, TimeSpan work, int workDay, TimeEntry te)
        {
            if (workDay > 6 || workDay < 0)
                throw new ArgumentException("InitClokiWorkByDay: workday = " + workDay);

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
            TimeData[workDay].TimeEntries.Clear();
            TimeData[workDay].TimeEntries.Add(te);
        }
        public TimeSpan getTotalWork()
        {
            TimeSpan retval = TimeSpan.Zero;
            foreach (var time in TimeData)
            {
                retval += time.Work;
            }
            return retval;
        }
        public TimeSpan getOriginalTotalWork()
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
