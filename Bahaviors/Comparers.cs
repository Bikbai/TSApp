using Syncfusion.Windows.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSApp.ViewModel;

namespace TSApp.Bahaviors
{
    public class CustomStateComparer : IComparer<object>, ISortDirection
    {
        public int Compare(object x, object y)
        {
            int nameX;
            int nameY;

            //While data object passed to comparer

            if (x.GetType() == typeof(GridEntry))
            {
                nameX = Helpers.CalcRank(((GridEntry)x).State);
                nameY = Helpers.CalcRank(((GridEntry)y).State);
            }
            if (x.GetType() == typeof(Syncfusion.Data.Group))
            {
                nameX = Helpers.CalcRank(((Syncfusion.Data.Group)x).Key.ToString());
                nameY = Helpers.CalcRank(((Syncfusion.Data.Group)y).Key.ToString());
            }
            else return 0;

            //returns the comparison result based in SortDirection.

            if (nameX > nameY)
                return SortDirection == ListSortDirection.Ascending ? 1 : -1;

            else if (nameX <= nameY)
                return SortDirection == ListSortDirection.Ascending ? -1 : 1;

            else
                return 0;
        }


        private ListSortDirection _SortDirection;

        /// <summary>
        /// Gets or sets the property that denotes the sort direction.
        /// </summary>
        /// <remarks>
        /// SortDirection gets updated only when sorting the groups. For other cases, SortDirection is always ascending.
        /// </remarks>

        public ListSortDirection SortDirection
        {
            get { return _SortDirection; }
            set { _SortDirection = value; }
        }
    }

    public class SortGroupComparers : IComparer<Group>, ISortDirection
    {
        public ListSortDirection SortDirection { get; set; }

        public int Compare(Group x, Group y)
        {
            return 0;
        }
    }
}
