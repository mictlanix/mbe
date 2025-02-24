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
using NHibernate.Mapping;
using System.Collections.Generic;
using Newtonsoft.Json;
using Castle.Core.Internal;
using System.Runtime.CompilerServices;
//using LinqKit;
//using NHibernate.Linq;


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
			var store = WebConfig.Store;
			var cashier = GetSession ().Cashier;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			//var predicate = PredicateBuilder.New<SalesOrder> ();

			//predicate.Or (x => x.Creator == cashier || x.Updater == cashier || x.SalesPerson == cashier);
			
			var from_all_users = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllUsers);
			var from_all_stores = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllStores);
			IQueryable<SalesOrder> query = from x in MBEQueryable.IQSalesOrders
						       where x.IsCompleted && !x.IsCancelled
						       && !x.IsPaid 
						       select x;
			// TODO: revisar si va el filtro de abajo

			//if (!WebConfig.ShowSalesOrdersFromAllStores) {
			//	query = query.Where (x => x.Store.Id == store.Id);
			//}

			//query = from_all_stores.AllowRead ? query : query.Where (x => x.Store == store);
			//if (!from_all_stores.AllowRead) {
			//	predicate.And (x => x.Store == store);
			//}

			//if (!from_all_users.AllowRead) {
			//	predicate.And (x => x.Creator == cashier || x.Updater == cashier || x.SalesPerson == cashier);
			//}

			query = from_all_users.AllowRead ? query : query.Where (x => x.Creator == cashier || x.SalesPerson == cashier);


			if (int.TryParse (pattern, out id) && id > 0) {
				query = MBEQueryable.IQSalesOrders.Where (x => x.Id == id || x.Serial == id);
				//predicate.Or (x => x.Id == id || x.Serial == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => (x.Customer.Name.Contains (pattern) ||
						     (x.SalesPerson.FirstName + " " + x.SalesPerson.LastName).Contains (pattern)));
			}

			//query = query.Where (predicate);

			query = query.OrderByDescending (x => x.Date);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		Search<CustomerPayment> SearchPayments (Search<CustomerPayment> search)
		{
			int id = 0;
			var store = WebConfig.Store;
			var cashier = GetSession ().Cashier;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			//var predicate = PredicateBuilder.New<SalesOrder> ();

			//var from_all_users = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllUsers);
			//var from_all_stores = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllStores);
			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
							    where x.PaymentType == PaymentType.CreditPayment || x.PaymentType == PaymentType.PaymentInAdvance
						       select x;


			if (int.TryParse (pattern, out id) && id > 0) {
				query = CustomerPayment.Queryable.Where (x => x.Id == id || x.Serial == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => (x.Customer.Name.Contains (pattern) ||
						     (x.CashSession.Cashier.Name).Contains (pattern)));
			}

			query = query.OrderByDescending (x => x.Id);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult CreditPayments ()
		{

			//Search<CustomerPayment> search = new Search<CustomerPayment> ();
			Search<CustomerPayment> search = SearchPayments(new Search<CustomerPayment>());

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

		Search<CustomerPayment> GetCustomerPaymentSearch (Search<CustomerPayment> search)
		{
			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
								    //orderby x.Id descending
							    select x;
			var pattern = string.IsNullOrEmpty (search.Pattern) ? string.Empty : search.Pattern.Trim ();

			if (!string.IsNullOrEmpty (pattern)) {
				if (Int32.TryParse (pattern, out int result)) {
					query = from x in query
						where x.Allocations.Any(Any => Any.SalesOrder.Id == result)
						select x;
				} else {
					query = from x in query
						where x.Customer.Name.Contains (pattern)
						|| x.Creator.FirstName.Contains (pattern)
						|| x.Updater.FirstName.Contains (pattern)
						|| x.Customer.Name.Contains(pattern)
						select x;

				}

			}

			query = query.OrderByDescending (x => x.Id);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Payments ()
		{

			Search<CustomerPayment> search = new Search<CustomerPayment> ();

			search.Limit = WebConfig.PageSize;
			search = GetCustomerPaymentSearch (search);


			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();
			var privilege = GetAccessPrivilege (SystemObjects.PaymentsEditor);

			if (!privilege.AllowRead || !CurrentUser.IsAdministrator) {
				return RedirectToAction ("Index");
			}

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}



			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);

		}

		[HttpPost]
		public ActionResult Payments (Search<CustomerPayment> search)
		{

			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			var privilege = GetAccessPrivilege (SystemObjects.PaymentsEditor);

			if (!privilege.AllowRead || !CurrentUser.IsAdministrator) {
				return RedirectToAction ("Index");
			}


			search.Limit = WebConfig.PageSize;

			search = GetCustomerPaymentSearch (search);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult PaymentsOnDelivery (Search<CustomerPayment> search)
		{

			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();

			//var privilege = GetAccessPrivilege (SystemObjects.Ondeli);

			//if (!privilege.AllowRead || !CurrentUser.IsAdministrator) {
			//	return RedirectToAction ("Index");
			//}


			search.Limit = WebConfig.PageSize;

			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
								    //orderby x.Id descending
							    select x;
			var pattern = string.IsNullOrEmpty (search.Pattern) ? string.Empty : search.Pattern.Trim ();

			if (!string.IsNullOrEmpty (pattern)) {
				if (Int32.TryParse (pattern, out int result)) {
					query = from x in query
						where x.Allocations.Any (Any => Any.SalesOrder.Id == result)
						select x;
				} else {
					query = from x in query
						where x.Customer.Name.Contains (pattern)
						|| x.Creator.FirstName.Contains (pattern)
						|| x.Updater.FirstName.Contains (pattern)
						|| x.Customer.Name.Contains (pattern)
						select x;

				}

			}

			query = query.OrderByDescending (x => x.Id);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			//return search;

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_DeliveryPayment", search);
			}

			return View (search);
		}

		public ActionResult EditPayment (int id)
		{
			var privilege = GetAccessPrivilege (SystemObjects.PaymentsEditor);

			if (!privilege.AllowUpdate) {
				return RedirectToAction ("Index");
			}

			return PartialView ("_EditPayment", CustomerPayment.Find (id));
		}

		[HttpPost]
		public ActionResult EditPayment (CustomerPayment item)
		{

			var privilege = GetAccessPrivilege (SystemObjects.PaymentsEditor);

			if (!privilege.AllowUpdate) {
				return RedirectToAction ("Index");
			}

			var payment = CustomerPayment.Find (item.Id);
			if (payment.CreationTime != payment.ModificationTime) {
				ModelState.AddModelError ("Error", Resources.ItemCanBeChangedOnlyOnce);
			}

			if (ModelState.IsValid) {
				payment.Amount = item.Amount;
				payment.Method = item.Method;
				payment.PaymentType = item.PaymentType;
				payment.ModificationTime = DateTime.Now;
				payment.Updater = CurrentUser.Employee;

				using (var scope = new TransactionScope ()) {
					payment = CustomerPayment.Find (item.Id);
					var incidence = new Incidence {
						SourceType = SourceType.CustomerPayment,
						Reference = item.Id,
						Updater = CurrentUser.Employee,
						PreviousState = JsonConvert.SerializeObject (payment.GetSerializable ()),
						ModificationTime = DateTime.Now,
					};
					incidence.CreateAndFlush ();
				}
				using (var scope = new TransactionScope ()) {
					payment.UpdateAndFlush ();
				}
			} else {
				return PartialView ("_EditPayment", item);
			}
			return PartialView ("_RefreshPayment");
		}

		public ActionResult Print (int id)
		{
			var model = SalesOrder.Find (id);
			if (model.IsCompleted) { return PdfTicketView ("Print", model); }
			return RedirectToAction ("Index");

		}

		//public ActionResult PrintDeliveryTicket (int id)
		//{
		//	var model = SalesOrder.Find (id);
		//	if (model.IsPaid && model.ShipTo == null) { return PdfTicketView ("DeliveryTicket", model); }
		//	return RedirectToAction ("Index");
		//}

		public ActionResult PrintCreditPayment (int id)
		{

			var model = CustomerPayment.Find (id);
			if (model.PaymentType == PaymentType.PaymentInAdvance) {
				return PdfTicketView ("PrintPaymentInAdvance", model);
			}
			return PdfTicketView ("PrintCreditPayment", model);
		}

		public ActionResult ViewCreditPayment (int id)
		{

			var item = CustomerPayment.Find (id);
			return View ("ViewCreditPayment", item);
		}

		public ActionResult PrintCashCount (int id)
		{

			var model = GetCashCountReport (id);
			return PdfTicketView ("_CashCountTicket", model);
		}

		private CashCountReport GetCashCountReport (int id)
		{
			var model = new CashCountReport ();
			var session = CashSession.Find (id);

			var qry = from x in CustomerPayment.Queryable
				  where x.CashSession.Id == session.Id
				  select new {
					  Method = x.Method,
					  Type = x.PaymentType,
					  Amount = x.Allocations.Sum (y => (decimal?) y.Amount) ?? 0,
				  };

			var refunds = (from x in CustomerRefund.Queryable
				       where !x.IsCancelled && x.IsCompleted
					      && x.Updater == session.Cashier
					      && x.CreationTime > session.Start && x.CreationTime < session.End
				       select x).ToList ();

			var expenses = (from x in ExpenseVoucher.Queryable
					where !x.IsCancelled && x.IsCompleted
					&& x.Updater == session.Cashier
					&& x.CreationTime > session.Start && x.CreationTime <= session.End
					select x).ToList ();

			var list = from x in qry.ToList ()
				   where x.Type != PaymentType.Refund
				   group x by x.Method into g
				   select new MoneyCount { Method = g.Key, Amount = g.Sum (y => y.Amount) };

			var list_refunds = from x in qry.ToList()
					   where x.Type == PaymentType.Refund
					   group x by x.Method into g
					   select new MoneyCount { Method = g.Key, Amount = g.Sum (y => y.Amount) };


			model.Cashier = session.Cashier;
			model.CashDrawer = session.CashDrawer;
			model.Start = session.Start;
			model.End = session.End;
			model.MoneyCounts = list.Where(x => x.Type != PaymentType.Refund).ToList ();
			model.Expenses = expenses;
			model.Refunds = list_refunds.ToList();
			model.CashCounts = session.CashCounts.Where (x => x.Type == CashCountType.CountedCash).ToList ();
			model.StartingCash = session.StartingCash;
			model.SessionId = session.Id;

			return model;
		}

		public ActionResult OpenSession ()
		{
			if (GetSession () != null) {
				return RedirectToAction ("Index");
			}

			IList<CashCount> cashcount = WebConfig.CashCountsByDenominations ?
				CashHelpers.ListDenominations ()
				: new List<CashCount> { new CashCount { Denomination = 1 } };

			var model = new CashSession {
				Start = DateTime.Now,
				CashCounts = cashcount,
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

		public JsonResult GetPaymentMethods ()
		{

			var items = WebConfig.CashierPaymentOptions
				.Select (x => new { id = x, name = x.GetDisplayName () });
			return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var customer = Customer.TryFind (value);

			//if (entity.IsCancelled || entity.IsPaid || entity.Customer.Id != WebConfig.DefaultCustomer) {
			if (entity.IsCancelled || entity.Customer.Id != WebConfig.DefaultCustomer) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (customer == null) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			//foreach (var payment in entity.Payments) {
			//	payment.Delete ();
			//}

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

		[HttpPost]
		public ActionResult PrintablePaymentTickets (string value)
		{
			string val = (value ?? string.Empty).Trim ();

			var query = CustomerPayment.Queryable.Where (x => (x.PaymentType == PaymentType.CreditPayment || x.PaymentType == PaymentType.PaymentInAdvance)
			&& x.Customer.Name.Contains(value)).Take(WebConfig.PageSize);


			//return Json (new { id = id, value = value });
			return PartialView ("_PaymentList", query.ToList());
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

			return Json (new {
				id = id,
				value = entity.ShipTo.ToString (),
				address_id = item.Id,
				type = "shipto"
			});
		}

		[HttpPost]
		public ActionResult SetTermToCredit (int id)
		{

			var entity = SalesOrder.Find (id);

			var expired = entity.Customer.HasExpiredCredits ();
			var debt = entity.Customer.Debt ();
			var credit = entity.Customer.CreditLimit;
			var dt = DateTime.Now;

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (!entity.Customer.HasCredit) {
				Response.StatusCode = 400;
				return Content (Resources.CreditLimitIsNotSet);
			}

			if (expired || entity.IsOverCreditLimit()) {
				Response.StatusCode = 400;
				return Content (Resources.CreditStatusNeedsToBeVerified);
			}

			if (!expired && entity.Customer.HasCredit) {

				entity.Terms = PaymentTerms.NetD;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.DueDate = dt.Date.AddDays (entity.Customer.CreditDays);
				

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.Terms,
				dueDate = entity.FormattedValueFor (x => x.DueDate),
				success = true
			});
		}

		[HttpPost]
		public ActionResult ApplyInAdvancePayment (int id, decimal value)
		{

			var entity = SalesOrder.Find (id);
			var to_pay = value;
			var dt = DateTime.Now;


			var payments = CustomerPayment.Queryable.Where (x => x.PaymentType == PaymentType.PaymentInAdvance
					&& x.Customer == entity.Customer).ToList ();
			var remaining = payments.Where(x=> !x.Allocations.Select(y => y.SalesOrder).Contains(entity))
						.Sum (x => (decimal?)x.Balance)??0;

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value > remaining) {
				Response.StatusCode = 400;
				return Content(Resources.InsufficientFunds);
			}



			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();

				foreach (var payment in payments) {
					var sop = new SalesOrderPayment {
						Amount = to_pay > payment.Balance ? payment.Balance : to_pay,
						Payment = payment,
						SalesOrder = entity
					};

					sop.CreateAndFlush ();

					to_pay -= sop.Amount;
					if (to_pay <= 0.01m) {
						break;
					}
				}
			}
			

			return Json (new {
				id = id,
				value = entity.Terms,
				dueDate = entity.FormattedValueFor (x => x.DueDate),
				success = true
			});
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
			var date = DateTime.Now;

			if (!entity.IsCompleted || entity.IsCancelled || entity.IsPaid) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = date;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Payments) {
					item.Delete ();
					if (item.Payment.PaymentType == PaymentType.Immediate) {
						item.Payment.Delete ();
					}
				}

				foreach (var detail in entity.Details.Where(x => x.Product.IsStockable)) {
					InventoryHelpers.ChangeNotification (TransactionType.CustomerRefund, entity.Id, date, detail.Warehouse, null ,detail.Product, detail.Quantity);
				}

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult AddPayment (int id, int type, decimal amount, string reference, int? fee, bool ondelivery)
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
					Currency = sales_order.Currency,
					//PaymentType = PaymentType.Immediate
				},
				Amount = amount
			};

			//if (fee.HasValue) {
			//	item.Payment.ExtraFee = PaymentMethodOption.Find (fee.Value);
			//	item.Payment.Commission = item.Payment.ExtraFee.CommissionByManage;
			//}

			// Store and Serial

			item.Payment.Store = store;

			//try {
			//	item.Payment.Serial = (from x in CustomerPayment.Queryable
			//			       where x.Store.Id == store.Id
			//			       select x.Serial).Max () + 1;
			//} catch {
			//	item.Payment.Serial = 1;
			//}

			item.Payment.Serial = (CustomerPayment.Queryable.Where(x => x.Store == store).Max(y => (int?)y.Serial) ?? 0) + 1;

			if (item.Amount > item.SalesOrder.Balance) {
				//if (item.Payment.Method == PaymentMethod.Cash) {
				//	item.Change = item.Amount - item.SalesOrder.Balance;
				//} else {
				//	item.Payment.Amount = item.SalesOrder.Balance;
				//}

				item.Amount = item.SalesOrder.Balance;
			}

			if (ondelivery) {

				item.Payment.CashSession = null;
				var amount_on_delivery = sales_order.Payments.Where (y => y.Payment.CashSession == null && y.Payment.Method == PaymentMethod.Cash)
					.Sum (x => (decimal?) x.Amount ?? 0.0m) + (item.Payment.Method == PaymentMethod.Cash ? item.Payment.Amount : 0);
				if (amount_on_delivery > WebConfig.MaxAmountOnPaymentDelivery) {
					Response.StatusCode = 400;
					return Content (String.Format (Resources.MaxAmountOnPaymentDelivery, WebConfig.MaxAmountOnPaymentDelivery));
				}
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
		public ActionResult CreditPayment (FormCollection form)
		{
			var item = new CustomerPayment ();
			TryUpdateModel (item, form);
			item.Customer = Customer.TryFind (Int32.Parse(form["credit_payer"]));
			item.PaymentType = PaymentType.CreditPayment;
			item.Method = (PaymentMethod) Int32.Parse (form ["Method"]);
			var dt = DateTime.Now;

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
			item.CreationTime = dt;
			item.Updater = item.Creator;
			item.ModificationTime = dt;
			item.Date = dt;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_CreditPaymentSuccesful", item);
			}

			return View ("_CreditPaymentSuccesful", item);
		}

		[HttpPost]
		public ActionResult PaymentInAdvance (FormCollection form)
		{
			var item = new CustomerPayment ();
			TryUpdateModel (item, form);
			item.Customer = Customer.TryFind (Int32.Parse (form ["CustomerId"]));
			item.PaymentType = PaymentType.PaymentInAdvance;
			item.Customer = Customer.TryFind (item.CustomerId);
			item.Method = (PaymentMethod) Int32.Parse (form ["PaymentInAdvanceMethod"]);
			var dt = DateTime.Now;

			if (!ModelState.IsValid) {
				return PartialView ("_PaymentInAdvance", item);
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
			item.CreationTime = dt;
			item.Updater = item.Creator;
			item.ModificationTime = dt;
			item.Date = dt;

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

		public ActionResult ReceivedPaymentsEditor (int id)
		{
			var item = SalesOrder.Find (id);

			return PartialView ("_Payments", item);
		}

		[HttpPost]
		//public JsonResult RemovePayment (int id)
		public ActionResult RemovePayment (int id)
		{
			var item = SalesOrderPayment.Find (id);

			//if (item.SalesOrder.IsCredit) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.ItemAlreadyCompletedOrCancelled);
			//}

			using (var scope = new TransactionScope ()) {

				item.DeleteAndFlush ();
				if (item.Payment.PaymentType == null) {
					item.Payment.DeleteAndFlush ();
				}
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
			var time = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				item.Payments.ForEach (x => { x.Payment.PaymentType =
							x.Payment.PaymentType.HasValue ? x.Payment.PaymentType : PaymentType.Immediate;
					x.UpdateAndFlush ();
				});

				if (item.Balance <= 0.1m) {
					item.IsPaid = true;
					item.ModificationTime = time;
					item.Updater = CurrentUser.Employee;
					item.UpdateAndFlush();
				}
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
			IList<CashCount> cashcount = WebConfig.CashCountsByDenominations ?
				CashHelpers.ListDenominations ()
				: new List<CashCount> { new CashCount { Denomination = 1 } };


			session.CashCounts = cashcount;

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
			return View (GetCashCountReport (id));
			//var session = CashSession.Find (id);
			//var qry = from x in CustomerPayment.Queryable
			//	  where x.CashSession.Id == session.Id
			//	  select new {
			//		  Type = x.Method,
			//		  Amount = x.Amount
			//	  };
			//var list = from x in qry.ToList ()
			//	   group x by x.Type into g
			//	   select new MoneyCount { Type = g.Key, Amount = g.Sum (y => y.Amount) };

			//return View (new MasterDetails<CashSession, MoneyCount> {
			//	Master = session,
			//	Details = list.ToList ()
			//});
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
