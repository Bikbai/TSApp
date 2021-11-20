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
        // учтено в TFS, в часах
        private double completedWork = 0;
        // учтено по дням в Клоки
        private TimeSpan[] workDaily = { new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0), new TimeSpan(0) }; // учтённая трудоемкость по дням недели
        // учтено по клоки, в часах
        private double totalWork = 0;
        private ObservableCollection<string> commentDaily = new ObservableCollection<string> {"c0", "c1", "c2", "c3", "c4", "c5", "c6"};  // комментарии по дням недели
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
        public string Title {get => id.ToString() + '.' + title; set => title = value;}
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

        public double TotalWork { get => Math.Round(totalWork, 2); set => SetProperty(ref totalWork, value); }
        #endregion

        #region Constructors
        public GridEntry(string state)
        {
            this.State = state;
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
            SetProperty(ref workDaily[workDay], workDaily[workDay] + TimeSpan.FromHours(work));
            TotalWork = Helpers.TimespanSum(workDaily);
        }

        public double WorkByDay(int workDay)
        {
            return workDaily[workDay].TotalHours;
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
                    SetProperty(ref workDaily[dayOfWeek], TimeSpan.FromHours(val));
                TotalWork = Helpers.TimespanSum(workDaily);
                return;
            }
            val = Helpers.GetDouble(inputValue.Substring(1, inputValue.Length-1), 0, out successEntry);
            if (!successEntry)
                return;
            currentValue = currentValue + val * (cmd == CMD.decr ? -1 : 1);
            //            SetProperty(ref workDaily[dayOfWeek], currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue));
            workDaily[dayOfWeek] = currentValue < 0 ? TimeSpan.FromHours(0) : TimeSpan.FromHours(currentValue);
            TotalWork = Helpers.TimespanSum(workDaily);
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
                workDaily[(int)start.DayOfWeek] += te.WorkTime;
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

        public WorkItemViewModel()
        {
            Init();
            currentWeekNumber = Helpers.CurrentWeekNumber();
        }

        public WorkItemViewModel(int? WeekNumber)
        {
            Init();
            if (WeekNumber == null)
                currentWeekNumber = Helpers.CurrentWeekNumber();
        }

        private void Init()
        {
            for (int i = 0; i < 7; i++)
            {
                if (Settings.Default.defaultWorkDayStart == null)
                    throw new NotSupportedException("Settings.Default.defaultWorkDayStart is null");
                _defaultWorkdayStart[i] = Settings.Default.defaultWorkDayStart;
            }

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
                ge.PropertyChanged += Ge_PropertyChanged;
                GridEntries.Add(ge);
            }
        }

        private void Ge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CompletedWorkMon")
                return;
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
                // парсим название задачи. Добавляем те, которые без задачи
                var te = new TimeEntry(d);
                if (te.Id == null)
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

        public async Task<bool> FillCurrentWork (ServerConnection cn)
        {
            if (_gridEntries == null)
                return false;
            // шуршим по всем timeEntry для каждой задачи  
            foreach (var t in _gridEntries)
            {
                var teList = await cn.FindAllTimeEntriesForUser(t.Id, null).ConfigureAwait(true);
                if (!teList.IsSuccessful)
                    return false;
                //и собираем затраченное время по дням недели
                
                foreach (var te in teList.Data)
                {
                    // скипуем не начатые и не законченные задачи
                    if (te.TimeInterval.Start == null || te.TimeInterval.End == null)
                        continue;
                    // учитываем задачи, которые попали в текущую неделю
                    DateTime start = ((DateTimeOffset)te.TimeInterval.Start).DateTime;
                    DateTime end = ((DateTimeOffset)te.TimeInterval.End).DateTime;
                    if (start >= Helpers.WeekBoundaries(CurrentWeekNumber, true) &&
                        start <= Helpers.WeekBoundaries(CurrentWeekNumber, false))
                    {
                        t.AddWorkByDay((int)start.DayOfWeek - 1, end.Subtract(start).TotalHours);
                    }
                    _timeEntries.Add(new TimeEntry(te));
                }
            }

            return true;
        }
    }
}
