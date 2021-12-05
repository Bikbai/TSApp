using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    /// <summary>
    /// Класс-обёртка для хранения сведений за календарный день, манипуляция данными снаружи!
    /// </summary>
    public class TimeData
    {
        public bool ManualDistribution { get; set; }
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
        public string Comment { get ; set; }
        /// <summary>
        /// Список TE, полученный из клоки, для нумера WI
        /// </summary>
        public List<TimeEntry> TimeEntries { get; set; } // учтённое в клоки время в этот день
        /// <summary>
        /// конструктор для указанного дня недели (0 это понедельник, 6 - воскресение)
        /// </summary>
        /// <param name="dayOfWeek">Нумер дня недели, начиная с нуля</param>
        /// <param name="totalWorkHours">Общее количество часов, учтённое на задаче в день</param>
        /// <param name="workItemId">TFS WorkItem Id</param>
        /// <param name="weekNumber">Нумер рабочей недели</param>
        public TimeData(int dayOfWeek, TimeSpan totalWorkHours, int workItemId, int weekNumber)
        {
            ManualDistribution = false;
            WorkItemId = workItemId;              
            Work = totalWorkHours;
            OriginalWork = totalWorkHours;
            Calday = Helpers.WeekBoundaries(weekNumber, true).AddDays(dayOfWeek);
        }

        public TimeData(DateTime calday, TimeSpan totalWorkHours, int workItemId)
        {
            ManualDistribution = false;
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
