using Clockify.Net.Models.TimeEntries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSApp.ProjectConstans;

namespace TSApp.Model
{
    internal static class TimeEntryRequestFabric
    {
        public static TimeEntryRequest GetRequest(TimeData te)
        {
            var rq = GetRequest();
            DateTimeOffset dt = (te.Calday + StaticData.weekTimeTable[te.DayOfWeek]).ToUniversalTime();
            rq.Start = dt;
            rq.Description = te.WorkItemId.ToString();
            rq.End = dt.AddHours(te.Work.TotalHours).ToUniversalTime();
            return rq;
        }

        public static TimeEntryRequest GetRequest()
        {
            var rq = new TimeEntryRequest();
            rq.ProjectId = StaticData.ProjectId;
            rq.UserId = StaticData.UserId;
            rq.WorkspaceId = StaticData.WorkspaceId;
            return rq;
        }
    }
}
