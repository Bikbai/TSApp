using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TSApp.ViewModel
{
    public class GridEntry : ObservableObject, ICloneable
    {
        private readonly string state; //State
        private readonly int id; //Id
        private string title; //Название
                              // потрачено времени по дням недели
        private double completedWorkMon = 1;
        private double completedWorkTue = 2;
        private double completedWorkWed = 3;
        private double completedWorkThu = 4;
        private double completedWorkFri = 5;
        private double completedWorkSun = 6;
        private double completedWorkSat = 7;
        private string[] commentDaily;
        private int weekNumber;
        public string State => state;
        public int Id => id;
        public string Title {get => title; set => title = value;}
        public string[] CommentDaily { get => commentDaily; set => commentDaily = value; }
        public string CompletedWorkMon { get => completedWorkMon.ToString(); set => completedWorkMon = CalcEntry(value, completedWorkMon); }
        public string CompletedWorkTue { get => completedWorkTue.ToString(); set => completedWorkTue = CalcEntry(value, completedWorkTue); }
        public string CompletedWorkWed { get => completedWorkWed.ToString(); set => completedWorkWed = CalcEntry(value, completedWorkTue); }
        public string CompletedWorkThu { get => completedWorkThu.ToString(); set => completedWorkThu = CalcEntry(value, completedWorkTue); }
        public string CompletedWorkFri { get => completedWorkFri.ToString(); set => completedWorkFri = CalcEntry(value, completedWorkTue); }
        public string CompletedWorkSun { get => completedWorkSun.ToString(); set => completedWorkSun = CalcEntry(value, completedWorkTue); }
        public string CompletedWorkSat { get => completedWorkSat.ToString(); set => completedWorkSat = CalcEntry(value, completedWorkTue); }

        GridEntry _clone;
        enum CMD : int { incr, decr, replace };
        private double CalcEntry(string inputValue, double value)
        {
            bool successEntry = true;
            CMD cmd = CMD.replace;
            double val = 0;
            if (inputValue[0] == '+')
                cmd = CMD.incr;
            else if (inputValue[0] == '-')
                cmd = CMD.decr;
            if (cmd == CMD.replace)
            {
                val = ParserUtility.GetDouble(inputValue, 0, out successEntry);
                return successEntry ? val : value;
            }
            val = ParserUtility.GetDouble(inputValue.Substring(1, inputValue.Length-1), 0, out successEntry);
            val = value + val*(cmd == CMD.decr ? -1 : 1);
            return val < 0 ? 0 : val;
        }

        public GridEntry(string State)
        {
            state = State;
            id = new Random(1000).Next();
            title = "Test item";
            weekNumber = 22;
            commentDaily = new string[7];
        }

        public object Clone()
        {
            if (_clone == null)
                return this.MemberwiseClone();
            return _clone;
        }
    }

    public class WorkItemViewModel : ObservableObject
    {
        public List<GridEntry> _gridEntries;
        private List<TFSWorkItem> _workItems;
        private List<TimeEntry> _timeEntries;
        private TimeSpan[] _defaultWorkdayStart = new TimeSpan[7];
        public WorkItemViewModel()
        {
            for (int i = 0; i < 7; i++) {
                if (Settings.Default.defaultWorkDayStart == null)
                    throw new NotSupportedException("Settings.Default.defaultWorkDayStart is null");
                _defaultWorkdayStart[i] = Settings.Default.defaultWorkDayStart;
            }
            _gridEntries = new List<GridEntry>();
            _gridEntries.Add(new GridEntry("Active"));
            _gridEntries.Add(new GridEntry("Active"));
            _gridEntries.Add(new GridEntry("Resolved"));
            _gridEntries.Add(new GridEntry("Resolved"));
        }

        public List<GridEntry> GridEntries { get => _gridEntries; set => SetProperty(ref _gridEntries, value); }
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
    }
}
