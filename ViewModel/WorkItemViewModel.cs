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
        private double[] workDaily = {0,0,0,0,0,0,0};
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
            state = item.State;
            title = item.Title;
            weekNumber = week;
        }
        #endregion

        // функция расчёта нового значения, в зависимости от команды вида
        // -число - вычесть
        // +число - добавить
        // число - присвоение
        // отслеживает создание новой версии
        private double CalcEntry(string inputValue, double value)
        {
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
            if (Helpers.GetWeekNumber(start) == weekNumber)
            {
                workDaily[(int)start.DayOfWeek] += te.WorkTime.Hours;
            }
        }
    }

    public class WorkItemViewModel : ObservableObject
    {
        public BindingList<GridEntry> _gridEntries = new BindingList<GridEntry>();
        private List<TFSWorkItem> _workItems = new List<TFSWorkItem>();
        private List<TimeEntry> _timeEntries = new List<TimeEntry>();
        private TimeSpan[] _defaultWorkdayStart = new TimeSpan[7];
        public WorkItemViewModel()
        {

            for (int i = 0; i < 7; i++) {
                if (Settings.Default.defaultWorkDayStart == null)
                    throw new NotSupportedException("Settings.Default.defaultWorkDayStart is null");
                _defaultWorkdayStart[i] = Settings.Default.defaultWorkDayStart;
            }
        }

        public BindingList<GridEntry> GridEntries { get => _gridEntries; set => SetProperty(ref _gridEntries, value); }
        public List<TFSWorkItem> WorkItems { get => _workItems; set => SetProperty(ref _workItems, value); }
        public List<TimeEntry> TimeEntries { get => _timeEntries; set => SetProperty(ref _timeEntries, value); }
        public TimeSpan GetWorkDayStart(DayOfWeek i)
        {
            return _defaultWorkdayStart[(int)i];
        }
        public void SetWorkDayStart(DayOfWeek i, TimeSpan value)
        {
            _defaultWorkdayStart[(int)i] = value;
        }

        public async Task<bool> Populate(ServerConnection cn)
        {
            var x = await cn.QueryMyTasks();
            Dictionary<object, TimeSpan> workDaily = new Dictionary<object, TimeSpan>();
            foreach (var item in x)
            {
                var tItem = new TFSWorkItem(item);
                var ge = new GridEntry(tItem, Helpers.CurrentWeekNumber());
                GridEntries.Add(ge);
                _workItems.Add(tItem);
//                var w = await GetClokifyTE(cn, ge, workDaily);
            }
            return true;
        } 
        
        public async Task<bool> PopulateClokiData(ServerConnection cn)
        {
            var x = await cn.FindAllTimeEntriesForUser(null, DateTime.Today.AddDays(-30));
            
            if (x == null || x.Data == null)
                return false;
            TimeEntryDtoImpl dto = null;
            foreach (var d in x.Data)
            {
                if (d.TimeInterval.End == null || d.TimeInterval.Start == null)
                    continue;
                _timeEntries.Add(new TimeEntry(d));
            }
            return true;
        }

        private async Task<bool> GetClokifyTE(ServerConnection cn, GridEntry ge, Dictionary<object, TimeSpan> workDaily)
        {
            TimeSpan hours = TimeSpan.Zero;
            DateTimeOffset calday = DateTime.Today;

            DateTime startOfWeek = DateTime.Today.AddDays(-1 * (int)(DateTime.Today.DayOfWeek));

            var x = await cn.FindAllTimeEntriesForUser(ge.Id, DateTime.Today.AddDays(-30));

            if (x == null || x.Data == null)
                return false;

            TimeEntryDtoImpl dto = null;
            foreach (var d in x.Data)
            {
                // задачи без конца пропускаем
                if (d.TimeInterval.End == null || d.TimeInterval.Start == null)
                    continue;
                var start = (DateTimeOffset)d.TimeInterval.Start;
                var end = (DateTimeOffset)d.TimeInterval.End;
                var key = new { calday = start.Date, id = ge.Id};
                if (workDaily.TryGetValue(key, out var value))
                    workDaily[key] = end.Subtract(start) + value;
                else workDaily.Add(key, end.Subtract(start));
            }
            return true;
        }
    }
}
