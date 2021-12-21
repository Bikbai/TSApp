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
            connection = new DAL(Settings.Default);            
            mdl = new MainFormModel(connection);
            var connectionTask = connection.Init();
            DataContext = mdl;
            connection.InitCompleted += Connection_OnInitComplete;

            mainGrid.QueryUnBoundRow += PaintUnbindedRows;
            mainGrid.CurrentCellRequestNavigate += OpenWorkItemLink;
            mainGrid.CurrentCellBeginEdit += SuppressRowEdit;


            mainGrid.SortComparers.Add(new SortComparer() { Comparer = new CustomStateComparer(), PropertyName = "State" });
            mainGrid.DataContext = mdl.WorkItemsModel.GridEntries;
            mainGrid.ItemsSource = mdl.WorkItemsModel.GridEntries;

            
            //            btnTimer.DataContext = mdl.workTimer;
            //            lblTimer.DataContext = mdl.workTimer;
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
            var ix = mainGrid.SelectedIndex;
            var row = mainGrid.CurrentItem as GridEntry;
            mdl.WorkItemsModel.GridEntries.ResetBindings();
        }

        private void InitSettings()
        {
            if (StaticData.weekTimeTable == null)
                StaticData.weekTimeTable = new System.Collections.Generic.Dictionary<DayOfWeek, TimeSpan>();
            foreach (var d in Enum.GetValues(typeof(DayOfWeek)))
                StaticData.weekTimeTable.Add((DayOfWeek)d, TimeSpan.FromHours(10));
            // TODO сделать сохранение и загрузку рабочего графика
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
    }
}
