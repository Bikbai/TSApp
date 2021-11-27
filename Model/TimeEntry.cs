using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Clockify.Net.Models.TimeEntries;

namespace TSApp.Model
{
    public class TimeEntry : ObservableObject
    {
        private readonly string id;
        private string userId;
        private string workspaceId;
        private string projectId;
        private int workItemId = -1;
        private string description;
        private TimeSpan workTime;
        private DateTimeOffset start;
        private DateTimeOffset end;
        /// <summary>
        /// Конструктор нового TimeEntry, для создания непривязанных к задачам записей
        /// </summary>
        /// <param name="description"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public TimeEntry(string description, DateTimeOffset start, DateTimeOffset end)
        {
            this.description = description;
            workTime = end - start;
            this.start = start;
            this.end = end;
        }
        /// <summary>
        /// Стандартный конструктор объекта на основе данных из клокифая
        /// </summary>
        /// <param name="te">Запись в клоки</param>
        public TimeEntry(TimeEntryDtoImpl te)
        {
            this.id = te.Id;
            this.description = te.Description;
            if (te.TimeInterval.End != null) {
                workTime = (DateTimeOffset)te.TimeInterval.End - (DateTimeOffset)te.TimeInterval.Start;
                this.start = (DateTimeOffset)te.TimeInterval.Start;
                this.end = (DateTimeOffset)te.TimeInterval.End;
            }
            int idx = 0;
            idx = te.Description.IndexOf('.');
            if (idx < 7 && idx > 0)
                int.TryParse(te.Description.Substring(0, idx), out workItemId);
        }
        /// <summary>
        /// Календарный день записи
        /// </summary>
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
    }
}
