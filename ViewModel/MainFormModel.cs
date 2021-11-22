using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TSApp.ViewModel
{
    public class MainFormModel : ObservableObject
    {
        public WorkItemViewModel gridModel;
        public ServerConnection connection;
        private string btnCnxnStatusText = "";
        private string btnCnxnStatusForeColor = "";
        private int presentedWeekNumber = Helpers.CurrentWeekNumber();

        public string BtnCnxnStatusText { get => btnCnxnStatusText; set => SetProperty(ref btnCnxnStatusText, value); }
        public string BtnCnxnStatusForeColor { get => btnCnxnStatusForeColor; set => SetProperty(ref btnCnxnStatusForeColor,value); }

        public string LblCurrentWeek { get => "Текущая неделя: " + presentedWeekNumber.ToString(); }

        public MainFormModel()
        {
            connection = new ServerConnection(Settings.Default);
            connection.OnInitComplete += Connection_OnInitComplete;
            var connectionTask = connection.Init();
            gridModel = new WorkItemViewModel(null);
        }

        public async void Publish()
        {
           var x = await gridModel.Publish(connection);
        }

        private async void Connection_OnInitComplete(OnInitCompleteEventArgs args)
        {
            if (connection == null)
                return;
            switch (args.result.Status)
            {
                case CONN_RESULT.OK:
                    BtnCnxnStatusText = "Успешно подключен";
                    var y = await gridModel.FetchTfsData(connection);                    
                    var work = await gridModel.FillCurrentWork(connection);
                    var x = await gridModel.FetchClokiData(connection);
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
