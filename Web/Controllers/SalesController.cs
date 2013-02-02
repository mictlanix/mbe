// 
// SalesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class SalesController : Controller
    {
        //
        // GET: /Sales/

        public ViewResult Index ()
		{
			var item = Configuration.PointOfSale;
			
			if (item == null) {
				return View ("InvalidPointOfSale");
			}
			
			var qry = from x in SalesOrder.Queryable
                      where x.Store.Id == item.Store.Id &&
							!x.IsCancelled &&
							!x.IsCompleted
					  orderby x.Id descending 
                      select x;

			return View (qry.ToList ());
		}

        public ViewResult PrintPromissoryNote (int id)
		{
			var item = SalesOrder.Find (id);

			item.Details.ToList ();

            return View("_PromissoryNoteTicket", item);
        }

        // GET: /Sales/PrintOrder/

        public ViewResult PrintOrder (int id)
		{
			var item = SalesOrder.Find (id);

			item.Details.ToList ();
			item.Payments.ToList ();
			
            return View(item.IsCompleted || item.IsCredit ? "_SalesTicket" : "_SalesNote", item);
        }

        // GET: /Sales/Details/

        public ViewResult Details (int id)
		{
			var item = SalesOrder.Find (id);

			item.Details.ToList ();

			return View (item);
        }

        //
        // GET: /Sales/New

        public ViewResult New ()
		{
			if (Configuration.PointOfSale == null) {
                return View ("InvalidPointOfSale");
            }

			var employee = SecurityHelpers.GetUser (User.Identity.Name).Employee;

            return View (new SalesOrder {
				CustomerId = 1, 
				Customer = Customer.Find(1),
				SalesPersonId = employee.Id, 
				SalesPerson = employee
			});
        }

        [HttpPost]
		public ActionResult New (SalesOrder item)
		{
			item.PointOfSale = Configuration.PointOfSale;

			if (item.PointOfSale == null) {
				return View ("InvalidPointOfSale");
			}

			item.Customer = Customer.TryFind (item.CustomerId);
			item.SalesPerson = Employee.TryFind (item.SalesPersonId);

			if (item.Customer == null || item.SalesPerson == null) {
				return View (item);
			}

			// Store and Serial
			item.Store = item.PointOfSale.Store;
			try {
				item.Serial = (from x in SalesOrder.Queryable
	            			   where x.Store.Id == item.Store.Id
	                      	   select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}
			
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.CreationTime = DateTime.Now;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;
			item.Customer = Customer.Find (item.CustomerId);
			item.SalesPerson = Employee.Find (item.SalesPersonId);
			item.Date = item.CreationTime;
			item.DueDate = item.IsCredit ? item.Date.AddDays (item.Customer.CreditDays) : item.Date;

			if (item.ShipToId > 0) {
				item.ShipTo = new Address ();
				item.ShipTo.Copy (Address.TryFind (item.ShipToId));
			}

			using (var scope = new TransactionScope()) {
				if (item.ShipTo != null) {
					item.ShipTo.Create();
				}
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ActionResult Edit (int id)
		{
			var item = SalesOrder.Find (id);
				
			if (Request.IsAjaxRequest ())
				return PartialView ("_MasterEditView", item);
			else {
				item.Details.ToList ();
				return View (item);
			}
        }
		
		public ActionResult DiscardChanges (int id)
		{
			return PartialView ("_MasterView", SalesOrder.TryFind (id));
		}

        [HttpPost]
		public ActionResult Edit (SalesOrder item)
		{
			var customer = Customer.Find (item.CustomerId);
			var salesperson = Employee.Find (item.SalesPersonId);

			if (customer == null || salesperson == null) {
				return View (item);
			}
			
			var entity = SalesOrder.Find (item.Id);
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;
			entity.Customer = customer;
			entity.SalesPerson = salesperson;
			entity.IsCredit = item.IsCredit;
			entity.DueDate = item.IsCredit ? entity.Date.AddDays (customer.CreditDays) : entity.Date;
			
			if (item.ShipToId > 0) {
				entity.ShipTo = entity.ShipTo ?? new Address ();
				entity.ShipTo.Copy (Address.TryFind (item.ShipToId));
			}

			using (var scope = new TransactionScope()) {
				if (item.ShipToId > 0) {
					entity.ShipTo.Save ();
				} else if (entity.ShipTo != null) {
					entity.ShipTo.Delete ();
					entity.ShipTo = null;
				}

				entity.UpdateAndFlush ();
			}

			return PartialView ("_MasterView", entity);
        }

        [HttpPost]
        public JsonResult AddDetail (int order, int product)
        {
			var s = SalesOrder.TryFind (order);
			var p = Product.TryFind (product);
			int pl = s.Customer.PriceList.Id;
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x.Price).SingleOrDefault();
			
            var item = new SalesOrderDetail {
				SalesOrder = s,
				Product = p,
				Warehouse = s.PointOfSale.Warehouse,
                ProductCode = p.Code,
                ProductName = p.Name,
                TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
                Quantity = 1,
				Price = price,
				ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate(),
				Currency = Configuration.DefaultCurrency
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();
            }

            return Json(new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity (int id, decimal quantity)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find (id);

            if (quantity > 0) {
                detail.Quantity = quantity;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount (int id, string value)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);
            bool success;
            decimal discount;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1) {
                detail.Discount = discount;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, discount = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDeliveryOrder (int id, int value)
        {
            var detail = SalesOrderDetail.Find (id);

            detail.IsDelivery = (value != 0);

			using (var scope = new TransactionScope ()) {
				detail.UpdateAndFlush ();
			}

            return Json(new { id = id, value = detail.IsDelivery });
        }

        public ActionResult GetTotals (int id)
		{
			var item = SalesOrder.Find (id);
				
			item.Details.ToList ();

			return PartialView ("_Totals", item);
        }

		public ActionResult GetDetail (int id)
		{
			return PartialView ("_DetailEditView", SalesOrderDetail.Find (id));
        }

        [HttpPost]
        public JsonResult RemoveDetail (int id)
        {
            var item = SalesOrderDetail.Find(id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult Confirm (int id)
        {
            var item = SalesOrder.Find (id);

			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
            item.IsCompleted = true;

            if (item.IsCredit) {
                item.IsPaid = true;

				if (item.ShipTo == null) {
					item.IsDelivered = true;
				}
            }

			using (var scope = new TransactionScope ()) {
				var warehouse = item.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				foreach (var x in item.Details) {
					x.Warehouse = warehouse;
					x.Update ();
					
					InventoryHelpers.ChangeNotification(TransactionType.SalesOrder, item.Id,
					                                    dt, warehouse, x.Product, -x.Quantity);
				}

				item.UpdateAndFlush ();
			}

            return RedirectToAction ("New");
        }

        [HttpPost]
        public ActionResult Cancel (int id)
        {
            var item = SalesOrder.Find (id);
			
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
            item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

            return RedirectToAction ("New");
        }
		
		public ViewResult Deliveries ()
		{
			var qry = from x in SalesOrder.Queryable
				where x.ShipTo != null &&
					!x.IsCancelled && !x.IsDelivered &&
					x.IsCompleted && x.IsPaid
					orderby x.Id descending 
					select x;
			
			return View (qry.ToList ());
		}

		public ViewResult Delivery (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}
		
		public ViewResult PrintDelivery (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}
		
		[HttpPost]
		public ActionResult ConfirmDelivery (int id)
		{
			var item = SalesOrder.Find (id);
			
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
			item.IsDelivered = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}
			
			return RedirectToAction ("Deliveries");
		}

        public JsonResult GetSuggestions (int order, string pattern)
		{
			var sales_order = SalesOrder.Find (order);
			int pl = sales_order.Customer.PriceList.Id;

			var qry = from x in ProductPrice.Queryable
					  where x.List.Id == pl && (
							x.Product.Name.Contains (pattern) ||
							x.Product.Code.Contains (pattern) ||
							x.Product.Model.Contains (pattern) ||
							x.Product.SKU.Contains (pattern) ||
							x.Product.Brand.Contains (pattern))
					  orderby x.Product.Name
					  select new { 
							id = x.Product.Id,
							name = x.Product.Name,
							code = x.Product.Code,
							model = x.Product.Model,
							sku = x.Product.SKU,
							url = Url.Content(x.Product.Photo),
							price = x.Price
					  };

			return Json (qry.Take(15), JsonRequestBehavior.AllowGet);
		}
    }
}
