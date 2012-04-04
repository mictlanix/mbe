// 
// SupplierAgreementsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
            var agreement = SupplierAgreement.Find(id);
            var supplier = agreement.Supplier;

            ViewBag.OwnerId = supplier.Id;
			
            return View(agreement);
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
        public ActionResult Create(SupplierAgreement item)
        {
            if (ModelState.IsValid)
            {
                item.Supplier = Supplier.Find(item.SupplierId);
                item.Save();

                return RedirectToAction("Details", "Suppliers", new { id = item.Supplier.Id });
            }

            return View(item);
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
        public ActionResult Edit(SupplierAgreement item)
        {
            if (ModelState.IsValid)
            {
                item.Supplier = SupplierAgreement.Find(item.Id).Supplier;
                item.Save();

                return RedirectToAction("Details", "Suppliers", new { id = item.Supplier.Id });
            }

            return View(item);
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