using System;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace TSApp.ViewModel
{
    public class GridEntry : ObservableObject, ICloneable, IComparable
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
        private TimeSpan[] workDaily = { new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0) }; // учтённая трудоемкость по дням недели
        // учтено по клоки, в часах во всех остальных периодах, чохом
        private TimeSpan restTotalWork = TimeSpan.Zero;
        private ObservableCollection<string> commentDaily = new ObservableCollection<string> { "c0", "c1", "c2", "c3", "c4", "c5", "c6" };  // комментарии по дням недели
        private int weekNumber;
        private TFSWorkItem _workItem = new TFSWorkItem();
        public EntryType Type { get; set; } // тип строки

        GridEntry _clone;

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
        public string CompletedWorkMon { get => workDaily[0].TotalHours.ToString("0.00"); set => ParseEntry(value, 0); }
        public string CompletedWorkTue { get => workDaily[1].TotalHours.ToString("0.00"); set => ParseEntry(value, 1); }
        public string CompletedWorkWed { get => workDaily[2].TotalHours.ToString("0.00"); set => ParseEntry(value, 2); }
        public string CompletedWorkThu { get => workDaily[3].TotalHours.ToString("0.00"); set => ParseEntry(value, 3); }
        public string CompletedWorkFri { get => workDaily[4].TotalHours.ToString("0.00"); set => ParseEntry(value, 4); }
        public string CompletedWorkSun { get => workDaily[5].TotalHours.ToString("0.00"); set => ParseEntry(value, 5); }
        public string CompletedWorkSat { get => workDaily[6].TotalHours.ToString("0.00"); set => ParseEntry(value, 6); }
        public int WeekNumber { get => weekNumber; set => weekNumber = value; }
        public double OriginalEstimate { get => originalEstimate; set => originalEstimate = value; }
        public double CompletedWork { get => Math.Round(completedWork, 2); }
        public string Stats { get => completedWork.ToString("0.00"); }
        // итого, посчитанное из суммы текущей недели и предыдущих
        public TimeSpan TotalWork { get => RestTotalWork + Helpers.TimespanSum(workDaily); set { } }
        public bool IsChanged { get => _clone == null ? false : true; }
        public TimeSpan RestTotalWork { get => restTotalWork; set { SetProperty(ref restTotalWork, value); OnPropertyChanged("TotalWork");} }
        #endregion

        #region Constructors
        public GridEntry(string state)
        {
            Type = EntryType.timeEntry;
            this.State = state;
        }

        public GridEntry(TFSWorkItem item, int week)
        {
            Type = EntryType.workItem;
            id = item.Id;
            completedWork = item.CompletedWork;
            state = item.State;
            title = item.Title;
            WeekNumber = week;
            _workItem = item;
        }
        #endregion

        // добавляем рабочие часы, уже учтённые в клокифае к общему времени плюс
        // в текущей неделе - в указанный день
        public void AddWorkByDay(int workDay, double work)
        {
            if (workDay > 7 || work < 0)
                throw new NotSupportedException("AddWorkByDay: workday = " + workDay);
            SetProperty(ref workDaily[workDay], workDaily[workDay] + TimeSpan.FromHours(work));
        }

        public TimeSpan WorkByDay(int workDay)
        {
            return workDaily[workDay];
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
            if (_clone == null)
                _clone = (GridEntry)Clone();

            // текущее значение
            double currentValue = workDaily[dayOfWeek].TotalHours;
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
                    workDaily[dayOfWeek] = TimeSpan.FromHours(val);
                OnPropertyChanged("TotalWork");
                return;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length - 1), 0, out successEntry);
            if (!successEntry)
                return;
            currentValue = currentValue + val * (cmd == CMD.decr ? -1 : 1);
            //            SetProperty(ref workDaily[dayOfWeek], currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue));
            workDaily[dayOfWeek] = currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue);
            OnPropertyChanged("TotalWork");
        }

        public object Clone()
        {
            if (_clone == null)
                return this.MemberwiseClone();
            return _clone;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 0;
            GridEntry item = obj as GridEntry;
            return Helpers.CalcRank(item.State);
        }

        public GridEntry GetUpdated()
        {
            if (state == "Clokify time entry")
                return this;
            return _clone;
        }

        public JsonPatchDocument GetUpdateData()
        {
            JsonPatchDocument result = new JsonPatchDocument();
            TimeSpan totals = _clone.TotalWork;
            for (int i = 0; i < workDaily.Length; i++)
            {
                // если модифицировано время в таймшите
                if (workDaily[i] != _clone.WorkByDay(i))
                {
                    totals += workDaily[i] - _clone.WorkByDay(i);
                    // тут должен был быть комментарий, но он не работает.
                }
            }
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
