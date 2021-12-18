using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TSApp.Model
{
    /// <summary>
    /// Класс-обёртка для хранения сведений за календарный день, манипуляция данными снаружи!
    /// </summary>
    public class WorkItemTimeData
    {
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
        public TimeSpan Work { get; set; }
        /// <summary>
        /// немодифицированное количество работы по таймшиту
        /// </summary>
        public TimeSpan OriginalWork { get; set; }
        // учтённое в клоки время в этот день
        /// <summary>
        /// конструктор для указанного дня недели (0 это понедельник, 6 - воскресение)
        /// </summary>
        /// <param name="dayOfWeek">Нумер дня недели, начиная с нуля</param>
        /// <param name="totalWorkHours">Общее количество часов, учтённое на задаче в день</param>
        /// <param name="workItemId">TFS WorkItem Id</param>
        /// <param name="weekNumber">Нумер рабочей недели</param>
        public WorkItemTimeData(int dayOfWeek, TimeSpan totalWorkHours, int workItemId, int weekNumber)
        {
            WorkItemId = workItemId;              
            Work = totalWorkHours;
            OriginalWork = totalWorkHours;
            Calday = Helpers.WeekBoundaries(weekNumber, true).AddDays(dayOfWeek);
        }

        public WorkItemTimeData(DateTime calday, TimeSpan totalWorkHours, int workItemId)
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
