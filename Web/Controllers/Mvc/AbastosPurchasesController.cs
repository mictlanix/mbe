using System;
using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Castle.ActiveRecord;
using Mictlanix.BE.Web.Models;
using System.Collections.Generic;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
	[Authorize]
	public class AbastosPurchasesController : PurchasesController
	{
		[HttpPost]
		public override ActionResult Confirm (int id) {
			PurchaseOrder item = PurchaseOrder.Find (id);

			if (item.Details.Any (x => x.Price <= 0)) {
				Response.StatusCode = 400;
				return Content (Resources.PuchaseOrderWithZeroCosts);
			}

			var qry = from x in item.Details
						group x by x.Warehouse into g
						select new {
								Warehouse = g.Key,
								Details = g.ToList ()
						};

			var dt = DateTime.Now;
			var employee = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
					foreach (var x in qry) {
					var master = new InventoryReceipt {
						Order = item,
						Warehouse = x.Warehouse,
						CreationTime = dt,
						ModificationTime = dt,
						Creator = employee,
						Updater = employee,
						Store = x.Warehouse.Store,
						IsCompleted = true
							};

							master.Create ();

							foreach (var y in x.Details) {
								var detail = new InventoryReceiptDetail {
									Receipt = master,
									Product = y.Product,
									QuantityOrdered = y.Quantity,
									Quantity = y.Quantity,
									ProductCode = y.ProductCode,
									ProductName = y.ProductName
								};

								detail.Create ();

							}
					}

					foreach (var x in item.Details) {
							var price = x.Product.Prices.SingleOrDefault (t => t.List.Id == 0);

							if (price == null) {
									price = new ProductPrice {
											List = PriceList.Find (0),
											Product = x.Product
									};
							}

							price.Value = x.Price;
							price.Save ();
					}

					item.IsCompleted = true;
					item.ModificationTime = DateTime.Now;
					item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public override ActionResult Cancel (int id)
		{
			var item = PurchaseOrder.Find (id);

			if(!AbastosInventoryHelpers.CancelPurchase (item, CurrentUser.Employee)) {
				ModelState.AddModelError (string.Empty, "Debe eliminar las ventas realizadas de este lote para poder cancelar la compra.");
				return View ("Details", item);
			}

			return RedirectToAction ("Index");
		}

		public ActionResult Pdf (int id)
		{
			var model = PurchaseOrder.Find (id);
			return PdfTicketView ("Print", model);
		}

		protected override Search<PurchaseOrder> SearchPurchaseOrders (Search<PurchaseOrder> search)
		{
			IQueryable<PurchaseOrder> qry;

			if (search.Pattern == null) {
				qry = from x in PurchaseOrder.Queryable
				      where !x.IsCancelled
				      orderby x.Id descending
				      select x;
			} else {
				qry = from x in PurchaseOrder.Queryable
				      where x.Supplier.Name.Contains (search.Pattern)
				      orderby x.Id descending
				      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult PurchaseAdjustments (int id) {
			var item = PurchaseOrder.Find (id);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_MasterEditView", item);
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			if (!item.IsCompleted && item.IsCancelled) {
				return RedirectToAction ("Edit", new {
					id = item.Id
				});
			}

			return View ("PurchaseAdjustments",item);
		}

		[HttpPost]
		public override JsonResult AddPurchaseDetail (int movement, int warehouse, int product) {

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

			AbastosInventoryHelpers.EnterProductLot (item, CurrentUser.Employee);

			return Json (new {
				id = item.Id
			});
		}

		public ActionResult RemovePurchaseDetail (int id)
		{

			var item = PurchaseOrderDetail.Find (id);
			var lot_code = AbastosInventoryHelpers.GetLotCode (item.Order);

			if (item.Order.IsCancelled || item.Order.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var remaining =
				AbastosInventoryHelpers.AvailableQuantityProduct (
					new LotSerialTracking { Product = item.Product,
								Warehouse = item.Warehouse,
								LotNumber = lot_code });

			if (remaining != item.Quantity) {
				Response.StatusCode = 400;
				return Content (Resources.Delete + " " + Resources.Sales);
			}

			var lot = LotSerialTracking.Queryable.Single (x => x.Reference == item.Id && x.Source == TransactionType.PurchaseOrder);
			using (var scope = new TransactionScope ()) {
				lot.DeleteAndFlush ();
			}


			return base.RemoveDetail (id);
			//var item = PurchaseOrderDetail.Find (id);

			//using (var scope = new TransactionScope ()) {
			//	item.DeleteAndFlush ();
			//}

			//return Json (new {
			//	id = id,
			//	result = true
			//});
		}

		[HttpPost]
		public ActionResult SetDetailQuantity (int id, decimal value) {


			var purchase_detail = PurchaseOrderDetail.Find (id);

			if (purchase_detail.Order.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var lot_detail = LotSerialTracking.Queryable.Single (x => x.Source == TransactionType.PurchaseOrder && x.Reference == id);

			if (purchase_detail.Quantity - value > AbastosInventoryHelpers.AvailableQuantityProduct (lot_detail)) {
				Response.StatusCode = 400;
				return Content (Resources.QuantitySold + ": " + (purchase_detail.Quantity - AbastosInventoryHelpers.AvailableQuantityProduct(lot_detail)) + " > " + Resources.Quantity + ": " + value);
			}

			using (var scope = new TransactionScope ()) {
				lot_detail.Quantity = value;
				lot_detail.UpdateAndFlush ();
			}

			return base.EditDetailQuantity (id, value);
			//var detail = PurchaseOrderDetail.Find (id);

			//if (value > 0) {
			//	detail.Quantity = value;

			//}

			//return Json (new {
			//	id = id,
			//	value = detail.Quantity,
			//	total = detail.Total.ToString ("c")
			//});
		}

		[HttpPost]
		public ActionResult SetDetailPrice (int id, string value) {

			var purchase_detail = PurchaseOrderDetail.Find (id);

			if (purchase_detail.Order.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			return base.EditDetailPrice (id, value);
			//var detail = PurchaseOrderDetail.Find (id);
			//bool success;
			//decimal val;

			//success = decimal.TryParse (value.Trim (),
			//			    System.Globalization.NumberStyles.Currency,
			//			    null, out val);

			//if (success && val >= 0) {
			//	detail.Price = val;

			//	using (var scope = new TransactionScope ()) {
			//		detail.UpdateAndFlush ();
			//	}
			//}

			//return Json (new {
			//	id = id,
			//	value = detail.Price.ToString ("c"),
			//	total = detail.Total.ToString ("c")
			//});
		}

		public JsonResult GetSuggestions (string pattern)
		{
			int id;
			List<PurchaseOrder> list;

			if (int.TryParse (pattern, out id)) {
				list = PurchaseOrder.Queryable.Where (x => x.IsCompleted && !x.IsCancelled && x.Id == id).ToList ();
			} else {
				list = PurchaseOrder.Queryable.Where (x => x.IsCompleted && !x.IsCancelled &&
							(x.Supplier.Name.Contains (pattern) || x.Supplier.Code.Contains (pattern)))
					.OrderByDescending (y => y.CreationTime).ToList ();
			}


			var query = from x in PurchaseOrder.Queryable
				    where x.IsCompleted && !x.IsCancelled && (
					    x.Id == id ||
					    x.Supplier.Name.Contains (pattern) ||
					    x.Supplier.Code.Contains (pattern))
				    select x;


			var items = list.ToList ().Select (x => new { id = x.Id, name = x.Supplier.Name , code = AbastosInventoryHelpers.GetLotCode(x) });

			return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		}
	}
}