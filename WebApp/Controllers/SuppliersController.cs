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
    public class SuppliersController : Controller
    {
        public JsonResult GetSuggestions(string pattern)
        {
            JsonResult result = new JsonResult();
            var qry = from x in Supplier.Queryable
                      where x.Name.Contains(pattern)
                      select new { id = x.Id, name = x.Name };

            result = Json(qry.Take(15).ToList());
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }

        //
        // GET: /Suppliers/

        public ViewResult Index()
        {
            var qry = from x in Supplier.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Supplier/Details/5

        public ViewResult Details(int id)
        {
            Supplier supplier = Supplier.Find(id);

            ViewBag.OwnerId = supplier.Id;
            
            return View(supplier);
        }

        //
        // GET: /Supplier/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Supplier/Create

        [HttpPost]
        public ActionResult Create(Supplier supplier)
        {   
            if (ModelState.IsValid)
            {
                supplier.Save();
                return RedirectToAction("Index");
            }

            return View(supplier);
        }

        //
        // GET: /Supplier/Edit/5

        public ActionResult Edit(int id)
        {
            Supplier supplier = Supplier.Find(id);
            return View(supplier);
        }

        //
        // POST: /Supplier/Edit/5

        [HttpPost]
        public ActionResult Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.Save();
                return RedirectToAction("Index");
            }
            return View(supplier);
        }

        //
        // GET: /Supplier/Delete/5

        public ActionResult Delete(int id)
        {
            Supplier supplier = Supplier.Find(id);
            return View(supplier);
        }

        //
        // POST: /Supplier/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Supplier supplier = Supplier.Find(id);
            supplier.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}