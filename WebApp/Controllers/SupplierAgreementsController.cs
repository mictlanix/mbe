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
    public class SupplierAgreementsController : Controller
    {
        //
        // GET: /SupplierAgreements/

        public ViewResult Index()
        {
            var qry = from x in SupplierAgreement.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /SupplierAgreements/Details/5

        public ViewResult Details(int id)
        {
            SupplierAgreement supplierAgreement = SupplierAgreement.Find(id);
            return View(supplierAgreement);
        }

        //
        // GET: /SupplierAgreements/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /SupplierAgreements/Create

        [HttpPost]
        public ActionResult Create(SupplierAgreement supplierAgreement)
        {
            if (ModelState.IsValid)
            {
                supplierAgreement.Save();
                return RedirectToAction("Index");
            }

            return View(supplierAgreement);
        }

        //
        // GET: /SupplierAgreements/Edit/5

        public ActionResult Edit(int id)
        {
            SupplierAgreement supplierAgreement = SupplierAgreement.Find(id);
            return View(supplierAgreement);
        }

        //
        // POST: /SupplierAgreements/Edit/5

        [HttpPost]
        public ActionResult Edit(SupplierAgreement supplierAgreement)
        {
            if (ModelState.IsValid)
            {
                supplierAgreement.Save();
                return RedirectToAction("Index");
            }
            return View(supplierAgreement);
        }

        //
        // GET: /SupplierAgreements/Delete/5

        public ActionResult Delete(int id)
        {
            SupplierAgreement supplierAgreement = SupplierAgreement.Find(id);
            return View(supplierAgreement);
        }

        //
        // POST: /SupplierAgreements/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SupplierAgreement supplierAgreement = SupplierAgreement.Find(id);
            supplierAgreement.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}