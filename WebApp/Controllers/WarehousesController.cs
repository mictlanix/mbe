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
    public class WarehousesController : Controller
    {
        //
        // GET: /Warehouses/

        public ViewResult Index()
        {
            var qry = from x in Warehouse.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Warehouses/Details/5

        public ViewResult Details(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // GET: /Warehouses/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Warehouses/Create

        [HttpPost]
        public ActionResult Create(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.Save();
                return RedirectToAction("Index");
            }

            return View(warehouse);
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // POST: /Warehouses/Edit/5

        [HttpPost]
        public ActionResult Edit(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.Save();
                return RedirectToAction("Index");
            }
            return View(warehouse);
        }

        //
        // GET: /Warehouses/Delete/5

        public ActionResult Delete(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            warehouse.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}