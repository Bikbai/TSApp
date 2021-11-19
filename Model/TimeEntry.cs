using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Clockify.Net.Models.TimeEntries;

namespace TSApp
{
    public class TimeEntry : ObservableObject, ICloneable
    {
        private readonly string id;
        private string userId;
        private string workspaceId;
        private string projectId;
        private string taskId = String.Empty;
        private string description;
        private TimeSpan workTime;
        private DateTimeOffset start;
        private DateTimeOffset end;

        public TimeEntry(string description, DateTimeOffset start, DateTimeOffset end)
        {
            this.description = description;
            workTime = end - start;
            this.start = start;
            this.end = end;
        }

        public TimeEntry(TimeEntryDtoImpl te)
        {            
            this.id = te.Id;
            /*
            this.userId = te.UserId;
            this.workspaceId = te.WorkspaceId;
            this.projectId = te.ProjectId;
            */
            this.description = te.Description;
            if (te.TimeInterval.End != null) { 
                workTime = (DateTimeOffset)te.TimeInterval.End - (DateTimeOffset)te.TimeInterval.Start;
                this.start = (DateTimeOffset)te.TimeInterval.Start;
                this.end = (DateTimeOffset)te.TimeInterval.End;
            }
            int idx = 0;
            idx = te.Description.IndexOf('.');
            if (idx < 7 && idx > 0) 
                if (int.TryParse(te.Description.Substring(0, idx), out idx))
                    taskId = idx.ToString();
        }

        public string TaskId { get => taskId; set => taskId = value; }
        // необходимо для калькуляций Cloki (период) <-> TFS (потрачено часов)
        // сам период придётся ребилдить каждый раз, когда закидываем часы в клоки
        // ибо он считается как функция (начало дня, позиция, WorkTime), см. метод TimeEntryRequest
        public TimeSpan WorkTime { get => workTime; set => workTime = value; }
        public DateTimeOffset Start { get => start; set => start = value; }
        public DateTimeOffset End { get => end; set => end = value; }
        public string Id { get => id; }
        public string UserId { get => userId; set => userId = value; }
        public string WorkspaceId { get => workspaceId; set => workspaceId = value; }
        public string ProjectId { get => projectId; set => projectId = value; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void SetWorkTime(DateTimeOffset startDate, double hours) 
        {
            // поменялась дата? смещаем
            if (this.start != startDate)
                this.start = startDate;
            // и пересчитываем часы, если поменялись
            if (hours != 0)
                this.end = ((DateTimeOffset)this.start).AddHours(Math.Round(hours, 2));
            WorkTime = (DateTimeOffset)this.end - (DateTimeOffset)this.start;
        }
    }
}
