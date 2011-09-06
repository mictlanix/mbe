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
    public class BanksAccountsController : Controller
    {
        //
        // GET: /BanksAccounts/

        public ViewResult Index()
        {
            var qry = from x in BankAccount.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /BanksAccounts/Details/5

        public ViewResult Details(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            return View(bankAccount);
        }

        //
        // GET: /BanksAccounts/Create

        public ActionResult CreateForSupplier(int id)
        {
            ViewBag.OwnerId = id;
            ViewBag.OwnerType = "Suppliers";
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
                string type = Request.Params["OwnerType"];

                var supplier = Supplier.Find(owner);
                bankAccount.Suppliers.Add(supplier);

                bankAccount.Save();
                return RedirectToAction("Details", type, new { id = owner });
            }

            return View(bankAccount);
        }

        //
        // GET: /BanksAccounts/Edit/5

        public ActionResult Edit(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            return View(bankAccount);
        }

        //
        // POST: /BanksAccounts/Edit/5

        [HttpPost]
        public ActionResult Edit(BankAccount bankAccount)
        {
            if (ModelState.IsValid)
            {
                bankAccount.Save();
                return RedirectToAction("Index");
            }
            return View(bankAccount);
        }

        //
        // GET: /BanksAccounts/Delete/5

        public ActionResult Delete(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            return View(bankAccount);
        }

        //
        // POST: /BanksAccounts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            BankAccount bankAccount = BankAccount.Find(id);
            bankAccount.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}