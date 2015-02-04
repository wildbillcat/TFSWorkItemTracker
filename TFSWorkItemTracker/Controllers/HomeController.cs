using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;//Excel
using System.Web.UI.WebControls;//Excel
using System.Web.Mvc;
using System.IO;//Excel
using System.Data;//Excel
using Microsoft.TeamFoundation.Client;//tfs
using Microsoft.TeamFoundation.WorkItemTracking.Client;//tfs
using PagedList;

namespace TFSWorkItemTracker.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Live()
        {
            return View();
        }

        //All if the bools represent different states
        public ActionResult Report(int? page, DateTime? StartDate, DateTime? EndDate, string currentFilter, string searchString, bool? New, bool? Approved, bool? Committed, bool? Done, bool? Removed)
        {
            List<WorkItem> WorkItems = new List<WorkItem>();
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;
            DateTime QStartDate = (StartDate ?? DateTime.Now.AddDays(-30));
            DateTime QEndDate = (EndDate ?? DateTime.Now);
            bool QNew = (New ?? false);
            bool QApproved = (Approved ?? false);
            bool QCommitted = (Committed ?? false);
            bool QDone = (Done ?? false);
            bool QRemoved = (Removed ?? false);
            if (QNew == QApproved == QCommitted == QDone == QRemoved == false)
            {
                QNew = true;
                QApproved = true;
                QCommitted = true;
                QDone = true;
                QRemoved = true;
            }
            ///Build Connection to TFS
            List<Uri> ProjectCollectionUris = new List<Uri>();
            //Fetch the list of uri to the tfs server(s) project collection(s)
            //Parses comma delimited array of Uri
            foreach (string CollectionURI in System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerUri").Split(','))
            {
                ProjectCollectionUris.Add(new Uri(CollectionURI.Trim()));
            }
            //This builds a list of project collections for querying later.
            List<TfsTeamProjectCollection> TfsProjectCollections = new List<TfsTeamProjectCollection>();
            foreach (Uri Location in ProjectCollectionUris)
            {
                TfsProjectCollections.Add(new TfsTeamProjectCollection(Location));
            }
            //Build Query
            string TFSServerQuery = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQuery"); //This needs to be changed
            Dictionary<string, Query>  TFSServerQuerys = new Dictionary<string, Query>();
            foreach (TfsTeamProjectCollection ProjectCollection in TfsProjectCollections)
            {
                TFSServerQuerys.Add(ProjectCollection.Name, new Query((WorkItemStore)ProjectCollection.GetService(typeof(WorkItemStore)), TFSServerQuery));
            }
            int TFSServerQueryTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQueryTimeout"));
            //Run Query to get all Workitems
            Dictionary<string, ICancelableAsyncResult> CallBacks = new Dictionary<string, ICancelableAsyncResult>();
            //Start the Async Queries
            foreach (string ProjectName in TFSServerQuerys.Keys)
            {
                CallBacks.Add(ProjectName, TFSServerQuerys[ProjectName].BeginQuery());
            }
            Parallel.ForEach(CallBacks.Keys, ProjName =>
            {
                CallBacks[ProjName].AsyncWaitHandle.WaitOne(TFSServerQueryTimeout, false);
            });






            int pageSize = 30;
            int pageNumber = (page ?? 1);
            return View();
        }

        //Filled with example code, will have to fill.
        public ActionResult ExportToExcel()
        {
            var products = new System.Data.DataTable("teste");
            products.Columns.Add("col1", typeof(int));
            products.Columns.Add("col2", typeof(string));

            products.Rows.Add(1, "product 1");
            products.Rows.Add(2, "product 2");
            products.Rows.Add(3, "product 3");
            products.Rows.Add(4, "product 4");
            products.Rows.Add(5, "product 5");
            products.Rows.Add(6, "product 6");
            products.Rows.Add(7, "product 7");


            var grid = new GridView();
            grid.DataSource = products;
            grid.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=MyExcelFile.xls");
            Response.ContentType = "application/ms-excel";

            Response.Charset = "";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Output.Write(sw.ToString());
            Response.Flush();
            Response.End();

            return View();
        }
    }
}