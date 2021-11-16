using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSApp.ViewModel
{
    class MainFormModel : ObservableObject
    {
        public WIDataSource gridModel;
        public ServerConnection connection;
        private string btnCnxnStatusText = "";
        private string btnCnxnStatusForeColor = "";

        public string BtnCnxnStatusText { get => btnCnxnStatusText; set => SetProperty(ref btnCnxnStatusText, value); }
        public string BtnCnxnStatusForeColor { get => btnCnxnStatusForeColor; set => SetProperty(ref btnCnxnStatusForeColor,value); }

        public MainFormModel()
        {
            connection = new ServerConnection(Settings.Default);
            connection.OnInitComplete += Connection_OnInitComplete;
            var connectionTask = connection.Init();
            gridModel = new WIDataSource(connection);
        }

        private void Connection_OnInitComplete(OnInitCompleteEventArgs args)
        {
            switch (args.result.Status)
            {
                case CONN_RESULT.OK:
                    BtnCnxnStatusText = "Успешно подключен";
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
