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