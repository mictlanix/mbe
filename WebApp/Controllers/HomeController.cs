using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Business.Essentials.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = Resources.Welcome;

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
