using Clockify.Net.Models.TimeEntries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSApp.ProjectConstans;

namespace TSApp.Model
{
    internal static class TimeEntryRequestFabric
    {
        public static TimeEntryRequest GetRequest(ClokifyEntry ce)
        {
            var rq = GetRequest();
            rq.ID = ce.Id;
            rq.Start = ce.Start.ToUniversalTime();
            rq.Description = ce.Description;
            rq.End = ce.End.ToUniversalTime();
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
