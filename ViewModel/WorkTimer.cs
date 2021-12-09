using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Threading;

namespace TSApp.ViewModel
{
    public class WorkTimer: ObservableObject
    {
        private DispatcherTimer timer;
        private TimeSpan started = TimeSpan.Zero;
        GridEntry _ge;

        public string ButtonLabel 
        { 
            get 
            {
                string retVal = "СТАРТ";
                if (Running)
                    retVal = (DateTime.Now.TimeOfDay - started).ToString(@"h\:mm\:ss");
                return retVal;
            }   
        }
        public string GridEntryTitle
        {
            get
            {
                if (_ge == null)
                    return null;
                return _ge.Title;
            }
        }

        public GridEntry GridEntry { set 
            {
                if (!Running)
                {
                    _ge = value;
                    OnPropertyChanged("GridEntryTitle");
                }
            } 
        }

        private bool Running { get => timer.IsEnabled;}
        public WorkTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);// tick every second
            timer.Tick += Timer_Tick;
        }

        public void StartStop(GridEntry ge)
        {
            if (Running)
                Stop();
            else Start(ge);
            OnPropertyChanged("ButtonLabel");
            OnPropertyChanged("GridEntryTitle");
        }

        private void Start (GridEntry ge)
        {
            if (ge == null)
                return;
            if (Running)
                return;
            started = DateTime.Now.TimeOfDay;
            timer.Start();
            _ge  = ge;
            return;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            OnPropertyChanged("ButtonLabel");
        }

        private void Stop()
        {
            if (!Running)
                return;
            timer.Stop();
            _ge = null;
            /// TODO создание по таймеру TimeEntry и всё такое
        }
    }
}
