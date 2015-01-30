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

namespace TSFWorkItemTracker.Hubs
{
    [HubName("chat")]
    public class ChatHub : Hub
    {
        //This Timer is used to trigger a query against TFS, then updating clients with new information
        static Timer TFSPoll;
        //This string maintains the state of the timer log since application start. Just a Proof of concept to later be replaced by workitems.
        static List<string> TimerLog;
        //Stores a list of WorkItems found by the Query
        static BlockingCollection<WorkItem> TFSWorkItems;
        
        //Stores a list of URIs to access the Work items in TFS
        static List<Uri> ProjectCollectionUris;
        //This is a list of all the project collections that this application connects to
        static List<TfsTeamProjectCollection> TfsProjectCollections;
        //This is the Query Run against each of the collections
        static string TFSServerQuery;
        //Timeout in Milliseconds for asyncronous query timeouts
        static int TFSServerQueryTimeout;
        //List of all the Async Query Objects used to find work Items
        static List<Query> TFSServerQueryList;

        static Object TimerEventLock;

        public ChatHub() : base()
        {
            if (TFSPoll == null)
            {
                TFSPoll = new System.Timers.Timer(int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerTimer")));
                //TFSPoll.Elapsed += OnTimedEvent;
                TFSPoll.Elapsed += OnTimedEvent2;
                TFSPoll.Enabled = true;
            }
            if (TimerLog == null)
            {
                TimerLog = new List<string>();
            }
            if (TFSWorkItems == null)
            {
                TFSWorkItems = new BlockingCollection<WorkItem>();
            }
            if (ProjectCollectionUris == null)
            {
                ProjectCollectionUris = new List<Uri>();
                //Fetch the list of uri to the tfs server(s) project collection(s)
                string TfsUriString = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerUri");
                //Parses comma delimited array of Uri
                foreach (string CollectionURI in TfsUriString.Split(','))
                {
                    ProjectCollectionUris.Add(new Uri(CollectionURI.Trim()));
                }
            }
            if (TfsProjectCollections == null)
            {
                //This builds a list of project collections for querying later.
                TfsProjectCollections = new List<TfsTeamProjectCollection>();
                foreach (Uri Location in ProjectCollectionUris)
                {
                    TfsProjectCollections.Add(new TfsTeamProjectCollection(Location));
                }
            }
            if (TFSServerQuery == null)
            {
                TFSServerQuery = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQuery");
            }
            if (TFSServerQueryList == null)
            {
                TFSServerQueryList = new List<Query>();
                foreach (TfsTeamProjectCollection ProjectCollection in TfsProjectCollections)
                {
                    TFSServerQueryList.Add(new Query((WorkItemStore)ProjectCollection.GetService(typeof(WorkItemStore)), TFSServerQuery));
                }
            }
            if (TimerEventLock == null)
            {
                TimerEventLock = new Object();
            }
            TFSServerQueryTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQueryTimeout"));
        }
        static private void OnTimedEvent2(Object source, ElapsedEventArgs e)
        {
            TimerLog.Add(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
            GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.newMessage(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
        }

        static private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //This is to keep the event method threads safe if the thread(s) take too long and another event fires before completion.
            //If this happens on a regular basis this is an issue, and the timer should be adjusted accordingly.
            lock (TimerEventLock)
            {
                TimerLog.Add(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
                GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
                //This holds the results from the queries. Blocking collection allows for queries to be handled in multiple threads
                BlockingCollection<WorkItem> FreshTFSWorkItems = new BlockingCollection<WorkItem>();
                //Start all of the queries against the server (Async)
                List<ICancelableAsyncResult> CallBacks = new List<ICancelableAsyncResult>();
                Dictionary<ICancelableAsyncResult, Query> ResultLookup = new Dictionary<ICancelableAsyncResult, Query>();
                foreach (Query TfsQuery in TFSServerQueryList)
                {
                    ICancelableAsyncResult callback = TfsQuery.BeginQuery();
                    CallBacks.Add(callback);
                    ResultLookup.Add(callback, TfsQuery);
                }
                //Now wait on all the queries and pull all the results of successfull queries
                Parallel.ForEach(CallBacks, callback =>
                {
                    callback.AsyncWaitHandle.WaitOne(TFSServerQueryTimeout, false);
                    if (!callback.IsCompleted)
                    {
                        //Query hasnt returned. Cancel the query
                        callback.Cancel();
                        GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("A query has timed out:", e.SignalTime));
                    }
                    else
                    {
                        //Query has returned, lets find any new results
                        WorkItemCollection nextresults = ResultLookup[callback].EndQuery(callback);
                        foreach (WorkItem result in nextresults)
                        {
                            //Add the WorkItem to the Fresh list for later comparison
                            FreshTFSWorkItems.TryAdd(result);
                            //If the WorkItem doesnt exist in the current Collection, add it and update clients.
                            if (!TFSWorkItems.Contains(result))
                            {
                                TFSWorkItems.TryAdd(result);
                                //Method for updating clients will have to be added.
                                //GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("A query has timed out:", e.SignalTime));
                            }
                        }
                    }
                });

                //Now all queries have been run and results have been agrigates. Update clients on which work Items have been removed
                foreach (WorkItem tfsworkitem in TFSWorkItems)
                {
                    if (!FreshTFSWorkItems.Contains(tfsworkitem))
                    {
                        //Method for updating clients will have to be added.
                        //GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("A query has timed out:", e.SignalTime));
                    }
                }
                //WARNING: If the timer thread takes too long and another firing of this method starts,
                TFSWorkItems = FreshTFSWorkItems;
            }
        }

        public void Send(string message)
        {
            //Contatenate the logged in username and send it to the clients for publish
            Clients.All.newMessage(string.Concat(Context.User.Identity.Name, ":", message));
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            foreach (string Log in TimerLog)
            {
                Clients.Client(Context.ConnectionId).addNewWorkItemToPage(Log);
            }
            return base.OnConnected();
        }
    }
}