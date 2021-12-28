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
        #region Parameters
        public string MondayStart { 
            get => StaticData.Settings.value.DailyStart[DayOfWeek.Monday].ToString(@"h\:mm");
            //set => StaticData.Settings.value.DailyStart[DayOfWeek.Monday] = value;
        }
        #endregion

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
        }

        /// <summary>
        /// Пробрасываем событие изменения времени в TE
        /// </summary>
        /// <param name="td"></param>
        private void WorkItemsModel_TimeChanged(WITimeChangedEventData td)
        {
            TimeEntriesModel.OnTimeChangedHandler(td);            
        }

        private async Task<bool> PublishClokiData() {
            try
            {
                Dictionary<TimeEntry, UpdatedTimeEntry> mlte = new Dictionary<TimeEntry, UpdatedTimeEntry>();
                foreach (var te in TimeEntriesModel.Entries)
                {
                    if (!te.IsChanged)
                        continue;

                    // удаление и добавление реализовано логикой внутри вызванного метода
                    var t = await Connection.UpdateClokiEntry(te.innerCE).ConfigureAwait(true);
                    if (t.Faulted)
                        return false;
                    else
                        mlte.Add(te, t);
                }
                foreach (var mt in mlte)
                {
                    var idx = TimeEntriesModel._entries.IndexOf(mt.Key);
                    if (mt.Value.updated == null) // удаление
                        TimeEntriesModel.Entries.RemoveAt(idx);
                    else {
                        var item = TimeEntriesModel.Entries[idx];
                        item = new TimeEntry(mt.Value.updated);
                        TimeEntriesModel._entries[idx] = item;
                        //var item = TimeEntriesModel.Entries[idx];
                        //Console.WriteLine(item.Title);
                        //TimeEntriesModel.Entries[idx] = new TimeEntry(mt.Value.updated);
                    }
                }

                TimeEntriesModel.Entries.ResetBindings();
                return true;
            }
            catch (Exception ex)
            {
                var m = ex.Message;
                return false;
            }
        }

        private async Task<bool> PublishTfsData()
        {
            GridEntry ge;
            for (int i = 0; i < WorkItemsModel.GridEntries.Count; i++)
            { 
                ge = WorkItemsModel.GridEntries[i];
                if (ge.IsChanged)
                {
                    var wi = await Connection.UpdateTFSEntry(ge.WorkItemId, ge.GetTfsUpdateData());                    
                    if (wi != null) { 
                        WorkItemsModel.GridEntries[i].WorkItem = wi;
                        WorkItemsModel.GridEntries[i].IsChanged = false;
                    }
                    else
                        return false;   
                }
            }
            WorkItemsModel.GridEntries.ResetBindings();
            return true;
        }
        public async void Publish()
        {
            await PublishClokiData().ConfigureAwait(true);
            await PublishTfsData().ConfigureAwait(true);
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
