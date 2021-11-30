using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using TSApp.Model;

namespace TSApp.ViewModel
{
    public class GridEntry : ObservableObject, IComparable
    {
        #region internal variable
        private enum CMD : int { incr, decr, replace };
        // учтено по дням в Клоки
        private WeekData workDaily;  // учтённая трудоемкость по дням недели
        // учтено по клоки, в часах во всех остальных периодах, чохом
        private TimeSpan restTotalWork = TimeSpan.Zero;
        // сведения о TFS
        private TFSWorkItem _workItem = new TFSWorkItem();
        public EntryType Type { get; set; } // тип строки

        private string _state = "";
        #endregion

        #region properties
        /// <summary>
        /// Статус/тип. Т.к. в гриде будет микс из задач и просто времени из клоки - вот такая странность
        /// </summary>
        public string State { get => _state; set => SetProperty(ref _state, value); }
        public int Id { get => _workItem.Id;}
        public string Title { get => Id.ToString() + '.' + _workItem.Title;}
        #region Комментарии по дням недели
        public string MondayComment { 
            get => GetTimeData(DayOfWeek.Monday, true); 
            set => workDaily.TimeDataDaily[DayOfWeek.Monday].Comment = value; 
        }
        public string TuesdayComment {
            get => GetTimeData(DayOfWeek.Wednesday, true);
            set => workDaily.TimeDataDaily[DayOfWeek.Wednesday].Comment = value; 
        }
        public string WednesdayComment { 
            get => GetTimeData(DayOfWeek.Tuesday, true); 
            set => workDaily.TimeDataDaily[DayOfWeek.Tuesday].Comment = value; 
        }
        public string ThursdayComment { 
            get => GetTimeData(DayOfWeek.Thursday, true);
            set => workDaily.TimeDataDaily[DayOfWeek.Thursday].Comment = value; 
        }
        public string FridayComment { 
            get => GetTimeData(DayOfWeek.Friday, true);
            set => workDaily.TimeDataDaily[DayOfWeek.Friday].Comment = value; }
        public string SundayComment { 
            get => GetTimeData(DayOfWeek.Sunday, true);
            set => workDaily.TimeDataDaily[DayOfWeek.Sunday].Comment = value; }
        public string SaturdayComment { 
            get => GetTimeData(DayOfWeek.Saturday, true);
            set => workDaily.TimeDataDaily[DayOfWeek.Saturday].Comment = value; }
        #endregion
        #region Работа по дням недели
        public string CompletedWorkMon { 
            get => GetTimeData(DayOfWeek.Monday, false);
            set => ParseEntry(value, DayOfWeek.Monday); }
        public string CompletedWorkTue { 
            get => GetTimeData(DayOfWeek.Wednesday, false);
            set => ParseEntry(value, DayOfWeek.Wednesday); }
        public string CompletedWorkWed { 
            get => GetTimeData(DayOfWeek.Tuesday, false);
            set => ParseEntry(value, DayOfWeek.Tuesday); }
        public string CompletedWorkThu { 
            get => GetTimeData(DayOfWeek.Thursday, false);
            set => ParseEntry(value, DayOfWeek.Thursday); }
        public string CompletedWorkFri { 
            get => GetTimeData(DayOfWeek.Friday, false);
            set => ParseEntry(value, DayOfWeek.Friday); }
        public string CompletedWorkSun { 
            get => GetTimeData(DayOfWeek.Sunday, false);
            set => ParseEntry(value, DayOfWeek.Sunday); }
        public string CompletedWorkSat { 
            get => GetTimeData(DayOfWeek.Saturday, false);
            set => ParseEntry(value, DayOfWeek.Saturday); }
        #endregion
        public int WeekNumber { get => workDaily.WeekNumber; set => workDaily.WeekNumber = value; }
        public double OriginalEstimate { get => _workItem.OriginalEstimate;}
        public double CompletedWork { get => Math.Round(_workItem.CompletedWork, 2); }
        public string Stats { get => CompletedWork.ToString("0.00"); }
        /// <summary>
        /// Суммарные трудозатраты с учётом модификации
        /// </summary>
        public TimeSpan TotalWork { get => RestTotalWork + workDaily.GetTotalWork(); }
        /// <summary>
        /// Суммарные трудозатраты, до модификации
        /// </summary>
        public TimeSpan OriginalTotalWork { get => RestTotalWork + workDaily.GetOriginalTotalWork(); }
        public bool IsChanged { get => workDaily.IsChanged; }
        /// <summary>
        /// Суммарные трудозатраты, учтённые в прочих неделях
        /// </summary>
        public TimeSpan RestTotalWork { get => restTotalWork; set { SetProperty(ref restTotalWork, value); OnPropertyChanged("TotalWork"); } }
        public string Uri { get => _workItem.LinkedWorkItem.Url; }
        #endregion

        #region Constructors
        public GridEntry(bool isTimeEntry, int weekNumber)
        {
            if (!isTimeEntry)
                throw new ArgumentException("Only EntryType.timeEntry supported.", nameof(isTimeEntry));
            Type = EntryType.timeEntry;
            workDaily.WeekNumber = weekNumber;
            this.State = "Clokify";
        }

        public GridEntry(TFSWorkItem item, int week)
        {
            Type = EntryType.workItem;
            _workItem = item;
            workDaily = new WeekData(week, item.Id);
            State = item.State;
        }
        #endregion

        // инициализируем рабочие часы, уже учтённые в клокифае
        // в текущей неделе - в указанный день
        public void AppendTimeEntry(TimeEntry te)
        {
            workDaily.AppendTimeEntry(te);
        }

        public TimeSpan WorkByDay(DayOfWeek workDay)
        {
            TimeData td;
            if (workDaily.TimeDataDaily == null || workDaily.TimeDataDaily.Count == 0)
                return TimeSpan.Zero;

            if (workDaily.TimeDataDaily.TryGetValue(workDay, out td))
                return td.Work;
            else return TimeSpan.Zero; 
        }

        /// <summary>
        /// функция ввода нового значения, в зависимости от команды вида
        /// -число - вычесть
        /// +число - добавить
        /// число - присвоение
        /// </summary>
        /// <param name="inputValue"></param>
        /// <param name="dayOfWeek"></param>
        /// <exception cref="NotSupportedException"></exception>
        private void ParseEntry(string inputValue, DayOfWeek dayOfWeek)
        {
            // текущее значение
            TimeData td;
            double currentValue = 0;
            if (workDaily.TimeDataDaily.TryGetValue(dayOfWeek, out td))
                currentValue = td.Work.TotalHours;
            else
                throw new NotSupportedException();
            // определяем, какая команда была введена
            CMD cmd = CMD.replace;
            double val = 0;
            if (inputValue[0] == '+')
            {
                cmd = CMD.incr;
            }
            else if (inputValue[0] == '-')
            {
                cmd = CMD.decr;
            }

            bool successEntry;
            if (cmd == CMD.replace)
            {
                val = Helpers.GetDouble(inputValue, 0, out successEntry);
                if (successEntry)
                    workDaily.TimeDataDaily[dayOfWeek].Work = TimeSpan.FromHours(val);
                // стреляем событием, чтобы грид пересчитал и тоталсы
                OnPropertyChanged("TotalWork");
                OnPropertyChanged("IsChanged");
                return;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length - 1), 0, out successEntry);
            if (!successEntry)
                return;
            currentValue = currentValue + val * (cmd == CMD.decr ? -1 : 1);
            //            SetProperty(ref workDaily[dayOfWeek], currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue));
            workDaily.TimeDataDaily[dayOfWeek].Work = currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue);
            // стреляем событием, чтобы грид пересчитал и тоталсы
            OnPropertyChanged("IsChanged");
            OnPropertyChanged("TotalWork");
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 0;
            GridEntry item = obj as GridEntry;
            return Helpers.CalcRank(item.State);
        }

        public JsonPatchDocument GetTfsUpdateData()
        {
            JsonPatchDocument result = new JsonPatchDocument();
            // оригинальное учтённое по клокифай время
            TimeSpan totals = OriginalTotalWork;
            double remnWork = _workItem.RemainingWork;
            TimeSpan delta = TimeSpan.Zero;
            foreach (var td in workDaily.TimeDataDaily)
            {
                // если модифицировано время в таймшите
                if (td.Value.OriginalWork != td.Value.Work)
                {
                    // считаем дельту внесённого времени на всех днях недели
                    delta += td.Value.Work - td.Value.OriginalWork;
                }
            }
            remnWork = Math.Round(remnWork - delta.TotalHours, 2);
            if (remnWork < 0) remnWork = 0;

            result.Add(new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + WIFields.RemainingWork,
                Value = remnWork.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            });

            result.Add(new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + WIFields.CompletedWork,
                Value = Math.Round((totals + delta).TotalHours, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            });
            return result;
        }

        private void PatchBuilder(JsonPatchDocument patch, Operation op, string field, string value)
        {
            patch.Add(new JsonPatchOperation()
            {
                Operation = op,
                Path = "/fields/" + field,
                Value = value
            });
        }

        public List<TimeData> GetClokiUpdateData()
        {
            List<TimeData> result = new List<TimeData>();

            foreach (var w in workDaily.TimeDataDaily)
            {
                if (w.Value.OriginalWork != w.Value.Work)
                    result.Add(w.Value);
            }
            return result;
        }

        public bool RemoveTimeEntry(string timeEntryId)
        {
            var removedWork = workDaily.RemoveTimeEntry(timeEntryId);
            if (removedWork == TimeSpan.Zero)
                return false;
            return true;
        }

        private string GetTimeData(DayOfWeek day, bool needComment)
        {
            TimeData td;
            if (workDaily.TimeDataDaily == null)
                return "";
            if (workDaily.TimeDataDaily.TryGetValue(day, out td))
                return needComment ? td.Comment : td.Work.TotalHours.ToString("0.00");
            else
                return "";
        }
    }
    
}
