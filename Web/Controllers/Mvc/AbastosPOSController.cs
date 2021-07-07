using System;
using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Castle.ActiveRecord;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using System.Collections.Generic;
using NHibernate;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class AbastosPOSController : SalesOrdersController {

		protected override Search<SalesOrder> SearchSalesOrders (Search<SalesOrder> search)
		{
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			var query = from x in SalesOrderDetail.Queryable
				    select x;

			if (!WebConfig.ShowSalesOrdersFromAllStores) {
				query = query.Where (x => x.SalesOrder.Store.Id == item.Id);
			}

			if (int.TryParse (pattern, out int id) && id > 0) {
				query = from x in query
					where x.SalesOrder.Id == id || x.SalesOrder.Serial == id
					
					select x;
			} else if (string.IsNullOrEmpty (pattern)) {
				query = from x in query
					where !x.SalesOrder.IsCancelled
					&& x.Quantity > (CustomerRefundDetail.Queryable.Where (y => y.SalesOrderDetail == x && y.Refund.IsCompleted && !y.Refund.IsCancelled).Sum (q => (decimal?) q.Quantity) ?? 0)
					select x;
			} else {
				query = from x in query
					where x.SalesOrder.Customer.Name.Contains (pattern) ||
						x.SalesOrder.SalesPerson.Nickname.Contains (pattern) ||
						(x.SalesOrder.SalesPerson.FirstName + " " + x.SalesOrder.SalesPerson.LastName).Contains (pattern)
					select x;
			}

			var qry = query.Select (x => x.SalesOrder).OrderByDescending(y=>y.Id);
			var qry2 = qry.Distinct ();

			search.Results = qry2.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = qry2.Count ();

			return search;
		}

		public override ActionResult Pdf (int id)
		{
			var model = SalesOrder.Find (id);
			//AbastosInventoryHelpers.RedoLotNumber ();
			return PdfTicketView ("Print", model);
		}

		public override JsonResult GetSuggestions (int order, string pattern)
		{
			var Order = SalesOrder.Queryable.Single (x => x.Id ==order);
			var pl = Order.Customer.PriceList;
			//var prices = ProductPrice.Queryable.Where (x => x.List.Id == pl.Id).ToList ();
			//var query = from x in LotSerialTracking.Queryable
			//	    join y in ProductPrice.Queryable on x.Product equals y.Product
			//	    where y.List.Id == pl.Id && (x.Source == TransactionType.InventoryReceipt || x.Source == TransactionType.PurchaseOrder) && (
			//		    x.Product.Name.Contains (pattern) ||
			//		    x.LotNumber.Contains (pattern) ||
			//		    x.Product.Model.Contains (pattern) ||
			//		    x.Product.SKU.Contains (pattern) ||
			//		    x.Product.Brand.Contains (pattern))
			//		    && (LotSerialTracking.Queryable.Where(w => w.Product == x.Product && x.Warehouse == w.Warehouse && x.LotNumber == w.LotNumber).Sum(q => (decimal?)q.Quantity)??0)
			//			- ((decimal?) SalesOrderDetail.Queryable.Where(z => z.SalesOrder.Id == order && z.Lot == x).Sum (m => m.Quantity) ?? 0) > 0
			//	    orderby x.LotNumber descending
			//		select new {
			//			x.Id,
			//			x.Product,
			//			Price = y.Value,
			//			Quantity = (LotSerialTracking.Queryable.Where (w => w.Product == x.Product && x.Warehouse == w.Warehouse && x.LotNumber == w.LotNumber).Sum (q => (decimal?) q.Quantity) ?? 0)
			//			- ((decimal?) SalesOrderDetail.Queryable.Where (z => z.SalesOrder.Id == order && z.Lot == x).Sum (m => m.Quantity) ?? 0),
			//			reference = x.Id,
			//			lotNumber = x.LotNumber,
			//			lot = x.Id,
			//			warehouse = x.Warehouse
			//		};

			//	var items = query.Take(15).ToList();

			var sql = @"SELECT l2.lot_serial_tracking_id Lot, p.name ProductName, p.code ProductCode, p.model ProductModel,
					p.sku ProductSKU, p.photo Photo,pp.price Price, l2.lot_number LotCode, l3.quantity Quantity
					FROM (SELECT l.product, l.warehouse, l.lot_number, SUM(l.quantity) quantity 
						FROM lot_serial_tracking l GROUP BY l.product, l.warehouse, l.lot_number
						HAVING SUM(l.quantity) > 0) AS l3
						JOIN  lot_serial_tracking l2 ON l2.warehouse = l3.warehouse AND l2.product = l3.product
						AND l3.lot_number = l2.lot_number AND (l2.source = 4 OR l2.source = 6)
					LEFT JOIN product p ON p.product_id = l3.product
					LEFT JOIN product_price pp ON pp.product = p.product_id
					WHERE pp.`list`= :PriceList AND LOWER(CONCAT_WS(p.code, p.name, p.brand, p.name, p.brand, l2.lot_number)) LIKE LOWER(:Pattern)
					ORDER BY l2.lot_serial_tracking_id DESC
					LIMIT 15";

			var results = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("Lot", NHibernateUtil.Int32);
				query.AddScalar ("ProductName", NHibernateUtil.String);
				query.AddScalar ("ProductCode", NHibernateUtil.String);
				query.AddScalar ("ProductModel", NHibernateUtil.String);
				query.AddScalar ("ProductSKU", NHibernateUtil.String);
				query.AddScalar ("Photo", NHibernateUtil.String);
				query.AddScalar ("LotCode", NHibernateUtil.String);
				query.AddScalar ("Price", NHibernateUtil.Decimal);
				query.AddScalar ("Quantity", NHibernateUtil.Decimal);
				query.SetParameter ("PriceList", pl.Id);
				query.SetParameter ("Pattern", "%"+pattern+"%");
				return query.DynamicList ();
			}, null);

			var items = from r in results
				select new {
					id = r.Lot,
					name = r.ProductName,
					code = r.ProductCode,
					model = r.ProductModel ?? Resources.None,
					sku = r.ProductSKU ?? Resources.None,
					url = Url.Content (r.Photo),
					price = r.Price,
					lot = r.LotCode,
					quantity = r.Quantity
				};

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public override ActionResult AddItem (int order_id, int lot_id)
		{
			var entity = SalesOrder.TryFind (order_id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var lot = LotSerialTracking.Queryable.Single (x => x.Id == lot_id);
			var p = lot.Product;
			int pl = entity.Customer.PriceList.Id;
			var cost = (from x in ProductPrice.Queryable
					where x.Product.Id == p.Id && x.List.Id == 0
					select x).SingleOrDefault ();
			var price = (from x in ProductPrice.Queryable
					where x.Product.Id == p.Id && x.List.Id == pl
					select x).SingleOrDefault ();
			var discount = (from x in CustomerDiscount.Queryable
					where x.Product.Id == p.Id && x.Customer.Id == entity.Customer.Id
					select x.Discount).SingleOrDefault ();

			if (AbastosInventoryHelpers.AvailableQuantityProduct (lot) - GetLockedQuantity (order_id, lot.Id) < p.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (Resources.TotalQuantityIssue);
			}

			if (cost == null) {
				cost = new ProductPrice {
					Value = decimal.Zero
				};
			}

			if (price == null) {
				price = new ProductPrice {
					Value = decimal.MaxValue
				};
			}

			var item = new SalesOrderDetail {
				SalesOrder = entity,
				Product = p,
				Warehouse = entity.PointOfSale.Warehouse,
				ProductCode = p.Code,
				ProductName = p.Name,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				Quantity = p.MinimumOrderQuantity,
				Cost = cost.Value,
				Price = price.Value,
				ConfirmedPrice = price.Value,
				DiscountRate = discount,
				Currency = entity.Currency,
				ExchangeRate = entity.ExchangeRate,
				Lot = lot,
				Comment = p.Comment ?? ""
			};

			if (p.Currency != entity.Currency) {
				item.Cost = cost.Value * CashHelpers.GetTodayExchangeRate (p.Currency, entity.Currency);
				item.Price = price.Value * CashHelpers.GetTodayExchangeRate (p.Currency, entity.Currency);
			}

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return Json (new {
				id = item.Id
			});
		}

		[HttpPost]
		public override ActionResult Confirm (int id)
		{
			var entity = SalesOrder.TryFind (id);
			bool changed = false;

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsDelivered = false;
			entity.IsCompleted = true;

			foreach (var detail in entity.Details) {
				if (detail.Price == decimal.Zero) {
					//return View ("ZeroPriceError", entity);
					ModelState.AddModelError ("error", Resources.SalesOrderWithZeroPrices);
					return View ("Edit", entity);
				}
			}

			foreach (var detail in entity.Details) {
				var available = AbastosInventoryHelpers.AvailableQuantityProduct (detail.Lot);
				if (detail.Quantity > available) {
					detail.Quantity = available;
					changed = true;
				}
			}

			if (changed) {
				ModelState.AddModelError ("error", Resources.QuantitiesHaveChanged);
				return View ("Edit", entity);
			}

			using (var scope = new TransactionScope ()) {
				var warehouse = entity.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				foreach (var x in entity.Details) {
					x.Warehouse = warehouse;
					x.Update ();

					AbastosInventoryHelpers.TakeProductFromLot(x, CurrentUser.Employee);
				}

				entity.UpdateAndFlush ();
			}

			if (entity.Terms == PaymentTerms.Immediate) {
				return RedirectToAction ("PayOrder", "Payments", new { id = id });
			}

			return RedirectToAction ("Index", new { id = id });

		}

		[HttpPost]
		public override ActionResult Cancel (int id)
		{
			var entity = SalesOrder.Find (id);

			if (entity.IsCancelled || entity.IsPaid) {
				return RedirectToAction ("Index");
			}

			AbastosInventoryHelpers.CancelSalesOrder (entity, CurrentUser.Employee);

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult SetDate (int id, DateTime? value)
		{
			var entity = SalesOrder.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				entity.Date = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Date)
			});
		}

		[HttpPost]
		public override ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = SalesOrderDetail.Find (id);

			var quantity = entity.SalesOrder.Details.Where (x => x.Lot.Id == entity.Lot.Id && x.Id != id).Sum (y => (decimal?) y.Quantity) ?? 0;
			

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value < entity.Product.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MinimumQuantityRequired, entity.Product.MinimumOrderQuantity));
			}

			if(value > AbastosInventoryHelpers.AvailableQuantityProduct (entity.Lot) - quantity) {
				Response.StatusCode = 400;
				return Content(string.Format(Resources.QuantityAvailable, AbastosInventoryHelpers.AvailableQuantityProduct(entity.Lot) - quantity, entity.Product.UnitOfMeasurement.Name));
			}

			entity.Quantity = value;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Quantity),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		public override ActionResult SetCustomer (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Customer.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Customer = item;
				entity.Contact = null;
				entity.ShipTo = null;
				entity.CustomerShipTo = null;
				entity.CustomerName = null;
				entity.Terms = PaymentTerms.Immediate;

				if (item.SalesPerson == null) {
					entity.SalesPerson = CurrentUser.Employee;
				} else {
					entity.SalesPerson = item.SalesPerson;
				}

				if (entity.Customer.HasCredit) {
					entity.Terms = PaymentTerms.NetD;
				}

				switch (entity.Terms) {
				case PaymentTerms.Immediate:
					entity.DueDate = entity.Date;
					break;
				case PaymentTerms.NetD:
					entity.DueDate = entity.Date.AddDays (entity.Customer.CreditDays);
					break;
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;
				entity.Recipient = string.Empty;
				entity.RecipientName = string.Empty;
				entity.RecipientAddress = null;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer),
				terms = entity.Terms,
				termsText = entity.Terms.GetDisplayName (),
				dueDate = entity.FormattedValueFor (x => x.DueDate),
				salesPerson = entity.SalesPerson.Id,
				salesPersonName = entity.SalesPerson.Name
			});
		}

		[HttpPost]
		public override ActionResult SetTerms (int id, string value)
		{
			bool success;
			PaymentTerms val;
			var entity = SalesOrder.Find (id);

			if (entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse (value.Trim (), out val);

			if (success) {
				if (val == PaymentTerms.NetD && !entity.Customer.HasCredit) {
					Response.StatusCode = 400;
					return Content (Resources.CreditLimitIsNotSet);
				}

				entity.Terms = val;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				switch (entity.Terms) {
				case PaymentTerms.Immediate:
					entity.DueDate = entity.Date;
					break;
				case PaymentTerms.NetD:
					entity.DueDate = entity.Date.AddDays (entity.Customer.CreditDays);
					break;
				}

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.Terms,
				dueDate = entity.FormattedValueFor (x => x.DueDate),
				totalsChanged = success
			});
		}

		protected decimal GetLockedQuantity (int order,int lot_id) {

			return SalesOrderDetail.Queryable.Where (x => x.Lot.Id == lot_id && x.SalesOrder.Id == order).Sum (y => (decimal?) y.Quantity) ?? 0;
		}
	}
}