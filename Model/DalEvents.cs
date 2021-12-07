using System;

namespace TSApp.Model
{
    public enum CONN_RESULT { OK = 0, ERROR = 1, CONNECTING = 2 }
    public class OnInitCompleteEventArgs : EventArgs
    {
        public ConnectResult result;
        public OnInitCompleteEventArgs(string message)
        {
            result = new ConnectResult(CONN_RESULT.ERROR, message);
        }
        public OnInitCompleteEventArgs(CONN_RESULT state, string message)
        {
            result = new ConnectResult(CONN_RESULT.ERROR, message);
            result.Status = state;
        }

    }

    public class ConnectResult
    {
        private CONN_RESULT status;
        private string errorMessage;

        public ConnectResult(CONN_RESULT Status, string ErrorMessage)
        {
            status = Status; errorMessage = ErrorMessage;
        }

        public CONN_RESULT Status { get => status; set => status = value; }
        public string ErrorMessage { get => errorMessage; set => errorMessage = value; }
    }
    public partial class DAL
    {
        #region TimeEntryDeleted
        public delegate void TimeEntryDeleteDelegate(DateTime Calday, int WorkItemId);
        /// <summary>
        /// Событие удаления записи из Клоки, вызывается при изменении количества часов на задаче (update = delete + insert)
        /// </summary>
        public event TimeEntryDeleteDelegate TimeEntryDeleted;
        /// <summary>
        /// Вызывает событие удаления
        /// </summary>
        /// <param name="TimeEntryId">Идентификатор записи в Cloki</param>
        public void OnTimeEntryDeleted(DateTime Calday, int WorkItemId)
        {
            if (TimeEntryDeleted != null)
                TimeEntryDeleted.Invoke(Calday, WorkItemId);
        }
        #endregion

        #region TimeEntryCreated
        public delegate void TimeEntryCreateDelegate(ClokifyEntry te);
        /// <summary>
        /// Событие создания новой записи в Клоки
        /// </summary>
        public event TimeEntryCreateDelegate TimeEntryCreated;
        /// <summary>
        /// Событие создания новой записи в Клоки
        /// </summary>
        /// <param name="TimeEntryId">Идентификатор записи в Клоки</param>
        public void OnTimeEntryCreated(ClokifyEntry te)
        {
            if (TimeEntryCreated != null)
                TimeEntryCreated.Invoke(te);
        }
        #endregion
        
        #region InitCompleted
        public delegate void InitCompletedDelegate(OnInitCompleteEventArgs args);
        /// <summary>
        /// Событие завершения инициализации подключений к TFS и Cloki
        /// </summary>
        public event InitCompletedDelegate InitCompleted;

        /// <summary>
        /// Вызов события окончания инициализации
        /// </summary>
        private void OnInitCompleted(CONN_RESULT state, string msg)
        {
            if (InitCompleted != null)
                InitCompleted.Invoke(new OnInitCompleteEventArgs(state, ""));
        }
        #endregion

        #region TFSQueryComplete
        public delegate void TfsQueryCompletedDelegate();
        /// <summary>
        /// Событие окончания асинхронного запроса к TFS
        /// </summary>
        public event TfsQueryCompletedDelegate TfsQueryCompleted;
        /// <summary>
        /// Вызов события окончания чтения задач из TFS
        /// </summary>
        private void OnTfsQueryCompleted()
        {
            if (TfsQueryCompleted != null)
                TfsQueryCompleted.Invoke();
        }
        #endregion

        #region WorkItemUpdated
        public delegate void WorkItemUpdateDelegate(TFSWorkItem wi);
        /// <summary>
        /// Событие успешного апдейта TFS WorkItem
        /// </summary>
        public event WorkItemUpdateDelegate WorkItemUpdated;
        /// <summary>
        /// Событие изменения TFS work item
        /// </summary>
        public void OnWorkItemUpdated(TFSWorkItem wi)
        {
            if (WorkItemUpdated != null)
                WorkItemUpdated.Invoke(wi);
        }
        #endregion
    }
}
