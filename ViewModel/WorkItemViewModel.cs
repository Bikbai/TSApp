using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TSApp.ViewModel
{
    public class GridEntry : ObservableObject
    {
        private readonly string state; //State
        private readonly int id; //Id
        private readonly string title; //Название        
        private Dictionary<DayOfWeek, double> completedWorkDaily;
        private Dictionary<DayOfWeek, string> commentDaily;
        private int weekNumber;

        public string State => state;
        public int Id => id;
        public string Title => title;
        public Dictionary<DayOfWeek, double> CompletedWorkDaily { get => completedWorkDaily; set => completedWorkDaily = value; }
        public Dictionary<DayOfWeek, string> CommentDaily { get => commentDaily; set => commentDaily = value; }

        public GridEntry(string State)
        {
            state = State;
            id = new Random(1000).Next();
            title = "Test item";
            weekNumber = 22;
            completedWorkDaily = new Dictionary<DayOfWeek, double>() {
                { DayOfWeek.Monday, 1 },
                { DayOfWeek.Tuesday, 2 },
                { DayOfWeek.Wednesday, 3 },
                { DayOfWeek.Thursday, 4 },
                { DayOfWeek.Friday, 5 },
                { DayOfWeek.Saturday, 6 },
                { DayOfWeek.Sunday, 7 },
            };
            commentDaily = new Dictionary<DayOfWeek, string>();
        }
    }

    public class WorkItemViewModel : ObservableObject
    {
        public List<GridEntry> _gridEntries;
        private List<TFSWorkItem> _workItems;
        private List<TimeEntry> _timeEntries;
        public WorkItemViewModel()
        {
            _gridEntries = new List<GridEntry>();
            _gridEntries.Add(new GridEntry("Active"));
            _gridEntries.Add(new GridEntry("Active"));
            _gridEntries.Add(new GridEntry("Resolved"));
            _gridEntries.Add(new GridEntry("Resolved"));
        }

        public List<GridEntry> GridEntries { get => _gridEntries; set => SetProperty(ref _gridEntries, value); }
        public List<TFSWorkItem> WorkItems { get => _workItems; set => SetProperty(ref _workItems, value); }
        public List<TimeEntry> TimeEntries { get => _timeEntries; set => SetProperty(ref _timeEntries, value); }
    }
}
