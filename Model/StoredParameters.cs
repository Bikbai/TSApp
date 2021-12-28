using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TSApp.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StoredParameters : ObservableObject
    {        
        [JsonProperty]
        private string _apiKey;
        [JsonProperty]
        private string _uri;
        [JsonProperty]
        private string _project;
        [JsonProperty]
        private Dictionary<DayOfWeek, TimeSpan> _dailyStart;
        public string ApiKey { get => _apiKey; set { SetProperty(ref _apiKey, value); } }
        public string CollectionURI { get => _uri; set { SetProperty(ref _uri, value); } }
        public string TeamProjectName { get => _project; set { SetProperty(ref _project, value); } }
        public Dictionary<DayOfWeek, TimeSpan> DailyStart { get => _dailyStart; set { SetProperty(ref _dailyStart, value); } }
        
        public StoredParameters() { }

        public void InitDefault()
        {
            ApiKey = "NTZjNzgzNDUtMjc5ZC00OWM5LTlkNTktNDBiNWI4NGFmNmE5";
            CollectionURI = "http://ztfs-2017.fintech.ru:8080/tfs/Fintech";
            TeamProjectName = "Mir";
            DailyStart = new Dictionary<DayOfWeek, TimeSpan>();
            foreach (DayOfWeek d in Enum.GetValues(typeof(DayOfWeek)))
                DailyStart[d] = TimeSpan.FromHours(10);
        }

        public delegate void MustInitDelegate(string message);
        public event MustInitDelegate MustInit;
        public void MustInitInvoke(string message)
        {
            if (MustInit != null)
                MustInit.Invoke(message);
        }
    }
}
