using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSApp.Model
{
    public static class Storage
    {
        /// <summary>
        /// общее хранилище TimeEntry
        /// </summary>
        private static List<TimeEntry> _timeEntries;
        public static List<TimeEntry> TimeEntries { get => _timeEntries; set => _timeEntries = value; }

        static Storage()
        {
            _timeEntries = new List<TimeEntry>();
        }

    }
}
