using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using System;
using System.Windows;
using System.Windows.Input;
using TSApp.Bahaviors;
using TSApp.ViewModel;
using TSApp.Views;

namespace TSApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainFormModel mdl = new MainFormModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = mdl;
            mdl.connection.OnInitComplete += Connection_OnInitComplete;
            mainGrid.ItemsSource = mdl.gridModel.GridEntries;
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.QueryCoveredRange += MainGrid_QueryCoveredRange;
            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            mdl.gridModel.GridEntries.ListChanged += GridEntries_ListChanged;
        }

        private void GridEntries_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
        }

        #region grid painting
        // схлопываем ячейки первой строки
        private void MainGrid_QueryCoveredRange(object sender, GridQueryCoveredRangeEventArgs e)
        {
            var x = e.RowColumnIndex.ColumnIndex;
            if (e.RowColumnIndex.ColumnIndex < 5)
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
                    else if (index >= 5 && index % 2 != 0 && index < 14)
                    {
                        int d = index / 2 - 2;
                        e.Value = mdl.gridModel.GetWorkDayStart((DayOfWeek)d).ToString(@"hh\:mm");
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
                    else if (index >= 5 && index % 2 != 0 && index < 14)
                    {
                        int d = index / 2 - 2;
                        e.Value = mdl.gridModel.GetTotalWork(d);
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

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
        }

        private void mainGrid_AddNewRowInitiating(object sender, AddNewRowInitiatingEventArgs e)
        {
            e.NewObject = new GridEntry("Clokify");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void mainGrid_CurrentCellBeginEdit(object sender, CurrentCellBeginEditEventArgs e)
        {
            var unboundRow = mainGrid.GetUnBoundRow(e.RowColumnIndex.RowIndex);

            if (unboundRow == null) 
                return;
            if (unboundRow != null && e.RowColumnIndex.ColumnIndex < 4)
                e.Cancel = true;
        }
    }
}
