// 
// SalesOrdersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013-2020 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Web.WebPages;
using NHibernate;
using System.Collections.Generic;
using Castle.Core.Internal;
using System.Text.RegularExpressions;
using Mictlanix.BE.Web.Services;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	[SessionState (System.Web.SessionState.SessionStateBehavior.Required)]
	public class SalesOrdersController : CustomController {
		public ViewResult Index ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			if (WebConfig.PointOfSale == null) {
				return View ("InvalidPointOfSale");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchSalesOrders (new Search<SalesOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<SalesOrder> search)
		{

			var attribs = search.GetType ().Attributes;

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
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			var user = CurrentUser.Employee;
			IQueryable<SalesOrder> query = MBEQueryable.IQSalesOrders.Where (x => x.Creator == user
							|| x.Updater == user || x.SalesPerson == user);
			query = query.Where (x => !x.IsCancelled);

			if (int.TryParse (pattern, out int id) && id > 0) {
				query = MBEQueryable.IQSalesOrders.Where (x => x.Id == id || x.Serial == id);
			} else if (!string.IsNullOrEmpty (pattern)) {

				query = from x in MBEQueryable.IQSalesOrders
						select x;

				if (!(pattern.Contains (Resources.WilcardStringPatternForSearch) && CurrentUser.IsAdministrator)) {
					query = from x in query
							where (x.Customer.Name.Contains (pattern) ||
								x.SalesPerson.Nickname.Contains (pattern)) && x.Store == item
							select x;
				}
			}

			query = query.OrderByDescending (x => x.Id).OrderBy (y => y.IsCompleted || y.IsCancelled ? 1 : 0);
			//query = query.OrderByDescending (x => x.Serial).OrderBy (y => y.IsCompleted || y.IsCancelled ? 1 : 0);

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
			var model = SalesOrder.Find (id);

			return View (model);
		}

		public virtual ActionResult Pdf (int id)
		{
			var model = SalesOrder.Find (id);
			return PdfView ("Print", model);
		}

		[HttpPost]
		public ActionResult New ()
		{
			var dt = DateTime.Now;
			var item = new SalesOrder ();

			item.PointOfSale = WebConfig.PointOfSale;

			if (item.PointOfSale == null) {
				return View ("InvalidPointOfSale");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			//var orders = SalesOrder.Queryable.Where (x => !x.IsPaid
			//				&& x.Creator == CurrentUser.Employee
			//				&& x.Terms == PaymentTerms.Immediate
			//				&& !x.IsCancelled
			//				&& x.IsCompleted
			//				).ToList();
			//if (orders.Count() > WebConfig.MaxSalesOrdersCompletedAndPayless) {
			//	return View ("MaxCountSalesOrders");
			//}

			// Store and Serial
			item.Store = item.PointOfSale.Store;

			item.Customer = Customer.TryFind (WebConfig.DefaultCustomer);
			item.SalesPerson = CurrentUser.Employee;
			item.Date = dt;
			item.PromiseDate = dt.AddDays (WebConfig.MaxDaysToDeliverStockables);
			item.Currency = WebConfig.DefaultCurrency;
			item.ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate ();
			item.Terms = item.Customer.HasCredit ? PaymentTerms.NetD : PaymentTerms.Immediate;
			item.DueDate = item.ComputeDueDate ();

			item.Creator = CurrentUser.Employee;
			item.CreationTime = dt;
			item.Updater = item.Creator;
			item.ModificationTime = dt;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Edit", new {
				id = item.Id
			});
		}

		[HttpPost]
		public ActionResult CreateFromSalesQuote (int id)
		{
			var dt = DateTime.Now;
			var item = new SalesOrder ();
			var salesquote = SalesQuote.Find (id);

			item.PointOfSale = WebConfig.PointOfSale;

			if (item.PointOfSale == null) {
				return View ("InvalidPointOfSale");
			}

			if (salesquote.HasExpired) {
				Response.StatusCode = 400;
				return Content (Resources.ExpirationDate);
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			if (salesquote.IsCancelled || !salesquote.IsCompleted) {
				return RedirectToAction ("Index", "Quotations");
			}

			// Store and Serial
			item.Store = item.PointOfSale.Store;

			//try {
			//	item.Serial = (from x in SalesOrder.Queryable
			//		       where x.Store.Id == item.Store.Id
			//		       select x.Serial).Max () + 1;
			//} catch {
			//	item.Serial = 1;
			//}

			//item.Serial = SalesOrder.Queryable.Where (x => x.Store == WebConfig.Store).Select (y => (int?)y.Serial).Max () + 1 ?? 1;

			item.Customer = salesquote.Customer;
			item.SalesPerson = salesquote.SalesPerson;
			item.Date = dt;
			item.PromiseDate = dt;
			item.Terms = salesquote.Terms;
			item.DueDate = item.ComputeDueDate ();
			item.Currency = salesquote.Currency;
			item.ExchangeRate = salesquote.ExchangeRate;
			item.Contact = salesquote.Contact;
			item.Comment = salesquote.Comment;
			item.ShipTo = salesquote.ShipTo;
			item.CustomerShipTo = salesquote.ShipTo == null ? "" : salesquote.ShipTo.ToString ();
			item.SalesQuote = salesquote;

			item.Creator = CurrentUser.Employee;
			item.CreationTime = dt;
			item.Updater = item.Creator;
			item.ModificationTime = dt;

			var details = salesquote.Details.Select (x => new SalesOrderDetail {
				Currency = x.Currency,
				ExchangeRate = x.ExchangeRate,
				IsTaxIncluded = x.IsTaxIncluded,
				Price = x.Price + x.PriceIncrement,
				Product = x.Product,
				ProductCode = x.ProductCode,
				ProductName = x.ProductName,
				Quantity = x.Quantity,
				SalesOrder = item,
				TaxRate = x.TaxRate,
				Comment = x.Comment,
				DiscountRate = x.DiscountRate,
				Warehouse = WebConfig.PointOfSale.Warehouse
			}).ToList ();


			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
				details.ForEach (x => x.CreateAndFlush ());
			}

			if (Request.IsAjaxRequest ()) {
				return Json (new { id = item.Id });
			}

			return RedirectToAction ("Edit", new {
				id = item.Id
			});
		}

		public ActionResult Edit (int id)
		{
			var item = SalesOrder.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new {
					id = item.Id
				});
			}

			foreach (var detail in item.Details) {
				detail.Errors = GetValidationMessages (detail);
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			return View (item);
		}

		public JsonResult Contacts (int id)
		{
			var item = SalesOrder.TryFind (id);
			var query = from x in item.Customer.Contacts
						select new {
							value = x.Id,
							text = x.ToString ()
						};

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult Recipients (int id)
		{
			var item = SalesOrder.TryFind (id);
			var query = from x in item.Customer.Taxpayers
						select new {
							value = x.Id,
							text = x.ToString ()
						};
			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult WarehouseStock (int id)
		{
			string sql = @"SELECT w.warehouse_id value,
					CONCAT(w.name, ' - (' ,ROUND(SUM(lst.quantity), 2), ' ',IFNULL(s.name, '** Definir **') , ')' ) text
					FROM product p
					LEFT JOIN lot_serial_tracking lst ON lst.product = p.product_id
					JOIN warehouse w ON lst.warehouse = w.warehouse_id
					JOIN sat_unit_of_measurement s ON s.sat_unit_of_measurement_id = p.unit_of_measurement
					WHERE p.product_id = :product AND w.disabled = 0
					GROUP BY lst.warehouse
					HAVING SUM(lst.quantity) >= 0";

			var items = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("value", NHibernateUtil.Int32);
				query.AddScalar ("text", NHibernateUtil.String);
				query.SetInt32 ("product", id);
				return query.DynamicList ();
			}, null);

			var qry = items.Select (x => new { value = x.value, text = x.text });
			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);

		}

		public JsonResult Addresses (int id)
		{
			var item = SalesOrder.TryFind (id);
			var query = from x in item.Customer.Addresses
						where x.IsDisabled == false
						select new {
							value = x.Id,
							text = x.ToString ()
						};

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult Terms ()
		{
			var query = from x in Enum.GetValues (typeof (PaymentTerms)).Cast<PaymentTerms> ()
						select new {
							value = (int) x,
							text = x.GetDisplayName ()
						};

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult PriorityLevels (int id)
		{
			var item = SalesOrder.TryFind (id);

			var priorities = Enum.GetValues (typeof (Priority))
				.Cast<Priority> ()
				.Select (x => new { value = (int) x, text = x.GetDisplayName () })
				.ToList ();

			return Json (priorities, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
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
				entity.Terms = entity.Customer.HasCredit && entity.Customer.Id != WebConfig.DefaultCustomer ? PaymentTerms.NetD : PaymentTerms.Immediate;
				entity.DueDate = entity.ComputeDueDate ();
				entity.SalesPerson = CurrentUser.Employee;

				//if (item.SalesPerson == null) {
				//	entity.SalesPerson = CurrentUser.Employee;
				//} else {
				//	entity.SalesPerson = item.SalesPerson;
				//}

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
		public ActionResult SetCustomerName (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.CustomerName = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = value });
		}

		public ActionResult GetCustomerName (int id)
		{
			return PartialView ("_CustomerName", SalesOrder.Find (id));
		}

		[HttpPost]
		public ActionResult SetSalesPerson (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Employee.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (!item.IsSalesPerson) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.InvalidEntity, Resources.SalesPerson));
			}

			if (item != null) {
				entity.SalesPerson = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.SalesPerson.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetContact (int id, int value)
		{
			var entity = SalesOrder.Find (id);
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
			}

			return Json (new {
				id = id,
				value = entity.Contact.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetShipTo (int id, int value)
		{
			var entity = SalesOrder.Find (id);
			var item = Address.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.ShipTo = item;
				entity.CustomerShipTo = item.ToString ();
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.ShipTo.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = SalesOrder.Find (id);
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
		public ActionResult SetRecipient (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();
			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var item = entity.Customer.Taxpayers.Single (x => x.Id == val);
			entity.Recipient = item.Id;
			entity.RecipientName = item.Name;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = item.Name
			});
		}

		[HttpPost]
		public ActionResult SetPromiseDate (int id, DateTime? value)
		{
			var entity = SalesOrder.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				entity.PromiseDate = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.PromiseDate)
			});
		}

		[HttpPost]
		public ActionResult SetCurrency (int id, string value)
		{
			var entity = SalesOrder.Find (id);
			CurrencyCode val;
			bool success;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse<CurrencyCode> (value.Trim (), out val);

			if (success) {
				decimal rate = CashHelpers.GetTodayExchangeRate (val);

				if (rate == 0m) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				entity.Currency = val;
				entity.ExchangeRate = rate;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (), out val);

			if (success) {
				if (entity.Currency == WebConfig.BaseCurrency) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidBaseExchangeRate);
				}

				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				entity.ExchangeRate = val;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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
			bool success;
			PaymentTerms val;
			var entity = SalesOrder.Find (id);
			var dt = DateTime.Now;
			var customer = entity.Customer;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse (value.Trim (), out val);

			if (success) {
				if (val == PaymentTerms.NetD) {

					if (entity.Customer.Id == WebConfig.DefaultCustomer) {
						Response.StatusCode = 400;
						return Content (Resources.CustomerNotFound);
					}

					if (!entity.Customer.HasCredit) {
						Response.StatusCode = 400;
						return Content (Resources.CreditLimitIsNotSet);
					}

					if (customer.HasExpiredCredits () || entity.IsOverCreditLimit ()) {
						Response.StatusCode = 400;
						return Content (Resources.CreditStatusNeedsToBeVerified);
					}
				}

				entity.Terms = val;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.DueDate = entity.ComputeDueDate ();

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

		[HttpPost]
		public ActionResult SetPriorityLevel (int id, string value)
		{
			bool success;
			Priority val = Priority.Low;
			var entity = SalesOrder.Find (id);

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


		[HttpPost]
		public ActionResult AddItem (int order, int product, int? warehouse_id)
		{
			var entity = SalesOrder.TryFind (order);
			var p = Product.TryFind (product);
			int pl = entity.Customer.PriceList.Id;
			var w = warehouse_id.HasValue ? Warehouse.TryFind (warehouse_id) : null;
			var cost = (from x in ProductPrice.Queryable
						where x.Product.Id == product && x.List.Id == 0
						select x).SingleOrDefault ();
			var price = (from x in ProductPrice.Queryable
						 where x.Product.Id == product && x.List.Id == pl
						 select x).SingleOrDefault ();
			var discount = (from x in CustomerDiscount.Queryable
							where x.Product.Id == product && x.Customer.Id == entity.Customer.Id
							select x.Discount).SingleOrDefault ();

			//if (p.StockRequired && p.IsStockable) {
			//	var stock = LotSerialTracking.Queryable.Where (x => x.Product == p && x.Warehouse == w).Sum (y => (decimal?) y.Quantity) ?? 0.0m;

			//	if (stock < p.MinimumOrderQuantity) {
			//		Response.StatusCode = 400;
			//		return Content (string.Format (Resources.NoStockEnough, stock));

			//	}
			//}


			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
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

			//SalesOrderDetail item2 = entity.Details.Where (x => x.Product == p && x.Warehouse == w && x.SalesOrder == entity).FirstOrDefault ();

			//if (item2 != null) {
			//	using (var scope = new TransactionScope ()) {
			//		item2.Quantity += 1;
			//		item2.UpdateAndFlush ();
			//	}
			//	return Json (new {
			//		id = item2.Id,
			//		updated = true
			//	});
			//}

			var item = new SalesOrderDetail {
				SalesOrder = entity,
				Product = p,
				//Warehouse = entity.PointOfSale.Warehouse,
				Warehouse = w,
				ProductCode = p.Code,
				ProductName = p.Name,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				Quantity = p.MinimumOrderQuantity,
				Cost = cost.Value,
				Price = price.Value,
				DiscountRate = discount,
				Currency = entity.Currency,
				ExchangeRate = entity.ExchangeRate,
				Comment = p.Comment
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
		public ActionResult RemoveItem (int id)
		{
			var entity = SalesOrderDetail.Find (id);

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		//[HttpPost]
		//public ActionResult DuplicateItem (int id)
		//{
		//	var entity = SalesOrderDetail.Find (id);

		//	if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
		//		Response.StatusCode = 400;
		//		return Content (Resources.ItemAlreadyCompletedOrCancelled);
		//	}

		//	var item = new SalesOrderDetail {
		//		ProductCode = entity.ProductCode,
		//		ProductName = entity.ProductName,
		//		Price = entity.Price,
		//		Warehouse = entity.Warehouse,
		//		Comment = entity.Comment,
		//		Cost = entity.Cost,
		//		Currency = entity.Currency,
		//		DiscountRate = entity.DiscountRate,
		//		ExchangeRate = entity.ExchangeRate,
		//		IsDelivery = entity.IsDelivery,
		//		IsTaxIncluded = entity.IsTaxIncluded,
		//		Product = entity.Product,
		//		Quantity = entity.Product.MinimumOrderQuantity,
		//		SalesOrder = entity.SalesOrder,
		//		TaxRate = entity.TaxRate
		//	};

		//	using (var scope = new TransactionScope ()) {
		//		item.SaveAndFlush ();
		//	}

		//	return Json (new {
		//		id = item.Id,
		//		result = true
		//	});
		//}

		public ActionResult Item (int id)
		{
			var entity = SalesOrderDetail.Find (id);
			entity.Errors = GetValidationMessages (entity);
			return PartialView ("_ItemEditorView", entity);
		}

		public ActionResult Items (int id)
		{
			var entity = SalesOrder.Find (id);
			foreach (var detail in entity.Details) {
				detail.Errors = GetValidationMessages (detail);
			}
			return PartialView ("_Items", entity.Details);
		}

		public ActionResult Totals (int id)
		{
			var entity = SalesOrder.Find (id);
			return PartialView ("_Totals", entity);
		}

		[HttpPost]
		public ActionResult SetItemProductName (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			string val = (value ?? string.Empty).Trim ();

			var validation = EvalDetailEditable (entity);


			if (!validation.Success) {
				Response.StatusCode = 400;
				return Content (string.Join (",", validation.Errors));
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
		public ActionResult SetItemComment (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			entity.Comment = string.IsNullOrWhiteSpace (value) ? null : value.Trim ();

			Result<SalesOrderDetail> validation = EvalDetailEditable (entity);

			if (!validation.Success) {
				Response.StatusCode = 400;
				return Content (string.Join (",", validation.Errors));
			}

			//if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.ItemAlreadyCompletedOrCancelled);
			//}


			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.Comment
			});
		}

		[HttpPost]
		public ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = SalesOrderDetail.Find (id);

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Quantity = value;

			var validation = EvalDetailEditable (entity).Bind (ValidateStock);

			if (value < entity.Product.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MinimumQuantityRequired, entity.Product.MinimumOrderQuantity));
			}


			//if (!validation.Success) {
			//	Response.StatusCode = 400;
			//	return Content (string.Join (",", validation.Errors));
			//}


			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			//return Json (new {
			//	id = entity.Id,
			//	value = entity.FormattedValueFor (x => x.Quantity),
			//	total = entity.FormattedValueFor (x => x.Total),
			//	total2 = entity.FormattedValueFor (x => x.TotalEx)
			//});

			return RedirectToAction ("Item", new { id = entity.Id });
		}

		[HttpPost]
		public ActionResult SetItemWarehouse (int id, int value)
		{
			var entity = SalesOrderDetail.Find (id);

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Warehouse = MBEQueryable.IQWarehouses.Single (x => x.Id == value);

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			//return Json (new {
			//	id = entity.Id,
			//	value = entity.FormattedValueFor (x => x.Quantity),
			//	total = entity.FormattedValueFor (x => x.Total),
			//	total2 = entity.FormattedValueFor (x => x.TotalEx)
			//});

			return RedirectToAction ("Item", new { id = entity.Id });
		}

		[HttpPost]
		public ActionResult SetItemPrice (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			var result = EvalDetailEditable (entity).Bind (ValidatePrice).Bind (ValidateStock);

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
							System.Globalization.NumberStyles.Currency,
							null, out val);

			if (success && entity.Price >= 0) {
				var price_in_list = ProductPrice.Queryable.Where (x => x.List == entity.SalesOrder.Customer.PriceList && x.Product == entity.Product).SingleOrDefault ();

				if (price_in_list != null) {
					var current_price = price_in_list.Value;

					if (price_in_list.Product.Currency != entity.Currency) {
						current_price = current_price * CashHelpers.GetTodayExchangeRate (price_in_list.Product.Currency, entity.Currency);
					}

					if (current_price > val) {
						Response.StatusCode = 400;
						return Content (Resources.Validation_WrongDiscount);
					}
				}

				entity.Price = val;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				discount_percentage = entity.FormattedValueFor (x => x.DiscountRate),
				discount_price = string.Format ("{0:C}", entity.Price * entity.DiscountRate),
				value = entity.FormattedValueFor (x => x.Price),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemDiscountPercentage (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val <= 1.0m && val >= 0.0m) {

				entity.DiscountRate = val;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.DiscountRate),
				discountPrice = string.Format ("{0:C}", entity.Price * entity.DiscountRate),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemDiscountPrice (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);

			if (success && val <= entity.Price && val >= 0 && entity.Price > 0) {
				entity.DiscountRate = val / entity.Price;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				discountRate = entity.FormattedValueFor (x => x.DiscountRate),
				value = string.Format ("{0:C}", entity.Price * entity.DiscountRate),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemTaxRate (int id, string value)
		{
			var entity = SalesOrderDetail.Find (id);
			bool success;
			decimal val;

			if (entity.SalesOrder.IsCompleted || entity.SalesOrder.IsCancelled) {
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
		public virtual ActionResult Confirm (int id)
		{
			var entity = SalesOrder.TryFind (id);
			var messages = new List<string> ();

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			if (WebConfig.DeliveryOrderRequiresPaidOrCreditSalesOrder && !CurrentUser.IsAdministrator) {
				if (entity.Customer.HasExpiredCredits ()) {
					//Response.StatusCode = 400;
					//return Content (Resources.CreditStatusNeedsToBeVerified);
					//messages.Add (Resources.CreditStatusNeedsToBeVerified);
				}
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsDelivered = false;
			entity.IsCompleted = true;

			entity.Serial = (SalesOrder.Queryable.Where (x => x.Store == WebConfig.Store).Max (x => (int?) x.Serial) + 1 ?? 1);

			foreach (var detail in entity.Details) {
				if (detail.Price == decimal.Zero) {
					return View ("ZeroPriceError", entity);
				}
				messages.AddRange (GetValidationMessages (detail));
			}

			if (messages.Count > 0) {
				return RedirectToAction ("Edit", new { id = entity.Id });
			}

			using (var scope = new TransactionScope ()) {
				var warehouse = entity.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				// TODO: y.warehouse comprobation shouldn't be necessary....

				entity.Details.Where (y => y.Product.IsStockable && y.Warehouse != null).ForEach (x => {
					x.Update ();
					InventoryHelpers.ChangeNotification (TransactionType.SalesOrder, entity.Id,
						dt, x.Warehouse, null, x.Product, -x.Quantity);
				});

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = SalesOrder.Find (id);
			var privilege = GetAccessPrivilege (SystemObjects.SalesOrders);

			if (entity.IsCancelled || entity.IsPaid) {
				return RedirectToAction ("Index");
			}

			if (!(entity.IsCompleted ? privilege.AllowDelete : privilege.AllowUpdate)) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		// TODO: Rename param: order -> id
		//public JsonResult GetSuggestions (int id, string pattern, bool AllWarehouses = false)
		//{
		//	int pl = SalesOrder.Queryable.Where (x => x.Id == id)
		//				.Select (x => x.Customer.PriceList.Id).Single ();
		//	var query = from x in ProductPrice.Queryable
		//		    where x.List.Id == pl && x.Product.IsSalable &&
		//			   !x.Product.IsDeactivated && (
		//			    x.Product.Name.Contains (pattern) ||
		//			    x.Product.Code.Contains (pattern) ||
		//			    x.Product.Model.Contains (pattern) ||
		//			    x.Product.SKU.Contains (pattern) ||
		//			    x.Product.Brand.Contains (pattern))
		//		    orderby x.Product.Name
		//		    select new {
		//			    x.Product.Id,
		//			    x.Product.Name,
		//			    x.Product.Code,
		//			    x.Product.Model,
		//			    x.Product.SKU,
		//			    x.Product.Photo,
		//			    Price = x.Value,
		//			    warehouse = WebConfig.PointOfSale.Warehouse.Name
		//		    };
		//	var items = from x in query.Take (15).ToList ()
		//		    select new {
		//			    id = x.Id,
		//			    name = x.Name,
		//			    code = x.Code,
		//			    model = x.Model ?? Resources.None,
		//			    sku = x.SKU ?? Resources.None,
		//			    url = Url.Photo (x.Photo),
		//			    price = x.Price,
		//			    quantity = LotSerialTracking.Queryable.Where (y => y.Product.Code == x.Code
		//						&& y.Warehouse == WebConfig.PointOfSale.Warehouse)
		//						.Sum (y => (decimal?) y.Quantity) ?? 0,
		//			    warehouse = x.warehouse ??Resources.None
		//		    };

		//	return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		//}

		public JsonResult GetSuggestions (int id, string pattern)
		{
			int pl = SalesOrder.Queryable.Where (x => x.Id == id)
						.Select (x => x.Customer.PriceList.Id).Single ();

			var warehouse = WebConfig.PointOfSale.Warehouse;
			string Pattern = "^\\d{13}$";
			string sql = "";
			var match = Regex.IsMatch (pattern, Pattern);

			var all_warehouses = pattern.EndsWith (Resources.WilcardStringPatternForSearch);
			pattern = pattern.TrimEnd (new char [] { '*' });

			string warehouse_filter = " AND ((p.stockable = TRUE AND w.warehouse_id = " + warehouse.Id + ") OR p.stockable = FALSE) ";
			string searchOn = @"p.name LIKE :pattern
						OR p.code LIKE :pattern
						OR p.sku LIKE :pattern
						OR p.brand LIKE :pattern
						OR p.model LIKE :pattern";
			if (match) {
				searchOn = " p.bar_code LIKE :pattern ";
			}

			if (all_warehouses) {
				warehouse_filter = "";
			}


			sql = @"SELECT		p.product_id		id,
						p.name			name,
						p.code			code,
						p.sku			sku,
						p.photo			url,
						p.model			model,
						lst.warehouse		warehouse_id,
						SUM(lst.quantity)	quantity,
						w.name			warehouse,
						pp.price		price,
						p.stockable		stockable
					FROM product p 
					LEFT JOIN product_price pp ON pp.product = p.product_id
					LEFT JOIN lot_serial_tracking lst ON p.product_id = lst.product
					LEFT JOIN warehouse w ON w.warehouse_id = lst.warehouse
					WHERE (
						SEARCH_FILTER
					      )
						AND pp.`list` = :pricelist
						AND p.deactivated = FALSE
						AND p.salable = TRUE
						AND (w.disabled = FALSE OR w.disabled IS NULL)
						WAREHOUSE_FILTER
					GROUP BY lst.warehouse, p.product_id
					ORDER BY p.product_id DESC
					LIMIT 15";

			sql = sql.Replace ("WAREHOUSE_FILTER", warehouse_filter);
			sql = sql.Replace ("SEARCH_FILTER", searchOn);

			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("name", NHibernateUtil.String);
				query.AddScalar ("code", NHibernateUtil.String);
				query.AddScalar ("sku", NHibernateUtil.String);
				query.AddScalar ("url", NHibernateUtil.String);
				query.AddScalar ("model", NHibernateUtil.String);
				query.AddScalar ("warehouse_id", NHibernateUtil.Int32);
				query.AddScalar ("quantity", NHibernateUtil.Decimal);
				query.AddScalar ("warehouse", NHibernateUtil.String);
				query.AddScalar ("price", NHibernateUtil.Decimal);
				query.AddScalar ("stockable", NHibernateUtil.Boolean);


				query.SetParameter ("pricelist", pl);
				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
						 select new {
							 id = x.id,
							 name = x.name,
							 code = x.code,
							 sku = x.sku,
							 model = x.model,
							 url = Url.Photo ((string) x.url),
							 warehouse_id = x.warehouse_id,
							 quantity = x.quantity,
							 warehouse = x.warehouse,
							 price = x.price,
							 stockable = x.stockable,
						 }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);
		}

		private int GetSalesOrdersPaylessCount (Employee e)
		{
			return SalesOrder.Queryable.Where (x => x.Creator == e && !x.IsPaid
			&& x.Terms == PaymentTerms.Immediate).Count ();
		}

		private Result<SalesOrder> EvalEditable (SalesOrder salesOrder)
		{

			if (salesOrder.IsCompleted || salesOrder.IsCancelled) {
				return Result.Failure<SalesOrder> (Resources.ItemAlreadyCompletedOrCancelled);
			}
			return salesOrder;
		}

		private Result<SalesOrderDetail> EvalDetailEditable (SalesOrderDetail detail)
		{
			var editable = EvalEditable (detail.SalesOrder);

			if (!editable.Success) {
				return Result.Failure<SalesOrderDetail> (editable.Errors);
			}

			return Result.Success (detail);

		}

		private Result<SalesOrderDetail> ValidateStock (SalesOrderDetail detail)
		{
			if (detail.Product.StockRequired && detail.Product.IsStockable) {

				if (detail.Warehouse == null) {
					return Result.Failure<SalesOrderDetail> (Resources.WarehouseToBeDefined);
				}

				var stock = LotSerialTracking.Queryable.
					Where (x => x.Warehouse == detail.Warehouse && x.Product == detail.Product).
					Sum (y => (decimal?) y.Quantity) ?? 0;
				var quantity = detail.Quantity + detail.SalesOrder.Details.
					Where (x => x.Product == detail.Product && x.Warehouse == detail.Warehouse && x != detail).
					Sum (y => (decimal?) y.Quantity) ?? 0;
				if (stock - quantity < 0) {
					return Result.Failure<SalesOrderDetail> (
						string.Format (Resources.NoStockEnough, detail.Product.Name,
						stock, detail.Product.UnitOfMeasurement.Name));
				}

			}

			return detail;

		}

		private Result<SalesOrderDetail> ValidatePrice (SalesOrderDetail detail)
		{

			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);

			//var price_list = entity.SalesQuote.Customer.PriceList;
			if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
				var minimal_price = detail.Product.GetMinimalPrice ();
				var maximum_price = detail.Product.GetMaximumPrice ();
				if (!detail.IsPriceInRange ()) {
					return Result.Failure<SalesOrderDetail> (
						string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
				}
			}

			return detail;
		}

		private List<string> GetValidationMessages (SalesOrderDetail detail)
		{
			var errors = new List<string> ();
			var stock = ValidateStock (detail);
			var price = ValidatePrice (detail);
			if (!stock.Success) {
				errors.AddRange (stock.Errors);
			}
			if (!price.Success) {
				errors.AddRange (price.Errors);
			}

			return errors;
		}
	}
}
