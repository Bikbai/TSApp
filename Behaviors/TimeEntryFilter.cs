using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSApp.ViewModel;

namespace TSApp.Behaviors
{
    public class TimeEntryFilter
    {
        public TimeEntryFilter()
        {
        }

        public DateTime? Calday { get; set; }
        public int? WorkItemId { get; set; }

        /// <summary>
        /// Фильтр данных клокифая
        /// </summary>
        /// <param name="args"></param>
        public bool FilterRecords(object o)
        {
            var item = o as TimeEntry;
            bool bCd = false;
            bool bWi = false;

            if (item == null)
                return false;
            /// нулевые фильтруем по-умолчанию
            if (item.innerCE.WorkTime == TimeSpan.Zero)
                return false;
            if ((Calday != null && item.Calday.Equals(Calday)) || Calday == null)
                bCd = true;
            if (WorkItemId == null || (WorkItemId != null && item.innerCE.WorkItemId.Equals(WorkItemId)))
                bWi = true;
            return bCd && bWi;
        }

    }
}
