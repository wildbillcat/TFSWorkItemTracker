using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TFSWorkItemTracker.Hubs
{
    [HubName("chat")]
    public class ChatHub : Hub
    {
        //This Timer is used to trigger a query against TFS, then updating clients with new information
        static Timer TFSPoll;
        
        //This string maintains the state of the timer log since application start. Just a Proof of concept to later be replaced by workitems.
        static List<string> TimerLog = new List<string>();
        //Stores a list of WorkItems found by the Query
        static Dictionary<string, List<WorkItem>> TFSWorkItems = new Dictionary<string, List<WorkItem>>();
        
        //Stores a list of URIs to access the Work items in TFS
        static string[] ProjectCollectionUris = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerUri").Split(',');
        //This is a list of all the project collections that this application connects to
        static Dictionary<string, TfsTeamProjectCollection> TfsProjectCollections = new Dictionary<string, TfsTeamProjectCollection>();
        //This is the Query Run against each of the collections
        static string TFSServerQuery = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQuery");
        //Timeout in Milliseconds for asyncronous query timeouts
        static int TFSServerQueryTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQueryTimeout"));
        //List of all the Async Query Objects used to find work Items
        static Dictionary<string, Query> TFSServerQuerys = new Dictionary<string, Query>();
        
        static Object TimerEventLock = new Object();

        public ChatHub() : base()
        {
            if (TFSPoll == null)
            {
                TFSPoll = new System.Timers.Timer(int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerTimer")));
                TFSPoll.Elapsed += OnTimedEvent;
                TFSPoll.Enabled = true;                
            }

            //Ensure Project Collections are fine
            if (!((TfsProjectCollections.Count() == ProjectCollectionUris.Length) && (TFSServerQuerys.Count() == ProjectCollectionUris.Length) && (TFSWorkItems.Count() == ProjectCollectionUris.Length)))
            {
                //This builds a list of project collections for querying later.
                foreach (string Location in ProjectCollectionUris)
                {
                    TfsTeamProjectCollection Team = new TfsTeamProjectCollection(new Uri(Location));
                    if (!TfsProjectCollections.ContainsKey(Team.Name))
                    {
                        TfsProjectCollections.Add(Team.Name, Team);
                    }
                    if (!TFSWorkItems.ContainsKey(Team.Name))
                    {
                        TFSWorkItems.Add(Team.Name, new List<WorkItem>());
                    }
                    if (!TFSServerQuerys.ContainsKey(Team.Name))
                    {
                        TFSServerQuerys.Add(Team.Name, new Query((WorkItemStore)Team.GetService(typeof(WorkItemStore)), TFSServerQuery));
                    }
                }
            }
        }

        static private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.newMessage(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));            
            //This holds the results from the queries. Blocking collection allows for queries to be handled in multiple threads
            Dictionary<string, List<WorkItem>> FreshTFSWorkItems = new Dictionary<string, List<WorkItem>>();
            //Start all of the queries against the server (Async)
            Dictionary<string, ICancelableAsyncResult> CallBacks = new Dictionary<string, ICancelableAsyncResult>();

            //Start the Async Queries
            foreach (string ProjectName in TFSServerQuerys.Keys)
            {
                CallBacks.Add(ProjectName, TFSServerQuerys[ProjectName].BeginQuery());
                FreshTFSWorkItems.Add(ProjectName, new List<WorkItem>());
            }
            //Read Only Query Section
            Parallel.ForEach(CallBacks.Keys, ProjName =>
            {
                CallBacks[ProjName].AsyncWaitHandle.WaitOne(TFSServerQueryTimeout, false);
            });           
            
            //This is to keep the event method threads safe if the thread(s) take too long and another event fires before completion.
            //If this happens on a regular basis this is an issue, and the timer should be adjusted accordingly.
            lock (TimerEventLock)
            {
                //Now wait on all the queries and pull all the results of successfull queries
                //foreach(string ProjName in CallBacks.Keys)
                Parallel.ForEach(CallBacks.Keys, ProjName =>
                {
                    if (!CallBacks[ProjName].IsCompleted)
                    {
                        //Query hasnt returned. Cancel the query
                        CallBacks[ProjName].Cancel();
                        GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.newMessage(string.Concat("A query has timed out to ", ProjName, " at: ", e.SignalTime));
                    }
                    else
                    {
                        //Query has returned, lets find any new results
                        WorkItemCollection nextresults = TFSServerQuerys[ProjName].EndQuery(CallBacks[ProjName]);
                        List<WorkItem> Deletion = new List<WorkItem>();

                        //Delete old Work Items Loop 
                        //Builds a list of Items to delete. For each loops do not reevaluate their index, so removing in the loop could cause issues.
                        foreach (WorkItem DeletionCandidate in TFSWorkItems[ProjName])
                        {
                            if (!FreshTFSWorkItems[ProjName].Contains(DeletionCandidate))
                            {
                                //Remove from client side
                                ////GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("A query has timed out:", e.SignalTime));
                            }
                        }
                        //Add new Work Items Loop.
                        foreach (WorkItem result in nextresults)
                        {
                            GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.newMessage(string.Concat("One Work Item Found. ", result.ToString()));   
                            //If the WorkItem doesnt exist in the current Collection, add it and update clients.
                            if (!TFSWorkItems[ProjName].Contains(result))
                            {
                                //Method for updating clients will have to be added.
                                //GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("A query has timed out:", e.SignalTime));
                            }
                        }
                        //Now that Clients have been Diff'd replace the WorkItemList
                        TFSWorkItems[ProjName] = FreshTFSWorkItems[ProjName];
                    }
                });
            }
        }

        public void Send(string message)
        {
            //Contatenate the logged in username and send it to the clients for publish
            Clients.All.newMessage(string.Concat(Context.User.Identity.Name, ":", message));
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            lock (TimerEventLock)
            {
                //Add a lock to prevent clients joining on query
                foreach (string Log in TimerLog)
                {
                    Clients.Client(Context.ConnectionId).addNewWorkItemToPage(Log);
                }
                return base.OnConnected();
            }
        }
    }
}