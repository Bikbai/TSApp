using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Windows;
using System.Windows.Input;
using TSApp.Bahaviors;
using TSApp.ViewModel;

namespace TSApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainFormModel mdl = new MainFormModel();
        private WorkItemViewModel WorkItemViewModel = new WorkItemViewModel();
        public MainWindow()
        {
            InitializeComponent();            
            this.DataContext = mdl;
            mdl.connection.OnInitComplete += Connection_OnInitComplete;
            mainGrid.ItemsSource = mdl.gridModel.GridEntries;
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.QueryCoveredRange += MainGrid_QueryCoveredRange;
            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
        }

        #region grid painting
        // схлопываем ячейки первой строки
        private void MainGrid_QueryCoveredRange(object sender, GridQueryCoveredRangeEventArgs e)
        {
            var x = e.RowColumnIndex.ColumnIndex;
            if (e.RowColumnIndex.ColumnIndex < 3)
            {
                e.Handled = true;
                return;
            }
            if (x % 2 != 0)
            {
                e.Range = new CoveredCellInfo(x, x+1, 1, 1);
                e.Handled = true;
            }
        }
        // рисуем первую строку
        private void MainGrid_QueryUnBoundRow(object sender, Syncfusion.UI.Xaml.Grid.GridUnBoundRowEventsArgs e)
        {
            int index = e.RowColumnIndex.ColumnIndex;
            if (e.UnBoundAction == UnBoundActions.QueryData)
            {
                if (index == 2)
                {
                    e.CellTemplate = App.Current.Resources["UnBoundRowCellTemplate"] as DataTemplate;
                    e.Value = "Начало рабочего дня:";
                    e.Handled = true;                    
                }
                // суббота и воскресенье - нерабочие дни =)
                else if (index >= 3 && index % 2 != 0 && index/2 < 7 )
                {
                    int d = index/2 - 1;
                    e.Value = WorkItemViewModel.GetWorkDayStart((DayOfWeek)d);
                    e.Handled = true;
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

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await mdl.gridModel.PopulateClokiData(mdl.connection);
            await mdl.gridModel.Populate(mdl.connection);
            this.mainGrid.UpdateLayout();
        }

        private void mainGrid_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void mainGrid_AddNewRowInitiating(object sender, AddNewRowInitiatingEventArgs e)
        {
            e.NewObject = new GridEntry("Clokify");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
