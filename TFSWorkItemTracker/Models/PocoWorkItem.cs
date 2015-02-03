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
        public string JsonId;
        public string Project;
        public string State;
        public DateTime ChangedDate;
        public string ChangedBy;
        public string Title;
        public string Collection;
        public string Uri;
        public string Toggler;

        public PocoWorkItem(WorkItem WI, string collection)
        {
            Id = WI.Id;
            JsonId = string.Concat(collection, WI.Id.ToString());
            Project = WI.Project.Name;
            State = WI.State;
            ChangedDate = WI.ChangedDate;
            ChangedBy = WI.ChangedBy;
            Title = WI.Title;
            Collection = collection.Split('\\')[1];
            Uri = WI.Uri.AbsoluteUri;
            Toggler = "void";
        }
    }
}