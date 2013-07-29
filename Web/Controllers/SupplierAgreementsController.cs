// 
// SupplierAgreementsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
    public class SupplierAgreementsController : Controller
    {
        //
        // GET: /SupplierAgreements/Details/5

        public ViewResult Details (int id)
        {
            var item = SupplierAgreement.Find (id);

            ViewBag.OwnerId = item.Supplier.Id;
			
            return View (item);
        }

        //
        // GET: /SupplierAgreements/Create

        public ActionResult CreateForSupplier (int id)
        {
            var supplier = Supplier.Find(id);
            return View ("Create", new SupplierAgreement { SupplierId = id, Supplier = supplier });
        }

        //
        // POST: /SupplierAgreements/Create

        [HttpPost]
        public ActionResult Create (SupplierAgreement item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
            item.Supplier = Supplier.Find (item.SupplierId);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Details", "Suppliers", new { id = item.Supplier.Id });
        }

        //
        // GET: /SupplierAgreements/Edit/5

        public ActionResult Edit (int id)
        {
            var item = SupplierAgreement.Find (id);
            return View (item);
        }

        //
        // POST: /SupplierAgreements/Edit/5

        [HttpPost]
        public ActionResult Edit(SupplierAgreement item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
            item.Supplier = SupplierAgreement.Find (item.Id).Supplier;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Details", "Suppliers", new { id = item.Supplier.Id });
        }

        //
        // GET: /SupplierAgreements/Delete/5

        public ActionResult Delete (int id)
        {
            var item = SupplierAgreement.Find (id);
            return View (item);
        }

        //
        // POST: /SupplierAgreements/Delete/5

        [HttpPost, ActionName ("Delete")]
        public ActionResult DeleteConfirmed (int id)
        {
            SupplierAgreement item = SupplierAgreement.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

            return RedirectToAction ("Details", "Suppliers", new { id = item.Supplier.Id });
        }
    }
}