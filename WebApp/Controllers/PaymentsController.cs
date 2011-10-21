// 
// PaymentsController.cs
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
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class PaymentsController : Controller
    {
        public ActionResult Index()
        {
            var drawer = GetDrawer();
            var session = GetSession();

            if (drawer == null)
            {
                return View("InvalidCashDrawer");
            }

            if (session == null)
            {
                return RedirectToAction("OpenSession");
            }

            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled
                      select x;

            return View(new MasterDetails<CashSession, SalesOrder> { Master = session, Details = qry.ToList() });
        }

        public ActionResult OpenSession()
        {
            if (GetDrawer() == null)
            {
                return View("InvalidCashDrawer");
            }

            if (GetSession() != null)
            {
                return RedirectToAction("Index");
            }

            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled
                      select x;

            return View(qry.ToList());
        }

        [HttpPost]
        public ActionResult OpenSession(CashSession item)
        {
            item = new CashSession();
            item.CashDrawer = GetDrawer();

            if (item.CashDrawer == null)
            {
                return View("InvalidCashDrawer");
            }

            item.Start = DateTime.Now;
            item.Cashier = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.CreateAndFlush();

            return RedirectToAction("Index");
        }

        public ActionResult PayOrder(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return View("PayOrder", order);
        }

        public ActionResult GetSalesOrderBalance(int id)
        {
            var order = SalesOrder.Find(id);

            return PartialView("_SalesOrderBalance", order);
        }

        [HttpPost]
        public JsonResult AddPayment(int order, int type, decimal amount, string reference)
        {
            var item = new CustomerPayment
            {
                CashSession = GetSession(),
                SalesOrder = SalesOrder.Find(order),
                Method = (PaymentMethod)type,
                Amount = amount,
                Date = DateTime.Now,
                Reference = reference
            };

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New Detail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        public ActionResult GetPayment(int id)
        {
            return PartialView("_Payment", CustomerPayment.Find(id));
        }

        [HttpPost]
        public JsonResult RemovePayment(int id)
        {
            CustomerPayment item = CustomerPayment.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult ConfirmPayment(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsPaid = true;
            item.Save();

            return RedirectToAction("Index");
        }

        public ActionResult CloseSession()
        {
            var session = GetSession();
            var qry = from x in CustomerPayment.Queryable
                      where x.CashSession.Id == session.Id
                      select new { Type = x.Method, Amount = x.Amount };
            var list = from x in qry.ToList()
                       group x by x.Type into g
                       select new MoneyCount { Type = g.Key, Amount = g.Sum(y => y.Amount) };

            return View(new MasterDetails<CashSession, MoneyCount>
            {
                Master = session,
                Details = list.ToList()
            });
        }


        [HttpPost]
        public ActionResult CloseSession(CashSession item)
        {
            item = GetSession();
            item.End = DateTime.Now;
            item.Update();

            return RedirectToAction("Index");
        }
        
        CashDrawer GetDrawer()
        {
            var addr = Request.UserHostAddress;

            return CashDrawer.Queryable.FirstOrDefault();//SingleOrDefault(x => x.HostAddress == addr);
        }

        CashSession GetSession()
        {
            var addr = Request.UserHostAddress;
            return CashSession.Queryable
                              .Where(x => x.End == null)
                              .FirstOrDefault();
                              //.SingleOrDefault(x => x.CashDrawer.HostAddress == addr);
        }
    }
}
