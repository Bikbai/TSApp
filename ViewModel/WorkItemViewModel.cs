using Clockify.Net.Models.TimeEntries;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TSApp.Model;

namespace TSApp.ViewModel
{
    public class WorkItemViewModel : ObservableObject
    {
        private int currentWeekNumber = 0;
        /// <summary>
        /// хранилище задач с привязанными к ним TimeEntry
        /// </summary>
        public BindingList<GridEntry> _gridEntries = new BindingList<GridEntry>();
        /// <summary>
        /// общее хранилище TimeEntry
        /// </summary>
        private List<TimeEntry> _timeEntries = new List<TimeEntry>();
        private TimeSpan[] _defaultWorkdayStart = new TimeSpan[7];

        private DAL Connection;

        public WorkItemViewModel()
        {
            Init();
            currentWeekNumber = Helpers.CurrentWeekNumber();
        }

        public WorkItemViewModel(int? WeekNumber, DAL connection)
        {
            Connection = connection;
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
//        public List<TimeEntry> TimeEntries { get => _timeEntries; set => SetProperty(ref _timeEntries, value); }
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
        public async Task<bool> FetchTfsData()
        {
            GridEntries.Clear();
            var x = await Connection.QueryMyTasks();
            Dictionary<object, TimeSpan> workDaily = new Dictionary<object, TimeSpan>();
            foreach (var item in x)
            {
                var tItem = new TFSWorkItem(item);
                var ge = new GridEntry(tItem, currentWeekNumber);
                GridEntries.Add(ge);
            }
            return true;
        } 
        /// <summary>
        /// Наполнение клокифайными данными за последние 30 суток.
        /// единственный метод, работающий с сырыми данными
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FetchClokiData()
        {
            var x = await Connection.FindAllTimeEntriesForUser(null, DateTime.Today.AddDays(-30));
            
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
            TimeSpan retval = TimeSpan.Zero;
            foreach(var w in _gridEntries)
            {
                retval += w.WorkByDay(workDay);
            }
            return Math.Round(retval.TotalHours, 2);
        }

        private async Task FillItemCurrentWork(GridEntry gridEntry)
        {
            var teList = await Connection.FindAllTimeEntriesForUser(gridEntry.Id, null).ConfigureAwait(true);
            if (!teList.IsSuccessful)
                return;
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
                    gridEntry.InitClokiWork((int)start.DayOfWeek - 1, end.Subtract(start), new TimeEntry(te));
                }
                else
                {
                    // не в текущем периоде - в сумму чохом
                    restWork += end.Subtract(start);
                }
            }
            gridEntry.RestTotalWork = restWork;
            return;
        }


        public async Task<bool> FillCurrentWork ()
        {
            if (_gridEntries == null)
                return false;
            // шуршим по всем timeEntry для каждой задачи  
            foreach (var t in _gridEntries)
            {
                await FillItemCurrentWork(t);
            }
            // апдейт грида
            this.GridEntries.ResetBindings();
            return true;
        }
        public async Task<bool> Publish()
        {
            List<Task> Workers = new List<Task>();
            bool x = false;

            foreach (var w in _gridEntries)
            {
                if  (w.IsChanged && w.Type == EntryType.workItem)
                {
                    try 
                    {
                        // сначала ждём апдейта клоки
                        var cl =  await Connection.UpdateClokiEntries(w.GetClokiUpdateData(), w.Title);
                        // потом подтягиваем в TFS и апдейтим грид
                        var tf = await Connection.UpdateTFSEntry(w.Id, w.GetTfsUpdateData()); 
                    }
                    catch (AggregateException ae) { throw ae; }                   
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

        public async void OnWorkItemUpdatedHandler(TFSWorkItem wi)
        {
            var nge = new GridEntry(wi, CurrentWeekNumber);
            await FillItemCurrentWork(nge);

            var ge = GridEntries.Where(p => p.Id == (int)wi.Id).First();
            if (ge != null)
                ge = nge;
            else
                GridEntries.Add(nge);
        }

        public void OnTimeEntryDeleteHandler(string timeEntryId, int workItemId)
        {
            foreach (var te in _timeEntries)
                if (te.Id == timeEntryId)
                    _timeEntries.Remove(te);
            foreach (var wi in GridEntries)
                if (wi.Id == workItemId)
                    wi.RemoveTimeEntry(timeEntryId);
        }

        public void OnTimeEntryCreateHandler(TimeEntry te)
        {
            // TODO OnTimeEntryCreateHandler
            if (te.WorkItemId != -1) 
            {
                var wi = GridEntries.First(p => p.Id == te.WorkItemId);
                if (wi != null)
                    /// TODO туду ёпта запихивания TimeEntry в нужные места
                    /// с переписыванием еще куска работающего кода

                    return;
                    
            }
        }
    }
}
