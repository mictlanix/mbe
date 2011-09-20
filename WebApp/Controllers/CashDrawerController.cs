using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class CashDrawerController : Controller
    {
        //
        // GET: /cashDrawer/

        public ViewResult Index()
        {
            var qry = from x in CashDrawer.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /cashDrawer/Details/5

        public ViewResult Details(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /cashDrawer/Create

        [HttpPost]
        public ActionResult Create(CashDrawer cashDrawer)
        {
            if (ModelState.IsValid)
            {
                cashDrawer.Save();
                return RedirectToAction("Index");
            }

            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Edit/5

        public ActionResult Edit(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // POST: /cashDrawer/Edit/5

        [HttpPost]
        public ActionResult Edit(CashDrawer cashDrawer)
        {
            if (ModelState.IsValid)
            {
                cashDrawer.Save();
                return RedirectToAction("Index");
            }
            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Delete/5

        public ActionResult Delete(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // POST: /cashDrawer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            cashDrawer.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}