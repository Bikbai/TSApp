using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clockify.Net.Models.TimeEntries;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Syncfusion.Data;

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
        private double completedWork = 7.5;
        private double[] workDaily = {0,0,0,0,0,0,0}; // учтённая трудоемкость по дням недели
        private string[] commentDaily;  // комментарии по дням недели
        private int weekNumber;

        GridEntry _clone;

        #endregion

        #region properties
        public string State { get {
                return state;
            }
            set
            {
                SetProperty(ref state, value);
            } 
        }
        public int Id {get => id; set => id = value;}
        public string Title {get => title; set => title = value;}
        public string[] CommentDaily { get => commentDaily; set => commentDaily = value; }
        public string CompletedWorkMon { get => workDaily[0].ToString(); set => workDaily[0] = CalcEntry(value, workDaily[0]); }
        public string CompletedWorkTue { get => workDaily[1].ToString(); set => workDaily[1] = CalcEntry(value, workDaily[1]); }
        public string CompletedWorkWed { get => workDaily[2].ToString(); set => workDaily[2] = CalcEntry(value, workDaily[2]); }
        public string CompletedWorkThu { get => workDaily[3].ToString(); set => workDaily[3] = CalcEntry(value, workDaily[3]); }
        public string CompletedWorkFri { get => workDaily[4].ToString(); set => workDaily[4] = CalcEntry(value, workDaily[4]); }
        public string CompletedWorkSun { get => workDaily[5].ToString(); set => workDaily[5] = CalcEntry(value, workDaily[5]); }
        public string CompletedWorkSat { get => workDaily[6].ToString(); set => workDaily[6] = CalcEntry(value, workDaily[6]); }
        public int WeekNumber { get => weekNumber; set => weekNumber = value; }
        public double OriginalEstimate { get => originalEstimate; set => originalEstimate = value; }
        public double CompletedWork { get => completedWork; set => completedWork = value; }
        public string Stats { get => TimeSpan.FromHours(completedWork).ToString(@"hh\:mm"); }

        #endregion

        #region Constructors
        public GridEntry(string state)
        {
            this.State = state;
            id = new Random(1000).Next();
            title = "Test item";
            commentDaily = new string[7];
        }

        public GridEntry(TFSWorkItem item, int week)
        {
            id = item.Id;
            completedWork = item.CompletedWork;
            state = item.State;
            title = item.Title;
            WeekNumber = week;
        }
        #endregion

        public void AddWorkByDay(int workDay, double work)
        {
            if (workDay > 7 || work < 0)
                throw new NotSupportedException("AddWorkByDay: workday = " + workDay);
            SetProperty(ref workDaily[workDay], workDaily[workDay] + work);
        }

        public double WorkByDay(int workDay)
        {
            return workDaily[workDay];
        }

        // функция расчёта нового значения, в зависимости от команды вида
        // -число - вычесть
        // +число - добавить
        // число - присвоение
        // отслеживает создание новой версии
        private double CalcEntry(string inputValue, double value)
        {
            double retval = 0;
            if (_clone == null)
                _clone = (GridEntry)Clone();

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
                return successEntry ? val : value;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length-1), 0, out successEntry);
            val = value + val * (cmd == CMD.decr ? -1 : 1);
            return val < 0 ? 0 : val;
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

        public void AddClokifyHours(TimeEntry te)
        {            
            DateTime start = ((DateTimeOffset)te.Start).DateTime;
            if (start == null)
                return;
            if (Helpers.GetWeekNumber(start) == WeekNumber)
            {
                workDaily[(int)start.DayOfWeek] += te.WorkTime.Hours;
            }
        }
    }

    public class WorkItemViewModel : ObservableObject
    {
        private int currentWeekNumber = 0;
        public BindingList<GridEntry> _gridEntries = new BindingList<GridEntry>();
        private List<TFSWorkItem> _workItems = new List<TFSWorkItem>();
        private List<TimeEntry> _timeEntries = new List<TimeEntry>();
        private TimeSpan[] _defaultWorkdayStart = new TimeSpan[7];
        public WorkItemViewModel(int? WeekNumber)
        {
            for (int i = 0; i < 7; i++) {
                if (Settings.Default.defaultWorkDayStart == null)
                    throw new NotSupportedException("Settings.Default.defaultWorkDayStart is null");
                _defaultWorkdayStart[i] = Settings.Default.defaultWorkDayStart;
            }
            if (WeekNumber == null)
                currentWeekNumber = Helpers.CurrentWeekNumber();
        }

        public BindingList<GridEntry> GridEntries { get => _gridEntries; set => SetProperty(ref _gridEntries, value); }
        public List<TFSWorkItem> WorkItems { get => _workItems; set => SetProperty(ref _workItems, value); }
        public List<TimeEntry> TimeEntries { get => _timeEntries; set => SetProperty(ref _timeEntries, value); }
        public int CurrentWeekNumber { get => currentWeekNumber; set => currentWeekNumber = value; }

        public TimeSpan GetWorkDayStart(DayOfWeek i)
        {
            return _defaultWorkdayStart[(int)i];
        }
        public void SetWorkDayStart(DayOfWeek i, TimeSpan value)
        {
            _defaultWorkdayStart[(int)i] = value;
        }

        public async Task<bool> FetchTfsData(ServerConnection cn)
        {
            var x = await cn.QueryMyTasks();
            Dictionary<object, TimeSpan> workDaily = new Dictionary<object, TimeSpan>();
            foreach (var item in x)
            {
                _workItems.Add(new TFSWorkItem(item));
            }
            PopulateGrid();
            return true;
        } 
        
        private void PopulateGrid()
        {
            foreach(var i in _workItems)
            {
                var ge = new GridEntry(i, currentWeekNumber);
                GridEntries.Add(ge);
            }
        }

        public async Task<bool> FetchClokiData(ServerConnection cn)
        {
            var x = await cn.FindAllTimeEntriesForUser(null, DateTime.Today.AddDays(-30));
            
            if (x == null || x.Data == null)
                return false;
            foreach (TimeEntryDtoImpl d in x.Data)
            {
                // задачи без конца и начала пропускаем
                if (d.TimeInterval.End == null || d.TimeInterval.Start == null)
                    continue;
                _timeEntries.Add(new TimeEntry(d));
            }
            return true;
        }

        public double GetTotalWork(int workDay)
        {
            double retval = 0;
            foreach(var w in _gridEntries)
            {
                retval += w.WorkByDay(workDay);
            }
            return retval;
        }

        public void FillCurrentWork ()
        {
            // шуршим по всем timeEntry для каждой задачи  
            foreach (var t in _gridEntries)
            {
                var teList = _timeEntries.Where(p => p.TaskId == t.Id.ToString());
                //и собираем затраченное время по дням недели
                
                foreach (var te in teList)
                {
                    if (te.Start >= Helpers.WeekBoundaries(CurrentWeekNumber, true) &&
                        te.Start <= Helpers.WeekBoundaries(CurrentWeekNumber, false))
                    {
                        t.AddWorkByDay((int)te.Start.DateTime.DayOfWeek - 1, te.WorkTime.Hours);
                    }
                }
            }
        }
    }
}
