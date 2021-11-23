using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clockify.Net.Models.TimeEntries;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Syncfusion.Data;

namespace TSApp.ViewModel
{
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

        public int GetChangedCount() 
        {
            return _gridEntries.Count(item => item.IsChanged == true);
        }
        public TimeSpan GetWorkDayStart(DayOfWeek i)
        {
            return _defaultWorkdayStart[(int)i];
        }
        public void SetWorkDayStart(DayOfWeek i, TimeSpan value)
        {
            _defaultWorkdayStart[(int)i] = value;
        }

        // извлекаем все задачи и наполняем грид и хранилище WI
        public async Task<bool> FetchTfsData(ServerConnection cn)
        {
            var x = await cn.QueryMyTasks();
            Dictionary<object, TimeSpan> workDaily = new Dictionary<object, TimeSpan>();
            foreach (var item in x)
            {
                var tItem = new TFSWorkItem(item);
                _workItems.Add(tItem);
                var ge = new GridEntry(tItem, currentWeekNumber);
                GridEntries.Add(ge);
            }
            return true;
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
            TimeSpan retval = TimeSpan.Zero;
            foreach(var w in _gridEntries)
            {
                retval += w.WorkByDay(workDay);
            }
            return Math.Round(retval.TotalHours, 2);
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
                
                TimeSpan restWork = TimeSpan.Zero;
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
                    else
                    {
                        restWork += end.Subtract(start);
                    }
                    _timeEntries.Add(new TimeEntry(te));
                }
                t.RestTotalWork = restWork;
            }

            return true;
        }
        public async Task<bool> Publish(ServerConnection conn)
        {
            List<Task> Workers = new List<Task>();
            bool x = false;

            foreach (var w in _gridEntries)
            {
                if  (w.IsChanged && w.Type == EntryType.workItem)
                {
                    Workers.Add(conn.UpdateTFSEntry(w.Id, w.GetUpdateData()));
                    // ищем все timeEntry
                    List<TimeEntry> teList = _timeEntries.FindAll(t => t.Id == w.Id.ToString());
                    if (teList.Count > 0)
                        // TODO тут надо вычислить все TimeEntry и радостно пересоздать по новым значениям таймшита.
                    Workers.Add(conn.PublishClokifyData(w.));
                }
            }
            while(Workers.Count > 0)
            {
                var finishedTask = await Task.WhenAny(Workers);
                Workers.Remove(finishedTask);
                ItemPublishedInvoke(false);
            }
            ItemPublishedInvoke(true);
            return x;
        }

        public delegate void ItemPublishedDelegate(bool finished);
        public event ItemPublishedDelegate ItemPublished;

        private void ItemPublishedInvoke(bool finished)
        {
            if (ItemPublished != null)
                ItemPublished.Invoke(finished);
        }

    }
}
