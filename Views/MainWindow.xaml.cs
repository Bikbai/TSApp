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
        private MainFormModel mdl;
        const int weekColumnId = 4;
        ParameterForm parameterForm;
        TimeEntryFilter filter = new TimeEntryFilter();
        public MainWindow()
        {
            InitSettings();
            InitializeComponent();
            // инициализируем подключение
            connection = new DAL();            
            mdl = new MainFormModel(connection);
            var connectionTask = connection.Init();
            DataContext = mdl;
            connection.InitCompleted += Connection_OnInitComplete;

            mainGrid.QueryUnBoundRow += QueryUnboundRow;
            mainGrid.CurrentCellRequestNavigate += OpenWorkItemLink;
            mainGrid.CurrentCellBeginEdit += DenyTotalsEdit;


            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            mainGrid.DataContext = mdl;
            mainGrid.ItemsSource = mdl.WorkItemsModel.GridEntries;
            mainGrid.UnBoundRowCellRenderers.Add("TimeSpanColumn", new UnboundCellTimeSpanRenderer());

            TimeEntryGrid.DataContext = mdl.TimeEntriesModel;
            TimeEntryGrid.ItemsSource = mdl.TimeEntriesModel.Entries;

            TimeEntryGrid.RecordDeleted += TimeEntryGrid_RecordDeleted;
            
            //            btnTimer.DataContext = mdl.workTimer;
            //            lblTimer.DataContext = mdl.workTimer;
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
    }
}
