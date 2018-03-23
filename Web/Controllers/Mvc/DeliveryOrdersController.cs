// 
// DeliveryOrdersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using System.Collections.Generic;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class DeliveryOrdersController : CustomController {
		public ViewResult Index ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			var search = SearchDeliveryOrders (new Search<DeliveryOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<DeliveryOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchDeliveryOrders (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<DeliveryOrder> SearchDeliveryOrders (Search<DeliveryOrder> search)
		{
			IQueryable<DeliveryOrder> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			query = DeliveryOrder.Queryable.Where (x => !x.IsCancelled && !x.Details.Any(y => y.OrderDetail == null)
								     && !x.Details.Any (y => y.OrderDetail.SalesOrder.IsCancelled))
						.OrderBy (x => x.IsCompleted || x.IsCancelled ? 1 : 0)
						.OrderByDescending (x => x.Id);

			if (int.TryParse (pattern, out id) && id > 0) {
				query = query.Where (y => y.Id == id || y.Serial == id);
			} else {
				query = query.Where (x => x.Customer.Name.Contains (pattern));
			}

			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = search.Results.Count ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = DeliveryOrder.Find (id);
			return View (item);
		}

		public ActionResult Print (int id)
		{
			var item = DeliveryOrder.Find (id);

			if (WebConfig.DeliveryOrdersUseMiniPrinter) {
				return PdfTicketView (WebConfig.DeliveryOrderTicket, item);
			} else {
				return PdfView (WebConfig.DeliveryOrderFormat, item);
			}

		}

		public ActionResult PrintFormat (int id)
		{
			var item = DeliveryOrder.Find (id);
			return PdfView (WebConfig.DeliveryOrderFormat, item);
		}

		[HttpPost]
		public ActionResult New (string value)
		{
			int id = 0;
			SalesOrder entity = null;
			if (int.TryParse (value, out id)) {
				entity = SalesOrder.TryFind (id);
			}

			if (entity == null) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotFound);
			}

			if (!entity.IsCompleted || entity.IsCancelled || entity.IsDelivered) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			//if (entity.ShipTo == null) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.ShipToRequired);
			//}

			if (entity.Store != WebConfig.Store) {
				Response.StatusCode = 400;
				return Content (Resources.InvalidStore);
			}

			DeliveryOrder item = new DeliveryOrder ();
			item.Customer = Customer.TryFind (WebConfig.DefaultCustomer);

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			item.Store = WebConfig.Store;

			item.Serial = 0;
			item.Date = DateTime.Now;
			item.CreationTime = DateTime.Now;
			item.Creator = CurrentUser.Employee;
			item.ModificationTime = item.CreationTime;
			item.Updater = item.Creator;
			item.Customer = entity.Customer;
			item.ShipTo = entity.ShipTo;
			item.Comment = entity.Comment;


			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();

				foreach (var detail in entity.Details.Where(x => GetRemainQuantityBySalesOrderDetail(x) > 0.0m)) {
					var deliverydetail = new DeliveryOrderDetail {
						DeliveryOrder = item,
						OrderDetail = detail,
						Product = detail.Product,
						ProductCode = detail.ProductCode,
						ProductName = detail.ProductName,
						Quantity = detail.Quantity
					};
					deliverydetail.CreateAndFlush ();
				}
			}

			return Json (new { url = Url.Action ("Edit", new { id = item.Id }) });
		}

		public ActionResult Edit (int id)
		{
			var item = DeliveryOrder.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new { id = item.Id });
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			foreach (var detail in item.Details) {
				if (detail.Quantity > GetRemainQuantityBySalesOrderDetail(detail.OrderDetail)) {
					detail.Quantity = GetRemainQuantityBySalesOrderDetail(detail.OrderDetail);
				}
			}

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return View (item);
		}

		public JsonResult Addresses (int id)
		{
			var item = DeliveryOrder.TryFind (id);
			var query = from x in item.Customer.Addresses
				    select new { value = x.Id, text = x.ToString () };

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetDate (int id, DateTime? value)
		{
			var entity = DeliveryOrder.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			if (value != null) {
				entity.Date = value.Value;
			} else {
				entity.Date = null;
			}
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Date)
			});
		}

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
		{
			var entity = DeliveryOrder.Find (id);
			var customer = Customer.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (customer == null) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			entity.ShipTo = null;
			entity.Customer = customer;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			foreach (var detail in entity.Details) {
				detail.Delete ();
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer)
			});
		}

		[HttpPost]
		public ActionResult SetShipTo (int id, int value)
		{
			var entity = DeliveryOrder.Find (id);
			var item = Address.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.ShipTo = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = id, value = entity.ShipTo.ToString () });
		}

		[HttpPost]
		public ActionResult AddItems (int id, string value)
		{

			var entity = DeliveryOrder.Find (id);
			SalesOrder sales_order = null;
			int sales_order_id = 0;
			int count = 0;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (int.TryParse (value, out sales_order_id)) {
				sales_order = SalesOrder.TryFind (sales_order_id);
			}

			if (sales_order == null) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotFound);
			}

			if (sales_order.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (sales_order.IsDelivered) {
				Response.StatusCode = 400;
				return Content (Resources.Delivered);
			}

			if (sales_order.Customer != entity.Customer) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MismatchCustomers, Resources.SalesOrder, Resources.DeliveryOrder));
			}

			var Details = sales_order.Details.Where(x => !entity.Details.Any(y => y.OrderDetail == x)).ToList();

			if (!(Details.Count () > 0)) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			using (var scope = new TransactionScope ()) {
				foreach (var x in Details.Where(y => GetRemainQuantityBySalesOrderDetail(y) > 0.0m)) {

					var item = new DeliveryOrderDetail {
						DeliveryOrder = entity,
						Product = x.Product,
						OrderDetail = x,
						ProductCode = x.ProductCode,
						ProductName = x.ProductName,
						Quantity = GetRemainQuantityBySalesOrderDetail (x)
					};

					item.Create ();
					count++;
				}
			}


			return Json (new { id = id, value = string.Empty, itemsChanged = count });
		}

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = DeliveryOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Comment = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.Comment
			});
		}

		[HttpPost]
		public ActionResult RemoveItem (int id)
		{
			var entity = DeliveryOrderDetail.Find (id);

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}

		public ActionResult Item (int id)
		{
			var item = DeliveryOrderDetail.Find (id);
			return PartialView ("_ItemEditorView", item);
		}

		public ActionResult Items (int id)
		{
			var item = DeliveryOrder.Find (id);
			return PartialView ("_Items", item.Details);
		}

		[HttpPost]
		public ActionResult SetItemProductName (int id, string value)
		{
			var entity = DeliveryOrderDetail.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (val.Length == 0) {
				entity.ProductName = entity.Product.Name;
			} else {
				entity.ProductName = val;
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.ProductName
			});
		}

		[HttpPost]
		public ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = DeliveryOrderDetail.Find (id);

			var product = entity.Product;

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value >= 0 && value <= GetRemainQuantityBySalesOrderDetail(entity.OrderDetail)) {
				entity.Quantity = value;
			} else {
				entity.Quantity = GetRemainQuantityBySalesOrderDetail(entity.OrderDetail);
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Quantity)
			});
		}

		[HttpPost]
		public ActionResult Confirm (int id)
		{
			var entity = DeliveryOrder.TryFind (id);
			bool inconsistencies = false;

			if (entity == null || entity.IsCompleted || entity.IsCancelled ||
					entity.Details.Any (x => x.OrderDetail.SalesOrder.IsCancelled) ||
					entity.Details.Any (x => x.OrderDetail.SalesOrder.IsDelivered)) {
				return RedirectToAction ("Index");
			}

			foreach (var detail in entity.Details) {
				
				if (detail.Quantity > GetRemainQuantityBySalesOrderDetail(detail.OrderDetail)) {
					detail.Quantity = GetRemainQuantityBySalesOrderDetail(detail.OrderDetail);
					inconsistencies = true;
				}
			}

			if (inconsistencies) {
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}

				return RedirectToAction ("Index");
			}

			try {
				entity.Serial = (from x in DeliveryOrder.Queryable
						 where x.Store.Id == entity.Store.Id
						 select x.Serial).Max () + 1;
			} catch {
				entity.Serial = 1;
			}

			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Details) {
					if (item.Quantity == 0)
						item.DeleteAndFlush ();
				}
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			var Orders = entity.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ();

			foreach (var Order in Orders) {

				bool isSalesOrderDeliveredCompletly = !Order.Details.Any (x => GetRemainQuantityBySalesOrderDetail(x) > 0.0m);

				if (isSalesOrderDeliveredCompletly) {
					Order.IsDelivered = true;
					using (var scope = new TransactionScope ()) {
						Order.UpdateAndFlush ();
					}
				}
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = DeliveryOrder.TryFind (id);

			if (entity == null || entity.IsCancelled || entity.IsDelivered) {
				return RedirectToAction ("Index");
			}

			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			var Orders = entity.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ();

			foreach (var Order in Orders) {

				bool NotYetCompleted = Order.Details.Any (x => GetRemainQuantityBySalesOrderDetail(x) > 0.0m);

				if (NotYetCompleted) {

					Order.IsDelivered = false;
					using (var scope = new TransactionScope ()) {

						Order.UpdateAndFlush ();
					}
				}
			}

			return RedirectToAction ("Index");
		}

		public ViewResult Delivery (int id)
		{

			var entity = DeliveryOrder.TryFind (id);

			return View (entity);
		}

		[HttpPost]
		public ActionResult Delivered (int id)
		{

			DeliveryOrder order = DeliveryOrder.Find (id);

			if (!(order.IsCancelled || order.IsDelivered || order.Details.Any (x => x.OrderDetail.SalesOrder.IsCancelled))) {
				using (var scope = new TransactionScope ()) {
					order.IsDelivered = true;
					order.Updater = CurrentUser.Employee;
					order.ModificationTime = DateTime.Now;

					foreach (var item in order.Details) {
						Helpers.InventoryHelpers.ChangeNotification (TransactionType.SalesOrder, order.Id,
							order.ModificationTime, WebConfig.PointOfSale.Warehouse, null, item.Product, -item.Quantity);
					}

					order.UpdateAndFlush ();
				}
			}

			return RedirectToAction ("Index");
		}

		public ViewResult PendantDeliveries ()
		{

			Search<OrderDetailDeliveries> search = new Search<OrderDetailDeliveries> ();
			search.Limit = WebConfig.PageSize;
			List<SalesOrderDetail> items = new List<SalesOrderDetail> ();
			var query = (from x in DeliveryOrderDetail.Queryable
				    where !x.DeliveryOrder.IsCancelled && x.OrderDetail != null
				    select x.OrderDetail.SalesOrder).Distinct().ToList();

			foreach (var list in query) {
				items.AddRange (list.Details);
			}

			search.Results = (from x in items.OrderByDescending (x => x.SalesOrder.Id).Skip (search.Offset).Take (search.Limit).ToList ()
					  select new OrderDetailDeliveries {
						  Id = x.Id,
						  SalesOrderId = x.SalesOrder.Id,
						  Date = x.SalesOrder.Date,
						  ProductName = x.ProductName,
						  Quantity = x.Quantity,
						  QuantityRemain = GetRemainQuantityBySalesOrderDetail (x),
						  QuantityDelivered = x.Quantity - GetRemainQuantityBySalesOrderDetail (x),
						  UnitOfMeasure = x.Product.UnitOfMeasurement.Id,
						  Details = DeliveryOrderDetail.Queryable.Where (y => y.OrderDetail == x && !y.DeliveryOrder.IsCancelled).ToList ()
					  }).ToList ();
			search.Total = search.Results.Count ();
			return View (search);
		}

		[HttpPost]
		public ActionResult PendantDeliveries (Search<OrderDetailDeliveries> search)
		{
			search.Limit = WebConfig.PageSize;
			int salesorder_id;
			List<SalesOrderDetail> items = new List<SalesOrderDetail> ();

			var query = (from x in DeliveryOrderDetail.Queryable
				     where !x.DeliveryOrder.IsCancelled && x.OrderDetail != null
				     select x.OrderDetail.SalesOrder).Distinct().ToList();

			if (int.TryParse (search.Pattern, out salesorder_id)) {
				query = query.Where (x => x.Id == salesorder_id).ToList();
			} else if (!string.IsNullOrEmpty(search.Pattern)) {
				query = query.Where (x => x.Customer.Name.ToLower().Contains(search.Pattern.ToLower())).ToList();
			}

			foreach (var list in query.ToList()) {
				items.AddRange (list.Details);
			}

			search.Results = (from x in items.OrderByDescending (x => x.Id).Skip (search.Offset).Take (search.Limit).ToList ()
					  select new OrderDetailDeliveries {
						  Id = x.Id,
						  SalesOrderId = x.SalesOrder.Id,
						  Date = x.SalesOrder.Date,
						  ProductName = x.ProductName,
						  Quantity = x.Quantity,
						  QuantityDelivered = x.Quantity - GetRemainQuantityBySalesOrderDetail (x),
						  QuantityRemain = GetRemainQuantityBySalesOrderDetail (x),
						  UnitOfMeasure = x.Product.UnitOfMeasurement.Id,
						  Details = DeliveryOrderDetail.Queryable.Where (y => y.OrderDetail == x && !y.DeliveryOrder.IsCancelled).ToList ()
					  }).ToList ();

			search.Total = search.Results.Count ();

			return PartialView ("_PendantDeliveries", search);
		}

		decimal GetRemainQuantityBySalesOrderDetail (SalesOrderDetail detail){

			var deliveredItems = DeliveryOrderDetail.Queryable.Where (x => x.OrderDetail == detail && x.DeliveryOrder.IsCompleted
						&& !x.DeliveryOrder.IsCancelled).Select(x=> x.Quantity).ToList ();

			return deliveredItems.Count () > 0 ? detail.Quantity - deliveredItems.Sum (): detail.Quantity;
		}
	}
}
