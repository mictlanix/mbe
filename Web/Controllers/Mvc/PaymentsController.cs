// 
// PaymentsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2020 Eddy Zavaleta, Mictlanix, and contributors.
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

			if (session.Start.Date < DateTime.Now.Date) {
				return RedirectToAction ("CloseSession");
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
				return PartialView ("_CashPaymentList", search.Results);
			}

			return View (new MasterDetails<CashSession, SalesOrder> { Master = session, Details = search.Results });
		}

		Search<SalesOrder> SearchSalesOrders (Search<SalesOrder> search)
		{
			int id = 0;
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			IQueryable<SalesOrder> query = from x in SalesOrder.Queryable
						       where x.IsCompleted && !x.IsCancelled && !x.IsPaid
						       orderby x.Date descending
						       select x;

			if (!WebConfig.ShowSalesOrdersFromAllStores) {
				query = query.Where (x => x.Store.Id == item.Id);
			}

			if (int.TryParse (pattern, out id) && id > 0) {
				query = query.Where (x => x.Id == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => x.Terms == PaymentTerms.Immediate && x.Customer.Name.Contains (pattern) ||
						     (x.SalesPerson.FirstName + " " + x.SalesPerson.LastName).Contains (pattern));
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult CreditPayments ()
		{

			Search<CustomerPayment> search = new Search<CustomerPayment> ();

			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
							    where x.Allocations.Count == 0 || x.Amount > x.Allocations.Sum (y => y.Amount + y.Change)
							    || x.Allocations.Any (y => y.SalesOrder.Terms != PaymentTerms.Immediate)
							    orderby x.Date descending
							    select x;



			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			search.Limit = WebConfig.PageSize;
			search.Results = query.Take (search.Limit).Skip (search.Offset).ToList ();
			search.Total = query.Count ();

			return View (search);

		}

		[HttpPost]
		public ActionResult CreditPayments (Search<CustomerPayment> search)
		{

			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			search.Limit = WebConfig.PageSize;

			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
							    where x.Allocations.Count == 0 || x.Amount > x.Allocations.Sum (y => y.Amount + y.Change)
							    || x.Allocations.Any (y => y.SalesOrder.Terms != PaymentTerms.Immediate)
							    orderby x.Date descending
							    select x;

			if (int.TryParse (pattern, out id) && id > 0) {
				search.Limit = int.MaxValue;
				query = query.Where (x => x.Id == id);

			} else if (!string.IsNullOrEmpty (pattern)) {
				search.Limit = int.MaxValue;
				query = query.Where (x => x.Customer.Name.Contains (pattern));
			}

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			search.Results = query.Take (search.Limit).Skip (search.Offset).ToList ();
			search.Total = query.Count ();


			if (Request.IsAjaxRequest ()) {
				return PartialView ("_CreditPayments", search);
			}

			return View (search);
		}

		public ActionResult Print (int id)
		{
			var model = SalesOrder.Find (id);
			if (model.IsPaid) { return PdfTicketView ("Print", model); }
			return RedirectToAction ("Index");

		}

		public ActionResult PrintDeliveryTicket (int id)
		{
			var model = SalesOrder.Find (id);
			if (model.IsPaid && model.ShipTo == null) { return PdfTicketView ("DeliveryTicket", model); }
			return RedirectToAction ("Index");
		}

		public ActionResult PrintCreditPayment (int id)
		{

			var model = CustomerPayment.Find (id);
			return PdfTicketView ("PrintCreditPayment", model);
		}

		public ActionResult ViewCreditPayment (int id)
		{

			var item = CustomerPayment.Find (id);
			return View ("ViewCreditPayment", item);
		}

		public ActionResult PrintCashCount (int id)
		{
			var model = new CashCountReport ();
			var session = CashSession.Find (id);
			var qry_pymnts = from x in CustomerPayment.Queryable
				  where x.CashSession.Id == session.Id
				  select new {
					  Type = x.Method,
					  Amount = x.Amount
				  };
			var list_mny = from x in qry_pymnts.ToList ()
				   group x by x.Type into g
				   select new MoneyCount { Type = g.Key, Amount = g.Sum (y => y.Amount) };

			model.Cashier = session.Cashier;
			model.CashDrawer = session.CashDrawer;
			model.Start = session.Start;
			model.End = session.End;
			model.MoneyCounts = list_mny.ToList ();
			model.CashCounts = session.CashCounts.Where (x => x.Type == CashCountType.CountedCash).ToList ();
			//model.ExpensesCounts = ExpenseVoucher.Queryable.Where(x => x.CashSession == session).ToList();
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

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var customer = Customer.TryFind (value);

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (customer == null) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			foreach (var payment in entity.Payments) {
				payment.Delete ();
			}

			if (entity.Customer.Id != WebConfig.DefaultCustomer) {
				entity.CustomerName = string.Empty;
			}

			entity.ShipTo = null;
			entity.CustomerShipTo = null;
			entity.Customer = customer;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer)
			});
		}

		[HttpPost]
		public ActionResult SetCustomerName (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsPaid || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.CustomerName = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = value });
		}

		public ActionResult GetCustomerName (int id)
		{
			return PartialView ("_CustomerName", SalesOrder.Find (id));
		}

		[HttpPost]
		public ActionResult SetCustomerShipTo (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsPaid || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.CustomerShipTo = string.IsNullOrEmpty (val) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			if (string.IsNullOrEmpty (entity.CustomerShipTo)) {
				foreach (var payment in entity.Payments) {
					payment.Delete ();
				}
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = value });
		}

		[HttpPost]
		public ActionResult SetShipTo (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = entity.Customer.Addresses.Where (x => x.Id == value).SingleOrDefault ();

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.ShipTo = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = id, value = entity.ShipTo.ToString (), type = "shipto" });
		}

		public JsonResult Addresses (int id)
		{
			var item = SalesOrder.TryFind (id);

			if (item.Customer.Id == WebConfig.DefaultCustomer) {
				return Json (null, JsonRequestBehavior.AllowGet);
			}

			var query = from x in item.Customer.Addresses
				    select new {
					    value = x.Id,
					    text = x.ToString ()
				    };

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
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
			//entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Payments) {
					var payment = PaymentOnDelivery.Find (item.Id);
					if (payment != null) {
						payment.DeleteAndFlush ();
					}
					item.Delete ();
				}

				entity.UpdateAndFlush ();
			}

			AbastosInventoryHelpers.CancelSalesOrder (entity, CurrentUser.Employee);

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public JsonResult AddPayment (int id, int type, decimal amount, string reference, int? fee, bool ondelivery)
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

			if (fee.HasValue) {
				item.Payment.ExtraFee = PaymentMethodOption.Find (fee.Value);
				item.Payment.Commission = item.Payment.ExtraFee.CommissionByManage;
			}

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

			if (ondelivery && !string.IsNullOrEmpty (sales_order.CustomerShipTo)) {

				item.Payment.CashSession = null;
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

		public ActionResult GetPayments (int id)
		{
			var item = SalesOrder.Find (id);

			return PartialView ("_Payments", item);
		}

		[HttpPost]
		public JsonResult RemovePayment (int id)
		{
			var item = SalesOrderPayment.Find (id);

			using (var scope = new TransactionScope ()) {

				PaymentOnDelivery payment = PaymentOnDelivery.Queryable.FirstOrDefault (x => x.CustomerPayment == item.Payment);
				if (payment != null) {
					payment.DeleteAndFlush ();
				}

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

			return PartialView ("_DetailsView", item);
		}

		public ActionResult ReceiveDeliveryPayment (int id)
		{
			var item = SalesOrderPayment.Find (id);
			if (item.Payment.CashSession == null) {
				item.Payment.CashSession = GetSession ();
				item.Payment.ModificationTime = DateTime.Now;
				using (var scope = new TransactionScope ()) {
					item.Payment.UpdateAndFlush ();
				}
			}
			return PartialView ("_Payment", item);
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

		[HttpPost]
		public ActionResult UnapplySalesOrderPayment (int id, int sales_order_payment_id)
		{

			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();
			var customer_payment = CustomerPayment.Find (id);
			var sales_order_payment = SalesOrderPayment.Find (sales_order_payment_id);
			var date = sales_order_payment.Payment.Date.AddDays (WebConfig.ModificationPaymentsDays);

			if (sales_order_payment.Payment.Date.AddDays(WebConfig.ModificationPaymentsDays) < DateTime.Now) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			using (var scope = new TransactionScope ()) {
				sales_order_payment.SalesOrder.IsPaid = false;
				sales_order_payment.SalesOrder.UpdateAndFlush ();
				sales_order_payment.Delete ();
			}

			if (Request.IsAjaxRequest ()) {
				return Json (new { id = id, result = true, empty = customer_payment.Allocations.Count() < 1 });
			}

			return View ();
		}

		CashSession GetSession ()
		{
			var item = WebConfig.CashDrawer;

			if (item == null)
				return null;

			return CashSession.Queryable.Where (x => x.End == null)
			      .SingleOrDefault (x => x.CashDrawer.Id == item.Id);
		}

		[HttpPost]
		public ActionResult DeleteCustomerPayment (int id) {
			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();
			var customer_payment = CustomerPayment.Find (id);

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}

			if (customer_payment.Allocations.Count > 0) {
				ModelState.AddModelError ("", "Este pago de crédito ha sido aplicado en algunos créditos. Elimínelos e intente nuevamente");
				return RedirectToAction ("ViewCreditPayment", new { id = id });
			}

			using (var scope = new TransactionScope ()) {
				customer_payment.Delete ();
			}
			return RedirectToAction ("CreditPayments");
		}
	}
}
