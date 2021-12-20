using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TSApp.ProjectConstans;

namespace TSApp.Model
{
    public class TFSWorkItem : ObservableObject, ICloneable
    {
        private readonly int id; //Id
        private string title; //Название        
        private string state; //State
        private readonly DateTime? activated; //ActivateDate
        private double originalEstimate; //OriginalEstimate
        private double remainingWork; //RemainingWork
        private double completedWork; //CompletedWork
        private List<ClokifyEntry> linkedTimeEntries;
        private WorkItem linkedWorkItem;

        public string ClokiProjectId { get
            {
                string[] s = AreaPath.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length > 2 && StaticData.Projects.TryGetValue(s[1], out var projectId)) {
                    return projectId;
                }
                throw new ArgumentException("ProjectId returns null");
            } 
        }
        public string AreaPath { get; set; }
        public int Id { get => id; }
        public string Title { get => title; set => title = value; }
        public string State { get => state;}
        public DateTime? Activated { get => activated; }
        public double OriginalEstimate { get => originalEstimate; set => SetProperty(ref originalEstimate , value); }
        public double RemainingWork { get => remainingWork; set => SetProperty(ref remainingWork , value); }
        public double CompletedWork { get => completedWork; set => SetProperty(ref completedWork, value); }
        public List<ClokifyEntry> LinkedTimeEntry { get => linkedTimeEntries; set => SetProperty(ref linkedTimeEntries , value); }
        public WorkItem LinkedWorkItem { get => linkedWorkItem; set => SetProperty(ref linkedWorkItem , value); }        
        public TFSWorkItem()
        {
            id = 100500;
            title = "Dummy";
            state = "Active";
            originalEstimate = 100;
            remainingWork = 200;
            completedWork = 50;
        }

        public TFSWorkItem(WorkItem i)
        {
            object outVal;
            if (i.Id == null) id = 0; else id = (int)i.Id;
            if (i.Fields.TryGetValue(WIFields.Title, out outVal)) title = (string)outVal; else title = "EMPTY";
            state = (string)i.Fields[WIFields.State];
            if (i.Fields.TryGetValue(WIFields.ActivateDate, out outVal)) activated = (DateTime)outVal; else activated = null;
            if (i.Fields.TryGetValue(WIFields.OriginalEstimate, out outVal)) originalEstimate = (double)outVal; else originalEstimate = 0;
            if (i.Fields.TryGetValue(WIFields.RemainingWork, out outVal)) remainingWork = (double)outVal; else remainingWork = 0;
            if (i.Fields.TryGetValue(WIFields.CompletedWork, out outVal)) completedWork = (double)outVal; else completedWork = 0;
            if (i.Fields.TryGetValue(WIFields.AreaPath, out outVal)) 
                AreaPath = (string)outVal; 
            else 
                AreaPath = string.Empty;
            linkedWorkItem = i;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }        
    }
}
