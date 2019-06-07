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
			IQueryable<SalesQuote> query;
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id) && id > 0) {
				query = from x in SalesQuote.Queryable
					where x.Store.Id == item.Id && (
					    x.Id == id || x.Serial == id)
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					select x;
			} else if (string.IsNullOrEmpty (pattern)) {
				query = from x in SalesQuote.Queryable
					where x.Store.Id == item.Id
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					select x;
			} else {
				query = from x in SalesQuote.Queryable
					where x.Store.Id == item.Id && (
					    x.Customer.Name.Contains (pattern) ||
					    x.SalesPerson.Nickname.Contains (pattern))
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					select x;
			}

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

			// Store and Serial
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

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Customer = item;
				entity.Contact = null;
				entity.ShipTo = null;

				if (item.SalesPerson == null) {
					entity.SalesPerson = CurrentUser.Employee;
				} else {
					entity.SalesPerson = item.SalesPerson;
				}

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
			var price = (from x in ProductPrice.Queryable
				     where x.Product.Id == product && x.List.Id == pl
				     select x).SingleOrDefault ();
			var discount = (from x in CustomerDiscount.Queryable
					where x.Product.Id == product && x.Customer.Id == entity.Customer.Id
					select x.Discount).SingleOrDefault ();

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

			var item = new SalesQuoteDetail {
				SalesQuote = entity,
				Product = p,
				ProductCode = p.Code,
				ProductName = p.Name,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				Quantity = p.MinimumOrderQuantity,
				Price = price.Value,
				DiscountRate = discount,
				Currency = entity.Currency,
				ExchangeRate = entity.ExchangeRate
			};

			if (p.Currency != entity.Currency) {
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

			if (entity.SalesQuote.IsCompleted || entity.SalesQuote.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
					System.Globalization.NumberStyles.Currency,
					null, out val);

			if (success && val >= 0) {
				entity.Price = val;

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
		public ActionResult SetItemDiscount (int id, string value)
		{
			var entity = SalesQuoteDetail.Find (id);
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

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.DiscountRate),
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


			using (var scope = new TransactionScope ()) {

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
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
				ContentObject = new ContentObject (GetPdf ("Print", model), ContentEncoding.Default),
				ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = filename + ".pdf"
			});

			SendEmailWithAttachments (WebConfig.DefaultSender, email, subject, message, attachments);

			return PartialView ("_SendEmailSuccesful");
		}

		// TODO: Rename param: order -> id
		public JsonResult GetSuggestions (int order, string pattern)
		{
			int pl = SalesQuote.Queryable.Where (x => x.Id == order)
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
					    x.Product.Id,
					    x.Product.Name,
					    x.Product.Code,
					    x.Product.Model,
					    x.Product.SKU,
					    x.Product.Photo,
					    Price = x.Value
				    };
			var items = from x in query.Take (15).ToList ()
				    select new {
					    id = x.Id,
					    name = x.Name,
					    code = x.Code,
					    model = x.Model,
					    sku = x.SKU,
					    url = Url.Content (x.Photo),
					    price = x.Price
				    };

			return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		}
	}
}
