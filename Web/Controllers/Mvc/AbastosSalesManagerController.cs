using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class AbastosSalesManagerController : CustomController {
		// GET: AbastosSalesManager
		public ActionResult Index () {

			var search = SearchPurchaseClearance (new Search<PurchaseClearance> { Limit = WebConfig.PageSize });
			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<PurchaseClearance> search)
		{
			if (ModelState.IsValid) {
				search = SearchPurchaseClearance (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult CreateFromPurchaseOrder (string value)
		{

			int id = 0;
			PurchaseOrder item = null;

			if (int.TryParse (value, out id)) {
				try {
					item = PurchaseOrder.Find (id);
				} catch {
					Response.StatusCode = 400;
					return Content (Resources.ItemNotFound);
				}
			}

			if (item.IsCancelled || !item.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.Complete + " " + Resources.Purchase);
			}

			var clearance = new PurchaseClearance {
				PurchaseOrder = item.Id,
				Supplier = item.Supplier.Name,
				LotCode = AbastosInventoryHelpers.GetLotCode(item),
				Creator = CurrentUser.Employee,
				Updater = CurrentUser.Employee,
				CreationTime = DateTime.Now,
				ModificationTime = DateTime.Now,
				Commission = 0.1m
			};

			using (var scope = new TransactionScope ()) {
				clearance.Create ();
				var lot_number = AbastosInventoryHelpers.GetLotCode (item);

				var sql = @"SELECT T.product Product, T.product_name ProductName, T.Price Price, -SUM(T.quantity) Quantity, T.lot Lot FROM 
					       (SELECT sd.sales_order_detail_id, l.product, sd.product_name, sd.price -sd.price * sd.discount Price, l.quantity, sd.lot_serial_tracking lot FROM lot_serial_tracking l
						LEFT JOIN sales_order_detail sd ON l.reference = sd.sales_order_detail_id
						WHERE l.lot_number LIKE :lot AND l.source = 1
						UNION 
						SELECT cd.sales_order_detail, l.product, cd.product_name, cd.price -cd.price * cd.discount Price, l.quantity, sd.lot_serial_tracking lot FROM lot_serial_tracking l
						LEFT JOIN customer_refund_detail cd ON l.reference = cd.customer_refund_detail_id
						LEFT JOIN sales_order_detail sd ON sd.sales_order_detail_id = cd.sales_order_detail
						WHERE l.lot_number LIKE :lot AND l.source = 2
						UNION
						SELECT sd.sales_order_detail_id, l.product, sd.product_name, sd.price -sd.price * sd.discount Price, l.quantity, sd.lot_serial_tracking lot FROM lot_serial_tracking l
						LEFT JOIN sales_order_detail sd ON l.reference = sd.sales_order_detail_id
						WHERE l.lot_number LIKE :lot AND l.source = 9) AS T
					GROUP BY T.product, T.Price, T.Lot
					HAVING SUM(T.quantity) < 0
					ORDER BY T.product";

				var items = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
					var query = session.CreateSQLQuery (sql);
					query.AddScalar ("Lot", NHibernateUtil.Int32);
					query.AddScalar ("Product", NHibernateUtil.Int32);
					query.AddScalar ("ProductName", NHibernateUtil.String);
					query.AddScalar ("Price", NHibernateUtil.Decimal);
					query.AddScalar ("Quantity", NHibernateUtil.Decimal);
					query.SetParameter ("lot", lot_number);
					return query.DynamicList ();
				}, null);

				var groups = items.GroupBy (x => x.Lot).ToList ();

				foreach (var detail in item.Details) {

					var clearanceDetail = new PurchaseClearanceDetail {
						Currency = detail.Currency,
						PurchaseClearance = clearance,
						DiscountRate = detail.DiscountRate,
						ExchangeRate = detail.ExchangeRate,
						IsTaxIncluded = detail.IsTaxIncluded,
						Price = detail.Price,
						ProductCode = detail.ProductCode,
						ProductName = detail.ProductName,
						Quantity = detail.Quantity,
						TaxRate = detail.TaxRate,
						Warehouse = detail.Warehouse.Name,
						IsCharge = true
					};

					clearanceDetail.Create ();
					var lot_detail = LotSerialTracking.Queryable.Single (x => x.Reference == detail.Id && x.Source == TransactionType.InventoryReceipt);

					foreach (var entry in items.Where (x => x.Lot == lot_detail.Id)) {
						new PurchaseClearanceDetailEntry {
							ProductName = entry.ProductName,
							Price = entry.Price,
							Quantity = entry.Quantity,
							PurchaseClearanceDetail = clearanceDetail
						}.Create ();

					}

				}

				var expense_items = ExpenseVoucherDetail.Queryable.Where (x => x.ExpenseVoucher.PurchaseOrder == item
								&& !x.ExpenseVoucher.IsCancelled && x.ExpenseVoucher.IsCompleted).ToList ();

				var expenses = new PurchaseClearanceDetail {
					Currency = CurrencyCode.MXN,
					PurchaseClearance = clearance,
					DiscountRate = 0,
					ExchangeRate = 0,
					IsTaxIncluded = false,
					Price = 0,
					ProductCode = Resources.Expenses,
					ProductName = Resources.Expenses,
					Quantity = expense_items.Count(),
					TaxRate = 0,
					Warehouse = WebConfig.PointOfSale.Warehouse.Name,
					IsCharge = false
				};
				expenses.Create();

				foreach (var expense in expense_items) {
					new PurchaseClearanceDetailEntry {
						ProductName = expense.Expense.Name + " - " + expense.Comment,
						Quantity = 1,
						Price = expense.Amount,
						PurchaseClearanceDetail = expenses
					}.Create ();
				}

			}

			return Json (new { url = Url.Action ("Edit", new { id = clearance.Id }) });
		}

		public ActionResult Edit (int id)
		{
			var item = PurchaseClearance.Find (id);

			if (item.IsCancelled)
				return RedirectToAction ("Index");
			if (item.IsCompleted)
				return RedirectToAction ("View");

			return View (item);
		}

		public ActionResult TotalPurchaseDetail (int id) {

			var summary = new PurchaseClearanceDetailSummary (id);
			return PartialView ("_TotalPurchaseDetail", summary);
		}

		public ActionResult TotalExpensesDetail (int id) {

			var summary = new PurchaseClearanceDetailSummary (id);
			return PartialView ("_TotalExpensesClearance", summary);
		}

		public ActionResult AddPurchaseClearanceDetailEntry (int id) {

			var detail = PurchaseClearanceDetail.Find(id);
			if (detail.PurchaseClearance.IsCancelled || detail.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			var item = new PurchaseClearanceDetailEntry {
				Currency = detail.Currency,
				DiscountRate = detail.DiscountRate,
				Price = 0,
				Quantity = 0,
				PurchaseClearanceDetail = detail
			};

			using (var scope = new TransactionScope ()) {
				item.Create ();
			}

			return Json (new { id = item.Id, result = true });

		}

		public ActionResult AddExpenseClearanceDetailEntry (int id) {

			var clearance = PurchaseClearanceDetail.Find (id);

			if (clearance.PurchaseClearance.IsCancelled || clearance.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var item = new PurchaseClearanceDetailEntry {
				Currency = WebConfig.DefaultCurrency,
				DiscountRate = 0,
				ProductName = "",
				Price = 0,
				Quantity = 1,
				PurchaseClearanceDetail = clearance
			};

			using (var scope = new TransactionScope ()) {
				item.Create ();
				clearance.Quantity = clearance.Details.Count();
				clearance.UpdateAndFlush ();
			}

			return Json (new { id = item.Id, result = true });

		}
		public ActionResult RemovePurchaseClearanceDetailEntry (int id) {
			var item = PurchaseClearanceDetailEntry.Find (id);
			var clearanceDetail = item.PurchaseClearanceDetail;

			if (item.PurchaseClearanceDetail.PurchaseClearance.IsCancelled || item.PurchaseClearanceDetail.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				item.Delete ();
				if (!clearanceDetail.IsCharge) {
					clearanceDetail.Quantity = clearanceDetail.Details.Count ();
					clearanceDetail.UpdateAndFlush ();
				}
			}

			return Json (new { id = item.Id });
		}

		public ActionResult Item (int id) {
			return PartialView ("_ItemEditorView", PurchaseClearanceDetailEntry.Find(id));
		}
		public ActionResult ItemExpense (int id) {
			return PartialView ("_ExpenseClearanceEditorView", PurchaseClearanceDetailEntry.Find(id));
		}

		public ActionResult SetItemPrice (int id, decimal value) {
			var item = PurchaseClearanceDetailEntry.Find (id);

			if (item.PurchaseClearanceDetail.PurchaseClearance.IsCancelled || item.PurchaseClearanceDetail.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				item.Price = value >= 0 ? value : 0;
				item.Update ();
			}

			return Json (new { id = item.Id, value = item.Price.ToString("C2") });
		}

		public ActionResult SetExpenseName (int id, string value) {
			var item = PurchaseClearanceDetailEntry.Find (id);

			if (item.PurchaseClearanceDetail.PurchaseClearance.IsCancelled || item.PurchaseClearanceDetail.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				item.ProductName = value;
				item.Update ();
			}

			return Json (new { id = item.Id, value = item.ProductName });
		}

		public ActionResult SetComment (int id, string value) {
			var item = PurchaseClearance.Find (id);

			if (item.IsCancelled || item.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				item.Comment = value;
				item.Update ();
			}

			return Json (new { id = item.Id, value = item.Comment });
		}

		public ActionResult SetItemQuantity (int id, decimal value) {
			var item = PurchaseClearanceDetailEntry.Find (id);

			if (item.PurchaseClearanceDetail.PurchaseClearance.IsCancelled || item.PurchaseClearanceDetail.PurchaseClearance.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var clearance_detail = item.PurchaseClearanceDetail;
			var remaining = clearance_detail.Quantity - clearance_detail.Details.Sum (x => x.Quantity) + item.Quantity;

			if (value > remaining) {
				Response.StatusCode = 400;
				return Content (Resources.QuantityRemaining + ": " + remaining);
			}

			using (var scope = new TransactionScope ()) {
				item.Quantity = value >= 0 ? value : 0;
				item.Update ();
			}

			return Json (new { id = item.Id, value = value });
		}

		public ActionResult SetCommission (int id, decimal value)
		{
			var entity = PurchaseClearance.Find (id);
			decimal val;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			val = value / 100m;
			entity.Commission = val;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor(x => x.Commission),
			});
		}

		public ActionResult Cancel (int id) {
			var item = PurchaseClearance.Find (id);
			item.IsCancelled = true;
			item.ModificationTime = DateTime.Now;
			item.Updater = CurrentUser.Employee;
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}
			return RedirectToAction ("Index");
		}

		public ActionResult View (int id) {
			var item = PurchaseClearance.Find (id);
			return View(item);
		}
		public virtual ActionResult Pdf (int id) {
			var item = PurchaseClearance.Find (id);

			if (item.IsCancelled || !item.IsCompleted) {
				return RedirectToAction ("Index");
			}
			return PdfView ("Print", item);
		}
		
		public ActionResult Confirm (int id) {
			var item = PurchaseClearance.Find (id);
			item.IsCompleted = true;
			item.ModificationTime = DateTime.Now;
			item.Updater = CurrentUser.Employee;
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}
			return RedirectToAction ("Index");
		}

		//public ActionResult ClearanceProduct () {
		//	var search = new Search<PurchaseOrder> {
		//		Limit = WebConfig.PageSize,
		//		Results = PurchaseOrder.Queryable.Where (x => x.IsCompleted && !x.IsCancelled).ToList (),
		//		Total = PurchaseOrder.FindAll ().Length
		//	};
		//	return View (search);
		//}

		public ActionResult NormalizeLotNumbers () {

			ViewBag.TotalUpdates = 0;

			ViewBag.TotalUpdates = AbastosInventoryHelpers.NormalizeLotNumbers ();

			return View ("NormalizeLotNumbers");
		}

		protected Search<PurchaseClearance> SearchPurchaseClearance (Search<PurchaseClearance> search) {
			IQueryable<PurchaseClearance> qry;

			if (search.Pattern == null) {
				qry = from x in PurchaseClearance.Queryable
				      where !x.IsCancelled
				      orderby x.Id descending
				      select x;
			} else {
				qry = from x in PurchaseClearance.Queryable
				      where x.Supplier.Contains (search.Pattern)
				      orderby x.Id descending
				      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();


			return search;
		}
	}
}