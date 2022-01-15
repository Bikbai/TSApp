using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using System;
using System.Diagnostics;
using System.Windows;
using TSApp.ViewModel;

namespace TSApp
{
    public partial class MainWindow : ChromelessWindow
    {
        #region grid painting
        /// <summary>
        /// Рисуем первую и последнюю строку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryUnboundRow(object sender, GridUnBoundRowEventsArgs e)
        {

            int index = e.RowColumnIndex.ColumnIndex;
            int rowindex = e.RowColumnIndex.RowIndex;
            if (e.UnBoundAction == UnBoundActions.QueryData)
            {
                if (rowindex == 1)
                {
                    if (index == 2)
                    {
                        e.CellTemplate = this.Resources["TopCellTemplate"] as DataTemplate;
                        e.Handled = true;
                    }
                    else if (index >= weekColumnId && index < weekColumnId + 7)
                    {
                        e.CellType = "TimeSpanColumn";
                        e.Value = mdl.WorkItemsModel.GetWorkDayStart(Helpers.DayOfWeekFromRus(index - weekColumnId)).ToString(@"hh\:mm");
                        e.Handled = true;
                    }
                }
                else
                {
                    if (index == 2)
                    {
                        e.CellTemplate = this.Resources["BottomCellTemplate"] as DataTemplate;                        
                        e.Handled = true;
                    }
                    else if (index >= weekColumnId && index < weekColumnId + 7)
                    {
                        e.Value = mdl.TimeEntriesModel.GetTotalWork(Helpers.DayOfWeekFromRus(index - weekColumnId));
                        e.Handled = true;
                    }
                }
            }
            if (e.UnBoundAction == UnBoundActions.CommitData)
            {
                int dayNum = e.RowColumnIndex.ColumnIndex - weekColumnId;
                if (dayNum >= 0 && dayNum <= 6)
                {
                    TimeSpan tsValue;
                    if (Helpers.ParseTimeEntry((string)e.Value, out tsValue))
                    {
                        mdl.WorkItemsModel.SetWorkDayStart(Helpers.DayOfWeekFromRus(dayNum), tsValue);
                        e.Value = tsValue.ToString(@"h\:mm");
                    }
                }
                e.Handled = true;
            }

        }
        #endregion
        /// <summary>
        /// Обработка клика на гиперссылку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenWorkItemLink(object sender, CurrentCellRequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("http://ztfs-2017:8080/tfs/Fintech/Mir/_workitems/edit/" + (e.RowData as GridEntry).WorkItemId));
        }

        private void mainGrid_CurrentCellActivated(object sender, CurrentCellActivatedEventArgs e)
        {
            var dayColumn = e.CurrentRowColumnIndex.ColumnIndex;
            var rowId = e.CurrentRowColumnIndex.RowIndex;
            var ge = (GridEntry)mainGrid.CurrentItem;
            DateTime? chosenDay = null;
            mdl.WorkTimer.GridEntry = ge;

            if (dayColumn >= weekColumnId && dayColumn < weekColumnId + 7)
                chosenDay = Helpers.WeekBoundaries(mdl.WeekNumber, true).AddDays(dayColumn - weekColumnId).Date;
            else
                chosenDay = null;

            mdl.TimeEntriesModel.Calday = chosenDay;

            if (ge == null)
                filter.WorkItemId = null;
            else
                filter.WorkItemId = ge.WorkItemId;
            filter.Calday = chosenDay;
            TimeEntryGrid.View.RefreshFilter();
        }

        /// <summary>
        /// Подавление попытки редактировать строку итогов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DenyTotalsEdit(object sender, CurrentCellBeginEditEventArgs e)
        {
            var unboundRow = mainGrid.GetUnBoundRow(e.RowColumnIndex.RowIndex);

            if (unboundRow == null)
                return;
            // последняя строка - не редактируется
            if (e.RowColumnIndex.RowIndex > 1)
            {
                e.Cancel = true;
            }
        }

    }
}
