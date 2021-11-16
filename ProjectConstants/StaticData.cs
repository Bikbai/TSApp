using System;

namespace TSApp.ProjectConstans
{
    public static class StaticData
    {
        private static string projectId;
        private static string userId;
        private static string workspaceId;
        private static DateTimeOffset defaultStartTime; 

        public static string ProjectId => projectId;
        public static string UserId => userId;
        public static string WorkspaceId => workspaceId;

        public static DateTimeOffset DefaultStartTime => defaultStartTime;

        public static void Init(string ProjectId, string UserId, string WorkspaceId, DateTimeOffset DefaultStartTime)
        {
            projectId = ProjectId; userId = UserId; workspaceId = WorkspaceId;
            defaultStartTime = DefaultStartTime;
        }
    }
}
