// 
// KardexController.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using Microsoft.Ajax.Utilities;
using Mictlanix.BE.Web.Security;


namespace Mictlanix.BE.Web.Controllers.Mvc {
	//FIXME: queries with discount column
	[Authorize]
	public class ReportsController : CustomController {
		#region Stock & Kardex

		public ViewResult WarehouseStockReport ()
		{

			return View ();
		}

		[HttpPost]
		public ActionResult WarehouseStockReport (int warehouse, string label, string brand, string productModel, bool showZeroInventory)
		{
			string sql = @"SELECT p.product_id id, p.brand Brand, p.model Model, p.code Code, p.name Name, SUM(quantity) Quantity
                            FROM lot_serial_tracking l 
                            INNER JOIN product p ON l.product = p.product_id
                            LEFT JOIN product_label pl ON pl.product = p.product_id
                            WHERE warehouse = :warehouse ";

			if (!string.IsNullOrEmpty (label)) {
				sql += "AND pl.label = :label ";
			}

			if (!string.IsNullOrWhiteSpace (brand)) {
				sql += "AND p.brand like :brand ";
			}

			if (!string.IsNullOrWhiteSpace (productModel)) {
				sql += "AND p.model like :productModel ";
			}

			sql += "GROUP BY l.product";

			if (!showZeroInventory) {
				sql += " HAVING SUM(quantity) <> 0 ";
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Id", NHibernateUtil.Int32);
				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse);

				if (!string.IsNullOrWhiteSpace (label)) {
					query.SetInt32 ("label", Int32.Parse (label));
				}

				if (!string.IsNullOrWhiteSpace (brand)) {
					query.SetString ("brand", brand);
				}

				if (!string.IsNullOrWhiteSpace (productModel)) {
					query.SetString ("productModel", productModel);
				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_WarehouseStockReport", items);
		}

		public ViewResult WarehouseStockByLotReport ()
		{

			return View ();
		}

		[HttpPost]
		public ActionResult WarehouseStockByLotReport (int warehouse, string brand)
		{

			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name, l.lot_number LotNumber, l.expiration_date ExpirationDate, SUM(quantity) Quantity
                            FROM lot_serial_tracking l INNER JOIN product p ON l.product = p.product_id
                            WHERE warehouse = :warehouse WHERE_BRAND
                            GROUP BY l.product, l.lot_number, l.expiration_date";

			if (string.IsNullOrWhiteSpace (brand)) {
				sql = sql.Replace ("WHERE_BRAND", string.Empty);
			} else {
				sql = sql.Replace ("WHERE_BRAND", "AND p.brand = :brand");
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("LotNumber", NHibernateUtil.String);
				query.AddScalar ("ExpirationDate", NHibernateUtil.Date);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse); 

				if (!string.IsNullOrWhiteSpace (brand)) {
					query.SetString ("brand", brand);
				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_WarehouseStockByLotReport", items);
		}

		public ViewResult WarehouseStockBySerialNumberReport ()
		{

			return View ();
		}

		[HttpPost]
		public ActionResult WarehouseStockBySerialNumberReport (int warehouse)
		{

			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name, l.lot_number LotNumber, 
					l.expiration_date ExpirationDate, l.serial_number SerialNumber, SUM(quantity) Quantity
                            FROM lot_serial_tracking l INNER JOIN product p ON l.product = p.product_id
                            WHERE l.serial_number IS NOT NULL AND warehouse = :warehouse
                            GROUP BY l.product, l.lot_number, l.expiration_date, l.serial_number
							HAVING SUM(quantity) <> 0";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("SerialNumber", NHibernateUtil.String);
				query.AddScalar ("LotNumber", NHibernateUtil.String);
				query.AddScalar ("ExpirationDate", NHibernateUtil.Date);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse);

				return query.DynamicList ();
			}, null);

			return PartialView ("_WarehouseStockBySerialNumberReport", items);
		}

		public ViewResult Kardex ()
		{

			ViewBag.Title = Resources.Kardex;
			return View (new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult Kardex (int warehouse, int product, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);


			//var balance2 = LotSerialTracking.Queryable.Where (x => x.Warehouse.Id == warehouse && x.Product.Id == product && x.Date.Date < start).Sum (y => (decimal?) y.Quantity) ?? 0.0m;


			string sql = @"SELECT DATE(l.date) Date, l.source Source, l.reference Reference, l.lot_number LotNumber, l.expiration_date ExpirationDate, SUM(quantity) Quantity
                            FROM lot_serial_tracking l
                            WHERE warehouse = :warehouse AND product = :product AND date >= :start AND date <= :end
                            GROUP BY DATE(l.date), l.source, l.reference, l.lot_number, l.expiration_date
                            ORDER BY l.date";

			string balance_query = @"SELECT IFNULL(SUM(quantity), 0) Quantity
                            FROM lot_serial_tracking l
                            WHERE warehouse = :warehouse AND product = :product AND date < :start";


			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Date", NHibernateUtil.Date);
				query.AddScalar ("Source", NHibernateUtil.Int32);
				query.AddScalar ("Reference", NHibernateUtil.Int32);
				query.AddScalar ("LotNumber", NHibernateUtil.String);
				query.AddScalar ("ExpirationDate", NHibernateUtil.Date);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse);
				query.SetInt32 ("product", product);
				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);

				return query.DynamicList ();
			}, null);

			var balance = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (balance_query);

				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse);
				query.SetInt32 ("product", product);
				query.SetDateTime ("start", start);

				return query.DynamicList ();
			}, null);

			ViewBag.OpeningBalance = (decimal)balance[0].Quantity;
			return PartialView ("_Kardex", items);
		}

		public ViewResult SerialNumberKardex ()
		{

			ViewBag.Title = Resources.SerialNumberKardex;
			return View ("Kardex", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult SerialNumberKardex (int warehouse, int product, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var balance = from x in LotSerialTracking.Queryable
				      where x.Warehouse.Id == warehouse && x.Product.Id == product && x.Date < start
				      select x.Quantity;

			ViewBag.OpeningBalance = balance.Count () > 0 ? balance.Sum () : 0m;

			string sql = @"SELECT l.date Date, l.source Source, l.reference Reference, l.lot_number LotNumber, l.expiration_date ExpirationDate, l.serial_number SerialNumber, quantity Quantity
                            FROM lot_serial_tracking l
                            WHERE warehouse = :warehouse AND product = :product AND date >= :start AND date <= :end
                            ORDER BY l.serial_number, l.date";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Date", NHibernateUtil.Date);
				query.AddScalar ("Source", NHibernateUtil.Int32);
				query.AddScalar ("Reference", NHibernateUtil.Int32);
				query.AddScalar ("LotNumber", NHibernateUtil.String);
				query.AddScalar ("ExpirationDate", NHibernateUtil.Date);
				query.AddScalar ("SerialNumber", NHibernateUtil.String);

				query.AddScalar ("Quantity", NHibernateUtil.Decimal);

				query.SetInt32 ("warehouse", warehouse);
				query.SetInt32 ("product", product);
				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);

				return query.DynamicList ();
			}, null);

			return PartialView ("_SerialNumberKardex", items);
		}

		#endregion

		#region Gross Profits

		public ViewResult GrossProfitsByCustomer ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsByCustomer;
			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult GrossProfitsByCustomer (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.Store.Id == store &&
				      x.IsCompleted &&
				      x.IsPaid &&
				      !x.IsCancelled &&
				      x.Date >= start &&
				      x.Date <= end
				  select new {
					  Id = x.Customer.Id,
					  Name = x.Customer.Name,
					  Units = y.Quantity,
					  Total = y.Quantity * (y.Price - y.Cost),
					  Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
				  };
			var qry2 = from x in qry.ToList ()
				   group x by new { x.Id, x.Name } into g
				   select new SummaryItem {
					   Id = g.Key.Id.ToString (),
					   Name = g.Key.Name,
					   Units = g.Sum (x => x.Units),
					   Total = g.Sum (x => x.Total),
					   Subtotal = g.Sum (x => x.Subtotal)
				   };
			var items = qry2.OrderByDescending (x => x.Total).ToList ();

			AnalyzeABC (items);

			return PartialView ("_SummaryReport", items);
		}

		public ViewResult GrossProfitsBySalesPerson ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsBySalesPerson;
			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult GrossProfitsBySalesPerson (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.Store.Id == store &&
				      x.IsCompleted &&
				      x.IsPaid &&
				      !x.IsCancelled &&
				      x.Date >= start &&
				      x.Date <= end
				  select new {
					  Id = x.SalesPerson.Id,
					  Name = x.SalesPerson.FirstName + " " + x.SalesPerson.LastName,
					  Units = y.Quantity,
					  Total = y.Quantity * (y.Price - y.Cost),
					  Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
				  };
			var qry2 = from x in qry.ToList ()
				   group x by new { x.Id, x.Name } into g
				   select new SummaryItem {
					   Id = g.Key.Id.ToString (),
					   Name = g.Key.Name,
					   Units = g.Sum (x => x.Units),
					   Total = g.Sum (x => x.Total),
					   Subtotal = g.Sum (x => x.Subtotal)
				   };
			var items = qry2.OrderByDescending (x => x.Total).ToList ();

			AnalyzeABC (items);

			return PartialView ("_SummaryReport", items);
		}

		public ViewResult GrossProfitsByProduct ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsByProduct;
			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult GrossProfitsByProduct (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.Store.Id == store &&
				  x.IsCompleted &&
				  x.IsPaid &&
				  !x.IsCancelled &&
				  x.Date >= start &&
				  x.Date <= end
				  select new {
					  Id = y.ProductCode,
					  Name = y.ProductName,
					  Units = y.Quantity,
					  Total = y.Quantity * (y.Price - y.Cost),
					  Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
				  };
			var qry2 = from x in qry.ToList ()
				   group x by new { x.Id, x.Name } into g
				   select new SummaryItem {
					   Id = g.Key.Id,
					   Name = g.Key.Name,
					   Units = g.Sum (x => x.Units),
					   Total = g.Sum (x => x.Total),
					   Subtotal = g.Sum (x => x.Subtotal),
				   };
			var items = qry2.OrderByDescending (x => x.Total).ToList ();

			AnalyzeABC (items);

			return PartialView ("_SummaryReport", items);
		}

		#endregion

		#region Best Selling Products

		public ViewResult BestSellingProductsByCustomer ()
		{
			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.BestSellingProductsByCustomer;
			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult BestSellingProductsByCustomer (int customer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.Customer.Id == customer &&
				      x.IsCompleted &&
				      x.IsPaid &&
				      !x.IsCancelled &&
				      x.Date >= start &&
				      x.Date <= end
				  select new {
					  Id = y.ProductCode,
					  Name = y.ProductName,
					  Units = y.Quantity,
					  Total = y.Quantity * y.Price,
					  Subtotal = y.Quantity * y.Price / (y.TaxRate + 1m)
				  };
			var qry2 = from x in qry.ToList ()
				   group x by new { x.Id, x.Name } into g
				   select new SummaryItem {
					   Id = g.Key.Id,
					   Name = g.Key.Name,
					   Units = g.Sum (x => x.Units),
					   Total = g.Sum (x => x.Total),
					   Subtotal = g.Sum (x => x.Subtotal),
				   };
			var items = qry2.OrderByDescending (x => x.Total).ToList ();

			AnalyzeABC (items);

			return PartialView ("_SummaryReport", items);
		}

		public ViewResult BestSellingProductsBySalesPerson ()
		{
			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.BestSellingProductsBySalesPerson;
			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult BestSellingProductsBySalesPerson (int employee, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.SalesPerson.Id == employee &&
				      x.IsCompleted &&
				      x.IsPaid &&
				      !x.IsCancelled &&
				      x.Date >= start &&
				      x.Date <= end
				  select new {
					  Id = y.ProductCode,
					  Name = y.ProductName,
					  Units = y.Quantity,
					  Total = y.Quantity * y.Price,
					  Subtotal = y.Quantity * y.Price / (y.TaxRate + 1m)
				  };
			var qry2 = from x in qry.ToList ()
				   group x by new { x.Id, x.Name } into g
				   select new SummaryItem {
					   Id = g.Key.Id,
					   Name = g.Key.Name,
					   Units = g.Sum (x => x.Units),
					   Total = g.Sum (x => x.Total),
					   Subtotal = g.Sum (x => x.Subtotal)
				   };
			var items = qry2.OrderByDescending (x => x.Total).ToList ();

			AnalyzeABC (items);

			return PartialView ("_SummaryReport", items);
		}

		#endregion

		//public ViewResult CustomerDebt ()
		//{

		//	ViewBag.EditorField = "customer";
		//	ViewBag.EditorTemplate = "CustomerSelector";
		//	ViewBag.Title = Resources.CustomerDebt;
		//	return View ("SummaryReport", new DateRange (DateTime.Now.AddDays (1 - DateTime.Now.Day), DateTime.Now));
		//}

		//[HttpPost]
		public ViewResult CustomersDebtSummary ()
		{
			var dt = DateTime.Now;

			var filter = new CustomersStatusFilter {
				DateRange = new DateRange {
					StartDate = dt.AddDays(-60),
					EndDate = dt
				},
				OnlyCredits = false,
				OnlyDebtors = true
			};
			return View ("CustomersDebtSummary", filter);
			
		}

		[HttpPost]
		public ActionResult CustomersDebtSummary (CustomersStatusFilter filter) {

			string ONLY_CREDITS_FILTER = filter.OnlyCredits ? " AND op.terms = 1" : string.Empty;
			string ONLY_DEBTORS_FILTER = filter.OnlyDebtors ? " HAVING (Balance < 0 OR OnDelivery > 0.01)" : string.Empty;
			string CUSTOMER_ID_FILTER = filter.CustomerId.HasValue? " AND op.customer_id = " + filter.CustomerId.Value : string.Empty;

			string sql = @"	SELECT op.customer_id AS CustomerId, op.customer_name AS CustomerName,
					op.customer_code AS CustomerCode, op.credit_limit CreditLimit,
					op.credit_days CreditDays, SUM(op.sales_order_total) Total,
					SUM(op.sales_order_due_status) NumberOfOverdueOrders,
					COUNT(*) NumberOfOrders,
					SUM(op.sales_order_refund) Refunds, SUM(op.paid) Payment,
					SUM(IF(op.sales_order_balance > 0, 0, op.sales_order_balance)) AS Balance,
					SUM(op.sales_order_payments_on_delivery_unregistered) AS OnDelivery, 
					MIN(op.due_date) AS OldestOverdueDate 
					FROM orders_payments_report_view op
					WHERE op.creation_time BETWEEN :start AND :end
					CUSTOMER_ID_FILTER
					GROUP BY op.customer_id
					ONLY_DEBTORS_FILTER
					ORDER BY OldestOverdueDate";

			sql = sql.Replace ("ONLY_CREDITS_FILTER", ONLY_CREDITS_FILTER);
			sql = sql.Replace ("ONLY_DEBTORS_FILTER", ONLY_DEBTORS_FILTER);
			sql = sql.Replace ("CUSTOMER_ID_FILTER", CUSTOMER_ID_FILTER);

			var items = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("CustomerId", NHibernateUtil.Int32);
				query.AddScalar ("CustomerName", NHibernateUtil.String);
				query.AddScalar ("CustomerCode", NHibernateUtil.String);
				query.AddScalar ("CreditLimit", NHibernateUtil.Decimal);
				query.AddScalar ("CreditDays", NHibernateUtil.Int32);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("NumberOfOrders", NHibernateUtil.Int32);
				query.AddScalar ("NumberOfOverdueOrders", NHibernateUtil.Int32);
				query.AddScalar ("Refunds", NHibernateUtil.Decimal);
				query.AddScalar ("Payment", NHibernateUtil.Decimal);
				query.AddScalar ("Balance", NHibernateUtil.Decimal);
				query.AddScalar ("OnDelivery", NHibernateUtil.Decimal);
				query.AddScalar ("OldestOverdueDate", NHibernateUtil.Date);

				query.SetParameter ("start", filter.DateRange.StartDate);
				query.SetParameter ("end", filter.DateRange.EndDate);

				return query.DynamicList ();
			}, null);


			return PartialView ("_CustomersDebtSummary", items.ToList ());
		}

		public ViewResult CustomerDebtReport (int id)
		{

			var customer = Customer.Queryable.Single(x => x.Id == id);

			string sql = @"	SELECT sales_order_id SalesOrderId, creation_time CreationTime ,customer_name CustomerName,
					terms_name Terms, due_date DueDate,
					creator_nickname User, salesperson_nickname SalesPerson,
					sales_order_total Total, sales_order_refund Refund,
					sales_order_refund_ids RefundsDesc, paid Paid,
					sales_order_balance Balance, sales_order_payments PaymentsDesc,
					sales_order_payments_on_delivery_unregistered OnDelivery,
					sales_order_paid_status OrderPaidStatus, sales_order_due_status Overdue
					FROM orders_payments_report_view
					WHERE customer_id = :customer_id
					ORDER BY SalesOrderId DESC;";

			var items = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrderId", NHibernateUtil.Int32);
				query.AddScalar ("CreationTime", NHibernateUtil.Date);
				query.AddScalar ("CustomerName", NHibernateUtil.String);
				query.AddScalar ("Terms", NHibernateUtil.String);
				query.AddScalar ("DueDate", NHibernateUtil.Date);
				query.AddScalar ("SalesPerson", NHibernateUtil.String);
				query.AddScalar ("User", NHibernateUtil.String);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("Refund", NHibernateUtil.Decimal);
				query.AddScalar ("RefundsDesc", NHibernateUtil.String);
				query.AddScalar ("Paid", NHibernateUtil.Decimal);
				query.AddScalar ("Balance", NHibernateUtil.Decimal);
				query.AddScalar ("PaymentsDesc", NHibernateUtil.String);
				query.AddScalar ("OnDelivery", NHibernateUtil.Decimal);
				query.AddScalar ("OrderPaidStatus", NHibernateUtil.Boolean);
				query.AddScalar ("Overdue", NHibernateUtil.Int32);

				query.SetParameter ("customer_id", id);
				return query.DynamicList ();
			}, null);


			ViewBag.CustomerName = customer.Name;

			return View ("CustomerDebtReport", items.ToList ());
		}

		public ViewResult CustomersReport ()
		{

			return View (new Search<Customer> ());
		}

		[HttpPost]
		public ActionResult CustomersReport (Search<Customer> search)
		{

			if (ModelState.IsValid) {
				search = GetCustomers (search);
			}

			return PartialView ("_CustomersReport", search);
		}

		Search<Customer> GetCustomers (Search<Customer> search)
		{

			search.Pattern = search.Pattern.Trim ();
			if (string.IsNullOrWhiteSpace (search.Pattern)) {
				var qry = from x in Customer.Queryable
					  orderby x.Name
					  select x;

				search.Total = qry.Count ();
				search.Results = qry.ToList ();
			} else {
				var qry = from x in Customer.Queryable
					  where x.Name.Contains (search.Pattern) ||
					      x.Code.Contains (search.Pattern) ||
					      x.Zone.Contains (search.Pattern)
					  orderby x.Name
					  select x;

				search.Total = qry.Count ();
				search.Results = qry.ToList ();
			}

			return search;
		}

		public ViewResult FiscalDocuments ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "taxpayer";
			ViewBag.EditorTemplate = "TaxpayerSelector";
			ViewBag.Title = Resources.FiscalDocumentsReport;

			ViewBag.FieldId = WebConfig.Store.Taxpayer.Id;
			ViewBag.FieldText = WebConfig.Store.Taxpayer.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult FiscalDocuments (string taxpayer, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in FiscalDocument.Queryable
				    where x.Issuer.Id == taxpayer && x.IsCompleted &&
				    ((x.Issued >= start && x.Issued <= end) || (x.CancellationDate >= start && x.CancellationDate <= end))
				    orderby x.Issued
				    select x;

			return PartialView ("_FiscalDocuments", query.ToList ());
		}

		public ViewResult CustomerSalesOrders ()
		{

			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.CustomerSalesOrders;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult CustomerSalesOrders (int customer, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
				    where x.Customer.Id == customer &&
					x.IsCompleted && !x.IsCancelled &&
					x.Date >= start && x.Date <= end
				    orderby x.Date
				    select x;

			return PartialView ("_CustomerSalesOrders", query.ToList ());
		}

		public ViewResult ProductSalesByCustomer ()
		{

			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.ProductSalesByCustomer;
			return View ("SummaryReport", new DateRange ());
		}


		//FIXME: Discount
		[HttpPost]
		public ActionResult ProductSalesByCustomer (int customer, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
				    from y in x.Details
				    where x.Customer.Id == customer &&
					x.IsCompleted && !x.IsCancelled &&
					x.Date >= start && x.Date <= end
				    select new {
					    SalesOrder = x.Id,
					    Code = y.ProductCode,
					    Name = y.ProductName,
					    Quantity = y.Quantity,
					    Price = y.Price,
					    ExchangeRate = y.ExchangeRate,
					    Discount = y.DiscountRate,
					    TaxRate = y.TaxRate,
					    IsTaxIncluded = y.IsTaxIncluded
				    };
			var items = from x in query.ToList ()
				    select new SummaryItem {
					    Category = x.SalesOrder.ToString (),
					    Id = x.Code,
					    Name = x.Name,
					    Units = x.Quantity,
					    Total = Model.ModelHelpers.Total (x.Quantity, x.Price, x.ExchangeRate, x.Discount, x.TaxRate, x.IsTaxIncluded),
					    Subtotal = Model.ModelHelpers.Subtotal (x.Quantity, x.Price, x.ExchangeRate, x.TaxRate, x.IsTaxIncluded)
				    };

			return PartialView ("_ProductSalesByCustomer", items);
		}

		public ViewResult ProductSalesByModel ()
		{

			ViewBag.EditorField = "productModel";
			ViewBag.EditorTemplate = "ProductModelSelector";
			ViewBag.Title = Resources.ProductSalesByModel;
			return View ("SummaryReport", new DateRange ());
		}

		//FIXME: Discount
		[HttpPost]
		public ActionResult ProductSalesByModel (string productModel, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
				    from y in x.Details
				    where x.IsCompleted && !x.IsCancelled &&
					x.Date >= start && x.Date <= end &&
					y.Product.Model.Contains (productModel)
				    orderby y.ProductName
				    select new {
					    Model = y.Product.Model,
					    Brand = y.Product.Brand,
					    Code = y.Product.Code,
					    Name = y.Product.Name,
					    Quantity = y.Quantity,
					    Price = y.Price,
					    ExchangeRate = y.ExchangeRate,
					    Discount = y.DiscountRate,
					    TaxRate = y.TaxRate,
					    IsTaxIncluded = y.IsTaxIncluded
				    };
			var items = from x in query.ToList ()
				    group x by new { x.Model, x.Brand, x.Code, x.Name } into g
				    select new SummaryItem {
					    Id = g.Key.Brand,
					    Category = g.Key.Model,
					    Code = g.Key.Code,
					    Name = g.Key.Name,
					    Units = g.Sum (y => y.Quantity),
					    Total = g.Sum (y => Model.ModelHelpers.Total (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded)),
					    Subtotal = g.Sum (y => Model.ModelHelpers.Subtotal (y.Quantity, y.Price, y.ExchangeRate, y.TaxRate, y.IsTaxIncluded))
				    };

			return PartialView ("_ProductSalesByCategory", items);
		}

		public ViewResult ProductSalesByBrand ()
		{

			ViewBag.EditorField = "brand";
			ViewBag.EditorTemplate = "ProductBrandSelector";
			ViewBag.Title = Resources.ProductSalesByBrand;
			return View ("SummaryReport", new DateRange ());
		}

		//FIXME: Discount
		[HttpPost]
		public ActionResult ProductSalesByBrand (string brand, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
				    from y in x.Details
				    where x.IsCompleted && !x.IsCancelled &&
					x.Date >= start && x.Date <= end &&
					y.Product.Brand.Contains (brand)
				    orderby y.ProductName
				    select new {
					    Model = y.Product.Model,
					    Brand = y.Product.Brand,
					    Code = y.Product.Code,
					    Name = y.Product.Name,
					    Quantity = y.Quantity,
					    Price = y.Price,
					    ExchangeRate = y.ExchangeRate,
					    Discount = y.DiscountRate,
					    TaxRate = y.TaxRate,
					    IsTaxIncluded = y.IsTaxIncluded
				    };
			var items = from x in query.ToList ()
				    group x by new { x.Model, x.Brand, x.Code, x.Name } into g
				    select new SummaryItem {
					    Id = g.Key.Brand,
					    Category = g.Key.Model,
					    Code = g.Key.Code,
					    Name = g.Key.Name,
					    Units = g.Sum (y => y.Quantity),
					    Total = g.Sum (y => Model.ModelHelpers.Total (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded)),
					    Subtotal = g.Sum (y => Model.ModelHelpers.Subtotal (y.Quantity, y.Price, y.ExchangeRate, y.TaxRate, y.IsTaxIncluded))
				    };

			return PartialView ("_ProductSalesByCategory", items);
		}

		public ViewResult SalesBySalesPerson ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesBySalesPerson;

			ViewBag.FieldId = WebConfig.Store.Id;
			ViewBag.FieldText = WebConfig.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesBySalesPerson (int store, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT salesperson SalesPersonId, first_name FirstName, last_name LastName,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) / IF(tax_included = 0, 1, 1 + tax_rate), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) * IF(tax_included = 0, 1 + tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN employee e ON m.salesperson = e.employee_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY salesperson, first_name, last_name";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				return session.CreateSQLQuery (sql)
				    .SetParameter ("start", start)
				    .SetParameter ("end", end)
				    .SetParameter ("store", store)
				    .DynamicList ();
			}, null);

			return PartialView ("_SalesBySalesPerson", items);
		}

		public ViewResult SalesByCustomer ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesByCustomer;

			ViewBag.FieldId = WebConfig.Store.Id;
			ViewBag.FieldText = WebConfig.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesByCustomer (int store, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT customer CustomerId, name Customer,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) / IF(tax_included = 0, 1, 1 + tax_rate), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) * IF(tax_included = 0, 1 + tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY customer";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				return session.CreateSQLQuery (sql)
				    .SetParameter ("start", start)
				    .SetParameter ("end", end)
				    .SetParameter ("store", store)
				    .DynamicList ();
			}, null);

			return PartialView ("_SalesByCustomer", items);
		}

		public ViewResult SalesByProduct ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesByProduct;

			ViewBag.FieldId = WebConfig.Store.Id;
			ViewBag.FieldText = WebConfig.Store.Name;

			return View ("SummaryReport", new DateRange (DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult SalesByProduct (int store, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT product ProductId, p.code Code, p.model Model, p.name Product,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY product";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				return session.CreateSQLQuery (sql)
				    .SetParameter ("start", start)
				    .SetParameter ("end", end)
				    .SetParameter ("store", store)
				    .DynamicList ();
			}, null);

			return PartialView ("_SalesByProduct", items);
		}

		public ViewResult SalesPersonOrders ()
		{

			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.SalesPersonOrders;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesPersonOrders (int employee, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT sales_order SalesOrder, date Date, name Customer,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY sales_order";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			return PartialView ("_SalesPersonOrders", items);
		}

		public ViewResult SalesOrderSummary ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesOrderSummary;

			ViewBag.FieldId = WebConfig.Store.Id;
			ViewBag.FieldText = WebConfig.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesOrderSummary (int store, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT * FROM
					(SELECT date Date, nickname SalesPerson, sales_order_id SalesOrder, m.serial Serial,
						m.payment_terms Terms, m.due_date DueDate, c.name Customer,
						GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
							FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
							WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
						SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) TotalEx,
						SUM(ROUND(d.quantity * d.price * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total,
						m.currency Currency
					FROM sales_order m
					INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
					INNER JOIN employee e ON m.salesperson = e.employee_id
					INNER JOIN customer c ON m.customer = c.customer_id
					WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
						m.date >= :start AND m.date <= :end
					GROUP BY sales_order_id) AS SalesOrdersSQ
					LEFT JOIN (
						SELECT sod.sales_order SalesOrderId, 
						SUM(ROUND(crd.quantity * crd.price * crd.exchange_rate * (1 - crd.discount) * IF(crd.tax_included = 0, IF(crd.tax_rate > 0, 1 + crd.tax_rate, 1), 1), 2)) RefundEx,
						SUM(ROUND(crd.quantity * crd.price * (1 - crd.discount) * IF(crd.tax_included = 0, IF(crd.tax_rate > 0, 1 + crd.tax_rate, 1), 1), 2)) Refund
						FROM customer_refund_detail crd
						JOIN customer_refund cr on cr.customer_refund_id = crd.customer_refund
						JOIN sales_order_detail sod ON crd.sales_order_detail = sod.sales_order_detail_id
						WHERE cr.cancelled = 0 AND cr.completed = 1
						GROUP BY sod.sales_order 
					) AS TRefunds ON SalesOrdersSQ.SalesOrder = TRefunds.SalesOrderId
					LEFT JOIN (
						SELECT sop.sales_order SalesOrderPayment, SUM(sop.amount) AmountPaid
						FROM sales_order_payment sop
						GROUP BY sop.sales_order
					) AS TPayments ON SalesOrdersSQ.SalesOrder = TPayments.SalesOrderPayment
					
					";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("SalesPerson", NHibernateUtil.String);
				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Serial", NHibernateUtil.Int32);
				query.AddScalar ("Terms", NHibernateUtil.Int32);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("DueDate", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("TotalEx", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("Refund", NHibernateUtil.Decimal);
				query.AddScalar ("RefundEx", NHibernateUtil.Decimal);
				query.AddScalar ("AmountPaid", NHibernateUtil.Decimal);
				query.AddScalar ("Currency", NHibernateUtil.Int32);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("store", store);

				return query.DynamicList ();
			}, null);

			return PartialView ("_SalesOrderSummary", items);
		}public ViewResult StoreMovementsSummary ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.StoreMovementsSummary;

			ViewBag.FieldId = WebConfig.Store.Id;
			ViewBag.FieldText = WebConfig.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult StoreMovementsSummary (int store, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var summary = new StoreMovementsSummaryViewModel ();
			string sql = @"SELECT * FROM
					
						(SELECT date Date, nickname SalesPerson, sales_order_id SalesOrder, m.serial Serial,
							m.payment_terms Terms, m.due_date DueDate, c.name Customer,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) TotalEx,
							SUM(ROUND(d.quantity * d.price * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total,
							m.currency Currency
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN employee e ON m.salesperson = e.employee_id
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY sales_order_id) AS SalesOrdersSQ
						LEFT JOIN (
							SELECT sod.sales_order SalesOrderId, GROUP_CONCAT(DISTINCT cr.customer_refund_id SEPARATOR ', ' ) RefundIDs,
							SUM(ROUND(crd.quantity * crd.price * crd.exchange_rate * (1 - crd.discount) * IF(crd.tax_included = 0, IF(crd.tax_rate > 0, 1 + crd.tax_rate, 1), 1), 2)) RefundEx,
							SUM(ROUND(crd.quantity * crd.price * (1 - crd.discount) * IF(crd.tax_included = 0, IF(crd.tax_rate > 0, 1 + crd.tax_rate, 1), 1), 2)) Refund
							FROM customer_refund_detail crd
							JOIN customer_refund cr on cr.customer_refund_id = crd.customer_refund
							JOIN sales_order_detail sod ON crd.sales_order_detail = sod.sales_order_detail_id
							WHERE cr.cancelled = 0 AND cr.completed = 1
							GROUP BY sod.sales_order 
						) AS TRefunds ON SalesOrdersSQ.SalesOrder = TRefunds.SalesOrderId
						LEFT JOIN (
							SELECT sop.sales_order SalesOrderPayment, SUM(sop.amount) AmountPaid
							FROM sales_order_payment sop
							GROUP BY sop.sales_order
						) AS TPayments ON SalesOrdersSQ.SalesOrder = TPayments.SalesOrderPayment
					
						";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Date", NHibernateUtil.Date);
				query.AddScalar ("SalesPerson", NHibernateUtil.String);
				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Serial", NHibernateUtil.Int32);
				query.AddScalar ("Terms", NHibernateUtil.Int32);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("DueDate", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("RefundIDs", NHibernateUtil.String);
				query.AddScalar ("TotalEx", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("Refund", NHibernateUtil.Decimal);
				query.AddScalar ("RefundEx", NHibernateUtil.Decimal);
				query.AddScalar ("AmountPaid", NHibernateUtil.Decimal);
				query.AddScalar ("Currency", NHibernateUtil.Int32);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("store", store);

				return query.DynamicList ();
			}, null);

			summary.SalesOrder = items;
			summary.Expenses = ExpenseVoucher.Queryable.Where(x => x.Store.Id == store
						&& x.IsCompleted && !x.IsCancelled && x.CreationTime > start && x.CreationTime < end).ToList();
			summary.Payments = CustomerPayment.Queryable.Where (x => x.CreationTime < end && x.CreationTime > start && x.Store.Id == store).ToList ();

			return PartialView ("_StoreMovementsSummary", summary);
		}

		public ViewResult ProductSalesBySalesPerson ()
		{

			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPerson;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPerson (int employee, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY d.product";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndLabel ()
		{

			ViewBag.EditorField = "label";
			ViewBag.EditorTemplate = "LabelSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndLabel;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndLabel (int employee, int? label, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						JOIN_LABEL
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end WHERE_LABEL
						GROUP BY d.product";

			if (label.HasValue) {
				sql = sql.Replace ("JOIN_LABEL", "INNER JOIN product_label l ON d.product = l.product");
				sql = sql.Replace ("WHERE_LABEL", "AND l.label = :label");
			} else {
				sql = sql.Replace ("JOIN_LABEL", string.Empty).Replace ("WHERE_LABEL", string.Empty);
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (label.HasValue) {
					query.SetInt32 ("label", label.Value);
				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndBrand ()
		{

			ViewBag.EditorField = "brand";
			ViewBag.EditorTemplate = "ProductBrandSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndBrand;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndBrand (int employee, string brand, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end WHERE_BRAND
						GROUP BY d.product";

			if (string.IsNullOrWhiteSpace (brand)) {
				sql = sql.Replace ("WHERE_BRAND", string.Empty);
			} else {
				sql = sql.Replace ("WHERE_BRAND", "AND p.brand = :brand");
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (!string.IsNullOrWhiteSpace (brand)) {
					query.SetString ("brand", brand);
				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndModel ()
		{

			ViewBag.EditorField = "productModel";
			ViewBag.EditorTemplate = "ProductModelSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndModel;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndModel (int employee, string productModel, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end WHERE_MODEL
						GROUP BY d.product";

			if (string.IsNullOrWhiteSpace (productModel)) {
				sql = sql.Replace ("WHERE_MODEL", string.Empty);
			} else {
				sql = sql.Replace ("WHERE_MODEL", "AND p.model = :model");
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (!string.IsNullOrWhiteSpace (productModel)) {
					query.SetString ("model", productModel);
				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult SalesPersonOrdersAndRefunds ()
		{

			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.SalesPersonOrdersAndRefunds;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesPersonOrdersAndRefunds (int employee, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT sales_order SalesOrder, 0 Refund, date Date, name Customer,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY sales_order";

			var orders = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Refund", NHibernateUtil.Int32);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			sql = @"SELECT sales_order SalesOrder, customer_refund Refund, s.date Date, name Customer,
						GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
							FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
							WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail) SEPARATOR ' ') Invoices,
						-SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
						-SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
					FROM customer_refund m
					INNER JOIN sales_order s ON m.sales_order = s.sales_order_id
					INNER JOIN customer_refund_detail d ON m.customer_refund_id = d.customer_refund
					INNER JOIN customer c ON m.customer = c.customer_id
					WHERE m.sales_person = :employee AND m.completed = 1 AND m.cancelled = 0 AND
						s.date >= :start AND s.date <= :end
					GROUP BY sales_order";

			var refunds = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Refund", NHibernateUtil.Int32);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			var items = orders.ToList ();

			items.AddRange (refunds);

			return PartialView ("_SalesPersonOrdersAndRefunds", items);
		}

		public ViewResult ProductsBySupplier ()
		{

			ViewBag.EditorField = "supplier";
			ViewBag.EditorTemplate = "SupplierSelector";
			ViewBag.Title = Resources.ProductsBySupplier;
			return View ();
		}

		[HttpPost]
		public ActionResult ProductsBySupplier (string supplier)
		{

			String sql = @" SELECT 	s.supplier_id SupplierId, s.code SupplierCode, 
                            s.name SupplierName, s.comment SupplierComment, 
		                    p.name ProductName, p.product_id ProductId, p.code ProductCode 
                            FROM supplier s INNER JOIN product p ON s.supplier_id = p.supplier ";

			if (!String.IsNullOrEmpty (supplier)) {
				sql += " WHERE s.supplier_id = :id ";

			}

			sql += " order by s.supplier_id, p.product_id desc;";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SupplierId", NHibernateUtil.Int32);
				query.AddScalar ("SupplierCode", NHibernateUtil.String);
				query.AddScalar ("SupplierName", NHibernateUtil.String);
				query.AddScalar ("SupplierComment", NHibernateUtil.String);

				query.AddScalar ("ProductId", NHibernateUtil.Int32);
				query.AddScalar ("ProductCode", NHibernateUtil.String);
				query.AddScalar ("ProductName", NHibernateUtil.String);

				if (!String.IsNullOrEmpty (supplier)) {
					query.SetInt32 ("id", Int32.Parse (supplier));

				}

				return query.DynamicList ();
			}, null);

			return PartialView ("_ProductsBySupplierReport", items);
		}

		public ViewResult ProductsOrderAndRefundsBySalesPerson ()
		{

			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.ProductsOrdersAndRefundsBySalesPerson;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductsOrderAndRefundsBySalesPerson (int employee, DateRange dates)
		{

			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT sales_order SalesOrder, 0 Refund, date Date, c.name Customer, product_name Product, 
                            d.quantity Quantity, product_code Code, model Model,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount_rate) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
                        INNER JOIN product p ON p.product_id = d.product
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
                            GROUP BY sales_order_detail_id";


			var orders = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Product", NHibernateUtil.String);
				query.AddScalar ("Quantity", NHibernateUtil.Int32);
				query.AddScalar ("Refund", NHibernateUtil.Int32);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			sql = @"SELECT sales_order SalesOrder, customer_refund Refund, s.date Date, c.name Customer, product_name Product, 
				d.quantity Quantity, product_code Code, model Model,
						GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
						FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
						WHERE f.cancelled = 0 AND fd.order_detail = d.sales_order_detail) SEPARATOR ' ') Invoices,
					-SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, IF(d.tax_rate > 0, 1 + d.tax_rate, 1)), 2)) Subtotal,
					-SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, IF(d.tax_rate > 0, 1 + d.tax_rate, 1), 1), 2)) Total
					FROM customer_refund m
					INNER JOIN sales_order s ON m.sales_order = s.sales_order_id
					INNER JOIN customer_refund_detail d ON m.customer_refund_id = d.customer_refund
					INNER JOIN customer c ON m.customer = c.customer_id
					INNER JOIN product p ON p.product_id = d.product
					WHERE m.sales_person = :employee AND m.completed = 1 AND m.cancelled = 0 AND
						s.date >= :start AND s.date <= :end
                            GROUP BY customer_refund_detail_id";

			var refunds = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Quantity", NHibernateUtil.Int32);
				query.AddScalar ("Product", NHibernateUtil.String);
				query.AddScalar ("Refund", NHibernateUtil.Int32);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList ();
			}, null);

			var items = orders.ToList ();

			items.AddRange (refunds.ToList ());

			return PartialView ("_ProductsOrderAndRefundsBySalesPerson", items);
		}

		public ViewResult PrintReceivedPayments (int store, DateTime start, DateTime end)
		{

			var qry = SalesOrderPayment.Queryable.Where (x => x.SalesOrder.IsPaid && x.Payment.Date > start.Date
									     && x.Payment.Date < end.Date.AddDays (1).AddMilliseconds (-1)
									     && x.Amount > 0 && x.SalesOrder.Store.Id == store)
							.Select (y => new ReceivedPayment {
								Date = y.Payment.Date,
								Customer = y.Payment.Customer,
								Amount = y.Amount,
								Method = y.Payment.Method,
								Serial = y.Payment.Serial,
								//SalesOrder = y.SalesOrder.Id
								//SalesOrders = y
							}).ToList ();

			return View ("_PdfReceivedPayments", qry);
		}

		public ViewResult ReceivedPayments ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			return View (new ReceivedPaymentsFilter { StartDate = DateTime.Now, EndDate = DateTime.Now });
		}

		[HttpPost]
		public ActionResult ReceivedPayments (ReceivedPaymentsFilter filter)
		{
			var received_payments = CustomerPayment.Queryable.Where (x => x.Date >= filter.StartDate.Date
			&& x.Date <= filter.EndDate.Date.AddDays (1).AddMilliseconds (-1));

			received_payments = filter.OnlyAppliedPayments ? received_payments.Where (x => x.Allocations.Count() > 0) : received_payments;

			var privileges = GetAccessPrivilege(SystemObjects.ReceivedPaymentsAdvancedSearchFilter);

			if (privileges.AllowRead) {
				received_payments = filter.StoreId.HasValue? received_payments.Where (x => x.Store.Id == filter.StoreId.Value):received_payments;
			} else {
				received_payments = received_payments.Where (x => x.Store.Id == WebConfig.Store.Id);
			}

			var items = received_payments.ToList ();

			return PartialView ("_ReceivedPayments", items);
		}

		public ViewResult CreditAndCollection ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			return View (new DateRange { StartDate = DateTime.Now, EndDate = DateTime.Now });
		}

		[HttpPost]
		public ActionResult CreditAndCollection (DateRange dates)
		{
			var summary = new CreditAndCollectionSummary (null, dates.StartDate, dates.EndDate);
			return PartialView ("_CreditAndCollection", summary);
		}

		#region Helpers

		void AnalyzeABC (IEnumerable<SummaryItem> items)
		{
			decimal total = items.Sum (x => x.Total);
			decimal sum = 0;
			decimal pct;

			foreach (var item in items) {
				pct = sum / total;
				sum += item.Total;

				if (pct < 0m) {
					item.Category = "X";
				} else if (pct < 0.7m) {
					item.Category = "A";
				} else if (pct < 0.95m) {
					item.Category = "B";
				} else {
					item.Category = "C";
				}
			}
		}

		#endregion
	}
}
