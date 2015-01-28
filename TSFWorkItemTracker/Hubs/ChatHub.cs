using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Timers;

namespace TSFWorkItemTracker.Hubs
{
    public class ChatHub : Hub
    {
        static Timer TFSPoll;
        static List<string> TimerLog;

        public ChatHub() : base()
        {
            if (TFSPoll == null)
            {
                TFSPoll = new System.Timers.Timer(5000);
                TFSPoll.Elapsed += OnTimedEvent;
                TFSPoll.Enabled = true;
            }
            if (TimerLog == null)
            {
                TimerLog = new List<string>();
            }
        }

        static private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            TimerLog.Add(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
            GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients.All.addNewWorkItemToPage(string.Concat("The Elapsed event was raised at {0}", e.SignalTime));
        }

        public void Send(string message)
        {
            // Call the addNewMessageToPage method to update clients.
            Clients.All.addNewMessageToPage(Context.User.Identity.Name, message);
            //Clients.All.addNewWorkItemToPage(new WorkItemTestObject());
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