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
        public static UpdateTimeEntryRequest GetUpdateRequest(ClokifyEntry ce)
        {
            var rq = new UpdateTimeEntryRequest();
            rq.Start = ce.Start.ToUniversalTime();
            rq.Description = ce.Description + "// " + ce.Comment;
            rq.End = ce.End.ToUniversalTime();
            rq.ProjectId = ce.ProjectId;
            return rq;
        }
        public static TimeEntryRequest GetCreateRequest(ClokifyEntry ce)
        {
            var rq = GetRequest();
            rq.ID = ce.Id;
            rq.Start = ce.Start.ToUniversalTime();
            rq.Description = ce.Description + "// " + ce.Comment;
            rq.End = ce.End.ToUniversalTime();
            rq.ProjectId = ce.ProjectId;
            return rq;
        }

        public static TimeEntryRequest GetRequest()
        {
            var rq = new TimeEntryRequest();
            rq.UserId = StaticData.UserId;
            rq.WorkspaceId = StaticData.WorkspaceId;
            return rq;
        }
    }
}
