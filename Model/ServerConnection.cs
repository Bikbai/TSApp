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

namespace TSApp
{
    public enum CONN_RESULT { OK = 0, ERROR = 1, CONNECTING = 2}
    public class OnInitCompleteEventArgs : EventArgs 
    {
        public ConnectResult result;
        public OnInitCompleteEventArgs(string message)
        {
            result = new ConnectResult(CONN_RESULT.ERROR, message);
        }
        public OnInitCompleteEventArgs(CONN_RESULT state, string message)
        {
            result = new ConnectResult(CONN_RESULT.ERROR, message);
            result.Status = state;
        }

    }

    public class ConnectResult
    {
        private CONN_RESULT status;
        private string errorMessage;

        public ConnectResult(CONN_RESULT Status, string ErrorMessage)
        {
            status = Status; errorMessage = ErrorMessage;
        }

        public CONN_RESULT Status { get => status; set => status = value; }
        public string ErrorMessage { get => errorMessage; set => errorMessage = value; }
    }

    public class ServerConnection : ObservableObject
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

        public ServerConnection(Settings s)
        {
            Settings.Default.SettingsLoaded += SettingsLoaded;
            if (s == null)
                OnInitCompleteInvoke(CONN_RESULT.ERROR, "Не настроены параметры подключения.");
        }

        private async void SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            await this.Init();
        }

        #region OnInitComplete
        public delegate void OnInitCompleteDelegate(OnInitCompleteEventArgs args);

        public event OnInitCompleteDelegate OnInitComplete;

        private void OnInitCompleteInvoke(CONN_RESULT state, string msg)
        {
            if (OnInitComplete != null)
                OnInitComplete.Invoke(new OnInitCompleteEventArgs(state, ""));
        }
        #endregion

        #region OnQueryComplete
        public delegate void OnQueryCompleteDelegate();

        public event OnQueryCompleteDelegate OnQueryComplete;

        private void OnQueryCompleteInvoke()
        {
            if (OnQueryComplete != null)
                OnQueryComplete.Invoke();
        }
        #endregion
        
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
                            StaticData.Init(prj.Data[0].Id, uid.Data.Id, ws.Data[0].Id, new DateTime(2021, 11, 11, 9, 0, 0));
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
            OnInitCompleteInvoke(CONN_RESULT.CONNECTING, "");
            clockify = new ClockifyClient(Settings.Default.ApiKey);
            Console.WriteLine("Init clokify connection .. ");
            var cr = await PerformClokiConnect();
            Console.WriteLine("Done.");          
            if (!ClockifyReady)
            {
                OnInitCompleteInvoke(CONN_RESULT.ERROR,  cr.ErrorMessage);
            }
                
            Console.WriteLine("Init TFS connection ...");
            var tr = await PerformTFSConnect();
            if (!TfsReady)
            {
                OnInitCompleteInvoke(CONN_RESULT.ERROR, tr.ErrorMessage);
            }
            OnInitCompleteInvoke(CONN_RESULT.OK, "");
            if (ClockifyReady && TfsReady)
                return true;
            else return false;
        }

        public async Task<IRestResponse<List<TimeEntryDtoImpl>>> FindAllTimeEntriesForUser(int? TFSworkItemId, DateTime? queryFrom) 
        {
            string query = TFSworkItemId == null ? null : TFSworkItemId.ToString();
            var ret = await clockify.FindAllTimeEntriesForUserAsync(StaticData.WorkspaceId, StaticData.UserId,
                                                           query,  
                                                           queryFrom,  null,
                                                           StaticData.ProjectId);
            //Thread.Sleep(100);
            return ret;
        }

        public bool PublishClokifyData(TimeEntry te)
        {

            return true;
        }

        private bool MakeEntry()
        {
            /*
            var rq = new Clockify.Net.Models.TimeEntries.TimeEntryRequest();
            rq.Description = wi.Id.ToString() + "." + wi.Name;
            DateTimeOffset dt = DateTime.Now.Date.AddHours(9).ToUniversalTime();
            rq.Start = dt;
            rq.End = dt.AddHours(wi.GetChanged()[WIFields.CompletedWork]).ToUniversalTime();
            rq.ProjectId = projectId;
            
            var x = clockify.CreateTimeEntryAsync(workspaceId, rq).Result;
            wi.ClockifyId = x.Data.Id;
            wi.ClokifyWork = TimeSpan.FromHours(wi.CompletedWork);
            wi.IsChanged = false;
            */
            return true;
        }

        private bool UpdateEntry()
        {
            /*
            var rq = new Clockify.Net.Models.TimeEntries.UpdateTimeEntryRequest();
            rq.Billable = true;
            rq.ProjectId = projectId;
            DateTimeOffset dt = DateTime.Now.Date.AddHours(9).ToUniversalTime();
            rq.Start = dt;
            rq.End = dt.AddMinutes(wi.ClokifyWork.TotalMinutes).ToUniversalTime();
            var x = clockify.UpdateTimeEntryAsync(workspaceId, wi.ClockifyId, rq).Result;
            */
            return true;
        }

        public async Task<bool> UpdateTFSEntry(int id, JsonPatchDocument patch) {
            WorkItemTrackingHttpClient witClient = tfsConnection.GetClient<WorkItemTrackingHttpClient>();
            try
            {
                WorkItem result = await witClient.UpdateWorkItemAsync(patch, id);
                return true;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Error creating bug: {0}", ex.InnerException.Message);
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
            OnQueryCompleteInvoke();
            return workItems;
        }

    }
}
