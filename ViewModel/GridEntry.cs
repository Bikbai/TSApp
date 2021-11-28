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
        public string MondayComment { get => workDaily.TimeData[0].Comment; set => workDaily.TimeData[0].Comment = value; }
        public string TuesdayComment { get => workDaily.TimeData[1].Comment; set => workDaily.TimeData[1].Comment = value; }
        public string WednesdayComment { get => workDaily.TimeData[2].Comment; set => workDaily.TimeData[2].Comment = value; }
        public string ThursdayComment { get => workDaily.TimeData[3].Comment; set => workDaily.TimeData[3].Comment = value; }
        public string FridayComment { get => workDaily.TimeData[4].Comment; set => workDaily.TimeData[4].Comment = value; }
        public string SundayComment { get => workDaily.TimeData[5].Comment; set => workDaily.TimeData[5].Comment = value; }
        public string SaturdayComment { get => workDaily.TimeData[6].Comment; set => workDaily.TimeData[6].Comment = value; }
        #endregion
        #region Работа по дням недели
        public string CompletedWorkMon { get => workDaily.TimeData[0].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 0); }
        public string CompletedWorkTue { get => workDaily.TimeData[1].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 1); }
        public string CompletedWorkWed { get => workDaily.TimeData[2].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 2); }
        public string CompletedWorkThu { get => workDaily.TimeData[3].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 3); }
        public string CompletedWorkFri { get => workDaily.TimeData[4].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 4); }
        public string CompletedWorkSun { get => workDaily.TimeData[5].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 5); }
        public string CompletedWorkSat { get => workDaily.TimeData[6].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 6); }
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
        public void AppendTimeEntry(TimeSpan work, TimeEntry te)
        {
            workDaily.AppendTimeEntry(this.Id, work, te);
        }

        public TimeSpan WorkByDay(int workDay)
        {
            if (workDaily.TimeData == null || workDaily.TimeData[workDay] == null)
                return TimeSpan.Zero;
            return workDaily.TimeData[workDay].Work;
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
        private void ParseEntry(string inputValue, int dayOfWeek)
        {
            if (dayOfWeek > 6)
                throw new NotSupportedException("ParseEntry: dayOfWeek = " + dayOfWeek);

            // текущее значение
            double currentValue = workDaily.TimeData[dayOfWeek].Work.TotalHours;
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
                    workDaily.TimeData[dayOfWeek].Work = TimeSpan.FromHours(val);
                // стреляем событием, чтобы грид пересчитал и тоталсы
                OnPropertyChanged("TotalWork");
                return;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length - 1), 0, out successEntry);
            if (!successEntry)
                return;
            currentValue = currentValue + val * (cmd == CMD.decr ? -1 : 1);
            //            SetProperty(ref workDaily[dayOfWeek], currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue));
            workDaily.TimeData[dayOfWeek].Work = currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue);
            // стреляем событием, чтобы грид пересчитал и тоталсы
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
            for (int i = 0; i < workDaily.TimeData.Length; i++)
            {
                // если модифицировано время в таймшите
                if (workDaily.TimeData[i].OriginalWork != workDaily.TimeData[i].Work)
                {
                    // считаем дельту внесённого времени на всех днях недели
                    delta += workDaily.TimeData[i].Work - workDaily.TimeData[i].OriginalWork;
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

            foreach (var w in workDaily.TimeData)
            {
                if (w.OriginalWork != w.Work)
                    result.Add(w);
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
    }
    
}
