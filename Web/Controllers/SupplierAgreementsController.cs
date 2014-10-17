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
        public ActionResult Details (int id)
        {
            var item = SupplierAgreement.Find (id);

            ViewBag.OwnerId = item.Supplier.Id;

            return PartialView ("_Details", item);
        }

        public ActionResult CreateForSupplier (int id)
        {
            return PartialView ("_Create", new SupplierAgreement { SupplierId = id });
        }

        [HttpPost]
        public ActionResult CreateForSupplier (SupplierAgreement item)
        {
            item.Supplier = Supplier.Find (item.SupplierId);

            if (!ModelState.IsValid)
                return PartialView ("_Create", item);
            
			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush();
			}

            return PartialView ("_Refresh");
        }

        public ActionResult Edit (int id)
        {
            var item = SupplierAgreement.Find (id);
            return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit(SupplierAgreement item)
        {
            if (!ModelState.IsValid) {
                return PartialView ("_Edit", item);
            }

            var entity = SupplierAgreement.Find (item.Id);

            entity.Start = item.Start;
            entity.End = item.End;
            entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
                entity.UpdateAndFlush ();
			}

            return PartialView ("_Refresh");
        }

        public ActionResult Delete (int id)
        {
            var item = SupplierAgreement.Find (id);

            ViewBag.OwnerId = id;
            return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName ("Delete")]
        public ActionResult DeleteConfirmed (int id)
        {
            var item = SupplierAgreement.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

            return PartialView ("_Refresh", new { id = item.Supplier.Id });
        }
    }
}