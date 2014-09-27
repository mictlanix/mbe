// 
// BankAccountsController.cs
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
    public class BankAccountsController : Controller
    {
        //
        // GET: /BanksAccounts/Details/5

        public ActionResult Details(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;

            return PartialView ("_Details", item);
        }

        //
        // GET: /BanksAccounts/Create

        public ActionResult CreateForSupplier(int id)
        {
            ViewBag.OwnerId = id;
            return PartialView ("_Create", new BankAccount());
        }

        //
        // POST: /BanksAccounts/Create

        [HttpPost]
        public ActionResult CreateForSupplier(BankAccount item)
        {

            if (!ModelState.IsValid)
                return PartialView ("_Create", item);

            int owner = int.Parse (Request.Params ["OwnerId"]);

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
							
                var supplier = Supplier.Find(owner);
                supplier.BanksAccounts.Add(item);
                supplier.Update ();
            }

            return PartialView ("_Refresh");
        }

        //
        // GET: /BanksAccounts/Edit/5

        public ActionResult Edit(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;

            return PartialView ("_Edit", item);
        }

        //
        // POST: /BanksAccounts/Edit/5

        [HttpPost]
        public ActionResult Edit(BankAccount item)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);

				using (var scope = new TransactionScope()) {
					item.Update ();
				}

                return PartialView("_Edit", new { id = owner });

            }
            return PartialView ("_Refresh");
        }

        //
        // GET: /BanksAccounts/Delete/5

        public ActionResult Delete(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;

            return PartialView ("_Delete", item);
        }

        //
        // POST: /BanksAccounts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = BankAccount.Find (id);

            int owner = int.Parse(Request.Params["OwnerId"]);
            
            using (var scope = new TransactionScope()) {
	            var supplier = item.Suppliers.FirstOrDefault();
	
	            if (supplier != null) {
	                supplier.BanksAccounts.Remove(item);
					supplier.Update ();
	            }

				item.DeleteAndFlush ();
			}
			
            return PartialView("_Delete", new { id = owner });
        }
    }
}