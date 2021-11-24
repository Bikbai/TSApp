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
        public MainWindow()
        {
            InitializeComponent();
            DataContext = mdl;
            mdl.connection.OnInitComplete += Connection_OnInitComplete;            
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.QueryCoveredRange += MainGrid_QueryCoveredRange;
            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            mainGrid.ItemsSource = mdl.gridModel.GridEntries;
            mainGrid.CurrentCellRequestNavigate += MainGrid_CurrentCellRequestNavigate;
        }

        private void MainGrid_CurrentCellRequestNavigate(object sender, CurrentCellRequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo((e.RowData as GridEntry).Uri)); 
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

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            mdl.Reload();
        }

        private void mainGrid_AddNewRowInitiating(object sender, AddNewRowInitiatingEventArgs e)
        {
            e.NewObject = new GridEntry(true, 10);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (mdl != null && mdl.gridModel != null & mdl.gridModel.GetChangedCount() != 0)
            {
                this.mdl.gridModel.ItemPublished += GridModel_ItemPublished;
                this.longTaskProgress.Visibility = Visibility.Visible;
                this.longTaskProgress.Maximum = mdl.gridModel.GetChangedCount()*10;
                longTaskProgress.Value = 0;
                mdl.Publish();
            }            
        }
        private void GridModel_ItemPublished(bool finished)
        {
            if (finished)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new Action(() =>
                            {
                            this.longTaskProgress.Visibility = Visibility.Hidden;
                                if ((string)tmpLabel.Content == "/")
                                    tmpLabel.Content = ".";
                                else
                                    tmpLabel.Content = "/";
                            }
                            ));
                
                this.mdl.gridModel.ItemPublished -= GridModel_ItemPublished;
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => { 
                                this.longTaskProgress.Value += 10;
                                if ((string)tmpLabel.Content == "/")
                                    tmpLabel.Content = ".";
                                else
                                    tmpLabel.Content = "/";
                            }
                            ));
                    
            }
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

        private void button3_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
