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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
	public class DeliveryOrdersController : Controller
	{
		public ViewResult Index ()
		{
			if (Configuration.Store == null) {
				return View ("InvalidStore");
			}

			var search = SearchDeliveryOrders (new Search<DeliveryOrder> {
				Limit = Configuration.PageSize
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

			if (int.TryParse (pattern, out id) && id > 0) {
				query = from x in DeliveryOrder.Queryable
						where !(!x.IsCompleted && x.IsCancelled) && (
							x.Id == id || x.Serial == id)
				        orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Id descending
				        select x;
			} else if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in DeliveryOrder.Queryable
                      	where !(!x.IsCompleted && x.IsCancelled)
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Id descending
                      	select x;
			} else {
				query = from x in DeliveryOrder.Queryable
						where !(!x.IsCompleted && x.IsCancelled) && x.Customer.Name.Contains (pattern)
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Id descending
                      	select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = DeliveryOrder.Find (id);
			return View (item);
		}

		public ViewResult Print (int id)
		{
			var item = DeliveryOrder.Find (id);

			return View (item);
		}

		[HttpPost]
		public ActionResult New (DeliveryOrder item)
		{
			item.Customer = Customer.TryFind (Configuration.DefaultCustomer);

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			item.Store = Configuration.Store;

			try {
				item.Serial = (from x in DeliveryOrder.Queryable
								where x.Store.Id == item.Store.Id
								select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			item.Date = DateTime.Now;
			item.CreationTime = DateTime.Now;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = item.CreationTime;
			item.Updater = item.Creator;

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Edit", new { id = item.Id });
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
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
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
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = id, value = entity.ShipTo.ToString () });
		}

		[HttpPost]
		public ActionResult AddItem (int id, int product)
		{
			var p = Product.Find (product);
			var entity = DeliveryOrder.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var item = new DeliveryOrderDetail {
				DeliveryOrder = DeliveryOrder.Find (id),
				Product = p,
				ProductCode = p.Code,
				ProductName = p.Name,
				Quantity = 1,
			};

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return Json (new { id = item.Id });
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

			using (var scope = new TransactionScope()) {
				foreach (var x in sales_order.Details) {
					var item = new DeliveryOrderDetail {
						DeliveryOrder = entity,
						Product = x.Product,
						OrderDetail = x,
						ProductCode = x.ProductCode,
						ProductName = x.ProductName,
						Quantity = x.Quantity
					};

					item.Create ();
					count++;
				}
			}
			
			if (count == 0) {
				Response.StatusCode = 400;
				return Content (Resources.InvoiceableItemsNotFound);
			}

			return Json (new { id = id, value = string.Empty, itemsChanged = count });
		}

		[HttpPost]
		public ActionResult RemoveItem (int id)
		{
			var entity = DeliveryOrderDetail.Find (id);

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope()) {
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

		public ActionResult Totals (int id)
		{
			var order = DeliveryOrder.Find (id);
			return PartialView ("_Totals", order);
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

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = entity.Id, value = entity.ProductName });
		}

		[HttpPost]
		public ActionResult SetItemProductCode (int id, string value)
		{
			var entity = DeliveryOrderDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (val.Length > 0) {
				entity.ProductCode = val;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = entity.Id, value = entity.ProductCode });
		}

		[HttpPost]
		public ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = DeliveryOrderDetail.Find (id);

			if (entity.DeliveryOrder.IsCompleted || entity.DeliveryOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value > 0) {
				entity.Quantity = value;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
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

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			foreach (var detail in entity.Details) {
				if (detail.Quantity <= 0) {
					return RedirectToAction ("Edit", new { id = entity.Id });
				}
			}

			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = DeliveryOrder.TryFind (id);

			if (entity == null || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}
	}
}
