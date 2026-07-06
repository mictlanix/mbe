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
//using Castle.Core.Internal;
using Castle.ActiveRecord.Testing;
using NHibernate.Engine;
using Newtonsoft.Json;
using NHibernate.Linq;

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
							&& (x.Creator == CurrentUser.Employee ||
							x.Updater == CurrentUser.Employee)
						);

			if (!string.IsNullOrEmpty (pattern)) {
				query = DeliveryOrder.Queryable;
				if (int.TryParse (pattern, out id) && id > 0) {
					query = query.Where (x => x.Id == id || x.Serial == id
					|| x.Details.Any (y => y.OrderDetail.SalesOrder.Id == id));
				} else {
					query = query.Where (x => x.Customer.Name.Contains (pattern));

					if (pattern.Contains (Resources.WilcardStringPatternForSearch)) {
						query = DeliveryOrder.Queryable;
					}

				}
			}

			//query = query.OrderBy (x => x.IsCompleted || x.IsCancelled ? 1 : 0)
			//			.OrderByDescending (x => x.Id);

			query = query.OrderByDescending (x => x.Id);

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

		public ActionResult Print (int id, bool? only_current_warehouse)
		{
			var item = this.GetDeliveryViewModel (id);
			item.DeliveryOrder.Details = only_current_warehouse.HasValue && only_current_warehouse.Value ?
				item.DeliveryOrder.Details.Where (x => x.OrderDetail.Warehouse == WebConfig.PointOfSale.Warehouse).ToList () :
				item.DeliveryOrder.Details;

			if (!item.DeliveryOrder.IsCompleted) {
				return RedirectToAction ("Edit", new { id });
			}

			if (WebConfig.DeliveryOrdersUseMiniPrinter) {
				var pickup_addresses = MBEQueryable.IQStores.Select (x => x.Address).ToList ();
				//if (pickup_addresses.Contains (item.DeliveryOrder.ShipTo)) {
				if (item.DeliveryOrder.IsPickedUpInStore) {
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

			if (!entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (!entity.IsPaid && entity.Terms == PaymentTerms.Immediate
				&& WebConfig.DeliveryOrderRequiresPaidOrCreditSalesOrder) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotPaidYet);
			}


			if (entity.DeliveryMode == DeliveryMode.PickUp) {
				Response.StatusCode = 400;
				return Content (Resources.PickUp);
			}

			//if (entity.Date.AddDays(WebConfig.MaxDaysToDeliver) < DateTime.Now.Date && !CurrentUser.IsAdministrator) {
			//	Response.StatusCode = 400;
			//	return Content (string.Format(Resources.ExpiredPromiseDateForDelivery, WebConfig.MaxDaysToDeliver));
			//}

			DeliveryOrder item = CreateFromSalesOrder (entity.Id);
			item.IsPickedUpInStore = MBEQueryable.IQStoresAddress.ToList ().Contains (item.ShipTo);
			item.ShipTo = entity.ShipTo;


			if (item.Details.Count () <= 0) {
				Response.StatusCode = 400;
				return Content (Resources.AlreadyFullyDelivered);
			}

			var details = item.Details;
			item.Details = null;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
				item.Refresh ();
				details.ForEach (x => { x.DeliveryOrder = item; x.CreateAndFlush (); });
				entity.DeliveryMode = DeliveryMode.PartialDeliveries;
				entity.UpdateAndFlush ();
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

			//if (item.Creator != CurrentUser.Employee || item.Updater != CurrentUser.Employee) {
			//	return RedirectToAction ("Index");
			//}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			//foreach (var detail in item.Details) {
			//	//decimal remainingQuantityForDetail = GetRemainQuantityBySalesOrderDetail (detail.OrderDetail);
			//	decimal remainingQuantityForDetail = detail.OrderDetail.GetDeliverableQuantity();
			//	if (detail.Quantity > remainingQuantityForDetail) {
			//		detail.Quantity = remainingQuantityForDetail;
			//	}
			//}

			//item.Details = item.Details.OrderByDescending(x => x.DeliveryOrder.Priority).ToList ();

			item.Details.ForEach (x => {
				//var remaining = GetRemainQuantityBySalesOrderDetail (x.OrderDetail);
				var remaining = x.OrderDetail.GetDeliverableQuantity ();
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
						select new { value = x.Id, text = x.Name + " - " + (!string.IsNullOrEmpty (x.Mobile) ? x.Mobile.ToString () : x.Email.ToString ()) };
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
				entity.Date = DateTime.Now;
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
				entity.IsPickedUpInStore = MBEQueryable.IQStoresAddress.ToList ().Contains (item);
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

			if (entity.Date.AddDays (WebConfig.MaxDaysToDeliverStockables) < DateTime.Now.Date) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.ExpiredPromiseDateForDelivery, WebConfig.MaxDaysToDeliverStockables));
			}

			var Details = sales_order.Details.Where (x => !entity.Details.Any (y => y.OrderDetail == x)).ToList ();

			if (!(Details.Count () > 0)) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			using (var scope = new TransactionScope ()) {
				foreach (var x in Details.Where (y => y.GetDeliverableQuantity () > 0.0m)) {

					var item = new DeliveryOrderDetail {
						DeliveryOrder = entity,
						Product = x.Product,
						OrderDetail = x,
						ProductCode = x.ProductCode,
						ProductName = x.ProductName,
						Quantity = x.GetDeliverableQuantity ()
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

			if (value >= 0 && value <= entity.OrderDetail.GetDeliverableQuantity ()) {
				entity.Quantity = value;
			} else {
				entity.Quantity = entity.OrderDetail.GetDeliverableQuantity ();
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

			//if (entity.Customer.Id == WebConfig.DefaultCustomer
			//	&& !MBEQueryable.IQStores.Select (x => x.Address).ToList().Contains (entity.ShipTo)) {
			//	Response.StatusCode = 400;
			//	return Content(Resources.ForbiddenDeliveryToDefaultCustomer);
			//}

			if (entity.Date.Date < DateTime.Now.Date) {
				Response.StatusCode = 400;
				return Content (Resources.InvalidDate);
			}

			if ((entity.Contact == null
				|| entity.ShipTo == null) && !entity.IsPickedUpInStore) {
				Response.StatusCode = 400;
				return Content (Resources.Message_NotContactOrShipTo);
			}

			if (!entity.IsPickedUpInStore && entity.ShipTo.Link == null) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.AttribValueMissing, Resources.AddressLinkURL));
			}

			if (entity.Customer.HasExpiredCredits ()) {
				Response.StatusCode = 400;
				//return Content (Resources.ExpiredCredits);
				return Content (Resources.ExpiredCredits);
			}

			var promise_dates = entity.Details.Select (x => x.OrderDetail.SalesOrder).ToList ();



			if (WebConfig.DeliveryOrderRequiresPaidOrCreditSalesOrder) {
				var orders = entity.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ();
				if (orders.Any (x => !x.IsPaid && x.Terms == PaymentTerms.Immediate)) {
					Response.StatusCode = 400;
					return Content (Resources.SalesOrderNotPaidYet);
				}
			}

			if (WebConfig.MaxDaysToDeliverStockables > 0) {
				var stockables_details = entity.Details.Where (x => x.Product.IsStockable).Select (x => x.OrderDetail.SalesOrder).Distinct ();
				if (stockables_details.Any (x => (DateTime.Now - x.Date).TotalDays > WebConfig.MaxDaysToDeliverStockables) && !CurrentUser.IsAdministrator) {
					Response.StatusCode = 400;
					return Content (Resources.PromiseDateOutOfRange);
				}
			}

			if (WebConfig.MaxDaysToDeliverNoStockables > 0) {
				var no_stockables_details = entity.Details.Where (x => !x.Product.IsStockable).Select (x => x.OrderDetail.SalesOrder).Distinct ();
				if (no_stockables_details.Any (x => (DateTime.Now - x.Date).TotalDays > WebConfig.MaxDaysToDeliverNoStockables) && !CurrentUser.IsAdministrator) {
					Response.StatusCode = 400;
					return Content (Resources.PromiseDateOutOfRange);
				}
			}

			if (entity.Details.Count () == 0) {
				Response.StatusCode = 400;
				return Content (Resources.Empty);
			}

			foreach (var detail in entity.Details) {
				var remainingQuantity = detail.OrderDetail.GetDeliverableQuantity ();
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

				bool isSalesOrderDeliveredCompletly = Order.Details.Any (x => x.GetDeliverableQuantity () > 0.0m);

				if (!isSalesOrderDeliveredCompletly) {
					Order.IsDelivered = true;

					if (Order.Terms == PaymentTerms.NetD) {
						Order.DueDate = Order.ComputeDueDate ();
					}

					using (var scope = new TransactionScope ()) {
						Order.UpdateAndFlush ();
					}
				}
			}

			if (Request.IsAjaxRequest ()) {
				return Json (new { id = entity.Id, done = true });
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

		//private decimal GetRemainQuantityBySalesOrderDetail (SalesOrderDetail detail)
		//{
		//	var delivered = DeliveryOrderDetail.Queryable
		//		.Where (x => x.OrderDetail == detail && x.DeliveryOrder.IsCompleted
		//			&& !x.DeliveryOrder.IsCancelled).ToArray ();

		//	var refund = CustomerRefundDetail.Queryable
		//		.Where (x => x.SalesOrderDetail == detail && !x.Refund.IsCancelled
		//			&& x.Refund.IsCompleted).ToArray ();

		//	return detail.Quantity - (delivered.Sum(y => (decimal?) y.Quantity)??0) - (refund.Sum (y => (decimal?) y.Quantity) ?? 0);
		//}

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

					var orders = item.Details.Select (x => x.OrderDetail.SalesOrder).Distinct ()
						.Where (x => x.Terms == PaymentTerms.NetD
							&& !x.Details.Any (y => y.GetDeliverableQuantity () > 0.0m));

					foreach (var order in orders) {
						order.DueDate = order.ComputeDueDate ();
						order.UpdateAndFlush ();
					}
				} else {
					var incidence = new Incidence {
						Reference = item.Id,
						SourceType = SourceType.DeliveryOrder,
						Updater = CurrentUser.Employee,
						PreviousState = string.Format (Resources.LogMessage, Resources.DeliveryOrder, value),
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

		//[HttpPost]
		//public ActionResult Delivered (int id, bool approval, string value)
		//{

		//	var item = DeliveryOrder.TryFind (id);
		//	if (item == null) {
		//		Response.StatusCode = 400;
		//		return Content (Resources.ItemNotFound);
		//	}

		//	//if (item.Date < DateTime.Now) {
		//	//	Response.StatusCode = 400;
		//	//	return Content (string.Format (Resources.Validation_DateGreaterThan, Resources.Date, DateTime.Now.Date));
		//	//}

		//	if (item.IsConfirmed) {
		//		Response.StatusCode = 400;
		//		return Content (Resources.ItemAlreadyCompletedOrCancelled);
		//	}


		//	using (var scope = new TransactionScope ()) {
		//		if (approval) {
		//			item.IsConfirmed = true;
		//			item.IsDelivered = true;
		//			item.UpdateAndFlush ();
		//		} else {

		//			var json = JsonConvert.SerializeObject (item.GetSerializable());
		//			var incidence = new Incidence {
		//				Comment = value,
		//				ModificationTime = DateTime.Now,
		//				PreviousState = json,
		//				Reference = item.Id,
		//				SourceType = SourceType.DeliveryOrder,
		//				Updater	= CurrentUser.Employee
		//			};

		//			incidence.CreateAndFlush ();

		//			item.IsConfirmed = false;
		//			item.IsCompleted = false;
		//			item.UpdateAndFlush ();
		//		}
		//	}



		//	if (Request.IsAjaxRequest ()) {
		//		return Json (new { id = id, done = true });
		//	}

		//	return RedirectToAction ("DeliveryOrdersApproval");
		//}

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
				return PartialView ("_DeliveryOrdersApproval", search);
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

			query = from x in DeliveryOrder.Queryable
					where !x.IsCancelled && x.IsCompleted && !x.IsConfirmed && x.ShipTo != null
					select x;

			if (!string.IsNullOrEmpty (search.Pattern)) {
				if (int.TryParse (pattern, out id) && id > 0) {
					query = DeliveryOrder.Queryable.Where (y => y.Id == id || y.Serial == id);
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
				Quantity = x.GetDeliverableQuantity () //GetRemainQuantityBySalesOrderDetail (x)
			});

			item.Details = details.Where (x => x.Quantity > 0).ToList ();
			return item;
		}

		public JsonResult PriorityLevels ()
		{
			var priorities = Enum.GetValues (typeof (Priority))
				.Cast<Priority> ()
				.Select (x => new { value = (int) x, text = x.GetDisplayName () })
				.ToList ();

			return Json (priorities, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetPriorityLevel (int id, string value)
		{
			bool success;
			Priority val = Priority.Low;
			var entity = DeliveryOrder.Find (id);

			//if (entity.IsCompleted || entity.IsCancelled) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.ItemAlreadyCompletedOrCancelled);
			//}

			success = Enum.TryParse (value.Trim (), out val);

			if (success) {

				entity.Priority = val;
				entity.Updater = CurrentUser.Employee;
				//entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.Priority
			});
		}
	}
}
