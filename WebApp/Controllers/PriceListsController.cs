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
    public class PriceListsController : Controller
    {
        //
        // GET: /PriceList/

        public ViewResult Index()
        {
            var qry = from x in PriceList.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /PriceList/Details/5

        public ViewResult Details(int id)
        {
            PriceList priceList = PriceList.Find(id);
            return View(priceList);
        }

        //
        // GET: /PriceList/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /PriceList/Create

        [HttpPost]
        public ActionResult Create(PriceList priceList)
        {
            if (ModelState.IsValid)
            {
                priceList.Save();
                return RedirectToAction("Index");
            }

            return View(priceList);
        }

        //
        // GET: /PriceList/Edit/5

        public ActionResult Edit(int id)
        {
            PriceList priceList = PriceList.Find(id);
            return View(priceList);
        }

        //
        // POST: /PriceList/Edit/5

        [HttpPost]
        public ActionResult Edit(PriceList priceList)
        {
            if (ModelState.IsValid)
            {
                priceList.Save();
                return RedirectToAction("Index");
            }
            return View(priceList);
        }

        //
        // GET: /PriceList/Delete/5

        public ActionResult Delete(int id)
        {
            PriceList priceList = PriceList.Find(id);
            return View(priceList);
        }

        //
        // POST: /PriceList/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            PriceList priceList = PriceList.Find(id);
            priceList.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
