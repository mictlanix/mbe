// 
// AccountsReceivablesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class AccountsReceivablesController : CustomController {

		public ViewResult Index ()
		{
			return View ();
		}

		[HttpPost]
		public ActionResult Index (int? customer)
		{
			string sql1 = @"SELECT * FROM (SELECT m.date Date, d.sales_order SalesOrder, m.due_date DueDate, c.name Customer,
						GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, LPAD(f.serial, 6, '0') SEPARATOR ' ')
							FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
							WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
						SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) TotalEx,
						SUM(ROUND(d.quantity * d.price * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total,
						IFNULL(SUM(ROUND(r.quantity * d.price * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)), 0) Refunds,
						m.currency Currency
					FROM sales_order m
					INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
					INNER JOIN customer c ON m.customer = c.customer_id
					LEFT JOIN customer_refund_detail r ON d.sales_order_detail_id = r.sales_order_detail
					WHERE m.completed = 1 AND m.cancelled = 0 AND m.paid = 0 and m.payment_terms = 1 CUSTOMER_FILTER
					GROUP BY d.sales_order
					ORDER BY m.due_date) AS innerTable WHERE Total > 1";
			string sql2 = @"SELECT m.sales_order_id SalesOrder, SUM(ROUND(amount, 2)) Payments
					FROM sales_order m
					INNER JOIN sales_order_payment p ON m.sales_order_id = p.sales_order
					WHERE m.completed = 1 AND m.cancelled = 0 AND m.paid = 0 and m.payment_terms = 1 CUSTOMER_FILTER
					GROUP BY m.sales_order_id";

			if (customer.HasValue) {
				sql1 = sql1.Replace ("CUSTOMER_FILTER", "AND m.customer = :customer");
				sql2 = sql2.Replace ("CUSTOMER_FILTER", "AND m.customer = :customer");
			} else {
				sql1 = sql1.Replace ("CUSTOMER_FILTER", string.Empty);
				sql2 = sql2.Replace ("CUSTOMER_FILTER", string.Empty);
			}

			var items = (IList<dynamic>) ActiveRecordMediator<SalesOrder>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql1);

				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("DueDate", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("TotalEx", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("Refunds", NHibernateUtil.Decimal);
				query.AddScalar ("Currency", NHibernateUtil.Int32);

				if (customer.HasValue) {
					query.SetInt32 ("customer", customer.Value);
				}

				return query.DynamicList ();
			}, null);

			var payments = (IList<dynamic>) ActiveRecordMediator<SalesOrder>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql2);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Payments", NHibernateUtil.Decimal);

				if (customer.HasValue) {
					query.SetInt32 ("customer", customer.Value);
				}

				return query.DynamicList ();
			}, null);

			foreach (var item in items) {
				item.Payments = 0m;
				item.Balance = item.Total - item.Refunds;
			}

			foreach (var payment in payments) {
				var item = items.Single (x => x.SalesOrder == payment.SalesOrder);
				item.Payments = payment.Payments;
				item.Balance -= payment.Payments;
			}

			return PartialView ("_Index", items);
		}

		public ActionResult ApplyPayment (int id)
		{
			var item = new SalesOrderPayment {
				SalesOrder = SalesOrder.TryFind (id)
			};

			ViewBag.Balance = item.SalesOrder.Balance - GetRefunds (item.SalesOrder.Id);
			ViewBag.Payments = GetRemainingPayments (item.SalesOrder.Customer.Id, item.SalesOrder.Currency);
			item.Amount = ViewBag.Balance;

			return PartialView ("_ApplyPayment", item);
		}

		[HttpPost]
		public ActionResult ApplyPayment (SalesOrderPayment item)
		{
			var entity = new SalesOrderPayment {
				SalesOrder = SalesOrder.TryFind (item.SalesOrder.Id),
				Payment = CustomerPayment.TryFind (item.PaymentId),
				Amount = item.Amount
			};
			var balance = entity.SalesOrder.Balance - GetRefunds (entity.SalesOrder.Id);

			if (entity.Amount > entity.Payment.Balance) {
				entity.Amount = entity.Payment.Balance;
			}

			balance -= entity.Amount;

			using (var scope = new TransactionScope ()) {
				if (balance <= 0) {
					entity.SalesOrder.IsPaid = true;
					entity.SalesOrder.Update ();
				}

				if (entity.Amount > 0) {
					entity.Create ();
				}

				scope.Flush ();
			}

			return PartialView ("_ApplyPaymentSuccesful");
		}

		decimal GetRefunds (int salesOrder)
		{
			var query = from x in CustomerRefundDetail.Queryable
				    where x.Refund.IsCompleted && !x.Refund.IsCancelled &&
					  x.SalesOrderDetail.SalesOrder.Id == salesOrder
				    select x;

			return query.ToList ().Sum (x => x.Total);
		}

		IEnumerable<CustomerPayment> GetRemainingPayments (int customer, CurrencyCode currency)
		{
			var query = from x in CustomerPayment.Queryable
				    where x.Customer.Id == customer && x.Currency == currency &&
				    	(x.Allocations.Count == 0 || x.Amount > x.Allocations.Sum (y => y.Amount + y.Change))
				    select x;
			var results = query.ToList ().Where (x => x.Balance >= 0.01m);

			return results;
		}

		public ActionResult Print (int id)
		{
			//AccountReceivable account = AccountReceivable
			return PartialView ();
		}
	}
}
