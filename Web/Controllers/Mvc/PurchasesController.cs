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

		protected virtual Search<PurchaseOrder> SearchPurchaseOrders (Search<PurchaseOrder> search)
		{
			IQueryable<PurchaseOrder> qry;

			if (search.Pattern == null) {
				qry = from x in PurchaseOrder.Queryable
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

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", new PurchaseOrder { Id = item.Id });
		}

		public ActionResult Edit (int id) {

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

		public ActionResult DiscardChanges (int id) {

			return PartialView ("_MasterView", PurchaseOrder.TryFind (id));

		}

		[HttpPost]
		public ActionResult Edit (PurchaseOrder item) {

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
		public virtual JsonResult AddPurchaseDetail (int movement, int warehouse, int product) {

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
		public virtual JsonResult EditDetailQuantity (int id, decimal value) {
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
		public virtual JsonResult EditDetailPrice (int id, string value) {
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
		public ActionResult EditDetailCurrency (int id, string value) {
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
		public JsonResult EditDetailDiscount (int id, string value) {
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
		public ActionResult SetItemTaxRate (int id, string value) {
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
		public JsonResult EditDetailWarehouse (int id, int value) {
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

		public ActionResult GetTotals (int id) {
			var order = PurchaseOrder.Find (id);
			return PartialView ("_Totals", order);
		}

		public ActionResult GetDetail (int id) {
			return PartialView ("_DetailEditView", PurchaseOrderDetail.Find (id));
		}

		[HttpPost]
		public virtual JsonResult RemoveDetail (int id) {
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
		public virtual ActionResult Confirm (int id) {
			PurchaseOrder item = PurchaseOrder.Find (id);

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
						Store = x.Warehouse.Store
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

						InventoryHelpers.ChangeNotification (TransactionType.PurchaseOrder, item.Id,
							item.ModificationTime, x.Warehouse, null, y.Product, y.Quantity);
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
		public virtual ActionResult Cancel (int id) {
			var item = PurchaseOrder.Find (id);

			item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

	}
}
