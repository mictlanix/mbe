// 
// BankAccountsController.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
    public class BankAccountsController : Controller
    {
        //
        // GET: /BanksAccounts/Details/5

        public ViewResult Details(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
		
        	return View(item);
        }

        //
        // GET: /BanksAccounts/Create

        public ActionResult CreateForSupplier(int id)
        {
            ViewBag.OwnerId = id;
            return View("Create");
        }

        //
        // POST: /BanksAccounts/Create

        [HttpPost]
        public ActionResult Create(BankAccount item)
        {
            if (!ModelState.IsValid)
				return View(item);

            int owner = int.Parse(Request.Params["OwnerId"]);

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();

            	System.Diagnostics.Debug.WriteLine("New BankAccount [Id = {0}]", item.Id);
			
                var supplier = Supplier.Find(owner);
                supplier.BanksAccounts.Add(item);
                supplier.Save();
            }
			
            return RedirectToAction("Details", "Suppliers", new { id = owner });
        }

        //
        // GET: /BanksAccounts/Edit/5

        public ActionResult Edit(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
		
        	return View(item);
        }

        //
        // POST: /BanksAccounts/Edit/5

        [HttpPost]
        public ActionResult Edit(BankAccount item)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
				
                item.Save();

                return RedirectToAction("Details", "Suppliers", new { id = owner });
            }
            return View(item);
        }

        //
        // GET: /BanksAccounts/Delete/5

        public ActionResult Delete(int id)
        {
            var item = BankAccount.Find(id);
            var supplier = item.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
			
            return View(item);
        }

        //
        // POST: /BanksAccounts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int owner = int.Parse(Request.Params["OwnerId"]);
            
            using (var scope = new TransactionScope()) {
	            var item = BankAccount.Find(id);
	            var supplier = item.Suppliers.FirstOrDefault();
	
	            if (supplier != null) {
	                supplier.BanksAccounts.Remove(item);
					supplier.Save();
	            }

				item.DeleteAndFlush ();
			}
			
            return RedirectToAction("Details", "Suppliers", new { id = owner });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}