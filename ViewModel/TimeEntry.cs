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
        private string _comment;
        private ClokifyEntry _entry;
        private ClokifyEntry _clone;
        #endregion
        #region properties
        public string Id { get => _entry.Id; }
        public bool IsChanged
        {
            get { return _clone != null; }
            set 
            { 
                if (_clone == null)
                    _clone = (ClokifyEntry)_entry.Clone();
                OnPropertyChanged("IsChanged");
            }
        }
        public string CaldayString { 
            get => _entry.Calday.ToShortDateString();
        }
        public DateTime Calday { get => _entry.Calday; }
        public string Title {
            get => _entry.Description;
            set => _entry.Description = value;
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
            get => _entry.Start.TimeOfDay.ToString(@"h\:mm");
            set
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > _entry.End.TimeOfDay)
                    newvalue = _entry.End.TimeOfDay;
                _entry.Start = _entry.Calday + newvalue;
                OnPropertyChanged("StartTime");
                RecalcFields(FT.StartTime);
            }
        }
        public string EndTime { 
            get => _entry.End.TimeOfDay.ToString(@"h\:mm"); 
            set 
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue < _entry.Start.TimeOfDay)
                    newvalue = _entry.Start.TimeOfDay;
                _entry.End = _entry.Calday + newvalue;
                OnPropertyChanged("EndTime");
                RecalcFields(FT.EndTime); 
            }
    }
        public string Work { 
            get => _entry.WorkTime.ToString(@"h\:mm");
            set
            {
                if (!ParseTimeEntry(value, out TimeSpan newvalue))
                    return;
                IsChanged = true;
                if (newvalue > TimeSpan.FromHours(12) )
                    newvalue = TimeSpan.FromHours(12);
                _entry.WorkTime = newvalue;
                OnPropertyChanged("Work");
                RecalcFields(FT.Work);
            }
        }        
        public double WorkDbl { get => _entry.WorkTime.TotalHours; }
        
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
                    _entry.End = _entry.Start + _entry.WorkTime;
                    OnPropertyChanged("EndTime");
                    break;
                case FT.EndTime:
                    _entry.WorkTime = _entry.End - _entry.Start;
                    OnPropertyChanged("Work");
                    break;
                case FT.Work:
                    _entry.End = _entry.Start + _entry.WorkTime;
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
            _comment = ce.Comment;
            _entry = ce;
        }
        /// <summary>
        /// Создание новой пустой записи для Wi
        /// </summary>
        public TimeEntry(GridEntry ge)
        {
            _entry = new ClokifyEntry();
            _entry.WorkItemId  = ge.WorkItemId;
            _entry.Start = DateTime.Now;
            _entry.End = DateTime.Now;
            _entry.Description = ge.WorkItemId + '.' + ge.Title;
        }

    }
}
