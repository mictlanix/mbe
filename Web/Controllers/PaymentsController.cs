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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class PaymentsController : Controller
    {
        public ActionResult Index ()
		{
			var drawer = Configuration.CashDrawer;
            var session = GetSession ();

            if (drawer == null) {
                return View ("InvalidCashDrawer");
            }

            if (session == null) {
                return RedirectToAction ("OpenSession");
            }

            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled && !x.IsCredit
					  orderby x.Id descending
                      select x;

            return View (new MasterDetails<CashSession, SalesOrder> { Master = session, Details = qry.ToList() });
        }

        [HttpPost]
		public ActionResult Index (int? id)
		{
			IList<SalesOrder> items;
			var drawer = Configuration.CashDrawer;
			var session = GetSession ();

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			if (id != null && id > 0) {
				var qry = from x in SalesOrder.Queryable
	                      where x.IsCompleted && !x.IsPaid && 
								!x.IsCancelled && !x.IsCredit &&
								x.Id == id
						  orderby x.Id descending
	                      select x;

				items = qry.ToList ();
			} else {
				var qry = from x in SalesOrder.Queryable
	                      where x.IsCompleted && !x.IsPaid && 
								!x.IsCancelled && !x.IsCredit
						  orderby x.Id descending
	                      select x;

				items = qry.ToList ();
            }

            if (Request.IsAjaxRequest()) {
                return PartialView ("_Index", items);
            }
            else {
                return View (new MasterDetails<CashSession, SalesOrder> { Master = session, Details = items }); 
            }
        }

        // GET: /Payments/PrintCashCount/

        public ViewResult PrintCashCount (int id)
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
            if (GetSession() != null) {
                return RedirectToAction ("Index");
            }
			
            var model = new CashSession {
                Start = DateTime.Now,
                CashCounts = CashHelpers.ListDenominations (),
                CashDrawer = Configuration.CashDrawer,
                Cashier = SecurityHelpers.GetUser(User.Identity.Name).Employee
            };

            if (model.CashDrawer == null) {
                return View ("InvalidCashDrawer");
            }

            return View (model);
        }

        [HttpPost]
		public ActionResult OpenSession (CashSession item)
		{            
			item.CashDrawer = Configuration.CashDrawer;

            if (item.CashDrawer == null)
                return View("InvalidCashDrawer");

			var cash_counts = item.CashCounts.Where(x => x.Quantity > 0).ToList();

            item.Start = DateTime.Now;
            item.Cashier = Model.User.Find(User.Identity.Name).Employee;
            item.CashCounts.Clear();

            using (var scope = new TransactionScope()) {
                item.Create ();

	            foreach (var x in cash_counts) {
	                x.Session = item;
	                x.Type = CashCountType.StartingCash;
	                x.Create ();
	            }
            }

			System.Diagnostics.Debug.WriteLine("New CashSession [Id = {0}]", item.Id);

            return RedirectToAction ("Index");
        }

        public ActionResult PayOrder (int id)
		{
			var item = SalesOrder.Find (id);

			item.Details.ToList ();
			item.Payments.ToList ();

			return View (item);
        }
		
        public ActionResult GetSalesOrderBalance (int id)
		{
			var item = SalesOrder.Find (id);

			item.Details.ToList ();
			item.Payments.ToList ();

			return PartialView ("_SalesOrderBalance", item);
        }

        [HttpPost]
		public JsonResult AddPayment (int order, int type, decimal amount, string reference)
		{
			var sales_order = SalesOrder.Find (order);

			sales_order.Details.ToList ();
			sales_order.Payments.ToList ();

			var dt = DateTime.Now;
			var employee = SecurityHelpers.GetUser (User.Identity.Name).Employee;

			var item = new CustomerPayment {
				Creator = employee,
				CreationTime = dt,
				Updater = employee,
				ModificationTime = dt,
                CashSession = GetSession (),
                SalesOrder = sales_order,
                Customer = sales_order.Customer,
                Method = (PaymentMethod)type,
                Amount = amount,
                Date = DateTime.Now,
                Reference = reference
            };
			
			// Store and Serial
			item.Store = item.CashSession.CashDrawer.Store;
			try {
				item.Serial = (from x in CustomerPayment.Queryable
	            			   where x.Store.Id == item.Store.Id
	                      	   select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			if (item.Method == PaymentMethod.Cash) {
				if (item.Amount > -item.SalesOrder.Balance) {
					item.Change = item.Amount + item.SalesOrder.Balance;
					item.Amount = -item.SalesOrder.Balance;
				}
			} else {
				if (item.Amount > -item.SalesOrder.Balance) {
					item.Amount = -item.SalesOrder.Balance;
				}
			}

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New Payment [Id = {0}]", item.Id);

			return Json (new { id = item.Id });
		}
		
        public ActionResult CreditPayment ()
        {
            if (Request.IsAjaxRequest ())
            {
                return PartialView("_CreditPayment", new CustomerPayment());
            }
			
            return View ("_CreditPayment", new CustomerPayment());
        }
		
        [HttpPost]
        public ActionResult CreditPayment (CustomerPayment item)
		{
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.CreationTime = DateTime.Now;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;
			item.CashSession = GetSession ();
			item.Customer = Customer.Find (item.CustomerId);
			item.Date = DateTime.Now;
			item.Change = 0m;
			
			// Store and Serial
			item.Store = item.CashSession.CashDrawer.Store;
			try {
				item.Serial = (from x in CustomerPayment.Queryable
	            			   where x.Store.Id == item.Store.Id
	                      	   select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New Payment [Id = {0}]", item.Id);
			
			if (Request.IsAjaxRequest ()) {
				return PartialView ("_CreditPaymentSuccesful", item);
			}
			
			return View ("_CreditPaymentSuccesful", item);
		}

        public ActionResult GetPayment (int id)
        {
            return PartialView ("_Payment", CustomerPayment.Find (id));
        }

        [HttpPost]
        public JsonResult RemovePayment (int id)
        {
            CustomerPayment item = CustomerPayment.Find (id);
            
			using (var scope = new TransactionScope ()) {
            	item.DeleteAndFlush ();
			}

            return Json (new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult ConfirmPayment (int id)
        {
            var item = SalesOrder.Find (id);

            item.IsPaid = true;
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction ("Index");
        }

        public ActionResult CloseSession ()
        {
            var session = GetSession ();
            session.CashCounts = CashHelpers.ListDenominations ();
            return View (session);
        }

        [HttpPost]
        public ActionResult CloseSession (CashSession item)
        {
        	var cash_counts = item.CashCounts.Where (x => x.Quantity > 0).ToList ();
			
            item = CashSession.Find (item.Id);
            item.End = DateTime.Now;

            using (var scope = new TransactionScope()) {
				foreach (var x in cash_counts) {
                    x.Session = item;
                    x.Type = CashCountType.CountedCash;
                    x.Create ();
                }

	            item.UpdateAndFlush ();
            }

            return RedirectToAction("CloseSessionConfirmed", new { id = item.Id });
        }

        public ActionResult CloseSessionConfirmed (int id)
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
        
        CashSession GetSession ()
		{
			var item = Configuration.CashDrawer;
			
			if (item == null)
				return null;
			
			return CashSession.Queryable.Where (x => x.End == null)
                              .SingleOrDefault (x => x.CashDrawer.Id == item.Id);
		}
    }
}
