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
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled && !x.IsCredit
                      select x;

            return View(new MasterDetails<CashSession, SalesOrder> { Master = session, Details = qry.ToList() });
        }

        [HttpPost]
        public ActionResult Index(int? id)
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
                      where x.IsCompleted && !x.IsPaid && 
                            !x.IsCancelled && !x.IsCredit
                      select x;

            if (id != null && id > 0)
            {
                qry = from x in qry
                      where x.Id == id
                      select x;
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Index", qry.ToList());
            }
            else
            {
                return View(new MasterDetails<CashSession, SalesOrder> { Master = session, Details = qry.ToList() }); 

            }
        }

        // GET: /Payments/PrintCashCount/

        public ViewResult PrintCashCount(int id)
        {
            var info = new CashCountReport();
            var session = CashSession.Find(id);
            var qry = from x in CustomerPayment.Queryable
                      where x.CashSession.Id == session.Id
                      select new { Type = x.Method, Amount = x.Amount };
            var list = from x in qry.ToList()
                       group x by x.Type into g
                       select new MoneyCount { Type = g.Key, Amount = g.Sum(y => y.Amount) };

            info.Cashier = session.Cashier;
            info.CashDrawer = session.CashDrawer;
            info.Start = session.Start;
            info.End = session.End;
            info.MoneyCounts = list.ToList();
            info.CashCounts = session.CashCounts.Where(x => x.Type == CashCountType.CountedCash).ToList();
            info.StartingCash = session.StartingCash;
            info.SessionId = session.Id;

            return View("_CashCountTicket", info);
        }

        public ActionResult OpenSession()
        {
            if (GetSession() != null)
            {
                return RedirectToAction("Index");
            }
			
            var model = new CashSession
            {
                Start = DateTime.Now,
                CashCounts = CashHelpers.ListDenominations(),
                CashDrawer = GetDrawer(),
                Cashier = SecurityHelpers.GetUser(User.Identity.Name).Employee
            };

            if (model.CashDrawer == null)
            {
                return View("InvalidCashDrawer");
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult OpenSession(CashSession item)
        {
            List<CashCount> cash_counts;
            
            item.CashDrawer = GetDrawer();

            if (item.CashDrawer == null)
            {
                return View("InvalidCashDrawer");
            }

            cash_counts = new List<CashCount>(item.CashCounts.Where(x => x.Quantity > 0));

            item.Start = DateTime.Now;
            item.Cashier = Model.User.TryFind(User.Identity.Name).Employee;
            item.CashCounts.Clear();

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New CashSession [Id = {0}]", item.Id);
            
            using (var session = new SessionScope())
            {
                foreach (var x in cash_counts)
                {
                    x.Session = item;
                    x.Type = CashCountType.StartingCash;
                    x.Create();
                }
            }

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
            var sales_order = SalesOrder.Find(order);

            var item = new CustomerPayment
            {
                CashSession = GetSession(),
                SalesOrder = sales_order,
                Customer = sales_order.Customer,
                Method = (PaymentMethod)type,
                Amount = amount,
                Date = DateTime.Now,
                Reference = reference
            };

            if (item.Method == PaymentMethod.Cash)
            {
                if (item.Amount > -item.SalesOrder.Balance)
                {
                    item.Change = item.Amount + item.SalesOrder.Balance;
                    item.Amount = -item.SalesOrder.Balance;
                }
            }
            else {
                if (item.Amount > -item.SalesOrder.Balance)
                {
                    item.Amount = -item.SalesOrder.Balance;
                }
            }

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New Payment [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }
		
        public ActionResult CreditPayment()
        {
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CreditPayment", new CustomerPayment());
            }
			
            return View("_CreditPayment", new CustomerPayment());
        }
		
        [HttpPost]
        public ActionResult CreditPayment(CustomerPayment item)
        {
            item.CashSession = GetSession();
            item.Customer = Customer.Find(item.CustomerId);
            item.Date = DateTime.Now;
            item.Change = 0m;

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New Payment [Id = {0}]", item.Id);
			
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CreditPaymentSuccesful", item);
            }
			
            return View("_CreditPaymentSuccesful", item);
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
            session.CashCounts = CashHelpers.ListDenominations();
            return View(session);
        }

        [HttpPost]
        public ActionResult CloseSession(CashSession item)
        {
            List<CashCount> cash_counts;

            cash_counts = new List<CashCount>(item.CashCounts.Where(x => x.Quantity > 0));
            item = CashSession.Find(item.Id);

            using (var session = new SessionScope())
            {
                foreach (var x in cash_counts)
                {
                    x.Session = item;
                    x.Type = CashCountType.CountedCash;
                    x.Create();
                }
            }

            item.End = DateTime.Now;
            item.Update();

            return RedirectToAction("CloseSessionConfirmed", new { id = item.Id });
        }

        public ActionResult CloseSessionConfirmed(int id)
        {
            var session = CashSession.Find(id);
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
        
        CashDrawer GetDrawer()
        {
            var addr = Request.UserHostAddress;

            return CashDrawer.Queryable.SingleOrDefault(x => x.HostAddress == addr);
        }

        CashSession GetSession()
        {
            var addr = Request.UserHostAddress;
            return CashSession.Queryable
                              .Where(x => x.End == null)
                              .SingleOrDefault(x => x.CashDrawer.HostAddress == addr);
        }
    }
}
