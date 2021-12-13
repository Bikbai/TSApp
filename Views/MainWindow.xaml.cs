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
using TSApp.ProjectConstans;
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
        private MainFormModel mdl = new MainFormModel();
        const int weekColumnId = 7;
        public MainWindow()
        {
            InitSettings();
            InitializeComponent();
            DataContext = mdl;
            mdl.connection.InitCompleted += Connection_OnInitComplete;            
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            mainGrid.DataContext = mdl.gridModel.GridEntries;
            mainGrid.ItemsSource = mdl.gridModel.GridEntries;
            mdl.gridModel.PropertyChanged += GridModel_PropertyChanged;
            mainGrid.CurrentCellRequestNavigate += MainGrid_CurrentCellRequestNavigate;            
//            btnTimer.DataContext = mdl.workTimer;
//            lblTimer.DataContext = mdl.workTimer;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {

        }

        private void GridModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ManualEntry")
                this.mainGrid.UpdateDataRow(mainGrid.ResolveToRowIndex(mainGrid.CurrentItem));
        }

        /// <summary>
        /// Обработка клика на гиперссылку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainGrid_CurrentCellRequestNavigate(object sender, CurrentCellRequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("http://ztfs-2017:8080/tfs/Fintech/Mir/_workitems/edit/" + (e.RowData as GridEntry).WorkItemId)); 
        }


        #region grid painting
        // рисуем первую и последнюю строку
        private void MainGrid_QueryUnBoundRow(object sender, Syncfusion.UI.Xaml.Grid.GridUnBoundRowEventsArgs e)
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
                        e.Value = mdl.gridModel.GetWorkDayStart(Helpers.DayOfWeekFromRus(index - weekColumnId)).ToString(@"hh\:mm");
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
                        e.Value = mdl.gridModel.GetTotalWork(Helpers.DayOfWeekFromRus(index - weekColumnId));
                        //e.Value = Helpers.DayOfWeekFromRus(index - 6).ToString();
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

        private void Connection_OnInitComplete(OnInitCompleteEventArgs args)
        {
            if (mdl.connection.ClockifyReady != true || mdl.connection.TfsReady != true)
            {
                ParameterForm f = new ParameterForm();
                f.ShowDialog();
            }

            if (mdl.connection.TfsReady)
            {
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ParameterForm f = new ParameterForm();
            f.ShowDialog();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            mdl.Reload();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            mdl.gridModel.Publish();
        }
        private void mainGrid_CurrentCellBeginEdit(object sender, CurrentCellBeginEditEventArgs e)
        {
            var unboundRow = mainGrid.GetUnBoundRow(e.RowColumnIndex.RowIndex);

            if (unboundRow == null) 
                return;
            // последняя строка - не редактируется
            if (unboundRow != null && e.RowColumnIndex.ColumnIndex < 4)
                e.Cancel = true;
        }

        private async void button3_Click(object sender, RoutedEventArgs e)
        {
            var ix = mainGrid.SelectedIndex;
            var row = mainGrid.CurrentItem as GridEntry;
            mdl.gridModel.GridEntries.ResetBindings();
            var x = JsonConvert.SerializeObject(row.WiTimeEntries[0], Formatting.Indented);
        }

        private void InitSettings()
        {
            if (StaticData.weekTimeTable == null)
                StaticData.weekTimeTable = new System.Collections.Generic.Dictionary<DayOfWeek, TimeSpan>();
            foreach (var d in Enum.GetValues(typeof(DayOfWeek)))
                StaticData.weekTimeTable.Add((DayOfWeek)d, TimeSpan.FromHours(10));
            // TODO сделать сохранение и загрузку рабочего графика
        }

        private void DetailsGrid_AddNewRowInitiating(object sender, AddNewRowInitiatingEventArgs e)
        {
            
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            var row = mainGrid.CurrentItem as GridEntry;
            mdl.WorkTimer.StartStop(row);
        }

        private void mainGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
        {
            mdl.WorkTimer.GridEntry = (GridEntry)mainGrid.CurrentItem;
        }

        private void TimeEntryGrid_GroupExpanded(object sender, GroupChangedEventArgs e)
        {

        }

        private void mainGrid_CurrentCellActivated(object sender, CurrentCellActivatedEventArgs e)
        {
            var dayColumn = e.CurrentRowColumnIndex.ColumnIndex;
            DateTime chosenDay = Helpers.WeekBoundaries(mdl.WeekNumber, true).AddDays(dayColumn - 7).Date;

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
    }
}
