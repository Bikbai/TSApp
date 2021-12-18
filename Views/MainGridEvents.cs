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
        private void PaintUnbindedRows(object sender, GridUnBoundRowEventsArgs e)
        {

            int index = e.RowColumnIndex.ColumnIndex;
            int rowindex = e.RowColumnIndex.RowIndex;
            if (e.UnBoundAction == UnBoundActions.QueryData)
            {
                if (rowindex == 1)
                {
                    if (index == 2)
                    {
                        e.CellTemplate = App.Current.Resources["TopCellTemplate"] as DataTemplate;
                        e.Handled = true;
                    }
                    // суббота и воскресенье - нерабочие дни =)
                    else if (index >= weekColumnId && index < weekColumnId + 7)
                    {
                        e.Value = mdl.WorkItemsModel.GetWorkDayStart(Helpers.DayOfWeekFromRus(index - weekColumnId)).ToString(@"hh\:mm");
                        e.Handled = true;
                    }
                }
                else
                {
                    if (index == 2)
                    {
                        e.CellTemplate = App.Current.Resources["BottomCellTemplate"] as DataTemplate;
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
                var editedValue = e.Value;
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
            if (rowId > 1)
                mdl.WorkTimer.GridEntry = (GridEntry)mainGrid.CurrentItem;

            DateTime chosenDay = Helpers.WeekBoundaries(mdl.WeekNumber, true).AddDays(dayColumn - weekColumnId).Date;

            if (mdl.TimeEntriesModel.Calday == chosenDay)
                return;

            if (dayColumn >= weekColumnId && dayColumn < weekColumnId + 7)
            {
                mdl.TimeEntriesModel.Calday = chosenDay;
                TimeEntryGrid.View.Filter = this.FilterRecords;
                TimeEntryGrid.View.RefreshFilter();
            }
            else
            {
                mdl.TimeEntriesModel.Calday = DateTime.MinValue;
                TimeEntryGrid.View.Filter = null;
                TimeEntryGrid.View.RefreshFilter();
            }

        }

        /// <summary>
        /// Фильтр данных клокифая
        /// </summary>
        /// <param name="args"></param>
        public bool FilterRecords(object o)
        {
            var item = o as TimeEntry;

            if (item != null)
            {
                if (item.Calday.Equals(mdl.TimeEntriesModel.Calday))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Подавление попытки редактировать строку итогов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SuppressRowEdit(object sender, CurrentCellBeginEditEventArgs e)
        {
            var unboundRow = mainGrid.GetUnBoundRow(e.RowColumnIndex.RowIndex);

            if (unboundRow == null)
                return;
            // последняя строка - не редактируется
            if (e.RowColumnIndex.RowIndex > 1 && unboundRow != null)
            {
                e.Cancel = true;
            }            
        }

    }
}
