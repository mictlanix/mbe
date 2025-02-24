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
using Castle.Core.Internal;
using Castle.ActiveRecord.Testing;
using NHibernate.Engine;
using Newtonsoft.Json;

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

			query = DeliveryOrder.Queryable.Where (x => !x.IsCancelled
							&& x.Creator == CurrentUser.Employee
						)
						.OrderBy (x => x.IsCompleted || x.IsCancelled ? 1 : 0)
						.OrderByDescending (x => x.Id);

			if (int.TryParse (pattern, out id) && id > 0) {
				query = query.Where (y => y.Id == id || y.Serial == id);
			} else {
				query = query.Where (x => x.Customer.Name.Contains (pattern));
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = DeliveryOrder.Find (id);
			//if (!item.IsCompleted) {
			//	return RedirectToAction("Edit", new { id = id });
			//}
			return View (item);
		}

		public ActionResult Print (int id)
		{
			var item = this.GetDeliveryViewModel (id);

			if (!item.DeliveryOrder.IsCompleted) {
				return RedirectToAction ("Edit", new { id });
			}

			if (WebConfig.DeliveryOrdersUseMiniPrinter) {
				var pickup_addresses = MBEQueryable.IQStores.Select (x => x.Address).ToList ();
				if (pickup_addresses.Contains (item.DeliveryOrder.ShipTo)) {
					return PdfTicketView (WebConfig.PickUpTicket, item);
				}
				return PdfTicketView (WebConfig.DeliveryOrderTicket, item);
			} else {
				return PdfView (WebConfig.DeliveryOrderTemplate, item);
			}

		}

		public ActionResult PrintFormat (int id)
		{
			var item = DeliveryOrder.Find (id);
			return PdfView (WebConfig.DeliveryOrderTemplate, item);
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

			if (entity.Creator != CurrentUser.Employee
				&& entity.Updater != CurrentUser.Employee
				&& entity.SalesPerson != CurrentUser.Employee) {
				Response.StatusCode = 400;
				return Content (Resources.CreatorDoesntMatchWithCurrentUser);
			}

			if (!entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (!entity.IsPaid && entity.Terms == PaymentTerms.Immediate) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotPaidYet);
			}

			DeliveryOrder item = CreateFromSalesOrder (entity.Id);


			if (item.Details.Count <= 0) {
				Response.StatusCode = 400;
				return Content (Resources.AlreadyFullyDelivered);
			}

			var details = item.Details;
			item.Details = null;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
				item.Refresh ();
				details.ForEach (x => { x.DeliveryOrder = item; x.CreateAndFlush (); });
			}


			//if (Request.IsAjaxRequest ()) {
			//	return Json (new { id = item.Id });
			//}

			return Json (new { url = Url.Action ("Edit", new { id = item.Id }) });
		}

		public ActionResult Edit (int id)
		{
			var item = DeliveryOrder.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new { id = item.Id });
			}

			if (item.Creator != CurrentUser.Employee || item.Updater != CurrentUser.Employee) {
				return RedirectToAction ("Index");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			foreach (var detail in item.Details) {
				decimal remainingQuantityForDetail = GetRemainQuantityBySalesOrderDetail (detail.OrderDetail);
				if (detail.Quantity > remainingQuantityForDetail) {
					detail.Quantity = remainingQuantityForDetail;
				}
			}

			item.Details.ForEach (x => {
				var remaining = GetRemainQuantityBySalesOrderDetail (x.OrderDetail);
				x.Quantity = x.Quantity > remaining ? remaining : x.Quantity;

			});

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
				item.Details.ForEach (x => x.UpdateAndFlush ());
			}

			return View (item);
		}

		public JsonResult Addresses (int id)
		{
			var item = DeliveryOrder.TryFind (id);
			var query = from x in item.Customer.Addresses
				    select new { value = x.Id, text = !string.IsNullOrEmpty (x.Nickname) ? x.Nickname : x.ToString () };

			var items = query.ToList ();
			var pickup = WebConfig.Store.Address;

			items.Add (new { value = pickup.Id, text = Resources.PickUp });

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Contacts (int id)
		{
			var item = DeliveryOrder.TryFind (id);
			var query = from x in item.Customer.Contacts
				    select new { value = x.Id, text = x.Name + " - " + (!x.Mobile.IsNullOrEmpty () ? x.Mobile.ToString () : x.Email.ToString ()) };
			var items = query.ToList ();
			return Json (items, JsonRequestBehavior.AllowGet);
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
				if (value.Value.Add (WebConfig.MinSpanHoursForDeliveries) <= DateTime.Now) {
					Response.StatusCode = 400;
					return Content (string.Format (Resources.MinimumHoursSpanForDeliveries, WebConfig.MinSpanHoursForDeliveries));
				}
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
		public ActionResult SetContact (int id, int value)
		{
			var entity = DeliveryOrder.Find (id);
			var item = Contact.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Contact = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
				return Json (new {
					id = id, value = entity.Contact.ToString ()
				});
			} else {
				Response.StatusCode = 400;
				return Content (Resources.None);
			}

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

			//if (sales_order.IsDelivered) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.Delivered);
			//}

			if (sales_order.Customer != entity.Customer) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MismatchCustomers, Resources.SalesOrder, Resources.DeliveryOrder));
			}

			var Details = sales_order.Details.Where (x => !entity.Details.Any (y => y.OrderDetail == x)).ToList ();

			if (!(Details.Count () > 0)) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			using (var scope = new TransactionScope ()) {
				foreach (var x in Details.Where (y => GetRemainQuantityBySalesOrderDetail (y) > 0.0m)) {

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

		public ActionResult Deliverable (int id)
		{
			var item = DeliveryOrder.Find (id);
			return PartialView ("_buttons", item);
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

			if (value >= 0 && value <= GetRemainQuantityBySalesOrderDetail (entity.OrderDetail)) {
				entity.Quantity = value;
			} else {
				entity.Quantity = GetRemainQuantityBySalesOrderDetail (entity.OrderDetail);
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
			bool restoredQuantities = false;

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			if (!DeliveryHelpers.IsReadyToDeliver (entity)) {
				Response.StatusCode = 400;
				return Content (Resources.Message_NotContactOrShipTo);
			}

			if (entity.Details.Count () == 0) {
				Response.StatusCode = 400;
				return Content (Resources.Empty);
			}

			foreach (var detail in entity.Details) {
				var remainingQuantity = GetRemainQuantityBySalesOrderDetail (detail.OrderDetail);
				if (detail.Quantity > remainingQuantity) {
					detail.Quantity = remainingQuantity;
					restoredQuantities = true;
				}
			}

			if (restoredQuantities) {
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
					entity.Details.Where (x => x.Quantity <= 0).ForEach (y => y.DeleteAndFlush ());
				}

				return RedirectToAction ("Edit", new { id = entity.Id });
			}

			entity.Serial = entity.Serial > 0 ? entity.Serial :
				(DeliveryOrder.Queryable.Where (x => x.Store == entity.Store).Select (y => (int?) y.Serial).Max () ?? 0) + 1;


			using (var scope = new TransactionScope ()) {
				foreach (var item in entity.Details) {
					if (item.Quantity == 0)
						item.DeleteAndFlush ();
				}
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			if (!WebConfig.DeliveryOrderApprovalRequired) {
				entity.IsConfirmed = true;
				entity.IsDelivered = true;
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			var Orders = entity.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ();

			foreach (var Order in Orders) {

				bool isSalesOrderDeliveredCompletly = Order.Details.Any (x => GetRemainQuantityBySalesOrderDetail (x) > 0.0m);

				if (!isSalesOrderDeliveredCompletly) {
					Order.IsDelivered = true;
					using (var scope = new TransactionScope ()) {
						Order.UpdateAndFlush ();
					}
				}
			}

			return RedirectToAction ("View", new { id = entity.Id });
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = DeliveryOrder.TryFind (id);

			if (entity == null || entity.IsCancelled || entity.IsCompleted) {
				return RedirectToAction ("Index");
			}

			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		public ViewResult Delivery (int id)
		{

			var entity = DeliveryOrder.TryFind (id);

			return View (entity);
		}
		//public ActionResult Delivered (int id)
		//{

		//	DeliveryOrder order = DeliveryOrder.Find (id);

		//	if (!(order.IsCancelled || order.IsDelivered)) {
		//		using (var scope = new TransactionScope ()) {
		//			order.IsDelivered = true;
		//			order.Updater = CurrentUser.Employee;
		//			order.ModificationTime = DateTime.Now;
		//			order.UpdateAndFlush ();
		//		}

		//		return RedirectToAction ("Print", new { id = id });
		//	}

		//	return RedirectToAction ("Index");
		//}

		//public ViewResult PendantDeliveries ()
		//{

		//	Search<RemainingOrderDetail> search = new Search<RemainingOrderDetail> ();
		//	search.Limit = WebConfig.PageSize;
		//	List<SalesOrderDetail> items = new List<SalesOrderDetail> ();
		//	var query = (from x in DeliveryOrderDetail.Queryable
		//		     where !x.DeliveryOrder.IsCancelled && x.OrderDetail != null
		//		     select x.OrderDetail.SalesOrder).Distinct ().ToList ();

		//	foreach (var list in query) {
		//		items.AddRange (list.Details);
		//	}

		//	search.Results = (from x in items.OrderByDescending (x => x.SalesOrder.Id).Skip (search.Offset).Take (search.Limit).ToList ()
		//			  select new RemainingOrderDetail {
		//				  Id = x.Id,
		//				  SalesOrderId = x.SalesOrder.Id,
		//				  Date = x.SalesOrder.Date,
		//				  ProductName = x.ProductName,
		//				  Quantity = x.Quantity,
		//				  QuantityRemain = GetRemainQuantityBySalesOrderDetail (x),
		//				  QuantityDelivered = x.Quantity - GetRemainQuantityBySalesOrderDetail (x),
		//				  UnitOfMeasure = x.Product.UnitOfMeasurement.Id,
		//				  Details = DeliveryOrderDetail.Queryable.Where (y => y.OrderDetail == x && !y.DeliveryOrder.IsCancelled).ToList ()
		//			  }).ToList ();
		//	search.Total = search.Results.Count ();
		//	return View (search);
		//}

		//[HttpPost]
		//public ActionResult PendantDeliveries (Search<RemainingOrderDetail> search)
		//{
		//	search.Limit = WebConfig.PageSize;
		//	int salesorder_id;
		//	List<SalesOrderDetail> items = new List<SalesOrderDetail> ();

		//	var query = (from x in DeliveryOrderDetail.Queryable
		//		     where !x.DeliveryOrder.IsCancelled && x.OrderDetail != null
		//		     select x.OrderDetail.SalesOrder).Distinct ().ToList ();

		//	if (int.TryParse (search.Pattern, out salesorder_id)) {
		//		query = query.Where (x => x.Id == salesorder_id).ToList ();
		//	} else if (!string.IsNullOrEmpty (search.Pattern)) {
		//		query = query.Where (x => x.Customer.Name.ToLower ().Contains (search.Pattern.ToLower ())).ToList ();
		//	}

		//	foreach (var list in query.ToList ()) {
		//		items.AddRange (list.Details);
		//	}

		//	search.Results = (from x in items.OrderByDescending (x => x.Id).Skip (search.Offset).Take (search.Limit).ToList ()
		//			  select new RemainingOrderDetail {
		//				  Id = x.Id,
		//				  SalesOrderId = x.SalesOrder.Id,
		//				  Date = x.SalesOrder.Date,
		//				  ProductName = x.ProductName,
		//				  Quantity = x.Quantity,
		//				  QuantityDelivered = x.Quantity - GetRemainQuantityBySalesOrderDetail (x),
		//				  QuantityRemain = GetRemainQuantityBySalesOrderDetail (x),
		//				  UnitOfMeasure = x.Product.UnitOfMeasurement.Id,
		//				  Details = DeliveryOrderDetail.Queryable.Where (y => y.OrderDetail == x && !y.DeliveryOrder.IsCancelled).ToList ()
		//			  }).Take (15).ToList ();

		//	search.Total = search.Results.Count ();

		//	return PartialView ("_PendantDeliveries", search);
		//}

		private decimal GetRemainQuantityBySalesOrderDetail (SalesOrderDetail detail)
		{

			var deliveredQuantity = DeliveryOrderDetail.Queryable
				.Where (x => x.OrderDetail == detail && x.DeliveryOrder.IsCompleted
					&& !x.DeliveryOrder.IsCancelled).Sum (x => (decimal?) x.Quantity) ?? 0;
			var refundQuantity = CustomerRefundDetail.Queryable
				.Where (x => x.SalesOrderDetail == detail && !x.Refund.IsCancelled
					&& x.Refund.IsCompleted).Sum (y => (decimal?) y.Quantity) ?? 0;
			return detail.Quantity - deliveredQuantity - refundQuantity;
		}

		private DeliveryViewModel GetDeliveryViewModel (int id)
		{
			var item = new DeliveryViewModel ();
			item.DeliveryOrder = DeliveryOrder.Find (id);
			item.SalesOrders = item.DeliveryOrder.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ().ToList ();
			item.PaymentsOnDelivery = item.SalesOrders.SelectMany (x => x.Payments.Where (y => y.Payment.CashSession == null)).ToList ();
			return item;
		}

		[HttpPost]
		public ActionResult DeliveryRequestConfirmation (int id, bool confirmation, string value)
		{
			if (string.IsNullOrEmpty (value)) {
				Response.StatusCode = 400;
				return Content (Resources.Empty);
			}

			var item = DeliveryOrder.TryFind (id);

			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			if (item.IsDelivered) {
				Response.StatusCode = 400;
				return Content (Resources.Delivered);
			}

			if (item.IsConfirmed == true) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			//if (item.Date < DateTime.Now) {
			//	Response.StatusCode = 400;
			//	return Content (string.Format(Resources.Validation_DateGreaterThan, Resources.Date, DateTime.Now.Date));
			//}

			using (var scope = new TransactionScope ()) {
				if (confirmation) {
					item.IsConfirmed = true;
					item.IsDelivered = true;
					item.UpdateAndFlush ();
				} else {
					var incidence = new Incidence {
						Reference = item.Id,
						SourceType = SourceType.DeliveryOrder,
						Updater = CurrentUser.Employee,
						PreviousState = JsonConvert.SerializeObject (item.GetSerializable ()),
						Comment = value,
						ModificationTime = DateTime.Now,
					};

					incidence.CreateAndFlush ();

					item.IsConfirmed = false;
					item.UpdateAndFlush ();

				}
			}


			if (Request.IsAjaxRequest ()) {
				return Json (new { id = id, done = true });
			}

			return RedirectToAction ("DeliveryOrdersApproval");
		}

		[HttpPost]
		public ActionResult Delivered (int id, bool approval, string value)
		{

			var item = DeliveryOrder.TryFind (id);
			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			//if (item.Date < DateTime.Now) {
			//	Response.StatusCode = 400;
			//	return Content (string.Format (Resources.Validation_DateGreaterThan, Resources.Date, DateTime.Now.Date));
			//}

			if (item.IsConfirmed) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			using (var scope = new TransactionScope ()) {
				if (approval) {
					item.IsConfirmed = true;
					item.IsDelivered = true;
					item.UpdateAndFlush ();
				} else {
					var incidence = new Incidence {
						Comment = value,
						ModificationTime = DateTime.Now,
						PreviousState = JsonConvert.SerializeObject (item.GetSerializable()),
						Reference = item.Id,
						SourceType = SourceType.DeliveryOrder,
						Updater	= CurrentUser.Employee.GetSerializable()
					};
					incidence.CreateAndFlush ();

					item.IsConfirmed = false;
					item.IsCompleted = false;
					item.UpdateAndFlush ();
				}
			}



			if (Request.IsAjaxRequest ()) {
				return Json (new { id = id, done = true });
			}

			return RedirectToAction ("DeliveryOrdersApproval");
		}

		public ViewResult DeliveryOrdersApproval ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			var search = SearchDeliveryOrdersForApproval (new Search<DeliveryOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult DeliveryOrdersApproval (Search<DeliveryOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchDeliveryOrdersForApproval (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		public ActionResult ViewDeliveryOrderApproval (int id)
		{

			var item = DeliveryOrder.TryFind (id);
			return View ("ViewDeliveryOrderApproval", item);
		}

		Search<DeliveryOrder> SearchDeliveryOrdersForApproval (Search<DeliveryOrder> search)
		{
			IQueryable<DeliveryOrder> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			query = DeliveryOrder.Queryable.Where (x => !x.IsCancelled
								     && x.IsCompleted
								     && !x.IsConfirmed
						);
			if (!string.IsNullOrEmpty (search.Pattern)) {
				if (int.TryParse (pattern, out id) && id > 0) {
					query = query.Where (y => y.Id == id || y.Serial == id);
				} else {
					query = query.Where (x => x.Customer.Name.Contains (pattern));
				}
			}

			query = query.OrderBy (x => x.IsCompleted || x.IsCancelled ? 1 : 0)
					.OrderByDescending (x => x.Id);

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		private DeliveryOrder CreateFromSalesOrder (int id)
		{
			var order = SalesOrder.TryFind (id);
			if (order == null || order.IsCancelled || !order.IsCompleted) { return null; }
			DeliveryOrder item = new DeliveryOrder ();
			item.Customer = Customer.TryFind (WebConfig.DefaultCustomer);


			item.Store = WebConfig.Store;

			item.Date = DateTime.Now.Add (WebConfig.MinSpanHoursForDeliveries);
			item.CreationTime = DateTime.Now;
			item.Creator = CurrentUser.Employee;
			item.Updater = item.Creator;
			item.Serial = 0;
			item.ModificationTime = item.CreationTime;
			item.Customer = order.Customer;
			item.ShipTo = order.Customer.Id == WebConfig.DefaultCustomer ? item.Store.Address : order.ShipTo;
			item.Comment = order.Comment;
			item.Store = order.Store;

			var details = order.Details.Select (x => new DeliveryOrderDetail {
				OrderDetail = x,
				Product = x.Product,
				ProductCode = x.ProductCode,
				ProductName = x.ProductName,
				Quantity = GetRemainQuantityBySalesOrderDetail (x)
			});

			item.Details = details.Where (x => x.Quantity > 0).ToList ();
			return item;
		}
	}
}
