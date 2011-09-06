using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class PointsOfSaleController : Controller
    {
        //
        // GET: /PointSale/

        public ViewResult Index()
        {
            var qry = from x in PointSale.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /PointSale/Details/5

        public ViewResult Details(int id)
        {
            PointSale pointSale = PointSale.Find(id);
            return View(pointSale);
        }

        //
        // GET: /PointSale/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /PointSale/Create

        [HttpPost]
        public ActionResult Create(PointSale pointSale)
        {
            if (ModelState.IsValid)
            {
                pointSale.Save();
                return RedirectToAction("Index");
            }

            return View(pointSale);
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit(int id)
        {
            PointSale pointSale = PointSale.Find(id);
            return View(pointSale);
        }

        //
        // POST: /PointSale/Edit/5

        [HttpPost]
        public ActionResult Edit(PointSale pointSale)
        {
            if (ModelState.IsValid)
            {
                pointSale.Save();
                return RedirectToAction("Index");
            }
            return View(pointSale);
        }

        //
        // GET: /PointSale/Delete/5

        public ActionResult Delete(int id)
        {
            PointSale pointSale = PointSale.Find(id);
            return View(pointSale);
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            PointSale pointSale = PointSale.Find(id);
            pointSale.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}