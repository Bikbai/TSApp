using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using TSApp.Model;

namespace TSApp.ViewModel
{
    public class ParameterModel : ObservableObject
    {
        private DAL connectionInfo;

        private string btnTfsLabel;
        private string btnTfsforeColor;

        private string btnClokiLabel;
        private string btnClokiForeColor;

        private string txtTfsErrorLabel;
        private string txtClokiErrorLabel;

        public string BtnTfsLabel { get => btnTfsLabel; set => SetProperty(ref btnTfsLabel,value); }
        public string BtnTfsForeColor { get => btnTfsforeColor; set => SetProperty(ref btnTfsforeColor, value); }
        public string TxtTfsErrorLabel { get => txtTfsErrorLabel; set => SetProperty(ref txtTfsErrorLabel, value); }
        
        public string BtnClokiLabel { get => btnClokiLabel; set => SetProperty(ref btnClokiLabel, value); }
        public string BtnClokiForeColor { get => btnClokiForeColor; set => SetProperty(ref btnClokiForeColor, value); }

        public string TxtClokiErrorLabel { get => txtClokiErrorLabel; set => SetProperty(ref txtClokiErrorLabel, value); }
        
        public DAL ConnectionInfo { get => connectionInfo;
            set
            {
                connectionInfo = value;
                connectionInfo.InitCompleted += ConnectionInfo_OnInitComplete;
                UpdateLabels();
            }
        }

        public ParameterModel()
        {

        }

        private void ConnectionInfo_OnInitComplete(OnInitCompleteEventArgs args)
        {
            UpdateLabels();
        }

        public bool SaveParameters()
        {
            try
            {
                Settings.Default.Save();
                Settings.Default.Upgrade();
                Settings.Default.Reload();
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }

        public async void BtnCloki_Click(object sender, EventArgs e)
        {
            var x = await ConnectionInfo.PerformClokiConnect();
            if (x == null)
            {
                TxtTfsErrorLabel = "Неизвестная ошибка";
            }
            if (x != null && x.Status != CONN_RESULT.OK)
            {
                TxtClokiErrorLabel = x.ErrorMessage;
            }
            UpdateLabels();
        }

        public async void TestTFSConnection_Click(object sender, EventArgs e)
        {
            var x = await ConnectionInfo.PerformTFSConnect();
            if (x == null)
            {
                TxtTfsErrorLabel = "Неизвестная ошибка";
            }
                if (x != null && x.Status != CONN_RESULT.OK)
            {
                TxtTfsErrorLabel = x.ErrorMessage;
            }            
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (ConnectionInfo != null && ConnectionInfo.TfsReady)
            {
                BtnTfsForeColor = "Color.DarkGreen";
                BtnTfsLabel = "Успешно подключено";
                TxtTfsErrorLabel = "";
            }
            else
            {
                BtnTfsForeColor = "Color.DarkRed";
                BtnTfsLabel = "Проверить подключение";
            }
            if (ConnectionInfo != null && ConnectionInfo.ClockifyReady)
            {
                BtnClokiForeColor = "Color.DarkGreen";
                BtnClokiLabel = "Успешно подключено";
                TxtClokiErrorLabel = "";
            }
            else
            {
                BtnClokiForeColor = "Color.DarkRed";
                BtnClokiLabel = "Проверить подключение";
            }
        }
    }
}
