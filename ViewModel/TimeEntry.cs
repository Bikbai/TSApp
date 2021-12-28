using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using TSApp.Model;

namespace TSApp.ViewModel
{
    /// <summary>
    /// Класс для отображения данных Клокифая, редактирования и сохранения.
    /// </summary>
    public class TimeEntry : ObservableObject
    {
        #region private fields
        private enum FT { StartTime, EndTime , Work }    
        private ClokifyEntry _clone;
        #endregion
        public readonly ClokifyEntry innerCE;
        #region properties
        public string Id { get => innerCE.Id; }
        public bool IsChanged
        {
            get { return _clone != null; }
            set 
            { 
                if (_clone == null)
                    _clone = (ClokifyEntry)innerCE.Clone();
                OnPropertyChanged("IsChanged");
            }
        }
        public string CaldayString { 
            get => innerCE.Calday.ToShortDateString();
        }
        public DateTime Calday { get => innerCE.Calday; }
        public string Title {
            get => innerCE.Description;
            set => innerCE.Description = value;
        }
        public string Comment {
            get => innerCE.Comment;
            set
            {
                IsChanged = true;
                innerCE.Comment = value;
                OnPropertyChanged("Comment");
            }
        }
        public string StartTime { 
            get => innerCE.Start.TimeOfDay.ToString(@"h\:mm");
            set
            {
                if (!Helpers.ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > innerCE.End.TimeOfDay)
                    newvalue = innerCE.End.TimeOfDay;
                innerCE.Start = innerCE.Calday + newvalue;
                OnPropertyChanged("StartTime");
                RecalcFields(FT.StartTime);
            }
        }
        public string EndTime { 
            get => innerCE.End.TimeOfDay.ToString(@"h\:mm"); 
            set 
            {
                if (!Helpers.ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue < innerCE.Start.TimeOfDay)
                    newvalue = innerCE.Start.TimeOfDay;
                innerCE.End = innerCE.Calday + newvalue;
                OnPropertyChanged("EndTime");
                RecalcFields(FT.EndTime); 
            }
    }
        public string Work { 
            get => innerCE.WorkTime.ToString(@"h\:mm");
            set
            {
                if (!Helpers.ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > TimeSpan.FromHours(12) )
                    newvalue = TimeSpan.FromHours(12);
                innerCE.WorkTime = newvalue;
                OnPropertyChanged("Work");
                RecalcFields(FT.Work);
            }
        }        
        public double WorkDbl { get => innerCE.WorkTime.TotalHours; }
        public double GetWorkDouble() { return innerCE.WorkTime.TotalHours; }
        public TimeSpan GetWorkTimeSpan() { return innerCE.WorkTime; }

        public ClokifyEntry Entry { get => innerCE; }
        #endregion
        private void RecalcFields(FT type)
        {
            switch (type)
            {
                case FT.StartTime:
                    innerCE.End = innerCE.Start + innerCE.WorkTime;
                    OnPropertyChanged("EndTime");
                    break;
                case FT.EndTime:
                    innerCE.WorkTime = innerCE.End - innerCE.Start;
                    OnPropertyChanged("Work");
                    break;
                case FT.Work:
                    innerCE.End = innerCE.Start + innerCE.WorkTime;
                    OnPropertyChanged("EndTime");
                    break;
            }            
        }
        /// <summary>
        /// Создание записи на основе данных из Клоки
        /// </summary>
        /// <param name="ce"></param>
        public TimeEntry(ClokifyEntry ce)
        {
            innerCE = ce;
        }
        public TimeEntry(DateTime calday, DateTime start, TimeSpan work, int workItemId, string title, string projectId)
        {
            innerCE = new ClokifyEntry();
            innerCE.Start = start;
            this.Title = workItemId + "." + title;
            this.Work = work.ToString(@"h\:mm");
            innerCE.WorkItemId = workItemId;
            innerCE.ProjectId = projectId;
        }
    }
}
