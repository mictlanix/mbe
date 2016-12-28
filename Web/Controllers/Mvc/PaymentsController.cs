// 
// PaymentsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class PaymentsController : CustomController {
		public ActionResult Index ()
		{
			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			var search = SearchSalesOrders (new Search<SalesOrder> {
				Limit = WebConfig.PageSize
			});

			return View (new MasterDetails<CashSession, SalesOrder> { Master = session, Details = search.Results });
		}

		[HttpPost]
		public ActionResult Index (string id)
		{
			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			var search = SearchSalesOrders (new Search<SalesOrder> {
				Limit = int.MaxValue,
				Pattern = id
			});

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search.Results);
			}

			return View (new MasterDetails<CashSession, SalesOrder> { Master = session, Details = search.Results });
		}

		Search<SalesOrder> SearchSalesOrders (Search<SalesOrder> search)
		{
			IQueryable<SalesOrder> query;
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id) && id > 0) {
				query = from x in SalesOrder.Queryable
					where x.Store.Id == item.Id && x.IsCompleted && !x.IsCancelled &&
						!x.IsPaid && x.Terms == PaymentTerms.Immediate &&
						x.Id == id
					orderby x.Date descending
					select x;
			} else if (string.IsNullOrEmpty (pattern)) {
				query = from x in SalesOrder.Queryable
					where x.Store.Id == item.Id && x.IsCompleted && !x.IsCancelled &&
						!x.IsPaid && x.Terms == PaymentTerms.Immediate
					orderby x.Date descending
					select x;
			} else {
				query = from x in SalesOrder.Queryable
					where x.Store.Id == item.Id && x.IsCompleted && !x.IsCancelled &&
						!x.IsPaid && x.Terms == PaymentTerms.Immediate &&
						(x.Customer.Name.Contains (pattern) ||
						 (x.SalesPerson.FirstName + " " + x.SalesPerson.LastName).Contains (pattern))
					orderby x.Date descending
					select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Print (int id)
		{
			var model = SalesOrder.Find (id);
			return PdfTicketView ("Print", model);
		}

		public ActionResult PrintCashCount (int id)
		{
			var model = new CashCountReport ();
			var session = CashSession.Find (id);
			var qry = from x in CustomerPayment.Queryable
				  where x.CashSession.Id == session.Id
				  select new {
					  Type = x.Method,
					  Amount = x.Amount
				  };
			var list = from x in qry.ToList ()
				   group x by x.Type into g
				   select new MoneyCount { Type = g.Key, Amount = g.Sum (y => y.Amount) };

			model.Cashier = session.Cashier;
			model.CashDrawer = session.CashDrawer;
			model.Start = session.Start;
			model.End = session.End;
			model.MoneyCounts = list.ToList ();
			model.CashCounts = session.CashCounts.Where (x => x.Type == CashCountType.CountedCash).ToList ();
			model.StartingCash = session.StartingCash;
			model.SessionId = session.Id;

			return PdfTicketView ("_CashCountTicket", model);
		}

		public ActionResult OpenSession ()
		{
			if (GetSession () != null) {
				return RedirectToAction ("Index");
			}

			var model = new CashSession {
				Start = DateTime.Now,
				CashCounts = CashHelpers.ListDenominations (),
				CashDrawer = WebConfig.CashDrawer,
				Cashier = CurrentUser.Employee
			};

			if (model.CashDrawer == null) {
				return View ("InvalidCashDrawer");
			}

			return View (model);
		}

		[HttpPost]
		public ActionResult OpenSession (CashSession item)
		{
			item.CashDrawer = WebConfig.CashDrawer;

			if (item.CashDrawer == null)
				return View ("InvalidCashDrawer");

			if (GetSession () != null)
				return RedirectToAction ("Index");

			var cash_counts = item.CashCounts.Where (x => x.Quantity > 0).ToList ();

			item.Start = DateTime.Now;
			item.Cashier = Model.User.Find (User.Identity.Name).Employee;
			item.CashCounts.Clear ();

			using (var scope = new TransactionScope ()) {
				item.Create ();

				foreach (var x in cash_counts) {
					x.Session = item;
					x.Type = CashCountType.StartingCash;
					x.Create ();
				}
			}

			return RedirectToAction ("Index");
		}

		public ActionResult PayOrder (int id)
		{
			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			var item = SalesOrder.Find (id);
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
		public ActionResult Cancel (int id)
		{
			var entity = SalesOrder.Find (id);

			if (!entity.IsCompleted || entity.IsCancelled || entity.IsPaid) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Payments) {
					item.Delete ();
				}

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public JsonResult AddPayment (int id, int type, decimal amount, string reference)
		{
			var dt = DateTime.Now;
			var session = GetSession ();
			var store = session.CashDrawer.Store;
			var sales_order = SalesOrder.Find (id);
			var employee = CurrentUser.Employee;
			var item = new SalesOrderPayment {
				SalesOrder = sales_order,
				Payment = new CustomerPayment {
					Creator = employee,
					CreationTime = dt,
					Updater = employee,
					ModificationTime = dt,
					CashSession = session,
					/* SalesOrder = sales_order, */
					Customer = sales_order.Customer,
					Method = (PaymentMethod) type,
					Amount = amount,
					Date = DateTime.Now,
					Reference = reference,
					Currency = sales_order.Currency
				},
				Amount = amount
			};

			// Store and Serial
			item.Payment.Store = store;

			try {
				item.Payment.Serial = (from x in CustomerPayment.Queryable
						       where x.Store.Id == store.Id
						       select x.Serial).Max () + 1;
			} catch {
				item.Payment.Serial = 1;
			}

			if (item.Amount > item.SalesOrder.Balance) {
				if (item.Payment.Method == PaymentMethod.Cash) {
					item.Change = item.Amount - item.SalesOrder.Balance;
				} else {
					item.Payment.Amount = item.SalesOrder.Balance;
				}

				item.Amount = item.SalesOrder.Balance;
			}

			using (var scope = new TransactionScope ()) {
				item.Payment.Create ();
				item.CreateAndFlush ();
			}

			return Json (new {
				id = item.Id
			});
		}

		public ActionResult CreditPayment ()
		{
			var model = new CustomerPayment { Date = DateTime.Now };

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_CreditPayment", model);
			}

			return View ("_CreditPayment", model);
		}

		[HttpPost]
		public ActionResult CreditPayment (CustomerPayment item)
		{
			item.Customer = Customer.TryFind (item.CustomerId);

			if (!ModelState.IsValid) {
				return PartialView ("_CreditPayment", item);
			}

			// Store and Serial
			item.CashSession = GetSession ();
			item.Store = item.CashSession.CashDrawer.Store;

			try {
				item.Serial = (from x in CustomerPayment.Queryable
					       where x.Store.Id == item.Store.Id
					       select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			item.Creator = CurrentUser.Employee;
			item.CreationTime = DateTime.Now;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_CreditPaymentSuccesful", item);
			}

			return View ("_CreditPaymentSuccesful", item);
		}

		public ActionResult GetPayment (int id)
		{
			return PartialView ("_Payment", SalesOrderPayment.Find (id));
		}

		[HttpPost]
		public JsonResult RemovePayment (int id)
		{
			var item = SalesOrderPayment.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
				item.Payment.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		[HttpPost]
		public ActionResult ConfirmPayment (int id)
		{
			var item = SalesOrder.Find (id);

			item.IsPaid = true;
			item.ModificationTime = DateTime.Now;
			item.Updater = CurrentUser.Employee;

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

			using (var scope = new TransactionScope ()) {
				foreach (var x in cash_counts) {
					x.Session = item;
					x.Type = CashCountType.CountedCash;
					x.Create ();
				}

				item.UpdateAndFlush ();
			}

			return RedirectToAction ("CloseSessionConfirmed", new {
				id = item.Id
			});
		}

		public ActionResult CloseSessionConfirmed (int id)
		{
			var session = CashSession.Find (id);
			var qry = from x in CustomerPayment.Queryable
				  where x.CashSession.Id == session.Id
				  select new {
					  Type = x.Method,
					  Amount = x.Amount
				  };
			var list = from x in qry.ToList ()
				   group x by x.Type into g
				   select new MoneyCount { Type = g.Key, Amount = g.Sum (y => y.Amount) };

			return View (new MasterDetails<CashSession, MoneyCount> {
				Master = session,
				Details = list.ToList ()
			});
		}

		CashSession GetSession ()
		{
			var item = WebConfig.CashDrawer;

			if (item == null)
				return null;

			return CashSession.Queryable.Where (x => x.End == null)
			      .SingleOrDefault (x => x.CashDrawer.Id == item.Id);
		}
	}
}
