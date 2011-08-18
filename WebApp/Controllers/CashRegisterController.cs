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
    public class CashRegisterController : Controller
    {
        //
        // GET: /CashRegister/

        public ViewResult Index()
        {
            var qry = from x in CashRegister.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /CashRegister/Details/5

        public ViewResult Details(int id)
        {
            CashRegister cashRegister = CashRegister.Find(id);
            return View(cashRegister);
        }

        //
        // GET: /CashRegister/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /CashRegister/Create

        [HttpPost]
        public ActionResult Create(CashRegister cashRegister)
        {
            if (ModelState.IsValid)
            {
                cashRegister.Save();
                return RedirectToAction("Index");
            }

            return View(cashRegister);
        }

        //
        // GET: /CashRegister/Edit/5

        public ActionResult Edit(int id)
        {
            CashRegister cashRegister = CashRegister.Find(id);
            return View(cashRegister);
        }

        //
        // POST: /CashRegister/Edit/5

        [HttpPost]
        public ActionResult Edit(CashRegister cashRegister)
        {
            if (ModelState.IsValid)
            {
                cashRegister.Save();
                return RedirectToAction("Index");
            }
            return View(cashRegister);
        }

        //
        // GET: /CashRegister/Delete/5

        public ActionResult Delete(int id)
        {
            CashRegister cashRegister = CashRegister.Find(id);
            return View(cashRegister);
        }

        //
        // POST: /CashRegister/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            CashRegister cashRegister = CashRegister.Find(id);
            cashRegister.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}