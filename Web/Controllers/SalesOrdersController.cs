// 
// SalesOrdersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
	public class SalesOrdersController : Controller
	{
        public ViewResult Index ()
		{
			var item = Configuration.PointOfSale;
			
			if (item == null) {
				return View ("InvalidPointOfSale");
			}
			
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}
			
			var search = SearchSalesOrders (new Search<SalesOrder> {
				Limit = Configuration.PageSize
			});
			
			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<SalesOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchSalesOrders (search);
			}
			
			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}
		
		Search<SalesOrder> SearchSalesOrders (Search<SalesOrder> search)
		{
			IQueryable<SalesOrder> query;
			var item = Configuration.PointOfSale;

			if (string.IsNullOrEmpty (search.Pattern)) {
				query = from x in SalesOrder.Queryable
					  	where x.Store.Id == item.Store.Id
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					  	select x;
			} else {
				query = from x in SalesOrder.Queryable
					  	where x.Store.Id == item.Store.Id && (
							x.Customer.Name.Contains (search.Pattern) ||
							x.SalesPerson.Name.Contains (search.Pattern))
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					  	select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			
			return search;
		}

		public ViewResult View (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}
		
		public ViewResult Print (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}

        [HttpPost]
		public ActionResult New ()
		{
			var item = new SalesOrder ();

			item.PointOfSale = Configuration.PointOfSale;
			
			if (item.PointOfSale == null) {
				return View ("InvalidPointOfSale");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
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

			item.Customer = Customer.TryFind (Configuration.DefaultCustomer);
			item.SalesPerson = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Date = item.CreationTime;
			item.PromiseDate = item.Date;
			item.Terms = PaymentTerms.Immediate;
			item.DueDate = item.Date;
			item.Currency = Configuration.DefaultCurrency;
			item.ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate ();
			
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.CreationTime = DateTime.Now;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ActionResult Edit (int id)
		{
			var item = SalesOrder.Find (id);

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			return View (item);
        }

		public JsonResult Contacts (int id)
		{
			var item = SalesOrder.TryFind (id);
			var qry = from x in item.Customer.Contacts
					  select new { value = x.Id, text = x.ToString () };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}
		
		public JsonResult Addresses (int id)
		{
			var item = SalesOrder.TryFind (id);
			var qry = from x in item.Customer.Addresses
					  select new { value = x.Id, text = x.ToString () };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}
		
		public JsonResult Terms ()
		{
			var qry = from x in Enum.GetValues (typeof(PaymentTerms)).Cast<PaymentTerms> ()
					  select new {
						value = (int)x,
						text = x.GetDisplayName ()
					  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public JsonResult SetCustomer (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Customer.TryFind (value);

			if (item != null) {
				entity.Customer = item;
				entity.Contact = null;
				entity.ShipTo = null;

				if (entity.Terms == PaymentTerms.NetD && !entity.Customer.HasCredit) {
					entity.Terms = PaymentTerms.Immediate;
				}

				switch (entity.Terms) {
				case PaymentTerms.Immediate:
					entity.DueDate = entity.Date;
					break;
				case PaymentTerms.NetD:
					entity.DueDate = entity.Date.AddDays (entity.Customer.CreditDays);
					break;
				}

				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer),
				terms = entity.Terms,
				termsText = entity.Terms.GetDisplayName (),
				dueDate = entity.FormattedValueFor (x => x.DueDate)
			});
		}

		[HttpPost]
		public JsonResult SetSalesPerson (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Employee.TryFind (value);

			if (item != null) {
				entity.SalesPerson = item;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new { id = id, value = entity.SalesPerson.ToString () });
		}

		[HttpPost]
		public JsonResult SetContact (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Contact.TryFind (value);

			if (item != null) {
				entity.Contact = item;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new { id = id, value = entity.Contact.ToString () });
		}

		[HttpPost]
		public JsonResult SetShipTo (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Address.TryFind (value);

			if (item != null) {
				entity.ShipTo = item;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new { id = id, value = entity.ShipTo.ToString () });
		}

		[HttpPost]
		public JsonResult SetPromiseDate (int id, DateTime? value)
		{
			var entity = SalesOrder.Find (id);

			if (value != null) {
				entity.PromiseDate = value.Value;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new { id = id, value = entity.FormattedValueFor (x => x.PromiseDate) });
		}

		[HttpPost]
		public ActionResult SetCurrency (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			CurrencyCode val;
			bool success;

			success = Enum.TryParse<CurrencyCode> (value.Trim (), out val);

			if (success) {
				decimal rate = CashHelpers.GetTodayExchangeRate (val);

				if (rate == 0m) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				entity.Currency = val;
				entity.ExchangeRate = rate;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					foreach (var item in entity.Details) {
						item.Currency = val;
						item.ExchangeRate = rate;
						item.Update ();
					}

					entity.UpdateAndFlush ();
				}
			}

			return Json (new { 
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Currency),
				rate = entity.FormattedValueFor (x => x.ExchangeRate),
				itemsChanged = success
			});
		}
		
		[HttpPost]
		public ActionResult SetExchangeRate (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.Trim (), out val);

			if (success) {
				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				entity.ExchangeRate = val;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					foreach (var item in entity.Details) {
						item.ExchangeRate = val;
						item.Update ();
					}

					entity.UpdateAndFlush ();
				}
			}

			return Json (new { 
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.ExchangeRate),
				itemsChanged = success
			});
		}

		[HttpPost]
		public ActionResult SetTerms (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			PaymentTerms val;
			bool success;

			success = Enum.TryParse<PaymentTerms> (value.Trim (), out val);

			if (success) {
				if (val == PaymentTerms.NetD && !entity.Customer.HasCredit) {
					Response.StatusCode = 400;
					return Content (Resources.CreditLimitIsNotSet);
				}

				entity.Terms = val;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				switch (entity.Terms) {
				case PaymentTerms.Immediate:
					entity.DueDate = entity.Date;
					break;
				case PaymentTerms.NetD:
					entity.DueDate = entity.Date.AddDays (entity.Customer.CreditDays);
					break;
				}

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new {
				id = id,
				value = entity.Terms,
				dueDate = entity.FormattedValueFor (x => x.DueDate),
				totalsChanged = success
			});
		}

        [HttpPost]
        public JsonResult AddItem (int order, int product)
        {
			var entity = SalesOrder.TryFind (order);
			var p = Product.TryFind (product);
			int pl = entity.Customer.PriceList.Id;
			var cost = (from x in ProductPrice.Queryable
			            where x.Product.Id == product && x.List.Id == 0
			            select x).SingleOrDefault();
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x).SingleOrDefault();
			
			if (cost == null) {
				cost = new ProductPrice {
					Value = decimal.Zero,
					Currency = Configuration.DefaultCurrency
				};
			}

			if (price == null) {
				price = new ProductPrice {
					Value = decimal.MaxValue,
					Currency = Configuration.DefaultCurrency
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
                Quantity = 1,
				Cost = cost.Value,
				Price = price.Value,
				Currency = entity.Currency,
				ExchangeRate = entity.ExchangeRate
            };
			
			if (cost.Currency != entity.Currency) {
				item.Cost = cost.Value * CashHelpers.GetTodayExchangeRate (cost.Currency, entity.Currency);
			}

			if (price.Currency != entity.Currency) {
				item.Price = price.Value * CashHelpers.GetTodayExchangeRate (price.Currency, entity.Currency);
			}

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();
            }

            return Json(new { id = item.Id });
		}
		
		[HttpPost]
		public JsonResult RemoveItem (int id)
		{
			var item = SalesOrderDetail.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}
		
		public ActionResult Item (int id)
		{
			var item = SalesOrderDetail.Find (id);
			return PartialView ("_ItemEditorView", item);
		}

		public ActionResult Items (int id)
		{
			var item = SalesOrder.Find (id);
			return PartialView ("_Items", item.Details);
		}

		public ActionResult Totals (int id)
		{
			var item = SalesOrder.Find (id);
			return PartialView ("_Totals", item);
		}

		[HttpPost]
		public JsonResult SetItemQuantity (int id, decimal value)
		{
			var entity = SalesOrderDetail.Find (id);

			if (value > 0) {
				entity.Quantity = value;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}
			
			return Json (new { 
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Quantity),
				total = entity.FormattedValueFor (x => x.Total), 
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}
		
		[HttpPost]
		public JsonResult SetItemPrice (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.Trim (),
			                            System.Globalization.NumberStyles.Currency,
			                            null, out val);

			if (success && val >= 0) {
				entity.Price = val;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}
			
			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Price),
				total = entity.FormattedValueFor (x => x.Total), 
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

        [HttpPost]
        public JsonResult SetItemDiscount (int id, string value)
        {
            var entity = SalesOrderDetail.Find (id);
            bool success;
            decimal val;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out val);
            val /= 100m;

            if (success && val >= 0 && val <= 1) {
                entity.Discount = val;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
            }
			
			return Json (new { 
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Discount),
				total = entity.FormattedValueFor (x => x.Total), 
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public JsonResult SetItemTaxRate (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);

			// TODO: VAT value range validation
			if (success) {
				entity.TaxRate = val;

				using (var scope = new TransactionScope()) {
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
        public ActionResult Confirm (int id)
        {
            var item = SalesOrder.Find (id);

			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
            item.IsCompleted = true;

            if (item.IsCredit) {
                item.IsPaid = true;
			}
			
			if (item.ShipTo == null) {
				item.IsDelivered = true;
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

            return RedirectToAction ("Index");
        }

        [HttpPost]
        public ActionResult Cancel (int id)
        {
            var entity = SalesOrder.Find (id);
			
			if (entity.IsCancelled || entity.IsPaid) {
				return RedirectToAction ("Index");
			}

			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;
            entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
        }

		// TODO: Rename param: order -> id
		public JsonResult GetSuggestions (int order, string pattern)
		{
			int pl = SalesOrder.Queryable.Where (x => x.Id == order)
							.Select (x => x.Customer.PriceList.Id).Single ();
			var query = from x in ProductPrice.Queryable
						where x.List.Id == pl && (
							x.Product.Name.Contains (pattern) ||
							x.Product.Code.Contains (pattern) ||
							x.Product.Model.Contains (pattern) ||
							x.Product.SKU.Contains (pattern) ||
							x.Product.Brand.Contains (pattern))
						orderby x.Product.Name
						select new {
							x.Product.Id, x.Product.Name, x.Product.Code,
							x.Product.Model, x.Product.SKU, x.Product.Photo, Price = x.Value
						};
			var items = from x in query.Take (15).ToList ()
						select new {
							id = x.Id, name = x.Name, code = x.Code, model = x.Model,
							sku = x.SKU, url = Url.Content (x.Photo), price = x.Price
						};

			return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		}
    }
}
