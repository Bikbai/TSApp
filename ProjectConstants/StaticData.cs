using System;
using System.Collections.Generic;

namespace TSApp.ProjectConstans
{
    public static class StaticData
    {
        private static string projectId;
        private static string userId;
        private static string workspaceId;
        private static TimeSpan defaultStartTime = TimeSpan.FromHours(9);       

        public static string ProjectId => projectId;
        public static string UserId => userId;
        public static string WorkspaceId => workspaceId;

        public static TimeSpan DefaultStartTime => defaultStartTime;

        public static Dictionary<DayOfWeek, TimeSpan> weekTimeTable { get; set; }
        public static void Init(string ProjectId, string UserId, string WorkspaceId)
        {
            projectId = ProjectId; userId = UserId; workspaceId = WorkspaceId;
        }

    }
}
