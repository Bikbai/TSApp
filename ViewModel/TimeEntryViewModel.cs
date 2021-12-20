using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSApp.Model;

namespace TSApp.ViewModel
{
    public class TimeEntryViewModel : ObservableObject
    {
        private static string CTE = "{\r\n  \"id\": \"61afaee6be737841a55dc648\",\r\n  \"workItemId\": 11716,\r\n  \"workTime\": \"02:00:00\",\r\n  \"start\": \"2021-12-07T10:00:00\",\r\n  \"end\": \"2021-12-07T12:00:00\",\r\n  \"comment\": null\r\n}";
        private DateTime? _date = DateTime.Now.Date;

        private List<TimeEntry> _entries;
        private BindingList<TimeEntry> _BLEntries;

        public DateTime? Calday
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged("CurrentViewedDay");
                OnPropertyChanged("Entries");
            }
        }

        public BindingList<TimeEntry> Entries { 
            get => _BLEntries; }
        public int CurrentWeekNumber { get; set; }
        public string CurrentViewedDay { get
            {
                string retval = "Учтённые данные рабочего времени за: ";
                if (_date != null)
                    return retval + ((DateTime)_date).ToShortDateString();
                else return retval + "весь период.";
            }
        }

        public DAL Connection { get; set; }
        /// <summary>
        /// Конструктор
        /// </summary>       
        public TimeEntryViewModel(DAL conn)
        {
            _entries = new List<TimeEntry>();
            _BLEntries = new BindingList<TimeEntry>(_entries);
            Connection = conn;
        }
        /// <summary>
        /// (Пере)Наполнение модели из коннекта
        /// </summary>
        public async void Fill()
        {
            var x = await Connection.FindAllTimeEntriesForUser(null, DateTime.Now.AddDays(-90));
            this._BLEntries.Clear();
            foreach (var item in x)
            {
                _BLEntries.Add(new TimeEntry(item));
            }
            Entries.ResetBindings();
        }

        /// <summary>
        /// Обработка события изменения времени в таймшите
        /// </summary>
        /// <param name="td"></param>
        public void OnTimeChangedHandler(WITimeChangedEventData td)
        {
            //var wiTE = _entries.Where(p => p.Entry.WorkItemId == td.td.WorkItemId).ToList();
            var totalTE = _entries.Where(p=> p.Calday == td.Calday && p.innerCE.WorkTime != TimeSpan.Zero).ToList();
            // тупо добавляем в начало, если пусто
            if (totalTE.Count() == 0) {
                _entries.Add(new TimeEntry(td.Calday, 
                                           td.Calday.Add(td.caldayStartTime), 
                                           td.Delta, 
                                           td.Wi.Id, 
                                           td.Wi.Title,
                                           td.Wi.ClokiProjectId));
                Entries.ResetBindings();
                return;
            }
            // Ищем последний ТЕ за этот день
            var lastTE = totalTE.OrderByDescending(p => p.innerCE.Start);

            if (td.Delta > TimeSpan.Zero) {
                // если это не по текущей задаче - создаём новый TE на дельту, цепляем к концу
                // комментированные задачи также не увеличиваем
                if (lastTE.First().innerCE.WorkItemId != td.Wi.Id || lastTE.First().innerCE.Comment != String.Empty) {
                    _entries.Add(new TimeEntry(td.Calday, 
                        lastTE.First().Entry.End, 
                        td.Delta, 
                        td.Wi.Id, 
                        td.Wi.Title,
                        td.Wi.ClokiProjectId));
                }
                // Если последняя задача - текущая, то увеличиваем её на дельту
                if (lastTE.First().innerCE.WorkItemId == td.Wi.Id && lastTE.First().innerCE.Comment == String.Empty)
                {
                    lastTE.First().Work = (lastTE.First().Entry.WorkTime+td.Delta).ToString(@"h\:mm");
                }
            } else
            // отрицательная дельта - уменьшаем задачи от последней до первой
            foreach (var lte in lastTE) 
            {
                if (!lte.innerCE.WorkItemId.Equals(td.Wi.Id) || lte.innerCE.WorkTime == TimeSpan.Zero)
                    continue;                
                if (lte.innerCE.WorkTime + td.Delta <= TimeSpan.Zero)
                {
                    td.Delta = td.Delta + lte.innerCE.WorkTime;
                    lte.Work = "0";
                }
                else
                {
                    lte.Work = (lte.innerCE.WorkTime + td.Delta).ToString(@"h\:mm");
                }                
            }
            //_entries = _entries.OrderByDescending(p => p.Calday).ThenBy(p => p.StartTime).ToList();
            Entries.ResetBindings();
            return;

        }

        /// <summary>
        /// Служебный метод - получение трудозатрат за день
        /// </summary>
        /// <param name="workDay"></param>
        /// <returns></returns>
        public double GetTotalWork(DayOfWeek workDay)
        {
            TimeSpan retval = TimeSpan.Zero;
            var calday = Helpers.WeekBoundaries(CurrentWeekNumber, true).AddDays(Helpers.RusDayNumberFromDayOfWeek(workDay));
            foreach (var w in _entries)
            {
                if (w.Calday == calday)
                    retval += w.GetWorkTimeSpan();
            }
            return Math.Round(retval.TotalHours, 2);
        }
        /// <summary>
        /// Получение всех записей времени по идентификатору WI
        /// </summary>
        /// <param name="WorkItemId"></param>
        /// <returns></returns>
        public List<ClokifyEntry> GetCeByWI(int WorkItemId)
        {
            var list = new List<ClokifyEntry>();
            foreach (var item in _entries)
                if (item.Entry.WorkItemId == WorkItemId)
                    list.Add(item.Entry);
            return list;
        }
        /// <summary>
        /// Метод получения списка TE для перезаливки в Клоки
        /// </summary>
        /// <returns></returns>
        public List<TimeEntry> GetChanges()
        {
            List<TimeEntry> result = new List<TimeEntry>();

            foreach (var w in _entries)
            {
                if (w.IsChanged)
                    result.Add(w);
            }
            return result;
        }

    }
}
