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
        public static TimeEntryRequest GetRequest(TimeData td)
        {
            var rq = GetRequest();
            DateTimeOffset dt = (td.Calday + StaticData.weekTimeTable[td.DayOfWeek]).ToUniversalTime();
            rq.Start = dt;
            rq.Description = td.WorkItemId.ToString();
            rq.End = dt.AddHours(td.Work.TotalHours).ToUniversalTime();
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
