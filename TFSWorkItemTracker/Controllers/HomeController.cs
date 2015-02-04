using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

        public ActionResult Report()
        {
            return View();
        }
    }
}