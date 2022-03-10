using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TSApp.Model;
using TSApp.Behaviors;
using TSApp.StaticData;
using TSApp.ViewModel;
using Newtonsoft.Json;
using Syncfusion.Windows.Shared;

namespace TSApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ChromelessWindow
    {
        private DAL connection;
        private MainFormView mdl;
        const int weekColumnId = 4;
        ParameterForm parameterForm;
        TimeEntryFilter filter = new TimeEntryFilter();
        public MainWindow()
        {
            InitSettings();
            InitializeComponent();
            // инициализируем подключение
            connection = new DAL();            
            mdl = new MainFormView(connection);
            var connectionTask = connection.Init();
            DataContext = mdl;
            connection.InitCompleted += Connection_OnInitComplete;

            mainGrid.QueryUnBoundRow += QueryUnboundRow;
            mainGrid.CurrentCellRequestNavigate += OpenWorkItemLink;
            mainGrid.CurrentCellBeginEdit += DenyTotalsEdit;

            mainGrid.UnBoundRowCellRenderers.Add("TimeSpanColumn", new UnboundCellTimeSpanRenderer());
            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            TimeEntryGrid.RecordDeleted += TimeEntryGrid_RecordDeleted;
        }

        private void TimeEntryGrid_RecordDeleted(object sender, RecordDeletedEventArgs e)
        {
            e.SelectedIndex = -1;
        }

        private void Connection_OnInitComplete(OnInitCompleteEventArgs args)
        {
            if (connection.ClockifyReady != true || connection.TfsReady != true)
            {
                if (parameterForm == null)
                    parameterForm = new ParameterForm();
                parameterForm = null;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (parameterForm == null)
                parameterForm = new ParameterForm();
            parameterForm = null;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            mdl.Reload();
        }

        private async void button3_Click(object sender, RoutedEventArgs e)
        {           
            TimeEntryGrid.ItemsSource = null;
            TimeEntryGrid.ItemsSource = mdl.TimeEntriesModel.Entries;
            TimeEntryGrid.View.Refresh();
            TimeEntryGrid.View.RefreshFilter();
        }

        private void InitSettings()
        {
            Settings.value.MustInit += Settings_MustInit;
            Settings.Load();
            // TODO сделать сохранение и загрузку рабочего графика
        }

        private void Settings_MustInit(string message)
        {
            MessageBox.Show(message);
            if (parameterForm == null)
                parameterForm = new ParameterForm();
            parameterForm = null;
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            var row = mainGrid.CurrentItem as GridEntry;
            mdl.WorkTimer.StartStop(row);
        }

        private async void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            mdl.Publish();
            TimeEntryGrid.View.Refresh();
            this.mainGrid.View.Refresh();
        }

        private void TimeEntryGrid_Loaded(object sender, RoutedEventArgs e)
        {
            TimeEntryGrid.View.Filter = this.filter.FilterRecords;
        }

        private void window_Closed(object sender, EventArgs e)
        {
            Settings.Save();
        }

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
