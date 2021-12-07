using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// если поле пустое, значит новая запись вручную
        /// </summary>
        private string _id;
        private int _workItemId;
        private DateTime _calday;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private TimeSpan _work;
        /// <summary>
        /// Чистый title из tfs workitem
        /// </summary>
        private string _title;
        /// <summary>
        /// Будет записан в клоки после .//
        /// </summary>
        private string _comment;
        private TimeEntry _clone;
        #endregion
        #region properties
        public bool IsChanged
        {
            get { return _clone != null; }
            set 
            { 
                if (_clone == null)
                    _clone = (TimeEntry)this.MemberwiseClone();
                OnPropertyChanged("IsChanged");
            }
        }
        public DateTime Calday { 
            get => _calday;
            set => SetProperty(ref _calday, value);
        }
        public string Title {
            get => _title;
        }
        public string Comment {
            get => _comment;
            set
            {
                IsChanged = true;
                SetProperty(ref _comment, value);
            }
        }
        public string StartTime { 
            get => _startTime.ToString(@"h\:mm");
            set
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > _endTime)
                    newvalue = _endTime;
                SetProperty(ref _startTime, newvalue);
                RecalcFields(FT.StartTime);
            }
        }
        public string EndTime { 
            get => _endTime.ToString(@"h\:mm"); 
            set 
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue < _startTime)
                    newvalue = _startTime;
                SetProperty(ref _endTime, newvalue);
                RecalcFields(FT.EndTime); 
            }
    }
        public string Work { 
            get => _work.ToString(@"h\:mm");
            set
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > TimeSpan.FromHours(12) )
                    newvalue = TimeSpan.FromHours(12);
                SetProperty(ref _work, newvalue);
                RecalcFields(FT.Work);
            }
        }
        public string Id { get => _id; set => _id = value; }
        #endregion
        /// <summary>
        /// Разбираемые форматы ввода: 
        /// h:mm
        /// hmm, hhmm, mm
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tsValue"></param>
        /// <returns></returns>
        private bool ParseTimeEntry(string value, out TimeSpan tsValue)
        {
            tsValue = TimeSpan.Zero;
            // проверяем на число
            if (int.TryParse(value, out int intValue))
            {
                if (intValue < 0)
                    return false;
                // числа из двух знаков - это минуты
                if (intValue < 100)
                {
                    tsValue = TimeSpan.FromMinutes(intValue);
                    return true;
                }
                // числа из трех знаков - часы и минуты (2399)
                if (intValue < 2399)
                {
                    tsValue = new TimeSpan(intValue/100*60, intValue - 100*intValue/100, 0);
                    return true;
                }
            }

            return TimeSpan.TryParseExact(value, @"h\:mm", CultureInfo.InvariantCulture, out tsValue);         
        }

        private void RecalcFields(FT type)
        {
            switch (type)
            {
                case FT.StartTime:
                    _endTime = _startTime + _work;
                    OnPropertyChanged("EndTime");
                    break;
                case FT.EndTime:
                    _work = _endTime - _startTime;
                    OnPropertyChanged("Work");
                    break;
                case FT.Work:
                    _endTime = _startTime + _work;
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
            _id = ce.Id;
            _workItemId = ce.WorkItemId;
            _calday = ce.Calday;
            _startTime = ce.Start.TimeOfDay;
            _endTime = ce.End.TimeOfDay;
            _work = ce.WorkTime;
            _title = ce.Description;
            _comment = ce.Comment;
        }
        /// <summary>
        /// Создание новой пустой записи для Wi
        /// </summary>
        public TimeEntry(GridEntry ge)
        {
            _workItemId = ge.WorkItemId;
            _calday = DateTime.Now;
            _startTime = DateTime.Now.TimeOfDay;
            EndTime = DateTime.Now.TimeOfDay.ToString(@"h\:mm");
            _title = ge.WorkItemId + '.' + ge.Title;
        }

    }
}
