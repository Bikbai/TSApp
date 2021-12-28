using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TSApp.Model;
using TSApp.StaticData;

namespace TSApp.ViewModel
{
    public class WorkItemViewModel : ObservableObject
    {
        #region private fields
        /// <summary>
        /// хранилище задач с привязанными к ним TimeEntry
        /// </summary>
        private BindingList<GridEntry> _gridEntries = new BindingList<GridEntry>();
        private int currentWeekNumber = 0;
        private Dictionary<DayOfWeek, TimeSpan> _defaultWorkdayStart = new Dictionary<DayOfWeek, TimeSpan>();
        #endregion

        public BindingList<GridEntry> GridEntries { get => _gridEntries; } 
        public int CurrentWeekNumber { get => currentWeekNumber; set => currentWeekNumber = value; }
        public DAL Connection { get; set; }

        #region инициализация
        public WorkItemViewModel()
        {
            Init();
            currentWeekNumber = Helpers.CurrentWeekNumber();
        }
        public WorkItemViewModel(int? WeekNumber, DAL conn)
        {            
            Init();
            Connection = conn;
            if (WeekNumber == null)
                currentWeekNumber = Helpers.CurrentWeekNumber();
        }
        private void Init()
        {
            foreach (DayOfWeek i in Enum.GetValues(typeof(DayOfWeek)))
            {
                _defaultWorkdayStart[i] = Settings.value.DailyStart[i];
            }
        }

        public int GetChangedCount() 
        {
            return GridEntries.Count(item => item.IsChanged == true);
        }
        public TimeSpan GetWorkDayStart(DayOfWeek i)
        {
            return _defaultWorkdayStart[i];
        }
        public void SetWorkDayStart(DayOfWeek i, TimeSpan value)
        {
            _defaultWorkdayStart[i] = value;
        }
        #endregion

        /// <summary>
        /// Полная очистка и загрузка задач из TFS
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Fill()
        {
            List<WorkItem> queryResult;
            GridEntries.Clear();
            try
            {
                queryResult = await Connection.QueryTfsTasks();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            
            Dictionary<object, TimeSpan> workDaily = new Dictionary<object, TimeSpan>();
            foreach (var item in queryResult)
            {
                var tItem = new TFSWorkItem(item);
                var ge = new GridEntry(tItem, currentWeekNumber);
                ge.TimeChanged += Ge_TimeChanged;
                GridEntries.Add(ge);
            }
            return true;
        }

        private void Ge_TimeChanged(WITimeChangedEventData td)
        {
            td.caldayStartTime = _defaultWorkdayStart[td.Calday.DayOfWeek];
            OnTimeChanged(td);
        }

       
        #region events
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

        public delegate void TimeChangedDelegate(WITimeChangedEventData td);
        public event TimeChangedDelegate TimeChanged;
        private void OnTimeChanged(WITimeChangedEventData td)
        {
            if (TimeChanged != null)
                TimeChanged.Invoke(td);
        }

        #endregion

        /// <summary>
        /// Обновляем сведения о TFS WI и пересчитываем учтённое время.
        /// </summary>
        /// <param name="wi"></param>
        public void OnWorkItemUpdatedHandler(TFSWorkItem wi)
        {
            var ge = GridEntries.First(p => p.WorkItemId == wi.Id);
            if (ge != null)
                ge.WorkItem = wi;
        }

        public void OnTimeEntryDeleteHandler(DateTime CalDay, int workItemId)
        {
            var ge = GridEntries.First(p => p.WorkItemId == workItemId);
            ge.RemoveTimeEntries(CalDay, workItemId);            
        }
        /// <summary>
        /// Обработчик события создания TE после заливки его в Клоки.
        /// </summary>
        /// <param name="te"></param>
        public void OnTimeEntryCreateHandler(ClokifyEntry te)
        {
        }
        
    }
}
