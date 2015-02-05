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
using System.Collections.Concurrent;//concurrency
using System.Threading.Tasks;//concurrency
using PagedList;//pagnation
using TFSWorkItemTracker.Models;

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
            ConcurrentBag<PocoWorkItem> WorkItems = new ConcurrentBag<PocoWorkItem>();
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

            ViewBag.bStartDate = QStartDate;
            ViewBag.bEndDate = QEndDate;
            ViewBag.bNew = QNew;
            ViewBag.bApproved = QApproved;
            ViewBag.bCommitted = QCommitted;
            ViewBag.bDone = QDone;
            ViewBag.bRemoved = QRemoved;

            //Build Query
            string TFSServerQuery = System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQuery2"); //This needs to be changed
            
            int TFSServerQueryTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerQueryTimeout"));
                    
            Parallel.ForEach(System.Configuration.ConfigurationManager.AppSettings.Get("TFSServerUri").Split(','), UriString =>
            {
                //Create Connection to Project Collection
                TfsTeamProjectCollection ProjectCollection = new TfsTeamProjectCollection(new Uri(UriString));
                //Build Query Object for Work Items
                Query AsyncQuery = new Query((WorkItemStore)ProjectCollection.GetService(typeof(WorkItemStore)), TFSServerQuery);
                //Start Async Query
                ICancelableAsyncResult AsyncStatus = AsyncQuery.BeginQuery();
                //Wait for query
                AsyncStatus.AsyncWaitHandle.WaitOne(TFSServerQueryTimeout, false);
                if (!AsyncStatus.IsCompleted)
                {
                    //Query hasnt returned. Cancel the query
                    AsyncStatus.Cancel();
                }
                else
                {
                    //Query has returned, lets find any new results
                    WorkItemCollection results = AsyncQuery.EndQuery(AsyncStatus);
                    //Builds master list of Items to delete. For each loops do not reevaluate their index, so removing in the loop could cause issues.
                    foreach (WorkItem WI in results)
                    {
                        WorkItems.Add(new PocoWorkItem(WI, WI.Project.Name));
                    }
                }
            });            

            int pageSize = 30;
            int pageNumber = (page ?? 1);
            return View(WorkItems.ToPagedList(pageNumber, pageSize));
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