using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private double _remainingWork = 0;

        private string _state = "";
        #endregion

        #region properties
        /// <summary>
        /// Статус/тип. Т.к. в гриде будет микс из задач и просто времени из клоки - вот такая странность
        /// </summary>
        public string State { get => _state; set => SetProperty(ref _state, value); }

        public string RemainingWork {
            get => _remainingWork.ToString("0.00");
            set {
                var val = ParseInput(value, _remainingWork);
                if (val != _remainingWork)
                    SetProperty(ref _remainingWork, val);
                OnPropertyChanged("IsChanged");
            }
           
        }
        public int WorkItemId { get => _workItem.Id;}
        public string Title { get => WorkItemId.ToString() + '.' + _workItem.Title;}
        #region Работа по дням недели
        public string CompletedWorkMon { 
            get => GetTimeData(DayOfWeek.Monday);
            set => ApplyValue(value, DayOfWeek.Monday); }
        public string CompletedWorkTue { 
            get => GetTimeData(DayOfWeek.Tuesday);
            set => ApplyValue(value, DayOfWeek.Tuesday); }
        public string CompletedWorkWed { 
            get => GetTimeData(DayOfWeek.Wednesday);
            set => ApplyValue(value, DayOfWeek.Wednesday); }
        public string CompletedWorkThu { 
            get => GetTimeData(DayOfWeek.Thursday);
            set => ApplyValue(value, DayOfWeek.Thursday); }
        public string CompletedWorkFri { 
            get => GetTimeData(DayOfWeek.Friday);
            set => ApplyValue(value, DayOfWeek.Friday); }
        public string CompletedWorkSun { 
            get => GetTimeData(DayOfWeek.Saturday);
            set => ApplyValue(value, DayOfWeek.Saturday); }
        public string CompletedWorkSat { 
            get => GetTimeData(DayOfWeek.Sunday);
            set => ApplyValue(value, DayOfWeek.Sunday); }
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
        public bool IsChanged { 
            get => workDaily.IsChanged || _workItem.RemainingWork != _remainingWork ; 
            set
            {
                if (value == false)
                    foreach (var item in workDaily.WorkDataDaily)
                    {
                        if (item.Value == null)
                            continue;
                        item.Value.OriginalWork = item.Value.Work;
                        OnPropertyChanged("IsChanged");
                    }
            }
        }
        /// <summary>
        /// Суммарные трудозатраты, учтённые в прочих неделях
        /// </summary>
        public TimeSpan RestTotalWork { get => restTotalWork; set { SetProperty(ref restTotalWork, value); OnPropertyChanged("TotalWork"); } }
        public string Uri { get => _workItem.LinkedWorkItem.Url; }
        public TFSWorkItem WorkItem { get => _workItem; set { SetProperty(ref _workItem, value); OnPropertyChanged("CompletedWork"); } }

        #endregion
        
        #region constructors
        public GridEntry(TFSWorkItem item, int week)
        {
            _remainingWork = item.RemainingWork;
            _workItem = item;
            workDaily = new WeekData(week, item.Id);
            State = item.State;
        }
        #endregion

        #region methods
        /// <summary>
        /// инициализируем рабочие часы, уже учтённые в клокифае 
        /// в текущей неделе - в указанный день
        /// </summary>
        /// <param name="te"></param>
        public void AppendTimeEntry(ClokifyEntry te)
        {
            workDaily.ApplyTimeData(te);
            OnPropertyChanged("IsChanged");
        }
        public TimeSpan WorkByDay(DayOfWeek workDay)
        {
            WorkItemTimeData td;
            if (workDaily.WorkDataDaily == null || workDaily.WorkDataDaily.Count == 0)
                return TimeSpan.Zero;

            if (workDaily.WorkDataDaily.TryGetValue(workDay, out td) && td != null)
                return td.Work;
            else return TimeSpan.Zero; 
        }
        /// <summary>
        /// парсер ввода нового значения типа double
        /// </summary>
        /// <param name="inputValue"></param>
        /// <param name="before"></param>
        /// <returns></returns>
        private double ParseInput(string inputValue, in double before)
        {
            bool successEntry;
            // определяем, какая команда была введена
            CMD cmd = CMD.replace;
            double val = 0;
            if (inputValue.Length == 0)
            {
                return before;
            }
            if (inputValue[0] == '+')
            {
                cmd = CMD.incr;
            }
            else if (inputValue[0] == '-')
            {
                cmd = CMD.decr;
            }

            if (cmd == CMD.replace)
            {
                val = Helpers.GetDouble(inputValue, 0, out successEntry);
                if (successEntry)
                    return val < 0 ? 0 : val;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length - 1), 0, out successEntry);
            if (!successEntry)
                return before;
            val = before + val * (cmd == CMD.decr ? -1 : 1);
            return val < 0 ? 0 : val;
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
        private void ApplyValue(string inputValue, DayOfWeek dayOfWeek)
        {
            // текущее значение
            double currentValue = 0;
            if (workDaily.WorkDataDaily[dayOfWeek] != null)
                currentValue = workDaily.WorkDataDaily[dayOfWeek].Work.TotalHours;
            else
                workDaily.WorkDataDaily[dayOfWeek] = new WorkItemTimeData(Helpers.RusDayNumberFromDayOfWeek(dayOfWeek), TimeSpan.Zero, this._workItem.Id, WeekNumber);

            double val = ParseInput(inputValue, currentValue);

            if (val == currentValue)
                return;

            workDaily.WorkDataDaily[dayOfWeek].Work = TimeSpan.FromHours(val);

            _remainingWork -= (val - currentValue);
            if (_remainingWork < 0) _remainingWork = 0;
            // стреляем событием, чтобы грид пересчитал и тоталсы
            OnPropertyChanged("IsChanged");
            OnPropertyChanged("TotalWork");
            OnPropertyChanged("RemainingWork");
            OnTimeChanged(
                new WITimeChangedEventData(TimeSpan.FromHours(val- currentValue), 
                                           dayOfWeek, 
                                           this.WorkItem,
                                           workDaily.WorkDataDaily[dayOfWeek].Calday));
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 0;
            GridEntry item = obj as GridEntry;
            return Helpers.CalcRank(item.State);
        }
        /// <summary>
        /// Получить изменения в TFS Workitem
        /// </summary>
        /// <returns></returns>
        public JsonPatchDocument GetTfsUpdateData()
        {
            JsonPatchDocument result = new JsonPatchDocument();
            // оригинальное учтённое по клокифай время
            TimeSpan totals = OriginalTotalWork;
            TimeSpan delta = TimeSpan.Zero;
            foreach (var td in workDaily.WorkDataDaily)
            {
                if (td.Value == null) continue;
                // если модифицировано время в таймшите
                if (td.Value.OriginalWork != td.Value.Work)
                {
                    // считаем дельту внесённого времени на всех днях недели
                    delta += td.Value.Work - td.Value.OriginalWork;
                }
            }

            if (_remainingWork != _workItem.RemainingWork)
                result.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Replace,
                    Path = "/fields/" + WIFields.RemainingWork,
                    Value = _remainingWork.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                });

            result.Add(new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + WIFields.CompletedWork,
                Value = Math.Round((totals + delta).TotalHours, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            });
                     
            return result;
        }
        /// <summary>
        /// Очистка учтённых трудоёмкостей в указанном дне
        /// </summary>
        /// <param name="calDay">День</param>
        /// <param name="workItemId">WorkItem</param>
        /// <returns>false, если ничего не получилось</returns>
        public bool RemoveTimeEntries(DateTime calDay, int workItemId)
        {
            if (!workDaily.WorkDataDaily.TryGetValue(calDay.DayOfWeek, out var result))
                return false;
            workDaily.WorkDataDaily[calDay.DayOfWeek] = new WorkItemTimeData(calDay, TimeSpan.Zero, workItemId);
            OnPropertyChanged("IsChanged");
            return true;
        }
        /// <summary>
        /// Хелпер-метод извлечения данных
        /// </summary>
        /// <param name="day">день недели</param>
        /// <param name="needComment">возвращать коммент или форматированное значение трудозатрат</param>
        /// <returns></returns>
        private string GetTimeData(DayOfWeek day)
        {
            if (workDaily.WorkDataDaily == null)
                return "";
            if (workDaily.WorkDataDaily.TryGetValue(day, out WorkItemTimeData td) && td != null)
                return td.Work.TotalHours.ToString("0.00");
            else
                return "";
        }
        #endregion

        #region events
        public delegate void TimeChangedDelegate(WITimeChangedEventData td);
        public event TimeChangedDelegate TimeChanged;
        public void OnTimeChanged(WITimeChangedEventData td)
        {
            if (TimeChanged != null)
                TimeChanged.Invoke(td);
        }
        #endregion

    }

    public class WITimeChangedEventData : EventArgs
    {
        public DateTime Calday;
        public TimeSpan Delta;
        public DayOfWeek WeekNo;
        public TFSWorkItem Wi;
        public TimeSpan caldayStartTime;

        public WITimeChangedEventData(TimeSpan delta, DayOfWeek weekNo, TFSWorkItem wi, DateTime calday)
        {
            Delta = delta;
            WeekNo = weekNo;
            Wi = wi;
            Calday = calday;
        }
    }
}
