using TSApp.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TSApp.ViewModel
{
    public class MainFormModel : ObservableObject
    {
        public WorkItemViewModel gridModel;
        public DAL connection;
        private string btnCnxnStatusText = "";
        private string btnCnxnStatusForeColor = "";
        private int presentedWeekNumber = Helpers.CurrentWeekNumber();

        public int WeekNumber { get => presentedWeekNumber;}

        public string BtnCnxnStatusText { get => btnCnxnStatusText; set => SetProperty(ref btnCnxnStatusText, value); }
        public string BtnCnxnStatusForeColor { get => btnCnxnStatusForeColor; set => SetProperty(ref btnCnxnStatusForeColor,value); }

        public string LblCurrentWeek { get => "Текущая неделя: " + presentedWeekNumber.ToString(); }

        public MainFormModel()
        {
            connection = new DAL(Settings.Default);
            connection.InitCompleted += InitCompleteHandler;
            var connectionTask = connection.Init();
            gridModel = new WorkItemViewModel(null, connection);
            connection.TimeEntryCreated += gridModel.OnTimeEntryCreateHandler;
            connection.TimeEntryDeleted += gridModel.OnTimeEntryDeleteHandler;
            connection.WorkItemUpdated += gridModel.OnWorkItemUpdatedHandler;
        }

        public void Publish()
        {
         
           var x = gridModel.Publish();
        }

        public async void Reload()
        {
            await ResetAndLoadData();            
        }


        private async void InitCompleteHandler(OnInitCompleteEventArgs args)
        {
            if (connection == null)
                return;
            switch (args.result.Status)
            {
                case CONN_RESULT.OK:
                    BtnCnxnStatusText = "Успешно подключен";                
                    //var x = await gridModel.FetchClokiData();
                    await ResetAndLoadData();
                    break;
                case CONN_RESULT.CONNECTING:
                    BtnCnxnStatusText = "В процессе подключения";
                    break;
                case CONN_RESULT.ERROR:
                    BtnCnxnStatusText = "Ошибка подключения";
                    break;
            }
        }
        /// <summary>
        /// Очистка данных и их полная перезагрузка
        /// </summary>
        /// <returns></returns>
        private async Task ResetAndLoadData()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(gridModel.FetchClokiData());
            tasks.Add(gridModel.FetchTfsData());

            while (tasks.Count > 0)
            {
                // TODO прогресс-бар загрузки надо бы
                Task finishedTask = await Task.WhenAny(tasks);
                tasks.Remove(finishedTask);
            }
            gridModel.FillCurrentWork();
        }
    }
}
