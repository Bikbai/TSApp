using System;
using System.Collections.Generic;

namespace TSApp.ProjectConstans
{
    public static class StaticData
    {
        private static Dictionary<string, string> _projects = new Dictionary<string, string>();
        private static string userId;
        private static string workspaceId;
        private static TimeSpan defaultStartTime = TimeSpan.FromHours(9);       

        public static string UserId => userId;
        public static string WorkspaceId => workspaceId;

        public static TimeSpan DefaultStartTime => defaultStartTime;

        public static Dictionary<string, string> Projects { get => _projects; }
        public static Dictionary<DayOfWeek, TimeSpan> weekTimeTable { get; set; }
        public static void Init(string ProjectId, string UserId, string WorkspaceId)
        {
            userId = UserId; 
            workspaceId = WorkspaceId;
            _projects.Add(@"2022", "2022");
            _projects.Add(@"FCOD-M", "ФЦОД-М");
        }

    }
}
