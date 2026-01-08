// 
// PurchasesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using System.Text;
using System.Text.RegularExpressions;
using Gma.QrCodeNet.Encoding.Masking;
using NHibernate;
using Castle.Core.Internal;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class PurchasesController : CustomController {
		public ActionResult Index ()
		{
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchPurchaseOrders (new Search<PurchaseOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<PurchaseOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchPurchaseOrders (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		public ActionResult Approvals ()
		{
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchPurchaseOrdersApproved (new Search<PurchaseOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Approvals (Search<PurchaseOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchPurchaseOrdersApproved (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<PurchaseOrder> SearchPurchaseOrdersApproved (Search<PurchaseOrder> search)
		{
			IQueryable<PurchaseOrder> qry = from x in PurchaseOrder.Queryable
							where x.IsCompleted && !x.IsApproved && !x.IsCancelled
							select x; ;

			if (search.Pattern != null) {
				search.Pattern = search.Pattern.Trim ();
				qry = from x in qry
				      where x.Supplier.Name.Contains (search.Pattern) ||
				      x.Creator.Nickname.Contains (search.Pattern)
				      select x;
			}

			qry = qry.OrderByDescending (x => x.Id);

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}
		Search<PurchaseOrder> SearchPurchaseOrders (Search<PurchaseOrder> search)
		{
			IQueryable<PurchaseOrder> qry = from x in PurchaseOrder.Queryable
							where
								(x.Creator == CurrentUser.Employee
								|| x.Updater == CurrentUser.Employee)
								&& !x.IsCancelled
							select x;

			string pattern = search.Pattern != null ? search.Pattern.Trim():null;

			if (!string.IsNullOrEmpty(pattern)) {

				int id = 0;
				if (Int32.TryParse (pattern, out id)) {
					qry = from x in PurchaseOrder.Queryable
					      where x.Id == id
					      select x;
				} else {
					qry = from x in PurchaseOrder.Queryable
					      where x.Supplier.Name.Contains (search.Pattern) ||
					      x.Details.Any(y => y.Warehouse.Name.Contains (search.Pattern))
					      select x;
					if (pattern.Contains (Resources.WilcardStringPatternForSearch)) {
						qry = from x in PurchaseOrder.Queryable
						      where !x.IsCancelled
						      select x;
					}
				}
			}

			qry = qry.OrderByDescending(x => x.Id);

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		//public ViewResult Print (int id)
		//{
		//	var item = PurchaseOrder.Find (id);
		//	return View (item);
		//}

		public ActionResult Details (int id)
		{
			var item = PurchaseOrder.Find (id);

			return View (item);
		}

		public ActionResult Approve (int id)
		{
			var item = PurchaseOrder.Find (id);

			if (!item.IsCancelled && item.IsCompleted && !item.IsApproved) {
				return View (item);
			}
			return RedirectToAction ("Approvals");
		}

		[HttpPost]
		public ActionResult Approve (int id, bool approve)
		{
			var item = PurchaseOrder.Find (id);

			if (!item.IsCancelled && item.IsCompleted && item.IsApproved) {
				return RedirectToAction("Approvals");
			}

			using (var scope = new TransactionScope ()) {
				item.Approver = CurrentUser.Employee;
				item.IsApproved = approve;
				item.IsCompleted = approve;
				item.ModificationTime = DateTime.Now;
				item.UpdateAndFlush ();
			}

			if (WebConfig.PurchaseOrderApprovalRequired && approve) {
				GenerateInventoryEntries (item);
			}

			return RedirectToAction ("Approvals");
		}

		public ActionResult Create ()
		{
			//if (!CashHelpers.ValidateExchangeRate ()) {
			//    return View ("InvalidExchangeRate");
			//}

			return PartialView ("_Create", new PurchaseOrder ());
		}

		[HttpPost]
		public ActionResult Create (PurchaseOrder item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			item.Supplier = Supplier.Find (item.SupplierId);
			item.Creator = CurrentUser.Employee;
			item.Updater = item.Creator;
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			//item.EstimatedReceiptDate = item.CreationTime;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", new PurchaseOrder { Id = item.Id });
		}

		//[HttpPost]
		public ActionResult CreatePurchaseBySupplier (int? id, int? warehouse_id)
		{
			var supplier = id.HasValue ? Supplier.TryFind (id) : null;
			var user = CurrentUser.Employee;
			var date = DateTime.Now;
			var PURCHASE_APPROVAL = WebConfig.PurchaseRequestApprovalRequired ? " prd.to_purchase = 1 " : string.Empty;

			var items = PurchaseRequestDetail.Queryable.Where(x => x.Product.Supplier == supplier
					&& !PurchaseOrderDetail.Queryable.Select(y => y.PurchaseRequestDetail).Any(y => y == x));
			items = WebConfig.PurchaseRequestApprovalRequired ? items.Where(x => x.ToPurchase) : items;

			items = warehouse_id.HasValue ? items.Where(x => x.Warehouse.Id == warehouse_id.Value) : items;

			var details = items.ToList ();

			if (details.Count <= 0) {
				return RedirectToAction ("ToPurchaseBySupplier");
			}

			var purchase = new PurchaseOrder {
				Updater = user,
				Supplier = supplier,
				Creator = user,
				CreationTime = date,
				ModificationTime = date
			};

			using (var scope = new TransactionScope ()) {
				purchase.CreateAndFlush ();
				foreach (var detail in details) {
					(new PurchaseOrderDetail {
						Order = purchase,
						PurchaseRequestDetail = detail,
						Warehouse = detail.Warehouse,
						Product = detail.Product,
						ProductCode = detail.Product.Code,
						ProductName = detail.Product.Name,
						Quantity = detail.Quantity,
						TaxRate = detail.Product.TaxRate,
						IsTaxIncluded = detail.Product.IsTaxIncluded,
						DiscountRate = 0,
						Price = ProductPrice.Queryable.Where(x => x.List == WebConfig.CostsList && x.Product == detail.Product).SingleOrDefault().Value,
						ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate (),
						Currency = WebConfig.DefaultCurrency
					}).CreateAndFlush ();
				}
			}

			return RedirectToAction("Edit", new { Id = purchase.Id });
		}

		public ActionResult ToPurchaseBySupplier ()
		{

			var PURCHASE_APPROVAL = WebConfig.PurchaseRequestApprovalRequired ? " AND prd.to_purchase = 1 " : string.Empty;

			var sql = @"SELECT s.name 'SupplierName', s.supplier_id 'SupplierId', 
					COUNT(*) 'PurchaseRequestDetailsCount', SUM(prd.quantity * pp.price) 'PurchaseTotal',
					GROUP_CONCAT(prd.purchase_request_detail_id SEPARATOR '|') 'PurchaseRequestDetailIds',
					GROUP_CONCAT(DISTINCT prd.warehouse SEPARATOR '|') 'WarehouseIds'
					FROM purchase_request_detail prd
					JOIN product p ON prd.product = p.product_id
					LEFT JOIN supplier s ON p.supplier = s.supplier_id
					LEFT JOIN purchase_order_detail pod ON prd.purchase_request_detail_id = pod.purchase_request_detail
					LEFT JOIN purchase_order po ON pod.purchase_order = po.purchase_order_id
					LEFT JOIN product_price pp ON pp.`list` = 0 AND pp.product = p.product_id
					WHERE (po.purchase_order_id IS NULL) PURCHASE_APPROVAL
					GROUP BY s.supplier_id;";
			sql = sql.Replace ("PURCHASE_APPROVAL", PURCHASE_APPROVAL);

			var items = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SupplierId", NHibernateUtil.Int32);
				query.AddScalar ("WarehouseIds", NHibernateUtil.String);
				query.AddScalar ("PurchaseRequestDetailsCount", NHibernateUtil.Int32);
				query.AddScalar ("SupplierName", NHibernateUtil.String);
				query.AddScalar ("PurchaseRequestDetailIds", NHibernateUtil.String);
				query.AddScalar ("PurchaseTotal", NHibernateUtil.Decimal);
				return query.DynamicList ();
			}, null);


			return View (items);
		}

		public ActionResult Edit (int id)
		{
			var item = PurchaseOrder.Find (id);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_MasterEditView", item);
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("Details", new {
					id = item.Id
				});
			}

			return View (item);
		}

		public ActionResult DiscardChanges (int id)
		{
			return PartialView ("_MasterView", PurchaseOrder.TryFind (id));
		}

		[HttpPost]
		public ActionResult Edit (PurchaseOrder item)
		{
			var entity = PurchaseOrder.Find (item.Id);

			entity.Supplier = Supplier.Find (item.SupplierId);
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_MasterView", entity);
		}

		[HttpPost]
		public ActionResult SetEstimatedReceiptDate (int id, DateTime? value)
		{
			var entity = PurchaseOrder.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {

				if (value.Value < DateTime.Now) {
					Response.StatusCode = 400;
					return Content (string.Format (Resources.Validation_DateGreaterThan, Resources.EstimatedReceiptDate, "Hoy"));
				}

				entity.EstimatedReceiptDate = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.EstimatedReceiptDate),
			});
		}

		[HttpPost]
		public JsonResult AddPurchaseDetail (int movement, int warehouse, int product)
		{
			var p = Product.Find (product);


			var cost = (from x in ProductPrice.Queryable
				    where x.Product.Id == product && x.List.Id == 0
				    select x.Value).SingleOrDefault ();

			var item = new PurchaseOrderDetail {
				Order = PurchaseOrder.Find (movement),
				Warehouse = Warehouse.Find (warehouse),
				Product = p,
				ProductCode = p.Code,
				ProductName = p.Name,
				Quantity = 1,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				DiscountRate = 0,
				Price = cost,
				ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate (),
				Currency = WebConfig.DefaultCurrency
			};

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return Json (new {
				id = item.Id
			});
		}

		[HttpPost]
		public ActionResult AddFromPurchaseRequest (int id, string value)
		{
			var details = new List<PurchaseRequestDetail> ();
			var entity = PurchaseOrder.TryFind (id);
			int request_id = 0;
			int supplier_id = entity.Supplier.Id;

			string request_filter = WebConfig.PurchaseRequestApprovalRequired ? " AND pr.approved = 1 " : "";


			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value.Contains (Resources.WilcardStringPatternForSearch)) {

				var sql = @"
					SELECT prd.purchase_request_detail_id id 
					FROM purchase_request_detail prd
					LEFT JOIN purchase_request pr ON prd.purchase_request = pr.purchase_request_id
					JOIN product p on prd.product = p.product_id
					LEFT JOIN (
						SELECT pod.purchase_request_detail detail, pod.product_name , pod.purchase_order 
						FROM purchase_order_detail pod
						JOIN purchase_order po ON po.purchase_order_id = pod.purchase_order
						WHERE pod.purchase_request_detail IS NOT NULL AND po.cancelled = FALSE
						) AS P1 ON P1.detail = prd.purchase_request_detail_id 
					WHERE pr.cancelled = 0 AND pr.completed = 1
					AND p.supplier = :supplier
					AND P1.detail IS NULL REQUEST_APPROVAL_FILTER;
					";

				sql = sql.Replace ("REQUEST_APPROVAL_FILTER", request_filter);

				var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
					var query = session.CreateSQLQuery (sql);
					query.SetParameter ("supplier", supplier_id);
					query.AddScalar ("id", NHibernateUtil.Int32);
					return query.DynamicList ();
				}, null);

				foreach (var x in raw) {
					details.Add (PurchaseRequestDetail.Find (x.id));
				}



			} else if (int.TryParse (value, out request_id) && request_id > 0) {
				var request = PurchaseRequest.TryFind (request_id);
				if (request == null || !request.IsCompleted || request.IsCancelled || request.Details.Count <= 0) {
					Response.StatusCode = 400;
					return Content (Resources.ItemNotFound);
				}
				if (!request.IsApproved && WebConfig.PurchaseRequestApprovalRequired) {
					Response.StatusCode = 400;
					return Content (string.Format (Resources.DoctoRequiresApproval, Resources.PurchaseRequest));
				}

				//TODO: Revisar por qué sigue metiendo items de requests cancelados
				var items = request.Details.Where (x => !PurchaseOrderDetail.Queryable
					.Select(y => y.PurchaseRequestDetail).Where(z =>
									  !z.PurchaseRequest.IsCancelled
									&& z.PurchaseRequest.IsCompleted
									&& z.PurchaseRequest.IsApproved).Contains (x));

				details.AddRange (items.ToList ());
			}

			List<int> added = new List<int> ();

			using (var scope = new TransactionScope ()) {
				foreach (var x in details) {

					var item = new PurchaseOrderDetail {
						Order = entity,
						Warehouse = x.Warehouse,
						WarehouseId = x.WarehouseId,
						Product = x.Product,
						PurchaseRequestDetail = x,
						ProductCode = x.Product.Code,
						ProductName = x.Product.Name,
						Quantity = x.Quantity,
						TaxRate = x.Product.TaxRate,
						IsTaxIncluded = x.Product.IsTaxIncluded,
						DiscountRate = 0,
						Price = (from y in ProductPrice.Queryable
							 where y.Product == x.Product && y.List.Id == 0
							 select y.Value).SingleOrDefault (),
						ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate (),
						Currency = WebConfig.DefaultCurrency
					};

					item.Create ();
					added.Add (item.Id);
				}
			}


			return Json (new { id = id, value = string.Empty, itemsChanged = string.Join (",", added) });
		}

		[HttpPost]
		public JsonResult EditDetailQuantity (int id, decimal value)
		{
			var detail = PurchaseOrderDetail.Find (id);

			if (value > 0) {
				detail.Quantity = value;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = detail.Quantity,
				total = detail.Total.ToString ("c")
			});
		}

		[HttpPost]
		public JsonResult EditDetailPrice (int id, string value)
		{
			var detail = PurchaseOrderDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.Trim (),
						    System.Globalization.NumberStyles.Currency,
						    null, out val);

			if (success && val >= 0) {
				detail.Price = val;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = detail.Price.ToString ("c"),
				total = detail.Total.ToString ("c")
			});
		}

		[HttpPost]
		public ActionResult EditDetailCurrency (int id, string value)
		{
			var detail = PurchaseOrderDetail.Find (id);
			CurrencyCode val;
			bool success;

			success = Enum.TryParse<CurrencyCode> (value.Trim (), out val);

			if (success) {
				decimal rate = CashHelpers.GetTodayExchangeRate (val);

				if (rate == 0) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				detail.Currency = val;
				detail.ExchangeRate = CashHelpers.GetTodayExchangeRate (val);

				using (var scope = new TransactionScope ()) {
					detail.Update ();
				}
			}

			return Json (new {
				id = id,
				value = detail.Currency.ToString (),
				rate = detail.ExchangeRate,
				total = detail.Total.ToString ("c")
			});
		}

		[HttpPost]
		public JsonResult EditDetailDiscount (int id, string value)
		{
			var detail = PurchaseOrderDetail.Find (id);
			bool success;
			decimal discount;

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out discount);
			discount /= 100m;

			if (success && discount >= 0 && discount <= 1) {
				detail.DiscountRate = discount;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = detail.DiscountRate.ToString ("p"),
				total = detail.Total.ToString ("c")
			});
		}

		[HttpPost]
		public ActionResult SetItemTaxRate (int id, string value)
		{
			var entity = PurchaseOrderDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Order.IsCompleted || entity.Order.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);

			// TODO: VAT value range validation
			if (success) {
				entity.TaxRate = val;

				using (var scope = new TransactionScope ()) {
					entity.Update ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.TaxRate),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public JsonResult EditDetailWarehouse (int id, int value)
		{
			var detail = PurchaseOrderDetail.Find (id);

			detail.Warehouse = Warehouse.Find (value);

			using (var scope = new TransactionScope ()) {
				detail.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = detail.Warehouse.Name
			});
		}

		public ActionResult GetTotals (int id)
		{
			var order = PurchaseOrder.Find (id);
			return PartialView ("_Totals", order);
		}

		public ActionResult GetDetail (int id)
		{
			return PartialView ("_DetailEditView", PurchaseOrderDetail.Find (id));
		}

		public ActionResult GetDetails (string itemsChanged)
		{

			bool ok = Regex.IsMatch (itemsChanged, "^\\d+(,\\d+)*$");
			if (!ok) {
				return null;
			}
			var ids = itemsChanged.Split (',').Select (x => Int32.Parse (x)).ToList ();
			var items = PurchaseOrderDetail.Queryable.Where (x => ids.Contains (x.Id)).ToList ();

			return PartialView ("_DetailsViewEdit", items);
		}

		[HttpPost]
		public JsonResult RemoveDetail (int id)
		{
			var item = PurchaseOrderDetail.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		// TODO: Remove inventory stuff
		[HttpPost]
		public ActionResult Confirm (int id)
		{
			PurchaseOrder item = PurchaseOrder.Find (id);
			var dt = DateTime.Now;
			var employee = CurrentUser.Employee;

			item.IsCompleted = true;
			item.ModificationTime = DateTime.Now;


			using (var scope = new TransactionScope ()) {
				foreach (var x in item.Details) {
					var price = x.Product.Prices.SingleOrDefault (t => t.List == WebConfig.CostsList);

					if (price == null) {
						price = new ProductPrice {
							List = WebConfig.CostsList,
							Product = x.Product
						};
					}

					price.Value = price.Value < x.Price ? x.Price : price.Value;
					price.Save ();
				}

				item.UpdateAndFlush ();
			}

			if (!WebConfig.PurchaseOrderApprovalRequired) {
				GenerateInventoryEntries (item);
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var item = PurchaseOrder.Find (id);
			if (item.IsCancelled || item.IsCompleted) {
				return RedirectToAction("Index");
			}

			item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
				item.Details.Where (x => x.PurchaseRequestDetail != null)
					.ForEach (x => { x.DeleteAndFlush(); });
			}

			return RedirectToAction ("Index");
		}

		public ViewResult View (int id)
		{
			var item = PurchaseOrder.Find (id);
			return View (item);
		}

		public ViewResult Print (int id)
		{
			var model = PurchaseOrder.Find (id);
			if (model.IsCompleted)
				return View ("PrintOrder", model);

			return View ("PrintQuotation", model);
		}

		public virtual ActionResult Pdf (int id)
		{
			var model = PurchaseOrder.Find (id);
			if (model.IsCompleted) {
				return PdfView ("PrintOrder", model);
			}
			return PdfView ("PrintQuotation", model);
		}

		[HttpPost]
		[ValidateInput (false)]
		public FileResult Export (int id)
		{
			var model = PurchaseOrder.Find (id);
			if (model.IsCompleted) {
				return null;
			}

			var html = PartialView ("PrintQuotation", model);

			return File (Encoding.ASCII.GetBytes (html.RenderToString ()),
				"application/vnd.ms-excel",
				Resources.PurchaseQuotation + " - " + model.Id + ".xls");
		}

		private void GenerateInventoryEntries (PurchaseOrder purchaseOrder)
		{
			if (purchaseOrder == null)
				return;
			if (purchaseOrder.IsCancelled)
				return;
			if (!purchaseOrder.IsCompleted)
				return;
			if (!purchaseOrder.IsApproved && WebConfig.PurchaseOrderApprovalRequired)
				return;

			var dt = DateTime.Now;
			var qry = from x in purchaseOrder.Details
				  group x by x.Warehouse into g
				  select new {
					  Warehouse = g.Key,
					  Details = g.ToList ()
				  };

			using (var scope = new TransactionScope ()) {

				foreach (var x in qry) {
					var master = new InventoryReceipt {
						Order = purchaseOrder,
						Warehouse = x.Warehouse,
						CreationTime = dt,
						ModificationTime = dt,
						Creator = CurrentUser.Employee,
						Updater = CurrentUser.Employee,
						Store = x.Warehouse.Store
					};

					master.Create ();

					foreach (var y in x.Details) {
						var already_received = InventoryReceiptDetail.Queryable.Where (z => z.PurchaseOrderDetail == y
									&& !z.Receipt.IsCancelled).Sum (w => (decimal?) w.Quantity) ?? 0;
						var detail = new InventoryReceiptDetail {
							Receipt = master,
							Product = y.Product,
							QuantityOrdered = y.Quantity,
							Quantity = y.Quantity - already_received,
							ProductCode = y.ProductCode,
							ProductName = y.ProductName,
							PurchaseOrderDetail = y
						};

						detail.Create ();
					}
				}
			}
		}

	}
}
