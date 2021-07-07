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
			var range = new DateRange ();
			return View (range);
		}

		[HttpPost]
		public ActionResult Index (DateRange range) {
			var summary = new AbastosCashCountReport {
				Stock = GetStock (range),
				Expenses = GetExpenses (range),
				Sales = GetSales (range),
				Payables = GetPayables(),
				Receivables = GetReceivables(range)
			};

			return PartialView (summary);
		}

		private IList<AbastosExpense> GetExpenses (DateRange range) {
			var list = new List<AbastosExpense> ();
			var sql = @"SELECT CONCAT(em.first_name, ' ' ,em.last_name) Cashier, ev.date Date, ex.expense Concept, 
					ed.amount Amount, ed.`comment` Comment FROM expense_voucher_detail ed
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
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);
			return results.Select(x => new AbastosExpense { Amount = x.Amount, Cashier = x.Cashier,
							Comment = x.Comment, Concept = x.Concept, Date = x.Date }).ToList();
		}

		private IList<AbastosSalesOrder> GetSales (DateRange range) {
			var list = new List<AbastosSalesOrder> ();
			var sql = @"SELECT c.name Customer, p.name Product, s.date Date ,m.name UnitOfMeasure, sd.quantity Quantity, sd.price * (1 + sd.tax_rate) - sd.price * sd.discount Price,  
					sd.quantity * ( sd.price * (1 + sd.tax_rate) - sd.price * sd.discount) Subtotal, if(s.payment_terms = 0, 'Contado', 'Crédito') Term,
					l.reference Reference, l.lot_number LotCode FROM sales_order s
					LEFT JOIN customer c ON s.customer = c.customer_id
					LEFT JOIN sales_order_detail sd ON s.sales_order_id = sd.sales_order
					LEFT JOIN product p ON p.product_id = sd.product
					LEFT JOIN sat_unit_of_measurement m ON p.unit_of_measurement LIKE m.sat_unit_of_measurement_id
					LEFT JOIN lot_serial_tracking l ON l.lot_serial_tracking_id = sd.lot_serial_tracking AND l.source IN (4, 6)
					WHERE s.date BETWEEN :Start AND :End AND s.completed = 1 AND s.cancelled = 0
					ORDER BY sd.product";

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
			var list = new List<AbastosStock> ();
			var sql = @"SELECT l.lot_number LotCode, w.name Warehouse, p.name Product, max(l.date) LastMovement,
					SUM(l.quantity) Quantity FROM lot_serial_tracking l
					LEFT JOIN product p ON p.product_id = l.product
					LEFT JOIN warehouse w ON w.warehouse_id = l.warehouse
					WHERE l.date < :End AND  l.lot_number IS NOT NULL
					GROUP BY l.warehouse, l.product, l.lot_number
					HAVING SUM(l.quantity) > 0 OR (SUM(l.quantity) = 0 AND MAX(l.date) BETWEEN :PreviousEnd AND :End )";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("LotCode", NHibernateUtil.String);
				query.AddScalar ("Warehouse", NHibernateUtil.String);
				query.AddScalar ("Product", NHibernateUtil.String);
				query.AddScalar ("LastMovement", NHibernateUtil.DateTime);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);
				query.SetParameter ("PreviousEnd", range.EndDate.AddDays(-2));
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);

			return results.Select(x => new AbastosStock { LotCode = x.LotCode, Product = x.Product,
				Quantity = x.Quantity, Warehouse = x.Warehouse }).ToList();
		}
		private IList<AbastosAccountPayable> GetPayables () {
			var list = new List<AbastosAccountPayable> ();
			var sql = @"";
			return list;
		}
		private IList<AbastosAccountReceivable> GetReceivables (DateRange range) {
			var list = new List<AbastosAccountReceivable> ();
			var sql = @"SELECT cp.customer_payment_id IDPayment, cp.method Method, IFNULL(sop.sales_order_payment_id,'Sin aplicar') IDOrder, 
					cp.amount Payment, IFNULL(sum(sop.amount), 0) Applied, cp.date 'Date', c.name Customer, 
					IF(so.payment_terms = 0, 'Contado', 'Crédito') Termino FROM customer_payment cp 
					LEFT JOIN sales_order_payment sop ON sop.customer_payment = cp.customer_payment_id
					LEFT JOIN sales_order so ON sop.sales_order = so.sales_order_id
					LEFT JOIN customer c ON cp.customer = c.customer_id
					WHERE cp.date BETWEEN :Start AND :End AND (sop.sales_order_payment_id IS NULL)
					GROUP BY cp.customer_payment_id";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				//query.AddEntity (typeof (AbastosSalesOrder));
				query.AddScalar ("IDPayment", NHibernateUtil.Int32);
				query.AddScalar ("Method", NHibernateUtil.Int32);
				query.AddScalar ("IDOrder", NHibernateUtil.String);
				query.AddScalar ("Payment", NHibernateUtil.Decimal);
				query.AddScalar ("Applied", NHibernateUtil.Decimal);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.SetParameter ("Start", range.StartDate);
				query.SetParameter ("End", range.EndDate);
				return query.DynamicList ();
			}, null);

			return results.Select(x => new AbastosAccountReceivable { Amount = x.Payment,
				Customer = x.Customer, Date = x.Date, PaymentMethod = (PaymentMethod)x.Method }).ToList();
		}

	}
}
