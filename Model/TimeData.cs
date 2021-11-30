using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    /// <summary>
    /// Класс-обёртка для хранения сведений, манипуляция данными снаружи!
    /// </summary>
    public class TimeData
    {
        private string comment = "";
        /// <summary>
        /// Текущий календарный день записи
        /// </summary>
        public DateTime Calday { get; set; }
        /// <summary>
        /// Идентификатор WI TFS
        /// </summary>
        public int WorkItemId { get; }
        /// <summary>
        /// модифицированное количество работы по таймшиту
        /// </summary>
        public TimeSpan Work { get; set; } // введенная работа по таймшиту
        /// <summary>
        /// немодифицированное количество работы по таймшиту
        /// </summary>
        public TimeSpan OriginalWork { get; set; } // оригинальная работа по таймшиту
        /// <summary>
        /// Комментарий к работе
        /// </summary>
        public string Comment { get => comment; set => comment = value; }
        /// <summary>
        /// Список TE, полученный из клоки, для нумера WI
        /// </summary>
        public List<TimeEntry> TimeEntries { get; set; } // учтённое в клоки время в этот день
        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="dayOfWeek">Нумер дня недели</param>
        /// <param name="totalWorkHours">Общее количество часов, учтённое на задаче в день</param>
        /// <param name="workItemId">TFS WorkItem Id</param>
        /// <param name="weekNumber">Нумер рабочей недели</param>
        public TimeData(int dayOfWeek, TimeSpan totalWorkHours, int workItemId, int weekNumber)
        {
            WorkItemId = workItemId;              
            Work = totalWorkHours;
            OriginalWork = totalWorkHours;
            Calday = Helpers.WeekBoundaries(weekNumber, true).AddDays(dayOfWeek);
        }

        public TimeData(DateTime calday, TimeSpan totalWorkHours, int workItemId, int weekNumber)
        {
            WorkItemId = workItemId;
            Work = totalWorkHours;
            OriginalWork = totalWorkHours;
            Calday = calday;
        }
        /// <summary>
        /// Текущий день недели
        /// </summary>
        public DayOfWeek DayOfWeek { get => Calday.DayOfWeek; }
    }
}
