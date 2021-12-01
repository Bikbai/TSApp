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
        private BindingList<GridEntry> _gridEntries = new BindingList<GridEntry>();
        /// <summary>
        /// общее хранилище TimeEntry
        /// </summary>
        private List<TimeEntry> _timeEntries = new List<TimeEntry>();
        private TimeSpan[] _defaultWorkdayStart = new TimeSpan[7];

        private DAL Connection;

        public BindingList<GridEntry> GridEntries { get => _gridEntries; } //  set => SetProperty(ref _gridEntries, value);
        //        public List<TimeEntry> TimeEntries { get => _timeEntries; set => SetProperty(ref _timeEntries, value); }
        public int CurrentWeekNumber { get => currentWeekNumber; set => currentWeekNumber = value; }

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
            this.ItemPublished += WorkItemViewModel_ItemPublished;
        }

        private void WorkItemViewModel_ItemPublished(bool finished, int workItemId)
        {
        }

        public int GetChangedCount() 
        {
            return GridEntries.Count(item => item.IsChanged == true);
        }
        public TimeSpan GetWorkDayStart(DayOfWeek i)
        {
            return _defaultWorkdayStart[(int)i];
        }
        public void SetWorkDayStart(DayOfWeek i, TimeSpan value)
        {
            _defaultWorkdayStart[(int)i] = value;
        }

        /// <summary>
        /// Полная очистка и загрузка задач из TFS
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FetchTfsData()
        {
            GridEntries.Clear();
            var x = await Connection.QueryTfsTasks();
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
        /// Полная загрузка последних 5000 Time Entry из Клокифая
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FetchClokiData()
        {
            _timeEntries.Clear();
            var x = await Connection.FindAllTimeEntriesForUser(null, null);
            _timeEntries.AddRange(x);
            return true;
        }
        /// <summary>
        /// Служебный метод - получение трудозатрат за день
        /// </summary>
        /// <param name="workDay"></param>
        /// <returns></returns>
        public double GetTotalWork(DayOfWeek workDay)
        {
            TimeSpan retval = TimeSpan.Zero;
            foreach(var w in GridEntries)
            {
                retval += w.WorkByDay(workDay);
            }
            return Math.Round(retval.TotalHours, 2);
        }

        private void FillItemCurrentWork(GridEntry gridEntry)
        {
            //и собираем затраченное время по дням недели
            var workItemTimeEntries = _timeEntries.FindAll(p => p.WorkItemId == gridEntry.Id);
            TimeSpan restWork = TimeSpan.Zero;
            foreach (var te in workItemTimeEntries)
            {
                // учитываем задачи, которые попали в текущую неделю
                DateTime start = ((DateTimeOffset)te.Start).DateTime;
                DateTime end = ((DateTimeOffset)te.End).DateTime;
                if (start >= Helpers.WeekBoundaries(CurrentWeekNumber, true) &&
                    start <= Helpers.WeekBoundaries(CurrentWeekNumber, false))
                {
                    gridEntry.AppendTimeEntry(te);
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

        public bool FillCurrentWork ()
        {
            if (GridEntries == null)
                return false;
            // шуршим по всем timeEntry для каждой задачи  
            foreach (var t in GridEntries)
            {
                FillItemCurrentWork(t);
            }
            // апдейт грида
            this.GridEntries.ResetBindings();
            return true;
        }
        public async Task<bool> Publish()
        {
            List<Task> Workers = new List<Task>();
            bool x = false;
            var changed = GridEntries.Where(p => p.IsChanged == true);

            foreach (GridEntry w in changed)
            {
                if  (w.IsChanged && w.Type == EntryType.workItem)
                {
                    try 
                    {
                        // сначала ждём апдейта клоки
                        var cl =  Connection.UpdateClokiEntries(w.GetClokiUpdateData(), w.Title);
                        // асинхронно передаём в TFS
                        Workers.Add(Connection.UpdateTFSEntry(w.Id, w.GetTfsUpdateData()));
                    }
                    catch (AggregateException ae) { throw ae; }                   
                }
            }
            while(Workers.Count > 0)
            {
                var finishedTask = await Task.WhenAny(Workers);
                OnItemPublished(false, ((Task<int>)finishedTask).Result);
                Workers.Remove(finishedTask);                
            }
            OnItemPublished(true, 0);
            return x;
        }

        public delegate void ItemPublishedDelegate(bool finished, int workItemId);
        /// <summary>
        /// Событие публикации WI в недра TFS, возвращает ID после публикации (finished == false), 
        /// либо 0 если процесс завершен (finished == true)
        /// </summary>
        public event ItemPublishedDelegate ItemPublished;
        /// <summary>
        /// Вызов события публикации WI в недра TFS
        /// </summary>
        /// <param name="finished"></param>
        /// <param name="workItemId">0 если закончена обработка</param>
        private void OnItemPublished(bool finished, int workItemId)
        {
            if (ItemPublished != null)
                ItemPublished.Invoke(finished, workItemId);
        }
        /// <summary>
        /// Обновляем сведения о TFS WI и пересчитываем учтённое время.
        /// </summary>
        /// <param name="wi"></param>
        public void OnWorkItemUpdatedHandler(TFSWorkItem wi)
        {
            var ge = GridEntries.First(p => p.Id == wi.Id);
            if (ge != null)
                ge.WorkItem = wi;
        }

        public void OnTimeEntryDeleteHandler(DateTime CalDay, int workItemId)
        {
            // чистим локальное хранилище
            var cnt = _timeEntries.RemoveAll(p => p.WorkItemId == workItemId && p.Calday == CalDay);
            if (cnt == 0)
                throw new Exception("OnTimeEntryDeleteHandler: Ошибка при очистке хранилища TimeEntry - не найдено записей!");
            var ge = GridEntries.First(p => p.Id == workItemId);
            ge.RemoveTimeEntry(CalDay, workItemId);            
        }
        /// <summary>
        /// Обработчик события создания TE после заливки его в Клоки.
        /// </summary>
        /// <param name="te"></param>
        public void OnTimeEntryCreateHandler(TimeEntry te)
        {
            if (te.WorkTime == TimeSpan.Zero)
                return;
            _timeEntries.Add(te);
            if (te.WorkItemId != -1) 
            {
                var ge = GridEntries.First(p => p.Id == te.WorkItemId);
                if (ge != null)
                {
                    // сначала чистим данные за день - их надо перезаписать полученным единственным TE
                    ge.RemoveTimeEntry(te.Calday, te.WorkItemId);
                    ge.AppendTimeEntry(te);
                }
            }
        }
    }
}
