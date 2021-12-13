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
    public class TimeEntryGridVM : ObservableObject
    {
        private static string CTE = "{\r\n  \"id\": \"61afaee6be737841a55dc648\",\r\n  \"workItemId\": 11716,\r\n  \"workTime\": \"02:00:00\",\r\n  \"start\": \"2021-12-07T10:00:00\",\r\n  \"end\": \"2021-12-07T12:00:00\",\r\n  \"comment\": null\r\n}";
        private DateTime _date = DateTime.Now.Date;

        private BindingList<TimeEntry> _entries;

        public BindingList<TimeEntry> Entries { get => _entries; }

        public string CurrentViewedDay { get
            {
                string retval = "Учтённые данные рабочего времени за: ";
                if (_date != DateTime.MinValue)
                    return retval + _date.ToShortDateString();
                else return retval + "весь период.";
            }
        }

        /// <summary>
        /// Конструктор по-умолчанию, создает тестовую запись, не использовать!!
        /// </summary>
        public TimeEntryGridVM()
        {
            ClokifyEntry ce = JsonConvert.DeserializeObject<ClokifyEntry>(CTE);
            _entries = new BindingList<TimeEntry>();
            _entries.Add(new TimeEntry(ce));
        }

        public void Fill()
        {
            _entries.Clear();
            var lst = Storage.TimeEntries.FindAll(p => p.Calday >= DateTime.Now.AddDays(-7));
            foreach (var entry in lst)
            {
                _entries.Add(new TimeEntry(entry));
            }
            //this._entries. ();            
        }

        public DateTime Calday
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged("CurrentViewedDay");
                OnPropertyChanged("Entries");
            }
        }
    }
}
