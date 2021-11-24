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
        private string state; //State
        private int id; //Id
        private string title; //Название                              
        private double originalEstimate = 10;
        // учтено в TFS, в часах
        private double completedWork = 0;
        // учтено по дням в Клоки
        private WeekData workDaily;  // учтённая трудоемкость по дням недели
        // учтено по клоки, в часах во всех остальных периодах, чохом
        private TimeSpan restTotalWork = TimeSpan.Zero;
        private ObservableCollection<string> commentDaily = new ObservableCollection<string> { "c0", "c1", "c2", "c3", "c4", "c5", "c6" };  // комментарии по дням недели
        private TFSWorkItem _workItem = new TFSWorkItem();

        public EntryType Type { get; set; } // тип строки

        #endregion

        #region properties
        public string State
        {
            get
            {
                return state;
            }
            set
            {
                SetProperty(ref state, value);
            }
        }
        public int Id { get => id; set => id = value; }
        public string Title { get => id.ToString() + '.' + title; set => title = value; }
        public ObservableCollection<string> CommentDaily { get => commentDaily; set => commentDaily = value; }
        public string CompletedWorkMon { get => workDaily.TimeData[0].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 0); }
        public string CompletedWorkTue { get => workDaily.TimeData[1].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 1); }
        public string CompletedWorkWed { get => workDaily.TimeData[2].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 2); }
        public string CompletedWorkThu { get => workDaily.TimeData[3].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 3); }
        public string CompletedWorkFri { get => workDaily.TimeData[4].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 4); }
        public string CompletedWorkSun { get => workDaily.TimeData[5].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 5); }
        public string CompletedWorkSat { get => workDaily.TimeData[6].Work.TotalHours.ToString("0.00"); set => ParseEntry(value, 6); }
        public int WeekNumber { get => workDaily.WeekNumber; set => workDaily.WeekNumber = value; }
        public double OriginalEstimate { get => originalEstimate; set => originalEstimate = value; }
        public double CompletedWork { get => Math.Round(completedWork, 2); }
        public string Stats { get => completedWork.ToString("0.00"); }
        // итого, посчитанное из суммы текущей недели и предыдущих
        public TimeSpan TotalWork { get => RestTotalWork + workDaily.getTotalWork(); }
        public TimeSpan OriginalTotalWork { get => RestTotalWork + workDaily.getOriginalTotalWork(); }
        public bool IsChanged { get => workDaily.IsChanged; }
        public TimeSpan RestTotalWork { get => restTotalWork; set { SetProperty(ref restTotalWork, value); OnPropertyChanged("TotalWork"); } }        
        public string Uri { get => _workItem.LinkedWorkItem.Url;}
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
            id = item.Id;
            completedWork = item.CompletedWork;
            state = item.State;
            title = item.Title;
            _workItem = item;
            workDaily = new WeekData(week, item.Id);
        }
        #endregion

        // инициализируем рабочие часы, уже учтённые в клокифае
        // в текущей неделе - в указанный день
        public void InitClokiWork(int workItemId, int workDay, TimeSpan work, TimeEntry te)
        {
            workDaily.InitClokiWorkByDay(workItemId, work, workDay, te);
        }

        public TimeSpan WorkByDay(int workDay)
        {
            if (workDaily.TimeData == null || workDaily.TimeData[workDay] == null) 
                return TimeSpan.Zero;
            return workDaily.TimeData[workDay].Work;
        }

        // функция расчёта нового значения, в зависимости от команды вида
        // -число - вычесть
        // +число - добавить
        // число - присвоение
        // отслеживает создание новой версии
        private void ParseEntry(string inputValue, int dayOfWeek)
        {
            if (dayOfWeek > 6)
                throw new NotSupportedException("ParseEntry: dayOfWeek = " + dayOfWeek);

            // текущее значение
            double currentValue = workDaily.TimeData[dayOfWeek].Work.TotalHours;
            // вычисляем введённую команду
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
                OnPropertyChanged("TotalWork");
                return;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length - 1), 0, out successEntry);
            if (!successEntry)
                return;
            currentValue = currentValue + val * (cmd == CMD.decr ? -1 : 1);
            //            SetProperty(ref workDaily[dayOfWeek], currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue));
            workDaily.TimeData[dayOfWeek].Work = currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue);
            OnPropertyChanged("TotalWork");
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 0;
            GridEntry item = obj as GridEntry;
            return Helpers.CalcRank(item.State);
        }

        public JsonPatchDocument GetUpdateData()
        {
            JsonPatchDocument result = new JsonPatchDocument();
            // оригинальное учтённое по клокифай время
            TimeSpan totals = OriginalTotalWork;
            double remaining = _workItem.RemainingWork;
            for (int i = 0; i < workDaily.TimeData.Length; i++)
            {
                // если модифицировано время в таймшите
                if (workDaily.TimeData[i].OriginalWork != workDaily.TimeData[i].Work )
                {
                    // добавляем к оригинальному - дельту внесённого в таймшит
                    totals += workDaily.TimeData[i].Work - workDaily.TimeData[i].OriginalWork;
                    // вычитаем из оставшейся
                    remaining -= workDaily.TimeData[i].Work.TotalHours + workDaily.TimeData[i].OriginalWork.TotalHours;
                }
            }
            if (remaining < 0) remaining = 0;

            result.Add(new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + WIFields.RemainingWork,
                Value = Math.Round(remaining, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            });
            
            result.Add(new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + WIFields.CompletedWork,
                Value = Math.Round(totals.TotalHours, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
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
    }
}
