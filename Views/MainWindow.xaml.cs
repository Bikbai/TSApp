using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            mainGrid.DataContext = WorkItemViewModel.GridEntries;
            mainGrid.ItemsSource = WorkItemViewModel.GridEntries;
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.QueryCoveredRange += MainGrid_QueryCoveredRange;
            WorkItemViewModel.PropertyChanged += WorkItemViewModel_PropertyChanged;
        }

        private void WorkItemViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var x = e.PropertyName;
        }

        private void MainGrid_QueryCoveredRange(object sender, GridQueryCoveredRangeEventArgs e)
        {
            var x = e.RowColumnIndex.ColumnIndex;
            if (e.RowColumnIndex.ColumnIndex < 4)
            {
                e.Handled = true;
                return;
            }
            if (x % 2 == 0)
            {
                e.Range = new CoveredCellInfo(x, x+1, 1, 1);
                e.Handled = true;
            }
        }

        private void MainGrid_QueryUnBoundRow(object sender, Syncfusion.UI.Xaml.Grid.GridUnBoundRowEventsArgs e)
        {
            int index = e.RowColumnIndex.ColumnIndex;
            if (e.UnBoundAction == UnBoundActions.QueryData)
            {
                if (index == 3)
                {
                    e.CellTemplate = App.Current.Resources["UnBoundRowCellTemplate"] as DataTemplate;
                    //e.Value = "Начало рабочего дня:";
                    e.Handled = true;                    
                }
                // суббота и воскресенье - нерабочие дни =)
                else if (index >= 4 && index % 2==0 && index/2 < 7 )
                {
                    int d = index/2 - 2;
                    e.Value = WorkItemViewModel.GetWorkDayStart ((DayOfWeek)d);
                    e.Handled = true;
                }

            }
            if (e.UnBoundAction == UnBoundActions.CommitData)
            {
                var editedValue = e.Value;
            }

        }

        private void Connection_OnInitComplete(OnInitCompleteEventArgs args)
        {
            if (mdl.connection.ClockifyReady != true || mdl.connection.TfsReady != true)
            {
                ParameterForm f = new ParameterForm();
                f.ShowDialog();
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
            await mdl.gridModel.Populate().ConfigureAwait(true);
            this.mainGrid.UpdateLayout();
        }

        private void mainGrid_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void mainGrid_AddNewRowInitiating(object sender, AddNewRowInitiatingEventArgs e)
        {
            var data = e.NewObject as RowItem; 
        }
    }
}
