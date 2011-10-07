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