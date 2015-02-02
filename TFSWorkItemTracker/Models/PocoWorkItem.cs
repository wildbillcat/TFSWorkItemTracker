using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFSWorkItemTracker.Models
{
    public class PocoWorkItem
    {
        public int Id;
        public string Project;
        public string State;
        public DateTime ChangedDate;
        public string Title;
        public string Collection;
        public string Uri;

        public PocoWorkItem(WorkItem WI, string collection)
        {
            Id = WI.Id;
            Project = WI.Project.Name;
            State = WI.State;
            ChangedDate = WI.ChangedDate;
            Title = WI.Title;
            Collection = collection;
            Uri = WI.Uri.AbsoluteUri;
        }
    }
}