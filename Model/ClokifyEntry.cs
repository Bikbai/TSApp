using System;
using Clockify.Net.Models.TimeEntries;
using Newtonsoft.Json;
using TSApp.ViewModel;

namespace TSApp.Model
{
    /// <summary>
    /// Класс-обёртка для отображения записи в Клокифай.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ClokifyEntry
    {
        #region Private fields
        [JsonProperty]
        private string id;
        [JsonProperty]
        private int workItemId = -1;
        [JsonProperty]
        private TimeSpan workTime;
        [JsonProperty]
        private DateTimeOffset start;
        [JsonProperty]
        private DateTimeOffset end;
        [JsonProperty]
        private string comment;
        #endregion

        public ClokifyEntry() { }

        /// <summary>
        /// Стандартный конструктор объекта на основе данных из клокифая
        /// </summary>
        /// <param name="te">Запись в клоки</param>
        public ClokifyEntry(TimeEntryDtoImpl te)
        {
            id = te.Id;
            if (te.TimeInterval.End != null) {
                workTime = (DateTimeOffset)te.TimeInterval.End - (DateTimeOffset)te.TimeInterval.Start;
                start = (DateTimeOffset)te.TimeInterval.Start;
                end = (DateTimeOffset)te.TimeInterval.End;
            }
            int idx = 0;
            idx = te.Description.IndexOf('.');
            if (idx < 7 && idx > 0)
            {
                int.TryParse(te.Description.Substring(0, idx), out this.workItemId);
                idx = te.Description.IndexOf("//", idx);
            }
                
            if (idx < 1)
                Description = te.Description;
            else
            if (idx > 10)
            {
                Description = te.Description.Substring(0, idx);
                Comment = te.Description.Substring(idx + 3);
            }
        }
        /// <summary>
        /// Календарный день записи
        /// </summary>        
        [JsonIgnore]
        public DateTime Calday { get => (DateTime)start.Date; }
        /// <summary>
        /// Идентификатор задачи в TFS
        /// </summary>
        public int WorkItemId { get => workItemId; set => workItemId = value; }
        /// <summary>
        /// Учтённая длительность
        /// </summary>
        public TimeSpan WorkTime { get => workTime; set => workTime = value; }
        /// <summary>
        /// Начало учтенного периода
        /// </summary>
        public DateTimeOffset Start { get => start; set => start = value; }
        /// <summary>
        /// Конец учтённого периода
        /// </summary>
        public DateTimeOffset End { get => end; set => end = value; }
        /// <summary>
        /// Идентификатор записи в Клоки
        /// </summary>
        public string Id { get => id; }
        /// <summary>
        /// День недели
        /// </summary>
        [JsonIgnore]
        public DayOfWeek DayOfWeek { get => Calday.DayOfWeek; }
        public string Comment { get => comment; set => comment = value; }
        public string Description { get; set; }
    }
}
