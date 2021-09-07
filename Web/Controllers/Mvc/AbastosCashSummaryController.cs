using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using NHibernate;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class AbastosCashSummaryController : Controller {
		// GET: AbastosCashSummary
		public ActionResult Index () {
			var range = new DateRange { StartDate = DateTime.Now, EndDate = DateTime.Now};
			//range.EndDate = range.EndDate.AddDays (-30);
			//range.StartDate = range.StartDate.AddDays (-30);
			return View (range);
		}

		[HttpPost]
		public ActionResult Index (DateRange range) {

			var refunds = CustomerRefund.Queryable.Where (x => !x.IsCancelled && x.IsCompleted && x.Date >= range.StartDate.Date
										&& x.Date < range.EndDate.Date.AddDays (1).AddMilliseconds (-1)).ToList ();
			var cash = CashSession.Queryable.Where (x => x.Start >= range.StartDate.Date && x.Start < range.EndDate.Date.AddDays (1).AddMilliseconds (-1)).ToList ();

			var summary = new AbastosCashCountReport {
				Start = range.StartDate.Date,
				End = range.EndDate.Date,
				Stock = GetStock (range),
				Expenses = GetExpenses (range),
				Sales = GetSales (range),
				Payables = GetPayables(range),
				Receivables = GetReceivables(range),
				TotalRefunds = refunds == null ? 0 : refunds.Sum(x => x.Total),
				InitialCash = cash == null ? 0 : cash.Sum(x => x.StartingCash)
			};

			return PartialView ("_Index", summary);
		}

		[HttpPost]
		public ViewResult Print (DateTime start, DateTime end) {

			var range = new DateRange { StartDate = start, EndDate = end };

			var summary = new AbastosCashCountReport {
				Start = range.StartDate.Date,
				End = range.EndDate.Date.AddDays(1),
				Stock = GetStock (range),
				Expenses = GetExpenses (range),
				Sales = GetSales (range),
				Payables = GetPayables (range),
				Receivables = GetReceivables (range)
			};

			return View (summary);
		}

		private IList<AbastosExpense> GetExpenses (DateRange range) {
			var list = new List<AbastosExpense> ();
			var sql = @"SELECT CONCAT(em.first_name, ' ' ,em.last_name) Cashier, ev.date Date, ex.expense Concept, 
					ed.amount Amount, ed.`comment` Comment, ev.purchase_order PurchaseOrder FROM expense_voucher_detail ed
					LEFT JOIN expenses ex ON ed.expense = ex.expense_id
					LEFT JOIN expense_voucher ev ON ev.expense_voucher_id = ed.expense_voucher
					LEFT JOIN cash_session c ON ev.cash_session = c.cash_session_id
					LEFT JOIN employee em ON c.cashier = em.employee_id
					WHERE ev.date BETWEEN :Start AND :End AND ev.cancelled=0 AND ev.completed = 1";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("Cashier", NHibernateUtil.String);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Concept", NHibernateUtil.String);
				query.AddScalar ("Amount", NHibernateUtil.Decimal);
				query.AddScalar ("Comment", NHibernateUtil.String);
				query.AddScalar ("PurchaseOrder", NHibernateUtil.Int32);
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);
			return results.Select(x => new AbastosExpense {
				Amount = x.Amount, Cashier = x.Cashier,
				Comment = x.Comment, Concept = x.Concept, Date = x.Date,
				LotCode = (x.PurchaseOrder != null? PurchaseOrder.Find ((int) x.PurchaseOrder).LotNumber : string.Empty)
			}
					).ToList();
		}

		private IList<AbastosSalesOrder> GetSales (DateRange range) {
			var list = new List<AbastosSalesOrder> ();

			/*This query gets sales details and it discounts refunds if there is so.*/

			var sql = @"SELECT c.name Customer, p.name Product, so.date Date ,m.name UnitOfMeasure,
					sod.quantity - IFNULL(Refund, 0) Quantity, sod.price * (1 + sod.tax_rate) - sod.price * sod.discount Price, so.payment_terms, 
					(sod.quantity - IFNULL(Refund, 0)) * ( sod.price * (1 + sod.tax_rate) - sod.price * sod.discount) Subtotal,
					if(so.payment_terms = 0,IF(sod.discount < 1, 'Contado', 'Merma'), 'Crédito') Term,
					 IFNULL(sum(R.Refund),0) Refund, sod.sales_order_detail_id Detail,
					l.reference Reference, l.lot_number LotCode  FROM sales_order_detail sod
					LEFT JOIN sales_order so ON so.sales_order_id = sod.sales_order AND so.completed = 1 AND so.cancelled = 0
					LEFT JOIN (SELECT crd.sales_order_detail Detail, SUM(crd.quantity) Refund FROM customer_refund cr 
									LEFT JOIN customer_refund_detail crd ON cr.customer_refund_id = crd.customer_refund 
									WHERE cr.cancelled = 0 AND cr.completed = 1
									GROUP BY crd.sales_order_detail) AS R ON sod.sales_order_detail_id = R.Detail
					LEFT JOIN product p ON p.product_id = sod.product
					LEFT JOIN sat_unit_of_measurement m ON p.unit_of_measurement LIKE m.sat_unit_of_measurement_id
					LEFT JOIN customer c ON so.customer = c.customer_id
					LEFT JOIN lot_serial_tracking l ON l.lot_serial_tracking_id = sod.lot_serial_tracking AND l.source IN (4, 6)
					WHERE so.date BETWEEN :Start AND :End
					GROUP BY sod.sales_order_detail_id
					HAVING Quantity > 0;";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Product", NHibernateUtil.String);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("UnitOfMeasure", NHibernateUtil.String);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);
				query.AddScalar ("Price", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Term", NHibernateUtil.String);
				query.AddScalar ("Reference", NHibernateUtil.Int32);
				query.AddScalar ("LotCode", NHibernateUtil.String);
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList();
			}, null);

			list = results.Select (x => new AbastosSalesOrder {
				Customer = x.Customer, Date = x.Date, LotCode = x.LotCode, Price = x.Price,
				Product = x.Product, Quantity = x.Quantity, Reference = x.Reference,
				Subtotal = x.Subtotal , Term = x.Term, UnitOfMeasure = x.UnitOfMeasure}).ToList ();

			return list;
		}

		private IList<AbastosStock> GetStock (DateRange range) {

			IList<AbastosStock> Stock = new List<AbastosStock> ();

			var sql = @"SELECT l.lot_number LotCode, w.name Warehouse, p.name Product,
					SUM(l.quantity) Quantity, 'Initial' Status FROM lot_serial_tracking l
					LEFT JOIN product p ON p.product_id = l.product
					LEFT JOIN warehouse w ON w.warehouse_id = l.warehouse
					WHERE l.date < :Start AND  l.lot_number IS NOT NULL
					GROUP BY l.warehouse, l.product, l.lot_number
					HAVING SUM(l.quantity) > 0
					UNION
					SELECT l.lot_number LotCode, w.name Warehouse, p.name Product,  
					SUM(l.quantity) Quantity, 'Purchase' Status FROM lot_serial_tracking l
					LEFT JOIN product p ON l.product = p.product_id
					LEFT JOIN warehouse w ON w.warehouse_id = l.warehouse
					WHERE l.source IN (6, 7) AND l.date BETWEEN :Start AND :End
					GROUP BY l.warehouse, l.product, l.lot_number
					UNION
					SELECT l.lot_number LotCode, w.name Warehouse, p.name Product,  
					SUM(l.quantity) Quantity, 'Sale' Status FROM lot_serial_tracking l
					LEFT JOIN product p ON l.product = p.product_id
					LEFT JOIN warehouse w ON w.warehouse_id = l.warehouse
					WHERE l.source IN (1,2,9) AND l.date BETWEEN :Start AND :End
					GROUP BY l.warehouse, l.product, l.lot_number
					UNION
					SELECT l.lot_number LotCode, w.name Warehouse, p.name Product,
					SUM(l.quantity) Quantity, 'Final' Status FROM lot_serial_tracking l
					LEFT JOIN product p ON p.product_id = l.product
					LEFT JOIN warehouse w ON w.warehouse_id = l.warehouse
					WHERE l.date < :End AND  l.lot_number IS NOT NULL
					GROUP BY l.warehouse, l.product, l.lot_number
					HAVING SUM(l.quantity) > 0";

			var data = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("LotCode", NHibernateUtil.String);
				query.AddScalar ("Warehouse", NHibernateUtil.String);
				query.AddScalar ("Product", NHibernateUtil.String);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);
				query.AddScalar ("Status", NHibernateUtil.String);
				query.SetParameter ("Start", range.StartDate.Date);
				query.SetParameter ("End", range.EndDate.Date.AddDays(1));
				return query.DynamicList ();
			}, null);

			var groups = data.GroupBy (x => new { x.LotCode, x.Warehouse, x.Product }, (key, group) =>
			new { Product = key.Product, Warehouse = key.Warehouse, LotCode = key.LotCode, Result = group.ToList () }).ToList ();

			foreach (var i in groups) {
				var item = new AbastosStock { LotCode = i.LotCode, Product = i.Product, Warehouse = i.Warehouse };
				Stock.Add (item);
				foreach (var e in i.Result) {

					switch (e.Status) {
					case "Initial":
						item.StartQuantity = e.Quantity;
						break;
					case "Purchase":
						item.PurchasedQuantity = e.Quantity;
						break;
					case "Sale":
						item.SoldQuantity = e.Quantity;
						break;
					default:
						item.FinalQuantity = e.Quantity;
						break;
					}
				}
			}

			return Stock;
		}

		private IList<AbastosAccountPayable> GetPayables (DateRange range) {
			var list = new List<AbastosAccountPayable> ();
			var sql = @"SELECT s.name Supplier, sp.method Method, sp.date 'Date', sp.amount Amount, p.lot_number Lot
					FROM supplier_payment sp 
					LEFT JOIN cash_session cs ON cs.cash_session_id = sp.cash_session
					LEFT JOIN supplier s ON s.supplier_id = sp.supplier
					LEFT JOIN purchase_order p on sp.purchase_order = p.purchase_order_id
					WHERE sp.date BETWEEN :Start AND :End AND sp.cancelled = 0";
			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("Amount", NHibernateUtil.Decimal);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Method", NHibernateUtil.Int32);
				query.AddScalar ("Lot", NHibernateUtil.String);
				query.AddScalar ("Supplier", NHibernateUtil.String);
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);
			return results.Select(x => new AbastosAccountPayable { Amount = x.Amount, Date = x.Date,
				PaymentMethod = (PaymentMethod)x.Method, Supplier = x.Supplier, LotNumber = x.Lot }).ToList();
		}

		private IList<AbastosAccountReceivable> GetReceivables (DateRange range) {
			var list = new List<AbastosAccountReceivable> ();
			var sql = @"SELECT cp.customer_payment_id IDPayment, cp.method Method, IFNULL(sop.sales_order_payment_id,'Sin aplicar') IDOrder, 
					cp.amount Amount, IFNULL(sum(sop.amount), 0) Applied, cp.date 'Date', c.name Customer, 
					so.payment_terms Term FROM customer_payment cp 
					LEFT JOIN sales_order_payment sop ON sop.customer_payment = cp.customer_payment_id
					LEFT JOIN sales_order so ON sop.sales_order = so.sales_order_id
					LEFT JOIN customer c ON cp.customer = c.customer_id
					WHERE cp.date BETWEEN :Start AND :End
					GROUP BY cp.customer_payment_id";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("IDPayment", NHibernateUtil.Int32);
				query.AddScalar ("Method", NHibernateUtil.Int32);
				query.AddScalar ("Term", NHibernateUtil.Int32);
				query.AddScalar ("IDOrder", NHibernateUtil.String);
				query.AddScalar ("Amount", NHibernateUtil.Decimal);
				query.AddScalar ("Applied", NHibernateUtil.Decimal);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);

			return results.Select(x => new AbastosAccountReceivable { Amount = x.Amount, Applied = x.Applied,
				Terms = x.Term != null ? (PaymentTerms)x.Term : PaymentTerms.NetD, 
				Customer = x.Customer, Date = x.Date, PaymentMethod = (PaymentMethod)x.Method }).ToList();
		}

	}
}
