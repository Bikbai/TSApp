using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    public class TimeData
    {
        public int WorkItemId { get; }
        public int DayOfWeek { get; set; } // на всякий случай
        public TimeSpan Work { get; set; } // введенная работа по таймшиту
        public TimeSpan OriginalWork { get; set; } // оригинальная работа по таймшиту
        public List<TimeEntry> TimeEntries { get; set; } // учтённое в клоки время в этот день
        public TimeData(int day, TimeSpan work, int workItemId)
        {
            WorkItemId = workItemId;
            DayOfWeek = day;
            Work = work;
            OriginalWork = work;
        }
    }
}
