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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Castle.ActiveRecord;
using LinqKit;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Services;
using Newtonsoft.Json;
using NHibernate;


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
				Limit = WebConfig.PageSize,
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
			var COUNT = 0;
			var store = WebConfig.Store;
			var cashier = GetSession ().Cashier;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			var WHERE_PATTERN = @"	 AND (c.name COLLATE utf8_general_ci LIKE :pattern
						OR sa.nickname COLLATE utf8_general_ci LIKE :pattern
						OR so.customer_name COLLATE utf8_general_ci LIKE :pattern
						OR sp.nickname COLLATE utf8_general_ci LIKE :pattern ) ";
			var WHERE_EMPLOYEE = @" AND (so.creator = :cashier OR so.updater = :cashier OR c.salesperson = :cashier ) ";
			var WHERE_PAID = @" AND so.paid = 0";
			var WHERE_ID = @" AND (so.sales_order_id = :id OR so.serial = :id) ";
			var OFFSET_TAG = @" LIMIT :limit OFFSET :offset ";
			var results = new List<SalesOrder> ();
			var offset = search.Offset * search.Limit;
			var limit = search.Limit;
			var escaped_wildcard = Regex.Escape (Resources.WilcardStringPatternForSearch) + "+";

			var sql = @"
					SELECT so.*
					FROM sales_order so
					JOIN sales_order_detail sod ON sod.sales_order = so.sales_order_id
					LEFT JOIN (	SELECT crd.sales_order_detail, SUM(crd.quantity) quantity 
							FROM customer_refund cr 
							JOIN customer_refund_detail crd ON crd.customer_refund = cr.customer_refund_id
							WHERE cr.completed = 1 AND cr.cancelled = 0
							GROUP BY crd.sales_order_detail) AS r ON r.sales_order_detail = sod.sales_order_detail_id
					JOIN customer c ON so.customer = c.customer_id
					JOIN point_sale ps ON so.point_sale = ps.point_sale_id
					LEFT JOIN employee sa ON c.salesperson = sa.employee_id
					LEFT JOIN employee sp ON so.salesperson = sp.employee_id
					LEFT JOIN employee cr ON so.creator = cr.employee_id
					WHERE so.completed = 1 AND so.cancelled = 0
					WHERE_PAID
					WHERE_PATTERN
					WHERE_EMPLOYEE
					WHERE_ID	
					GROUP BY so.sales_order_id
					HAVING SUM(sod.quantity - IFNULL(r.quantity,0)) > 0
					ORDER BY so.sales_order_id DESC
					OFFSET_TAG
					";

			var sql_count_rows = "SELECT COUNT(*) AS rows_count FROM (" + sql.Replace ("OFFSET_TAG", string.Empty) + ") AS pagging;";
			var sql_pagging = sql.Replace("OFFSET_TAG", OFFSET_TAG);

			if(pattern.Contains(Resources.WilcardStringPatternForSearch)) {
				WHERE_EMPLOYEE = string.Empty;
				pattern = Regex.Replace (pattern, escaped_wildcard, string.Empty);
			}

			if (string.IsNullOrEmpty (pattern)) {
				WHERE_PATTERN = string.Empty;
				WHERE_ID = string.Empty;
			} else {
				if(int.TryParse(pattern, out id) && id > 0) {
					WHERE_EMPLOYEE = string.Empty;
					WHERE_PATTERN = string.Empty;
				} else {
					WHERE_ID = string.Empty;
				}
				WHERE_PAID = string.Empty;

			}

			sql_count_rows = sql_count_rows.Replace ("WHERE_PATTERN", WHERE_PATTERN);
			sql_count_rows = sql_count_rows.Replace ("WHERE_EMPLOYEE", WHERE_EMPLOYEE);
			sql_count_rows = sql_count_rows.Replace ("WHERE_PAID", WHERE_PAID);
			sql_count_rows = sql_count_rows.Replace ("WHERE_ID", WHERE_ID);

			sql_pagging = sql_pagging.Replace ("WHERE_PATTERN", WHERE_PATTERN);
			sql_pagging = sql_pagging.Replace ("WHERE_EMPLOYEE", WHERE_EMPLOYEE);
			sql_pagging = sql_pagging.Replace ("WHERE_PAID", WHERE_PAID);
			sql_pagging = sql_pagging.Replace ("WHERE_ID", WHERE_ID);


			ISession session = ActiveRecordMediator.GetSessionFactoryHolder ().CreateSession (typeof (SalesOrder));
			ISession count_session = ActiveRecordMediator.GetSessionFactoryHolder ().CreateSession (typeof (SalesOrder));
			var pagging = session.CreateSQLQuery (sql_pagging)
				.AddEntity (typeof (SalesOrder))
				.SetParameter ("offset", offset)
				.SetParameter ("limit", limit);
			var counting = count_session.CreateSQLQuery (sql_count_rows)
				.AddScalar("rows_count", NHibernateUtil.Int32);
			if (!string.IsNullOrEmpty (WHERE_PATTERN)) {
				pagging.SetParameter ("pattern", "%" + pattern + "%");
				counting.SetParameter ("pattern", "%" + pattern + "%");
			}
			if (!string.IsNullOrEmpty (WHERE_EMPLOYEE)) {
				pagging.SetParameter ("cashier", cashier.Id);
				counting.SetParameter ("cashier", cashier.Id);
			}
			if (!string.IsNullOrEmpty (WHERE_ID)) {
				pagging.SetParameter ("id", id);
				counting.SetParameter ("id", id);
			}

			results = (List<SalesOrder>) pagging.List<SalesOrder> ();
			COUNT = counting.UniqueResult<int> ();

			search.Total = COUNT;
			search.Results = results;

			return search;
		}

		Search<CustomerPayment> SearchPayments (Search<CustomerPayment> search)
		{
			int id = 0;
			var store = WebConfig.Store;
			var cashier = GetSession ().Cashier;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
 
			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
							    where x.PaymentType == PaymentType.CreditPayment
							    || x.PaymentType == PaymentType.PaymentInAdvance
						       select x;


			if (int.TryParse (pattern, out id) && id > 0) {
				query = query.Where (x => x.Id == id || x.Serial == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => (x.Customer.Name.Contains (pattern) ||
						     (x.CashSession.Cashier.Name).Contains (pattern)));
			}

			query = query.OrderByDescending (x => x.Id);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		Search<CustomerPayment> SearchPaymentsValidation (Search<CustomerPayment> search)
		{
			int id = 0;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			var methods = WebConfig.PaymentMethodsVerificationRequired;
			var list = methods.ToList ();
			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
							    //where x.Method == PaymentMethod.EFT
				    select x;

			//if (methods is IEnumerable<PaymentMethod>) {
			//	query = from x in query
			//		where methods.Contains (x.Method)
			//		select x;
			//}


			if (!string.IsNullOrEmpty (pattern)) {
				if (int.TryParse (pattern, out id) && id > 0) {
					query = query.Where (x => x.Id == id || x.Serial == id || x.Allocations.Any(y => y.SalesOrder.Id == id));
				} else {
					query = query.Where (x => (x.Customer.Name.Contains (pattern) ||
						     (x.CashSession.Cashier.FirstName).Contains (pattern)
						     || (x.CashSession.Cashier.Nickname).Contains (pattern)));
				}
			}

			query = query.Where (x => list.Contains (x.Method));

			search.Total = query.Count ();
			query = query.OrderByDescending (x => x.Id);
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		//Search<CustomerPayment> SearchBankPayments (Search<CustomerPayment> search)
		//{
		//	int id = 0;
		//	var store = WebConfig.Store;
		//	var cashier = GetSession ().Cashier;
		//	var pattern = (search.Pattern ?? string.Empty).Trim ();
		//	//var predicate = PredicateBuilder.New<SalesOrder> ();

		//	//var from_all_users = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllUsers);
		//	//var from_all_stores = GetAccessPrivilege (SystemObjects.SearchAllSalesOrderFromAllStores);
		//	IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
		//					    where x.PaymentType == PaymentType.CreditPayment || x.PaymentType == PaymentType.PaymentInAdvance
		//					    select x;


		//	if (int.TryParse (pattern, out id) && id > 0) {
		//		query = CustomerPayment.Queryable.Where (x => x.Id == id || x.Serial == id);
		//	} else if (!string.IsNullOrEmpty (pattern)) {
		//		query = query.Where (x => (x.Customer.Name.Contains (pattern) ||
		//				     (x.CashSession.Cashier.Name).Contains (pattern)));
		//	}

		//	query = query.OrderByDescending (x => x.Id);

		//	search.Total = query.Count ();
		//	search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

		//	return search;
		//}

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

		Search<CustomerPayment> SearchCustomerPayment (Search<CustomerPayment> search)
		{
			IQueryable<CustomerPayment> query = from x in CustomerPayment.Queryable
								    //orderby x.Id descending
							    select x;
			var pattern = string.IsNullOrEmpty (search.Pattern) ? string.Empty : search.Pattern.Trim ();

			if (!string.IsNullOrEmpty (pattern)) {
				if (Int32.TryParse (pattern, out int result)) {
					query = from x in query
						where x.Allocations.Any(Any => Any.SalesOrder.Id == result) || x.Id == result
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
			search = SearchCustomerPayment (search);


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

			search = SearchCustomerPayment (search);

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

		public ActionResult PaymentsValidation ()
		{

			Search<CustomerPayment> search = new Search<CustomerPayment> ();

			search.Limit = WebConfig.PageSize;
			search = SearchPaymentsValidation (search);


			var drawer = WebConfig.CashDrawer;
			var session = GetSession ();
			var privilege = GetAccessPrivilege (SystemObjects.PaymentsVerification);

			if (!privilege.AllowRead && !CurrentUser.IsAdministrator) {
				return RedirectToAction ("Index");
			}

			if (drawer == null) {
				return View ("InvalidCashDrawer");
			}

			if (session == null) {
				return RedirectToAction ("OpenSession");
			}



			if (Request.IsAjaxRequest ()) {
				return PartialView ("_PaymentsValidation", search);
			}

			return View (search);

		}

		[HttpPost]
		public ActionResult PaymentsValidation (Search<CustomerPayment> search)
		{

			var drawer = WebConfig.CashDrawer;
			//var session = GetSession ();

			var privilege = GetAccessPrivilege (SystemObjects.PaymentsVerification);

			if (!privilege.AllowRead && !CurrentUser.IsAdministrator) {
				return RedirectToAction ("Index");
			}


			search.Limit = WebConfig.PageSize;

			search = SearchPaymentsValidation (search);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_PaymentsValidation", search);
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
			//if (payment.CreationTime != payment.ModificationTime) {
			//	ModelState.AddModelError ("Error", Resources.ItemCanBeChangedOnlyOnce);
			//}

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

			//if (model.Customer.Id == WebConfig.DefaultCustomer) {
			//	model.PartialDeliveries = false;
			//}
			//model.IsDelivered = true;
			//model.ModificationTime = DateTime.Now;
			//model.Updater = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				model.UpdateAndFlush ();
			}

			if (model.IsCompleted) {
				return PdfTicketView ("Print", model);
			}

			return RedirectToAction ("Index");

		}

		public ActionResult PrintPayment (int id)
		{
			var model = CustomerPayment.Find (id);

			switch (model.PaymentType) {
				case PaymentType.CreditPayment:
					return PdfTicketView ("TicketCreditPayment", model);
				case PaymentType.PaymentInAdvance:
					return PdfTicketView ("TicketPaymentInAdvance", model);
				case PaymentType.CreditNote:
					var credit_note = CreditNote.Queryable.Where(x=> x.CustomerPayment == model).SingleOrDefault ();
					if (credit_note != null) {
						return PdfTicketView ("TicketCreditNote", credit_note);
					}
					return null;
				default:
					return null;
			}			
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

			var session_payments = CustomerPayment.Queryable.Where(x => x.CashSession == session).ToList ();

			var qry = from x in CustomerPayment.Queryable
				  where x.CashSession.Id == session.Id
				  select new {
					  Method = x.Method,
					  Type = x.PaymentType,
					  Amount = x.Allocations.Sum (y => (decimal?) y.Amount) ?? 0,
				  };


			var list = from x in qry.ToList ()
				   //where x.Type != PaymentType.Refund
				   group x by x.Method into g
				   select new MoneyCount { Method = g.Key, Amount = g.Sum (y => y.Amount) };

			//var list_refunds = from x in qry.ToList()
			//		   where x.Type == PaymentType.Refund
			//		   group x by x.Method into g
			//		   select new MoneyCount { Method = g.Key, Amount = g.Sum (y => y.Amount) };


			model.Cashier = session.Cashier;
			model.CashDrawer = session.CashDrawer;
			model.Start = session.Start;
			model.End = session.End;


			model.MoneyCounts = list.Where (x => x.Type != PaymentType.CreditNote).ToList ();
			model.Expenses = session_payments.Where (x => x.PaymentType == PaymentType.Expense).ToList ();

			var expenses = ExpenseVoucher.Queryable.Where (x => x.CashSession == session).ToList();
			model.Expenses = expenses.Select (x => new CustomerPayment {
							Amount = (decimal?)x.Total??0,
							CashSession = session,
							CreationTime = x.CreationTime,
							PaymentType = PaymentType.Expense,
							Method = PaymentMethod.Cash,
							}).ToList ();
			model.Refunds = session_payments.Where (x => x.PaymentType == PaymentType.CreditNote).ToList ();
			model.CashCounts = session.CashCounts.Where (x => x.Type == CashCountType.CountedCash).ToList ();
			model.Payments = session_payments.Where (x => x.PaymentType == PaymentType.Immediate
					|| x.PaymentType == PaymentType.CreditPayment || x.PaymentType == PaymentType.PaymentInAdvance || x.PaymentType == PaymentType.CreditNote).ToList ();
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

			if (item.IsCancelled) {
				return RedirectToAction("Index");
			}

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
			entity.Terms = PaymentTerms.Immediate;

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
		public ActionResult ValidatePayment (int id)
		{
			var entity = CustomerPayment.Find (id);
			entity.Verifier = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_PaymentToValidate", entity);
		}

		[HttpPost]
		public ActionResult PrintablePaymentTickets (string value)
		{
			string val = (value ?? string.Empty).Trim ();
			int id = 0;

			var query = CustomerPayment.Queryable.Where (x => (
					x.PaymentType == PaymentType.CreditPayment
					|| x.PaymentType == PaymentType.PaymentInAdvance
					|| (x.PaymentType == PaymentType.CreditNote
						&& x.Customer.Id != WebConfig.DefaultCustomer)));

			if (!string.IsNullOrEmpty (value)) {
				if (Int32.TryParse (val, out id)) {
					query = query.Where (x => x.Id == id || x.Serial == id);
				} else {
					query = query.Where(x => x.Customer.Name.Contains(value));
				}
			}

			query = query.OrderByDescending(x => x.Id).Take (WebConfig.PageSize);

			return PartialView ("_CustomerPaymentList", query.ToList());
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

			if (entity.IsCancelled || entity.IsPaid || entity.Terms == PaymentTerms.NetD) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (entity.Customer.Id == WebConfig.DefaultCustomer) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			if (entity.Customer.HasCredit) {
				if (entity.IsOverCreditLimit () || entity.Customer.HasExpiredCredits()) {
					Response.StatusCode = 400;
					return Content (Resources.CreditStatusNeedsToBeVerified);
				}

				entity.Terms = PaymentTerms.NetD;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.DueDate = dt.Date.AddDays (entity.Customer.CreditDays);

			} else {
				if (entity.Customer.Debt() > 0) {
					Response.StatusCode = 400;
					return Content (Resources.CreditStatusNeedsToBeVerified);
				}

				if (entity.BalanceInCashDrawer() > WebConfig.MaxAmountOneSingleCredit) {
					Response.StatusCode = 400;
					return Content (Resources.CreditLimitExceeded);
				}

				entity.Terms = PaymentTerms.NetD;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.DueDate = dt.Date.AddDays (WebConfig.MaxDaysOneSingleCredit);
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
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

			var qry = @"	SELECT * FROM customer_payment cp
					JOIN customer c ON cp.customer = c.customer_id
					LEFT JOIN (SELECT sop.customer_payment, SUM(sop.amount + sop.amount_change) allocation
						FROM sales_order_payment sop 
						GROUP BY sop.customer_payment) AS a ON cp.customer_payment_id = a.customer_payment
					WHERE c.name LIKE '%MATERIALES ALTAMIRANO%'
					AND cp.payment_type IN (2,3) AND cp.amount - IFNULL(allocation, 0) > 0
					ORDER BY cp.customer_payment_id;";


			var payments = CustomerPayment.Queryable.Where (x =>
					(x.PaymentType == PaymentType.PaymentInAdvance || x.PaymentType == PaymentType.CreditPayment)
					&& !x.Allocations.Any(y => y.SalesOrder == entity)
					&& x.Customer == entity.Customer
					&& (x.Amount -
						(x.Allocations.Sum(y => (decimal?)(y.Amount + y.Change)) ?? 0)
						) > 0).ToList ();
			//var remaining = payments.Where(x=> !x.Allocations.Select(y => y.SalesOrder).Contains(entity))
			//			.Sum (x => (decimal?)x.Balance)??0;
			/*
			 var sql_count_rows = "SELECT COUNT(*) AS rows_count FROM (" + sql.Replace ("OFFSET_TAG", string.Empty) + ") AS pagging;";
			var sql_pagging = sql.Replace("OFFSET_TAG", OFFSET_TAG);

			if(pattern.Contains(Resources.WilcardStringPatternForSearch)) {
				WHERE_EMPLOYEE = string.Empty;
				pattern = Regex.Replace (pattern, escaped_wildcard, string.Empty);
			}

			if (string.IsNullOrEmpty (pattern)) {
				WHERE_PATTERN = string.Empty;
				WHERE_ID = string.Empty;
			} else {
				if(int.TryParse(pattern, out id) && id > 0) {
					WHERE_EMPLOYEE = string.Empty;
					WHERE_PATTERN = string.Empty;
				} else {
					WHERE_ID = string.Empty;
				}
				WHERE_PAID = string.Empty;

			}

			sql_count_rows = sql_count_rows.Replace ("WHERE_PATTERN", WHERE_PATTERN);
			sql_count_rows = sql_count_rows.Replace ("WHERE_EMPLOYEE", WHERE_EMPLOYEE);
			sql_count_rows = sql_count_rows.Replace ("WHERE_PAID", WHERE_PAID);
			sql_count_rows = sql_count_rows.Replace ("WHERE_ID", WHERE_ID);

			sql_pagging = sql_pagging.Replace ("WHERE_PATTERN", WHERE_PATTERN);
			sql_pagging = sql_pagging.Replace ("WHERE_EMPLOYEE", WHERE_EMPLOYEE);
			sql_pagging = sql_pagging.Replace ("WHERE_PAID", WHERE_PAID);
			sql_pagging = sql_pagging.Replace ("WHERE_ID", WHERE_ID);


			ISession session = ActiveRecordMediator.GetSessionFactoryHolder ().CreateSession (typeof (SalesOrder));
			ISession count_session = ActiveRecordMediator.GetSessionFactoryHolder ().CreateSession (typeof (SalesOrder));
			var pagging = session.CreateSQLQuery (sql_pagging)
				.AddEntity (typeof (SalesOrder))
				.SetParameter ("offset", offset)
				.SetParameter ("limit", limit);
			var counting = count_session.CreateSQLQuery (sql_count_rows)
				.AddScalar("rows_count", NHibernateUtil.Int32);
			if (!string.IsNullOrEmpty (WHERE_PATTERN)) {
				pagging.SetParameter ("pattern", "%" + pattern + "%");
				counting.SetParameter ("pattern", "%" + pattern + "%");
			}
			if (!string.IsNullOrEmpty (WHERE_EMPLOYEE)) {
				pagging.SetParameter ("cashier", cashier.Id);
				counting.SetParameter ("cashier", cashier.Id);
			}
			if (!string.IsNullOrEmpty (WHERE_ID)) {
				pagging.SetParameter ("id", id);
				counting.SetParameter ("id", id);
			}

			results = (List<SalesOrder>) pagging.List<SalesOrder> ();
			COUNT = counting.UniqueResult<int> ();

			search.Total = COUNT;
			search.Results = results;

			return search;
			 
			 */
			var funds = payments.Sum (x => x.Amount - (x.Allocations.Sum (y => (decimal?) (y.Amount + y.Change)) ?? 0));

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value > funds) {
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

		[HttpPost]
		public ActionResult ApplyCreditNotes (int id, decimal value)
		{

			var entity = SalesOrder.Find (id);
			var to_pay = value;
			var dt = DateTime.Now;
			var customer = entity.Customer;
			var credit_notes = customer.GetCreditNotes ();
			var payments = credit_notes.Select (x => x.CustomerPayment)
					.Where(x => x.Allocations.All(y => y.SalesOrder != entity));

			var refund_balance = customer.RefundBalance(entity);

			if (customer.Id == WebConfig.DefaultCustomer) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value > refund_balance) {
				Response.StatusCode = 400;
				return Content (Resources.InsufficientFunds);
			}

			to_pay = value > entity.Balance ? entity.Balance : value;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();

				foreach (var payment in payments) {
					var sop = new SalesOrderPayment {
						Amount = to_pay > payment.Balance ? payment.Balance : to_pay,
						Payment = payment,
						SalesOrder = entity,
						Applier = CurrentUser.Employee,
						IsConfirmed = false,
						Date = dt
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

		[HttpPost]
		public ActionResult ApplyCreditNote (int id, int credit_note, decimal value)
		{

			var entity = SalesOrder.Find (id);
			var to_pay = value;
			var dt = DateTime.Now;
			var note = CreditNote.Queryable.Where(x => x.CustomerPayment.Id == credit_note
				&& x.CashSession == null).SingleOrDefault ();

			if (entity.IsCancelled || entity.IsPaid) {
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (note == null) {
				Response.StatusCode = 400;
				return Content(Resources.ItemNotFound);
			}

			if (note.CustomerPayment.Allocations.Any (x => x.SalesOrder == entity)) {
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyAdded);
			}

			value = value > entity.Balance ? entity.Balance : value;

			if (value > note.CustomerPayment.Balance) {
				Response.StatusCode = 400;
				return Content(Resources.InsufficientFunds);
			}



			using (var scope = new TransactionScope ()) {
				var sop = new SalesOrderPayment {
					Amount = value,
					Payment = note.CustomerPayment,
					SalesOrder = entity,
					Applier = CurrentUser.Employee,
					Date = dt,
				};

				sop.CreateAndFlush ();
			}

			return Json (new {
				id = id
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

			if (DeliveryOrderDetail.Queryable.Where(x => x.DeliveryOrder.IsCompleted && !x.DeliveryOrder.IsCancelled)
				.Any (x => entity.Details.Contains (x.OrderDetail))) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrdersHasDeliveryOrders);
			}

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Payments.Where(x => x.Payment.PaymentType == PaymentType.Immediate)) {
					item.DeleteAndFlush ();
					item.Payment.DeleteAndFlush ();
				}

				foreach (var item in entity.Payments.Where (x => x.Payment.PaymentType != PaymentType.Immediate)) {
					item.Delete ();
				}

				foreach (var detail in entity.Details.Where(x => x.Product.IsStockable)) {
					InventoryHelpers.ChangeNotification (TransactionType.CustomerRefund,
						entity.Id, date, detail.Warehouse, null ,detail.Product, detail.Quantity);
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = date;
				entity.IsCancelled = true;

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
				Applier = employee,
				Date = dt,
				Payment = new CustomerPayment {
					Creator = employee,
					CreationTime = dt,
					Updater = employee,
					ModificationTime = dt,
					CashSession = session,
					Customer = sales_order.Customer,
					Method = (PaymentMethod) type,
					Amount = amount,
					Date = DateTime.Now,
					Reference = reference,
					Currency = sales_order.Currency,
					PaymentType = sales_order.Terms == PaymentTerms.Immediate ? PaymentType.Immediate : PaymentType.CreditPayment,
				},
				Amount = amount
			};

			//if (fee.HasValue) {
			//	item.Payment.ExtraFee = PaymentMethodOption.Find (fee.Value);
			//	item.Payment.Commission = item.Payment.ExtraFee.CommissionByManage;
			//}

			// Store and Serial

			item.Payment.Store = store;
			item.Payment.Serial = (CustomerPayment.Queryable.Where(x => x.Store == store).Max(y => (int?)y.Serial) ?? 0) + 1;


			if (item.Amount > item.SalesOrder.Balance) {
				if (item.Payment.Method == PaymentMethod.Cash) {
					item.Change = item.Amount - item.SalesOrder.Balance;
				} else {
					item.Payment.Amount = item.SalesOrder.Balance;
				}

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

			if (item.SalesOrder.IsPaid || item.SalesOrder.IsCancelled || item.IsConfirmed) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompleted);
			}

			using (var scope = new TransactionScope ()) {

				item.DeleteAndFlush ();
				if (item.Payment.PaymentType == PaymentType.Immediate) {
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
				foreach (var payment in item.PaymentsToConfirm()) {
					payment.IsConfirmed = true;
					payment.Date = time;
					payment.Applier = CurrentUser.Employee;
					payment.UpdateAndFlush ();
				}

				if (item.BalanceInCashDrawer() <= 0.1m) {
					item.IsPaid = true;
					item.ModificationTime = time;
					item.Updater = CurrentUser.Employee;
					item.BalanceZeroedTime = time;
					item.DeliveryMode = DeliveryMode.PartialDeliveries;
					item.UpdateAndFlush();
				}
			}

			return PartialView ("_DetailsView", item);
		}


		[HttpPost]
		public ActionResult SetPartialDeliveries(int id, DeliveryMode value)
		{
			var item = SalesOrder.Find (id);
			var time = DateTime.Now;
			//var mode = value ? DeliveryMode.PartialDeliveries : DeliveryMode.PickUp;

			if (DeliveryOrderDetail.Queryable.Any (x => item.Details.Contains (x.OrderDetail))) {
				value = DeliveryMode.PartialDeliveries;
			}

			item.DeliveryMode = value;


			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return Json (new { id });
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

			var privileges = GetAccessPrivilege (SystemObjects.CashSessionClose);

			//if (!privileges.AllowUpdate) {
			//	Response.StatusCode = 400;
			//	return Content (string.Format(Resources.DocumentRequiressToBeConfirmedByManager, Resources.CloseSession));
			//}

			item = CashSession.Find (item.Id);

			if (item.End != null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompleted);
			}

			item.End = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				foreach (var x in cash_counts) {
					x.Session = item;
					x.Type = CashCountType.CountedCash;
					x.Create ();
				}
				item.CashSupervisor = CurrentUser.Employee;
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
