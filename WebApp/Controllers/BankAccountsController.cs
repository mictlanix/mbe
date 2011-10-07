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
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class BankAccountsController : Controller
    {
        //
        // GET: /BanksAccounts/Details/5

        public ViewResult Details(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            var supplier = bankAccount.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
            return View(bankAccount);
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
        public ActionResult Create(BankAccount bankAccount)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);

                var supplier = Supplier.Find(owner);
                bankAccount.Suppliers.Add(supplier);

                bankAccount.Save();
                return RedirectToAction("Details", "Suppliers", new { id = owner });
            }

            return View(bankAccount);
        }

        //
        // GET: /BanksAccounts/Edit/5

        public ActionResult Edit(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            var supplier = bankAccount.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
            return View(bankAccount);
        }

        //
        // POST: /BanksAccounts/Edit/5

        [HttpPost]
        public ActionResult Edit(BankAccount bankAccount)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                BankAccount item = BankAccount.Find(bankAccount.Id);

                bankAccount.Suppliers = item.Suppliers;
                bankAccount.Save();

                return RedirectToAction("Details", "Suppliers", new { id = owner });
            }
            return View(bankAccount);
        }

        //
        // GET: /BanksAccounts/Delete/5

        public ActionResult Delete(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            var supplier = bankAccount.Suppliers.First();

            ViewBag.OwnerId = supplier.Id;
            return View(bankAccount);
        }

        //
        // POST: /BanksAccounts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int owner = int.Parse(Request.Params["OwnerId"]);
            BankAccount bankAccount = BankAccount.Find(id);

            bankAccount.Delete();
            return RedirectToAction("Details", "Suppliers", new { id = owner });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}