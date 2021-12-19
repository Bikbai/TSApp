using TSApp.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace TSApp.ViewModel
{
    public class MainFormModel : ObservableObject
    {
        public DAL Connection { get; set; }
        private string btnCnxnStatusText = "";
        private string btnCnxnStatusForeColor = "";
        private int presentedWeekNumber = Helpers.CurrentWeekNumber();

        public WorkItemViewModel WorkItemsModel { get; set; }
        public TimeEntryViewModel TimeEntriesModel { get; set; }
        public WorkTimer WorkTimer { get; set; }
        public int WeekNumber { get => presentedWeekNumber;}
        public string BtnCnxnStatusText { get => btnCnxnStatusText; set => SetProperty(ref btnCnxnStatusText, value); }
        public string BtnCnxnStatusForeColor { get => btnCnxnStatusForeColor; set => SetProperty(ref btnCnxnStatusForeColor,value); }

        public string LblCurrentWeek { get => "Текущая неделя: " + presentedWeekNumber.ToString(); }

        public MainFormModel(DAL conn)
        {
            Connection = conn;
            this.WorkTimer = new WorkTimer();            
            Connection.InitCompleted += InitCompleteHandler;

            TimeEntriesModel = new TimeEntryViewModel(Connection);
            TimeEntriesModel.CurrentWeekNumber = presentedWeekNumber;
            WorkItemsModel = new WorkItemViewModel(null, Connection);
            WorkItemsModel.TimeChanged += WorkItemsModel_TimeChanged; ;


            Connection.TimeEntryCreated += WorkItemsModel.OnTimeEntryCreateHandler;
            Connection.TimeEntryDeleted += WorkItemsModel.OnTimeEntryDeleteHandler;
            Connection.WorkItemUpdated += WorkItemsModel.OnWorkItemUpdatedHandler;

            TimeEntriesModel.Entries.ListChanged += Entries_ListChanged;
        }

        private void Entries_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            var x = e.ListChangedType;
        }

        private void WorkItemsModel_TimeChanged(WITimeChangedEventData td)
        {
            TimeEntriesModel.OnTimeChangedHandler(td);
            
        }

        public async void Publish()
        {
            /// TODO: прогресс-бар апдейта
            List<Task<UpdatedTimeEntry> > tasks = new List<Task<UpdatedTimeEntry>>();
            foreach (var te in TimeEntriesModel.GetChanges())
            {
                // удаление и добавление реализовано логикой внутри вызванного метода
                tasks.Add(Connection.UpdateClokiEntry(te.Entry));
            }

            while (tasks.Count > 0)
            {                
                var ft = await Task.WhenAny(tasks);
                if (ft.IsFaulted || ft.Result.Faulted)
                    throw new Exception(ft.Result == null ? ft.Exception.Message : ft.Result.Description);                
                tasks.Remove(ft);
                foreach (var te in TimeEntriesModel.Entries)
                {
                    if (te.Entry == ft.Result.timeEntry)
                    {
                        // удаляем старую запись
                        TimeEntriesModel.Entries.Remove(te);
                        // апдейт
                        if (ft.Result.updated != null)
                        {
                            TimeEntriesModel.Entries.Add(new TimeEntry(ft.Result.updated));                            
                        }
                        break;
                    }
                }
            }



        }

        public void Reload()
        {
            ResetAndLoadData();            
        }

        private void InitCompleteHandler(OnInitCompleteEventArgs args)
        {
            if (Connection == null)
                return;
            switch (args.result.Status)
            {
                case CONN_RESULT.OK:
                    BtnCnxnStatusText = "Успешно подключен";                
                    ResetAndLoadData();
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
        private async void ResetAndLoadData()
        {
            // Заполняем TE модель
            TimeEntriesModel.Fill();
            TimeEntriesModel.Entries.ResetBindings();
            // Заполняем WI модель
            await WorkItemsModel.Fill();
            // заполняем WI модель сведениями из Клоки
            // если пустая - уходим, задач нет
            if (WorkItemsModel.GridEntries == null)
                return;
            foreach (var t in WorkItemsModel.GridEntries)
            {
                FillItemCurrentWork(t);
            }

        }

        private void FillItemCurrentWork(GridEntry gridEntry)
        {
            //и собираем затраченное время по дням недели
            var workItemTimeEntries = TimeEntriesModel.GetCeByWI(gridEntry.WorkItemId);
            TimeSpan restWork = TimeSpan.Zero;
            foreach (var te in workItemTimeEntries)
            {
                // учитываем задачи, которые попали в текущую неделю
                DateTime start = ((DateTimeOffset)te.Start).DateTime;
                DateTime end = ((DateTimeOffset)te.End).DateTime;
                if (start >= Helpers.WeekBoundaries(presentedWeekNumber, true) &&
                    start <= Helpers.WeekBoundaries(presentedWeekNumber, false))
                {
                    gridEntry.AppendTimeEntry(te);
                }
                else
                {
                    // не в текущем периоде - в сумму чохом
                    restWork += end.Subtract(start);
                }
            }
            gridEntry.RestTotalWork = restWork;
        }
    }
}
