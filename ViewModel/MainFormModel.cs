using TSApp.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;

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
            var y = await gridModel.FetchTfsData();
            var work = await gridModel.FillCurrentWork();
            var x = await gridModel.FetchClokiData();
        }


        private async void InitCompleteHandler(OnInitCompleteEventArgs args)
        {
            if (connection == null)
                return;
            switch (args.result.Status)
            {
                case CONN_RESULT.OK:
                    BtnCnxnStatusText = "Успешно подключен";
                    var y = await gridModel.FetchTfsData();                    
                    var work = await gridModel.FillCurrentWork();                    
                    //var x = await gridModel.FetchClokiData();
                    break;
                case CONN_RESULT.CONNECTING:
                    BtnCnxnStatusText = "В процессе подключения";
                    break;
                case CONN_RESULT.ERROR:
                    BtnCnxnStatusText = "Ошибка подключения";
                    break;
            }
        }
    }
}
