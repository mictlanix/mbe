// 
// CustomerRefundsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
using Castle.ActiveRecord.Queries;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using Castle.Core.Internal;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class CustomerRefundsController : CustomController {
		public ViewResult Index ()
		{
			var item = WebConfig.Store;

			if (item == null) {
				return View ("InvalidStore");
			}

			var search = SearchRefunds (new Search<CustomerRefund> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<CustomerRefund> search)
		{
			if (ModelState.IsValid) {
				search = SearchRefunds (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		Search<CustomerRefund> SearchRefunds (Search<CustomerRefund> search)
		{
			IQueryable<CustomerRefund> query;
			var item = WebConfig.Store;

			if (string.IsNullOrEmpty (search.Pattern)) {
				query = from x in CustomerRefund.Queryable
					where x.Store.Id == item.Id
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending, x.Id descending
					select x;
			} else {
				query = from x in CustomerRefund.Queryable
					where x.Store.Id == item.Id &&
						  x.Customer.Name.Contains (search.Pattern)
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending, x.Id descending
					select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult View (int id)
		{
			var entity = CustomerRefund.Find (id);
			return View (entity);
		}

		public ViewResult Print (int id)
		{
			var entity = CustomerRefund.Find (id);
			return View (entity);
		}

		public ActionResult Pdf (int id)
		{
			var model = CustomerRefund.Find (id);
			if (!model.IsCompleted) {
				return RedirectToAction ("Edit", new { id = model.Id });
			}
			return PdfTicketView ("Print", model);
		}

		[HttpPost]
		public ActionResult CreateFromSalesOrder (string value)
		{
			int id = 0;
			SalesOrder entity = null;

			if (int.TryParse (value, out id)) {
				entity = SalesOrder.TryFind (id);
			}

			if (CustomerRefund.Queryable.Any (x => x.SalesOrder == entity && !x.IsCancelled && x.IsCompleted)) {
				Response.StatusCode = 400;
				return Content (Resources.RefundableItemsNotFound);
			}

			if (entity == null) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotFound);
			}

			if (!entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderIsNotRefundable);
			}

			if (entity.Creator != CurrentUser.Employee && entity.Updater != CurrentUser.Employee && !CurrentUser.IsAdministrator) {
				Response.StatusCode = 400;
				return Content (Resources.CreatorDoesntMatchWithCurrentUser);
			}


			//El sistema debe permitir realizar la devolución en cualquier tienda
			//Las políticas de la empresa definirán los términos de las devoluciones

			//if (entity.Store != WebConfig.Store) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.InvalidStore);
			//}

			var item = new CustomerRefund ();

			item.Store = WebConfig.Store;


			item.Serial = CustomerRefund.Queryable.Where(x => x.Store == WebConfig.Store).Max (x => (int?)x.Serial) + 1 ?? 1;

			item.SalesOrder = entity;
			item.SalesPerson = entity.SalesPerson;
			item.Customer = entity.Customer;
			item.Currency = entity.Currency;
			item.ExchangeRate = entity.ExchangeRate;

			item.CreationTime = DateTime.Now;
			item.Date = DateTime.Now;
			item.Creator = CurrentUser.Employee;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;

			foreach (var x in entity.Details) {
				var qty = GetRefundableQuantity (x.Id);

				if (qty <= 0)
					continue;

				var detail = new CustomerRefundDetail {
					Refund = item,
					SalesOrderDetail = x,
					Product = x.Product,
					ProductCode = x.ProductCode,
					ProductName = x.ProductName,
					DiscountRate = x.DiscountRate,
					TaxRate = x.TaxRate,
					IsTaxIncluded = x.IsTaxIncluded,
					Quantity = 0,
					Price = x.Price,
					ExchangeRate = x.ExchangeRate,
					Currency = x.Currency,
					Warehouse = WebConfig.PointOfSale.Warehouse,
				};

				item.Details.Add (detail);
			}

			if (item.Details.Count == 0) {
				Response.StatusCode = 400;
				return Content (Resources.RefundableItemsNotFound);
			}

			using (var scope = new TransactionScope ()) {
				item.Create ();

				foreach (var detail in item.Details) {
					detail.Create ();
				}
			}

			return Json (new { url = Url.Action ("Edit", new { id = item.Id }) });
		}

		public ActionResult Edit (int id)
		{
			var entity = CustomerRefund.Find (id);
			return View (entity);
		}

		public ActionResult Totals (int id)
		{
			var item = CustomerRefund.Find (id);
			return PartialView ("_Totals", item);
		}

		[HttpPost]
		public JsonResult SetItemQuantity (int id, decimal value)
		{
			var entity = CustomerRefundDetail.Find (id);
			var sales_order = entity.SalesOrderDetail.SalesOrder;

			decimal sum = GetRefundableQuantity (entity.SalesOrderDetail.Id);

			entity.Quantity = (value >= 0 && value <= sum) ? value : sum;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Quantity),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx),
			});
		}

		[HttpPost]
		public JsonResult SetItemWarehouse (int id, int value)
		{
			var entity = CustomerRefundDetail.Find (id);
			entity.Warehouse = MBEQueryable.IQWarehouses.Single (x => x.Id == value);

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Warehouse),
			});
		}

		[HttpPost]
		public JsonResult RemoveItem (int id)
		{
			var item = CustomerRefundDetail.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}

		[HttpPost]
		public ActionResult Confirm (int id)
		{
			var dt = DateTime.Now;
			bool changed = false;
			var entity = CustomerRefund.Find (id);
			var order = entity.SalesOrder;
			var current_warehouse = WebConfig.PointOfSale.Warehouse;
			var cash_session = GetSession ();

			if (cash_session == null) {
				return RedirectToAction ("OpenSession", "Payments");
			}

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Details) {
					var qty = GetRefundableQuantity (item.SalesOrderDetail.Id);

					if (qty < item.Quantity) {
						changed = true;

						if (qty > 0.0m) {
							item.Quantity = qty;
							item.Update ();
						} else {
							item.Delete ();
						}
					}
				}

				if (changed) {
					entity.Updater = CurrentUser.Employee;
					entity.ModificationTime = dt;
					entity.UpdateAndFlush ();

					return RedirectToAction ("Edit", new { id = entity.Id, notify = true });
				}
			}

			using (var scope = new TransactionScope ()) {

				//foreach (var detail in entity.Details.Where (x => !(x.Quantity > 0.0m)).ToList ()) {
				//	detail.DeleteAndFlush ();
				//}
				entity.Details.Where (x => x.Quantity <= 0).ForEach (x => {
					x.DeleteAndFlush () ;
				});
				
				foreach (var x in entity.Details.Where(x => x.SalesOrderDetail.Product.IsStockable)) {
					//InventoryHelpers.ChangeNotification (TransactionType.CustomerRefund, entity.Id, dt,
					//	current_warehouse, null, x.Product, x.Quantity);

					InventoryHelpers.ChangeNotification (TransactionType.CustomerRefund, entity.Id, dt,
						x.SalesOrderDetail.Warehouse, null, x.Product, x.Quantity);
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.Date = dt;
				entity.IsCompleted = true;
				entity.UpdateAndFlush ();

				if (entity.Total > order.Balance) {
					order.IsPaid = true;
					order.UpdateAndFlush();
					var refund = entity.Total - order.Balance;

					//var item = new SalesOrderPayment {
					//	SalesOrder = order,
					//	Payment = new CustomerPayment {
					//		CashSession = cash_session,
					//		CreationTime = dt,
					//		Creator = CurrentUser.Employee,
					//		Amount = -refund,
					//		Currency = entity.Currency,
					//		Customer = entity.Customer,
					//		PaymentType = PaymentType.Refund,
					//		Method = order.Payments.First ().Payment.Method,
					//		ModificationTime = dt,
					//		Serial = (CustomerPayment.Queryable.Where (x => x.Store == WebConfig.Store).Max (y => (int?) y.Serial) ?? 0) + 1,
					//		Store = WebConfig.Store,
					//		Updater = CurrentUser.Employee,
					//		Date = dt,
					//		CustomerId = entity.Customer.Id,
					//	},
					//	Amount = -refund
					//};

					//item.Payment.CreateAndFlush();
					//item.CreateAndFlush ();
				}
			}

			return RedirectToAction ("View", new { id = entity.Id });
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = CustomerRefund.Find (id);

			if (entity.IsCompleted) {
				return RedirectToAction ("View", entity);
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.Date = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		decimal GetRefundableQuantity (int id)
		{
			var item = SalesOrderDetail.Find (id);
			string sql = @"SELECT SUM(d.quantity) quantity
                           FROM customer_refund_detail d INNER JOIN customer_refund m 
                           ON d.customer_refund = m.customer_refund_id
                           WHERE m.completed <> 0 AND m.cancelled = 0 AND d.sales_order_detail = :detail ";

			IList<decimal> quantities = (IList<decimal>) ActiveRecordMediator<CustomerRefundDetail>.Execute (
			    delegate (ISession session, object instance) {
				    try {
					    return session.CreateSQLQuery (sql)
							  .SetParameter ("detail", id)
						      .SetMaxResults (1)
						      .List<decimal> ();
				    } catch (Exception) {
					    return null;
				    }
			    }, null);

			if (quantities != null && quantities.Count > 0) {
				return item.Quantity - quantities [0];
			}

			return item.Quantity;
		}

		Search<CustomerRefund> SearchRefunds (DateRange dates, Search<CustomerRefund> search)
		{
			var qry = from x in CustomerRefund.Queryable
				  where (x.IsCompleted || x.IsCancelled) &&
					  (x.ModificationTime >= dates.StartDate.Date && x.ModificationTime <= dates.EndDate.Date.Add (new TimeSpan (23, 59, 59)))
				  orderby x.Id descending
				  select x;

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
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
