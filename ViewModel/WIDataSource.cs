using Clockify.Net.Models.TimeEntries;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TSApp.ProjectConstans;
using TSApp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TSApp
{
    public class WIDataSource : ObservableCollection<RowItem>
    {
        private ServerConnection tdClient;

        public WIDataSource(ServerConnection Client)
        {
            tdClient = Client;
        }
        public class WIListChangedEventArgs : ListChangedEventArgs
        {
            public int ColumnIndex { get; }

            public WIListChangedEventArgs(int newIndex, int item) : base(ListChangedType.ItemChanged, newIndex)
            {
                ColumnIndex = item;
            }
        }

        public void Swap(int indexA, int indexB)
        {
            var tmp = this[indexA];
            this[indexA] = this[indexB];
            this[indexB] = tmp;
        }

        public async Task Populate()
        {
            this.Clear();
            // синхронно запрашиваем TFS
            var wi = tdClient.QueryMyTasks().Result;
            int idx = 0;
            foreach (var i in wi)
            {
                var item = new RowItem(new TFSWorkItem(i), StaticData.DefaultStartTime);
                item.SortIndex = idx++;
                item.PropertyChanged += Item_PropertyChanged;
                item.EntryValidated += Item_EntryValidated;
                this.Add(item);
                
            }
            return;
            foreach (var item in this)
            {
                await FetchClokiDataAsync(item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));            
        }

        private void Item_EntryValidated(EntryValidatedEventArgs e)
        {
            this.EntryValidatedInvoke(e);
        }

        // метод асинхронной инициализации данных из клокифая
        private async Task FetchClokiDataAsync(RowItem item)
        {
            TimeSpan hours = TimeSpan.Zero;
            DateTimeOffset calday = DateTime.Today;
            DateTime activated = item.Activated == null ? DateTime.Today : ((DateTime)item.Activated).Date;

            var x = await tdClient.FindAllTimeEntriesForUser(item.Id, activated);
            
            if (x == null || x.Data == null)
                return;

            TimeEntryDtoImpl dto = null;
            foreach (var d in x.Data)
            {
                if (d.TimeInterval.End != null && d.TimeInterval.Start != null)
                    // считаем общее время
                    hours = hours + (TimeSpan)(d.TimeInterval.End - d.TimeInterval.Start);
                // вычисляем самую свежую задачу
                if (d.TimeInterval.Start != null && calday < d.TimeInterval.Start)
                {
                    calday = (DateTimeOffset)d.TimeInterval.Start;
                    dto = d;
                }
            }
            // прописываем найденный клоки TE
            if (dto != null)
                item.AppendTimeEntry(new TimeEntry(dto));
            // и прописываем суммарно потраченное время
            if (hours != TimeSpan.Zero)
                item.SetClokiCompletedWork(hours.Hours);
        }
        // ловим изменение строки и выплёвываем наружу событие
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, ((RowItem)sender).SortIndex);
            this.OnCollectionChanged(a);
        }

        public delegate void EntryValidatedDelegate(EntryValidatedEventArgs e);
        public event EntryValidatedDelegate EntryValidated;

        private void EntryValidatedInvoke(EntryValidatedEventArgs e)
        {
            if (EntryValidated != null)
                EntryValidated.Invoke(e);
        }

    }
}
