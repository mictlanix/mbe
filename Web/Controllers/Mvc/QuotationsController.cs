// 
// QuotationsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013-2018 Eddy Zavaleta, Mictlanix, and contributors.
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
using MimeKit;
using System.Collections.Generic;
using Mictlanix.BE.Web.Utils;
using NHibernate;
using System.Text.RegularExpressions;
using Lucene.Net.Search;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class QuotationsController : CustomController {

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

			var search = SearchSalesQuotes (new Search<SalesQuote> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<SalesQuote> search)
		{
			if (ModelState.IsValid) {
				search = SearchSalesQuotes (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		Search<SalesQuote> SearchSalesQuotes (Search<SalesQuote> search)
		{
			var item = WebConfig.Store;
			IQueryable<SalesQuote> query = from x in SalesQuote.Queryable
						       where x.Creator == CurrentUser.Employee || x.Updater == CurrentUser.Employee
						       select x;

			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id) && id > 0) {

				query = from x in SalesQuote.Queryable
					where (x.Id == id || x.Serial == id)
					select x;

			} else if (!string.IsNullOrEmpty (pattern)) {
				if (!pattern.Contains (Resources.WilcardStringPatternForSearch)) {
					query = from x in SalesQuote.Queryable
						where (
						    x.Customer.Name.Contains (pattern) ||
						    x.SalesPerson.Nickname.Contains (pattern) ||
						    x.Customer.Name.Contains (pattern)
						    )
						select x;
				} else {
					query = from x in SalesQuote.Queryable
						select x;
				}
			}

			query = from x in query
				orderby (x.IsCompleted || x.IsCancelled ? 1 : 0),
							       x.Date descending
				select x;

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult View (int id)
		{

			var item = SalesQuote.Find (id);

			if (item.IsCancelled == true || item.IsCompleted == true)

				return View (item);

			return RedirectToAction ("Edit", new { id = id });
		}

		public ViewResult Print (int id)
		{
			var model = SalesQuote.Find (id);
			return View (model);
		}

		public ActionResult Pdf (int id)
		{
			var model = SalesQuote.Find (id);

			if (model.IsCompleted == true || model.IsCancelled == true)
				return PdfView ("Print", model);

			return RedirectToAction ("Edit", new { id = id });
		}

		[HttpPost]
		public ActionResult New ()
		{
			var dt = DateTime.Now;
			var item = new SalesQuote ();

			item.Store = WebConfig.Store;

			if (item.Store == null) {
				return View ("InvalidStore");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			//Store and Serial
			try {
				item.Serial = (from x in SalesQuote.Queryable
					       where x.Store.Id == item.Store.Id
					       select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			item.Customer = Customer.TryFind (WebConfig.DefaultCustomer);
			item.SalesPerson = CurrentUser.Employee;
			item.Date = dt;
			item.Terms = PaymentTerms.Immediate;
			item.DueDate = DateTime.Now.AddDays (WebConfig.DefaultQuotationDueDays);
			item.Currency = WebConfig.DefaultCurrency;
			item.ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate ();

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

		public ActionResult Edit (int id)
		{
			var item = SalesQuote.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new {
					id = item.Id
				});
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			return View (item);
		}

		public ActionResult Duplicate (int id)
		{
			var item = SalesQuote.Find (id);


			SalesQuote copy = new SalesQuote {
				IsCompleted = false,
				Contact = item.Contact,
				CreationTime = DateTime.Now,
				Creator = CurrentUser.Employee,
				IsCancelled = false,
				Comment = item.Comment,
				Currency = item.Currency,
				Customer = item.Customer,
				Date = DateTime.Now,
				DueDate = DateTime.Now.AddDays (WebConfig.DefaultQuotationDueDays),
				ExchangeRate = item.ExchangeRate,
				ModificationTime = DateTime.Now,
				SalesPerson = CurrentUser.Employee,
				ShipTo = item.ShipTo,
				Store = item.Store,
				Terms = item.Terms,
				Updater = item.Updater
			};


			using (var scope = new TransactionScope ()) {
				copy.CreateAndFlush ();

				foreach (var detail in item.Details) {
					(new SalesQuoteDetail {
						SalesQuote = copy,
						Comment = detail.Comment,
						Currency = detail.Currency,
						DiscountRate = detail.DiscountRate,
						ExchangeRate = detail.ExchangeRate,
						IsTaxIncluded = detail.IsTaxIncluded,
						Price = detail.Product.GetPrice(item.Customer.PriceList.Id),
						PriceIncrement = detail.PriceIncrement,
						Product = detail.Product,
						ProductCode = detail.ProductCode,
						ProductName = detail.ProductName,
						Quantity = detail.Quantity,
						TaxRate = detail.TaxRate
					}).CreateAndFlush ();
				}
			}

			return RedirectToAction ("Edit", new { id = copy.Id });
		}

		public JsonResult Contacts (int id)
		{
			var item = SalesQuote.TryFind (id);
			var query = from x in item.Customer.Contacts
				    select new {
					    value = x.Id,
					    text = x.ToString ()
				    };

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult Addresses (int id)
		{
			var item = SalesQuote.TryFind (id);
			var query = from x in item.Customer.Addresses
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

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
		{
			var entity = SalesQuote.Find (id);
			var item = Customer.TryFind (value);
			var dt = DateTime.Now;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Customer = item;
				entity.Contact = null;
				entity.ShipTo = null;
				entity.Date = DateTime.Now;

				//entity.Terms = entity.Customer.HasCredit && entity.Customer.Id != WebConfig.DefaultCustomer ?
				//	PaymentTerms.NetD : PaymentTerms.Immediate;
				entity.Terms = PaymentTerms.Immediate;
				entity.DueDate = dt;

				if (item.SalesPerson == null) {
					entity.SalesPerson = CurrentUser.Employee;
				} else {
					entity.SalesPerson = item.SalesPerson;
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

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
		public ActionResult SetSalesPerson (int id, int value)
		{
			var entity = SalesQuote.Find (id);
			var item = Employee.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
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
			var entity = SalesQuote.Find (id);
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
			var entity = SalesQuote.Find (id);
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

			return Json (new {
				id = id,
				value = entity.ShipTo.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = SalesQuote.Find (id);
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
		public ActionResult SetDueDate (int id, DateTime? value)
		{
			var entity = SalesQuote.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				entity.DueDate = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.DueDate)
			});
		}

		[HttpPost]
		public ActionResult SetDate (int id, DateTime? value)
		{
			var entity = SalesQuote.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				entity.Date = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;
				entity.DueDate = value.Value.AddDays (WebConfig.DefaultQuotationDueDays);

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Date),
				dueDate = entity.FormattedValueFor (x => x.DueDate)
			});
		}

		[HttpPost]
		public ActionResult SetCurrency (int id, string value)
		{
			var entity = SalesQuote.Find (id);
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
			var entity = SalesQuote.Find (id);
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
			var entity = SalesQuote.Find (id);
			PaymentTerms val;
			bool success;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse<PaymentTerms> (value.Trim (), out val);

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

		[HttpPost]
		public ActionResult AddItem (int order, int product)
		{
			var entity = SalesQuote.TryFind (order);
			var p = Product.TryFind (product);
			int pl = entity.Customer.PriceList.Id;
			var cost = (from x in ProductPrice.Queryable
				    where x.Product.Id == product && x.List.Id == 0
				    select x).SingleOrDefault ();
			//var cost = p.GetCost ();
			//var price = (from x in ProductPrice.Queryable
			//	     where x.Product.Id == product && x.List.Id == pl
			//	     select x).SingleOrDefault ();
			var price = p.GetPrice (pl);
			var discount = (from x in CustomerDiscount.Queryable
					where x.Product.Id == product && x.Customer.Id == entity.Customer.Id
					select x.Discount).SingleOrDefault ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			//if (cost == 0) {
			//	cost = new ProductPrice {
			//		Value = decimal.Zero
			//	};
			//}

			//if (price == null) {
			//	price = new ProductPrice {
			//		Value = decimal.MaxValue
			//	};
			//}

			var item = new SalesQuoteDetail {
				SalesQuote = entity,
				Product = p,
				ProductCode = p.Code,
				ProductName = p.Name,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				Quantity = p.MinimumOrderQuantity,
				//Price = price.Value,
				Price = p.GetPrice (pl),
				PriceIncrement = 0,
				DiscountRate = discount,
				Currency = entity.Currency,
				ExchangeRate = entity.ExchangeRate
			};

			if (p.Currency != entity.Currency) {
				item.Price = price * CashHelpers.GetTodayExchangeRate (p.Currency, entity.Currency);
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
			var entity = SalesQuoteDetail.Find (id);

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
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

		public ActionResult Item (int id)
		{
			var entity = SalesQuoteDetail.Find (id);
			return PartialView ("_ItemEditorView", entity);
		}

		public ActionResult Items (int id)
		{
			var entity = SalesQuote.Find (id);
			return PartialView ("_Items", entity.Details);
		}

		public ActionResult Totals (int id)
		{
			var entity = SalesQuote.Find (id);
			return PartialView ("_Totals", entity);
		}

		[HttpPost]
		public ActionResult SetItemProductName (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
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
		public ActionResult SetItemComment (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Comment = string.IsNullOrWhiteSpace (value) ? null : value.Trim ();

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
			var entity = SalesQuoteDetail.Find (id);

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value < entity.Product.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MinimumQuantityRequired, entity.Product.MinimumOrderQuantity));
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

		[HttpPost]
		public ActionResult SetItemPrice (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			bool success;
			decimal val;
			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
					System.Globalization.NumberStyles.Currency,
					null, out val);

			if (success && val >= 0) {
				entity.Price = val;
				//var price_list = entity.SalesQuote.Customer.PriceList;
				if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
					var minimal_price = entity.Product.GetMinimalPrice ();
					var maximum_price = entity.Product.GetMaximumPrice ();
					if (!entity.IsPriceInRange()) {
						Response.StatusCode = 400;
						return Content (string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
					}
				}

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
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
		public ActionResult SetItemIncrementPrice (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);
			bool success;
			decimal val;

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
					System.Globalization.NumberStyles.Currency,
					null, out val);

			if (success && val >= 0) {
				entity.PriceIncrement = val;

				if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
					var minimal_price = entity.Product.GetMinimalPrice ();
					var maximum_price = entity.Product.GetMaximumPrice ();
					if (!entity.IsPriceInRange()) {
						Response.StatusCode = 400;
						return Content (string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
					}
				}

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.PriceIncrement),
				incrementrate = entity.FormattedValueFor (x => x.PriceIncrementRate),
				discount = entity.FormattedValueFor (x => x.DiscountByUnit),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemIncrementPriceRate (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);
			bool success;
			decimal val;

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
					System.Globalization.NumberStyles.Currency,
					null, out val);

			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				entity.PriceIncrementRate = val;

				if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
					var minimal_price = entity.Product.GetMinimalPrice ();
					var maximum_price = entity.Product.GetMaximumPrice ();
					if (!entity.IsPriceInRange()) {
						Response.StatusCode = 400;
						return Content (string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
					}
				}

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.PriceIncrementRate),
				increment = entity.FormattedValueFor (x => x.PriceIncrement),
				discount = entity.FormattedValueFor (x => x.DiscountByUnit),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemDiscountRate (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);

			bool success;
			decimal val;

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				entity.DiscountRate = val;

				if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
					var minimal_price = entity.Product.GetMinimalPrice ();
					var maximum_price = entity.Product.GetMaximumPrice ();
					if (!entity.IsPriceInRange()) {
						Response.StatusCode = 400;
						return Content (string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
					}
				}
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.DiscountRate),
				discount = entity.FormattedValueFor (x => x.DiscountByUnit),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemDiscount (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			var privileges = GetAccessPrivilege (SystemObjects.ExcludePriceRangeValidation);
			bool success;
			decimal val;

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);

			if (success && val >= 0 && val <= entity.Price) {
				entity.Discount = val;
				if (WebConfig.PriceValidationInRangeRequired && !privileges.AllowUpdate) {
					var minimal_price = entity.Product.GetMinimalPrice ();
					var maximum_price = entity.Product.GetMaximumPrice ();
					if (!entity.IsPriceInRange()) {
						Response.StatusCode = 400;
						return Content (string.Format (Resources.PriceInvalidRange, minimal_price, maximum_price));
					}
				}
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.DiscountByUnit),
				discountrate = entity.FormattedValueFor (x => x.DiscountRate),
				total = entity.FormattedValueFor (x => x.Total),
				total2 = entity.FormattedValueFor (x => x.TotalEx)
			});
		}

		[HttpPost]
		public ActionResult SetItemTaxRate (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
			bool success;
			decimal val;

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
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
		public ActionResult Confirm (int id)
		{
			var entity = SalesQuote.TryFind (id);
			entity.IsCompleted = true;
			//entity.Serial = entity.Serial = (SalesQuote.Queryable.Where (x => x.Store == WebConfig.Store).Max (x => (int?) x.Serial) + 1 ?? 1);


			using (var scope = new TransactionScope ()) {

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("View", new { id = id });
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = SalesQuote.Find (id);

			if (entity.IsCancelled) {
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

		public ActionResult SendEmail (int id)
		{
			var model = SalesQuote.Find (id);

			return PartialView ("_SendEmail", model);
		}

		[HttpPost]
		public ActionResult SendEmail (int id, string email)
		{
			var model = SalesQuote.Find (id);
			var filename = string.Format (Resources.SalesQuoteFilenameFormatString, model.Id, model.Serial);
			var subject = string.Format (Resources.SalesQuoteEmailSubjectFormatString, WebConfig.Company, model.Serial);
			var message = string.Format (Resources.SalesQuoteEmailBodyFormatString, model.Id, model.SalesPerson.Name);
			var attachments = new List<MimePart> ();

			attachments.Add (new MimePart {
				Content = new MimeContent (GetPdf ("Print", model), ContentEncoding.Default),
				ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = filename + ".pdf"
			});

			SendEmailWithAttachments (WebConfig.DefaultSender, email, subject, message, attachments);

			return PartialView ("_SendEmailSuccesful");
		}

		public JsonResult GetSuggestions (int id, string pattern)
		{
			int pl = SalesQuote.Queryable.Where (x => x.Id == id)
						.Select (x => x.Customer.PriceList.Id).Single ();

			var employee_id = CurrentUser.Employee.Id;
			//var warehouse = WebConfig.PointOfSale.Warehouse;

			//var all_warehouses = pattern.EndsWith (Resources.WilcardStringPatternForSearch);
			//pattern = pattern.TrimEnd (new char [] { '*' });
			pattern = pattern.Trim ();

			//string warehouse_filter = all_warehouses ? "" : " AND ((p.stockable = TRUE AND w.warehouse_id = " + warehouse.Id + ") OR p.stockable = FALSE) ";


			/*var sql = @"SELECT	p.product_id		id,
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
					LEFT JOIN lot_serial_tracking lst ON p.product_id = lst.product
					LEFT JOIN warehouse w ON w.warehouse_id = lst.warehouse
					JOIN product_price pp ON pp.product = p.product_id
					WHERE (
						p.name LIKE :pattern
						OR p.code LIKE :pattern
						OR p.sku LIKE :pattern
						OR p.brand LIKE :pattern
						OR p.model LIKE :pattern
					      )
						AND pp.`list` = :pricelist
						AND p.deactivated = FALSE
						AND p.salable = TRUE
						AND (w.disabled = FALSE || w.disabled IS NULL)
					WAREHOUSE_FILTER
					GROUP BY lst.warehouse, p.product_id
					ORDER BY p.product_id DESC
					LIMIT 15";*/

			var sql = @"SELECT	p.product_id		id,
						p.name			name,
						p.code			code,
						p.sku			sku,
						p.photo			url,
						p.model			model,
						SUM(lst.quantity)	quantity,
						pp.price		price,
						p.stockable		stockable
					FROM product p 
					LEFT JOIN lot_serial_tracking lst ON p.product_id = lst.product
					LEFT JOIN warehouse w ON w.warehouse_id = lst.warehouse
					JOIN product_price pp ON pp.product = p.product_id
					WHERE (
						p.name LIKE :pattern
						OR p.code LIKE :pattern
						OR p.sku LIKE :pattern
						OR p.brand LIKE :pattern
						OR p.model LIKE :pattern
					      )
						AND pp.`list` = :pricelist
						AND p.deactivated = FALSE
						AND p.salable = TRUE
						AND (w.disabled = FALSE || w.disabled IS NULL)
					GROUP BY p.product_id
					ORDER BY p.product_id DESC
					LIMIT 15";

			//sql = sql.Replace ("WAREHOUSE_FILTER", warehouse_filter);

			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("name", NHibernateUtil.String);
				query.AddScalar ("code", NHibernateUtil.String);
				query.AddScalar ("sku", NHibernateUtil.String);
				query.AddScalar ("url", NHibernateUtil.String);
				query.AddScalar ("model", NHibernateUtil.String);
				//query.AddScalar ("warehouse_id", NHibernateUtil.Int32);
				query.AddScalar ("quantity", NHibernateUtil.Decimal);
				//query.AddScalar ("warehouse", NHibernateUtil.String);
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
					     url = x.url,
					     //warehouse_id = x.warehouse_id,
					     quantity = x.quantity,
					     //warehouse = x.warehouse,
					     price = x.price,
					     stockable = x.stockable,
				     }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);
		}
	}
}
