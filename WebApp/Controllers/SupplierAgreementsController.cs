using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class SupplierAgreementsController : Controller
    {
        //
        // GET: /SupplierAgreements/Details/5

        public ViewResult Details(int id)
        {
            SupplierAgreement supplierAgreement = SupplierAgreement.Find(id);
            var supplier = supplierAgreement.Supplier;

            ViewBag.OwnerId = supplier.Id;
            return View(supplierAgreement);
        }

        //
        // GET: /SupplierAgreements/Create

        public ActionResult CreateForSupplier(int id)
        {
            Supplier supplier = Supplier.Find(id);
            return View("Create", new SupplierAgreement { SupplierId = id, Supplier = supplier });
        }

        //
        // POST: /SupplierAgreements/Create

        [HttpPost]
        public ActionResult Create(SupplierAgreement agreement)
        {
            if (ModelState.IsValid)
            {
                Supplier supplier = Supplier.Find(agreement.SupplierId);

                agreement.Supplier = supplier;
                agreement.Save();

                return RedirectToAction("Details", "Suppliers", new { id = agreement.Supplier.Id });
            }

            return View(agreement);
        }

        //
        // GET: /SupplierAgreements/Edit/5

        public ActionResult Edit(int id)
        {
            SupplierAgreement item = SupplierAgreement.Find(id);
            return View(item);
        }

        //
        // POST: /SupplierAgreements/Edit/5

        [HttpPost]
        public ActionResult Edit(SupplierAgreement agreement)
        {
            if (ModelState.IsValid)
            {
                SupplierAgreement item = SupplierAgreement.Find(agreement.Id);

                agreement.Supplier = item.Supplier;
                agreement.Save();

                return RedirectToAction("Details", "Suppliers", new { id = agreement.Supplier.Id });
            }

            return View(agreement);
        }

        //
        // GET: /SupplierAgreements/Delete/5

        public ActionResult Delete(int id)
        {
            SupplierAgreement item = SupplierAgreement.Find(id);
            return View(item);
        }

        //
        // POST: /SupplierAgreements/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SupplierAgreement item = SupplierAgreement.Find(id);

            item.Delete();

            return RedirectToAction("Details", "Suppliers", new { id = item.Supplier.Id });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}