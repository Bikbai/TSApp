using Clockify.Net;
using Clockify.Net.Models.TimeEntries;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TSApp.Model;
using TSApp.ProjectConstans;

namespace TSApp.Model
{
    public partial class DAL : ObservableObject
    {
        private bool clockifyReady = false;
        private bool tfsReady = false;
        public string APIkey = Settings.Default.ApiKey;
        public ClockifyClient clockify;
        public VssConnection tfsConnection;

        private string collectionUri = Settings.Default.CollectionURI;
        private string teamProjectName = Settings.Default.teamProjectName;

        public bool ClockifyReady { get => clockifyReady; set => SetProperty(ref clockifyReady, value); }
        public bool TfsReady { get => tfsReady; set => SetProperty(ref tfsReady,value); }

        public DAL(Settings s)
        {
            Settings.Default.SettingsLoaded += SettingsLoaded;
            if (s == null)
                OnInitCompleted(CONN_RESULT.ERROR, "Не настроены параметры подключения.");
        }

        private async void SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            await this.Init();
        }
        
        public async Task<ConnectResult> PerformTFSConnect()
        {
            ConnectResult result = new ConnectResult(CONN_RESULT.OK, "");
            try
            {
                tfsConnection = new VssConnection(new Uri(Settings.Default.CollectionURI), new VssCredentials());
                await tfsConnection.ConnectAsync(VssConnectMode.Automatic);
                result.Status = CONN_RESULT.OK;
            }
            catch (Exception e)
            {
                result.Status = CONN_RESULT.ERROR;
                result.ErrorMessage = e.Message;
            }
            TfsReady =  result.Status == CONN_RESULT.OK ? true : false;
            return result;
        }
        
        public async Task<ConnectResult> PerformClokiConnect()
        {
            ConnectResult result = new ConnectResult(CONN_RESULT.ERROR, "");
            try
            {
                var uid = await clockify.GetCurrentUserAsync();
                if (uid != null && uid.Data != null)
                {
                    var ws = await clockify.GetWorkspacesAsync();
                    if (ws != null && ws.Data != null)
                    {
                        var prj = await clockify.FindAllProjectsOnWorkspaceAsync(ws.Data[0].Id, null, "ФЦОД-М");
                        if (prj != null && prj.Data != null)
                        {
                            StaticData.Init(prj.Data[0].Id, uid.Data.Id, ws.Data[0].Id);
                            result.Status = CONN_RESULT.OK;
                        } else { result.ErrorMessage = prj.StatusDescription + " " + prj.Content; }
                    } else { result.ErrorMessage = ws.StatusDescription + " " + ws.Content; }
                }
                else { result.ErrorMessage = uid.StatusDescription + " " + uid.Content; }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }

            ClockifyReady = result.Status == CONN_RESULT.OK? true : false;
            return result;
        }
        public async Task<bool> Init()
        {
            OnInitCompleted(CONN_RESULT.CONNECTING, "");
            clockify = new ClockifyClient(Settings.Default.ApiKey);
            Console.WriteLine("Init clokify connection .. ");
            var cr = await PerformClokiConnect();
            Console.WriteLine("Done.");          
            if (!ClockifyReady)
            {
                OnInitCompleted(CONN_RESULT.ERROR,  cr.ErrorMessage);
            }
                
            Console.WriteLine("Init TFS connection ...");
            var tr = await PerformTFSConnect();
            if (!TfsReady)
            {
                OnInitCompleted(CONN_RESULT.ERROR, tr.ErrorMessage);
            }
            OnInitCompleted(CONN_RESULT.OK, "");
            if (ClockifyReady && TfsReady)
                return true;
            else return false;
        }

        public async Task<IRestResponse<List<TimeEntryDtoImpl>>> FindAllTimeEntriesForUser(int? TFSworkItemId, DateTime? queryFrom) 
        {
            string query = TFSworkItemId == null ? null : TFSworkItemId.ToString();
            var ret = await clockify.FindAllTimeEntriesForUserAsync(StaticData.WorkspaceId, StaticData.UserId,
                                                           query,
                                                           queryFrom, null,
                                                           StaticData.ProjectId, null, null, null, null, null, null, 1, 5000);
            return ret;
        }

        public async Task<bool> UpdateClokiEntries(List<TimeData> entries, string wiTitle)
        {
            IRestResponse r;
            TimeEntryDtoImpl x;
            if (entries == null || entries.Count == 0)
                return false;
            foreach (var e in entries)
            {
                // удаляем старые записи, как ненужные - пересоздадим
                if (e.TimeEntries != null && e.TimeEntries.Count != 0)
                {
                    foreach (var t in e.TimeEntries)
                    {
                        r = clockify.DeleteTimeEntryAsync(StaticData.WorkspaceId, t.Id).Result;
                        if (!r.IsSuccessful)
                            throw new Exception("Ошибка при удалении Clokify TimeEntry");
                        OnTimeEntryDeleted(t.Id, t.WorkItemId);
                    }
                }

                var rq = TimeEntryRequestFabric.GetRequest(e);
                rq.Description = wiTitle + '.' + e.Comment;
                x = clockify.CreateTimeEntryAsync(StaticData.WorkspaceId, rq).Result.Data;
                if (x == null)
                    throw new Exception("Ошибка при создании Clokify TimeEntry");
                OnTimeEntryCreated(new TimeEntry(x));
            }
            return true;
        }

        public async Task<bool> UpdateTFSEntry(int id, JsonPatchDocument patch) {
            WorkItemTrackingHttpClient witClient = tfsConnection.GetClient<WorkItemTrackingHttpClient>();
            try
            {
                WorkItem result = await witClient.UpdateWorkItemAsync(patch, id);
                OnWorkItemUpdated(new TFSWorkItem(result));
                return true;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Error updating workitem: {0}", ex.InnerException.Message);
                return false;
            }

        }


        public async Task<List<WorkItem>> QueryMyTasks()
        {
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = tfsConnection.GetClient<WorkItemTrackingHttpClient>();

            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            List<WorkItem> workItems = new List<WorkItem>();

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
            if (myQueriesFolder != null)
            {
                string queryName = "WiClokifyQuery";

                QueryHierarchyItem myTasksQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    myTasksQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                }
                if (myTasksQuery == null)
                {
                    // if the 'REST Sample' query does not exist, create it.
                    myTasksQuery = new QueryHierarchyItem()
                    {
                        Name = queryName,
                        Wiql = "SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State] " +
                                "FROM WorkItems WHERE " +
                                "[System.WorkItemType] = 'Task' AND " +
                                "[System.State] <> 'Closed' AND " +
                                "[System.AssignedTo] = @Me",
                        IsFolder = false
                    };
                    myTasksQuery = witClient.CreateQueryAsync(myTasksQuery, teamProjectName, myQueriesFolder.Name).Result;
                }

                WorkItemQueryResult result = witClient.QueryByIdAsync(myTasksQuery.Id).Result;

                if (result.WorkItems.Any())
                {
                    int skip = 0;
                    const int batchSize = 100;
                    IEnumerable<WorkItemReference> workItemRefs;
                    do
                    {
                        workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);
                        if (workItemRefs.Any())
                        {
                            // get details for each work item in the batch
                            workItems = witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id)).Result;
                        }
                        skip += batchSize;
                    }
                    while (workItemRefs.Count() == batchSize);
                }
            }
            OnTfsQueryCompleted();
            return workItems;
        }


        public void TestClokiCreate()
        {
            var rq = TimeEntryRequestFabric.GetRequest();
            rq.Description = "Test";
            rq.Start = DateTime.Now;
            rq.End = DateTime.Now.AddMinutes(15);
            var ct = clockify.CreateTimeEntryAsync(StaticData.WorkspaceId, rq).Result;
            var Id = ct.Data.Id;
            var rm = clockify.DeleteTimeEntryAsync(StaticData.WorkspaceId, Id).Result;
        }

    }
}
