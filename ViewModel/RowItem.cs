using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using TSApp.Model;
using TSApp.ProjectConstans;

namespace TSApp
{
    public class RowItem : ObservableObject, IComparable
    {
        private DateTimeOffset defaultStartDate;
        private bool initialising = true;
        private int sortIndex = 0;
        private readonly int id;
        private readonly string title;
        private string state; //State
        private readonly DateTime? activated; //ActivateDate
        private string originalEstimate; //OriginalEstimate
        private string remainingWork; //RemainingWork
        private double completedWork; //CompletedWork
        private double enteredCompletedWork = 0; // сколько часов надо учесть
        private double clokiCompletedWork; // сколько часов уже учтено в клоки чохом
        private Dictionary<string, object> oldValues = new Dictionary<string, object>();

        // ссылка на WI в TFS
        private TFSWorkItem wi;
        // изменившиеся поля - нужно для специфики апдейта TFS, он там командой апдейт
        private TFSWorkItem wi_original;
        // содержимое текущего TE в клоки
        private TimeEntry cte;
        // тут дельту хранить смысла нет - нужен новый экземпляр, а меньше cte.workTime не выставишь.
        private bool cteChanged = false;

        public int SortIndex { get => sortIndex; set => sortIndex = value; }
        public int Id => id;
        public string Title => title;
        public string State { get => state; set => state = value; }
        public DateTime? Activated => activated;
        public string OriginalEstimate { get => originalEstimate; set => SetProperty(ref originalEstimate, value); }
        public string RemainingWork { get => remainingWork; set => SetProperty(ref remainingWork, value); }
        public string CompletedWork {
            get 
            {
                StringBuilder sb = new StringBuilder();
                sb.Append((completedWork + enteredCompletedWork).ToString());
                sb.Append(" / ");
                sb.Append(clokiCompletedWork.ToString());
                sb.Append(" / ");
                sb.Append(enteredCompletedWork.ToString());
                return sb.ToString();
            }
            set
            {
                bool deltaMode = false;
                double val = 0;
                if (value[0] == '+' || value[0] == '-')
                {
                   // val = ParserUtility.GetDouble(value.Substring(1), 0);
                    if (value[0] == '-')
                        val = -1 * val;
                    deltaMode = true;
                }
                else
                {
                    deltaMode = false;
                 //   val = ParserUtility.GetDouble(value, 0);
                }
                if (val == 0)
                    return;
                // у нас изменения!
                if (cteChanged == false) {
                    if (cte == null)
                        cte = new TimeEntry(wi.Title, StaticData.DefaultStartTime, StaticData.DefaultStartTime);                    
                    cteChanged = true;
                }
                checkShallowCopy(true);
                // прописываем в изменяемую версию WI новое значение
                // вычисляем новое значение, при этом нельзя уменьшать меньше учтённого за сегодня.

                if (deltaMode)
                    enteredCompletedWork += val;
                else
                    enteredCompletedWork = val - completedWork;
                if (cte.WorkTime.Hours + enteredCompletedWork < 0)
                {
                    enteredCompletedWork = -1 * Math.Abs(cte.WorkTime.Hours);
                    EntryValidatedInvoke(new EntryValidatedEventArgs(SortIndex, "CompletedWork",
                        "Учтённое время не может быть уменьшено более, чем на " + Math.Abs(cte.WorkTime.Hours)));
                }
            }
        }

        public RowItem(TFSWorkItem workItem, DateTimeOffset DefaultStartDate)
        {
            defaultStartDate = DefaultStartDate;
            id = workItem.Id;
            title = workItem.Title;
            state = workItem.State;
            activated = workItem.Activated;
            originalEstimate = workItem.OriginalEstimate.ToString();
            remainingWork = workItem.RemainingWork.ToString();
            completedWork = workItem.CompletedWork;
            // ссылка на оригинал данных TFS
            this.wi = workItem;
            initialising = false;
        }

        void checkShallowCopy(bool CompletedWorkChanged)
        {
            if (this.wi_original == null)
                wi_original = (TFSWorkItem)wi.Clone();
            cteChanged = true;
        }

        // метод для асинхронной подгрузки в модели содержимого клокифая
        public void AppendTimeEntry (TimeEntry te)
        {
            if (cte != null)
                throw new NotSupportedException("AppendTimeEntry: Double TimeEntry initialisation");
            cte = te;
        }
        public void SetClokiCompletedWork(double value)
        {
            SetProperty(ref clokiCompletedWork, value);
        }

        // нужно для спец. сортировки списка WI по-умолчанию через BindingList.Sort()
        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }


        private void RowItem_PropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            if (initialising) return;
            // если оно меняеся - делаем клона оригинала
            var prop = sender.GetType().GetProperty(e.PropertyName);
            if (prop != null && prop.MemberType == System.Reflection.MemberTypes.Property)
                oldValues[e.PropertyName] = prop.GetValue(sender);
        }

        public delegate void EntryValidatedDelegate(EntryValidatedEventArgs e);
        public event EntryValidatedDelegate EntryValidated;

        private void EntryValidatedInvoke(EntryValidatedEventArgs e)
        {
            if (EntryValidated != null)
                EntryValidated.Invoke(e);
        }
    }

    public class EntryValidatedEventArgs : EventArgs
    {
        public int RowIndex { get; }

        public string ColumnName { get; }
        public string Message { get; }

        public EntryValidatedEventArgs(int index, string columnName, string message) : base()
        {
            RowIndex = index;
            Message = message;
            ColumnName = columnName;
        }
    }
}
