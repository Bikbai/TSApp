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
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = mdl;
            mdl.connection.OnInitComplete += Connection_OnInitComplete;
            mainGrid.QueryUnBoundRow += MainGrid_QueryUnBoundRow;
            mainGrid.QueryCoveredRange += MainGrid_QueryCoveredRange;
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
            if (e.UnBoundAction == UnBoundActions.QueryData)
            {
                if (e.RowColumnIndex.ColumnIndex == 3)
                {
                    e.Value = "Начало рабочего дня:";
                    e.Handled = true;
                }

                else if (e.RowColumnIndex.ColumnIndex == 2)
                {
                    e.Value = 10;
                    e.Handled = true;
                }

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
    }
}
