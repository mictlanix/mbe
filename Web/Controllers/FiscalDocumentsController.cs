// 
// FiscalDocumentsController.cs
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
	public class FiscalDocumentsController : Controller
	{
		public ViewResult Index ()
		{
			if (Configuration.Store == null) {
				return View ("InvalidStore");
			}
			
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchFiscalDocuments (new Search<FiscalDocument> {
				Limit = Configuration.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<FiscalDocument> search)
		{
			if (ModelState.IsValid) {
				search = SearchFiscalDocuments (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<FiscalDocument> SearchFiscalDocuments (Search<FiscalDocument> search)
		{
			IQueryable<FiscalDocument> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id) && id > 0) {
				query = from x in FiscalDocument.Queryable
						where !(!x.IsCompleted && x.IsCancelled) && (
							x.Id == id || x.Serial == id)
				        orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Issued descending
				        select x;
			} else if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in FiscalDocument.Queryable
                      	where !(!x.IsCompleted && x.IsCancelled)
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Issued descending
                      	select x;
			} else {
				query = from x in FiscalDocument.Queryable
						where !(!x.IsCompleted && x.IsCancelled) && (
							x.Issuer.Id.Contains (pattern) ||
							x.Recipient.Contains (pattern) ||
							x.RecipientName.Contains (pattern) ||
							x.Customer.Name.Contains (pattern))
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Issued descending
                      	select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = FiscalDocument.Find (id);
			return View (item);
		}

		public ViewResult Print (int id)
		{
			var item = FiscalDocument.Find (id);
			var batch = TaxpayerBatch.Queryable.First (x => x.Batch == item.Batch);
			var view = string.Format ("Print{0:00}{1}", item.Version * 10, batch.Template);

			return View (view, item);
		}

		public ViewResult Pdf (int id)
		{
			var item = FiscalDocument.Find (id);
			var view = string.Format ("Pdf{0:00}", item.Version * 10);

			return View (view, item);
		}

		public ActionResult New ()
		{
			var store = Configuration.Store;

			if (store == null) {
				return View ("InvalidStore");
			}

			var item = new FiscalDocument {
				Issuer =  store.Taxpayer
			};

			return PartialView ("_Create", item);
		}

		[HttpPost]
		public ActionResult New (FiscalDocument item)
		{
			TaxpayerBatch batch = null;

			item.Issuer = TaxpayerIssuer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			var recipient = TaxpayerRecipient.TryFind (item.Recipient);

			if (recipient != null) {
				item.Recipient = recipient.Id;
				item.RecipientName = recipient.Name;
			}

			if (item.Issuer != null) {
				batch = item.Issuer.Batches.FirstOrDefault ();
			}

			if (batch == null) {
				ModelState.AddModelError ("IssuerId", Resources.BatchRangeNotFound);
			}

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}
			
			// Store
			item.Store = Configuration.Store;
			item.IssuedAt = item.Store.Address;
			item.IssuedLocation = item.Store.Location;

			// Issuer
			item.IssuerName = item.Issuer.Name;
			item.IssuerRegime = item.Issuer.Regime;
			item.IssuerAddress = item.Issuer.Address;
			
			// Recipient
			item.RecipientAddress = recipient.Address;

			// Fiscal doc's info
			item.Batch = batch.Batch;
			item.Type = batch.Type;
			item.Currency = Configuration.BaseCurrency;
			item.ExchangeRate = 1;
			item.PaymentMethod = PaymentMethod.Unidentified;
			item.PaymentReference = null;
			item.CreationTime = DateTime.Now;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = item.CreationTime;
			item.Updater = item.Creator;

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", new FiscalDocument { Id = item.Id });
		}

		public ActionResult Edit (int id)
		{
			var item = FiscalDocument.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new { id = item.Id });
			}
			
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			return View (item);
		}

		public JsonResult Batches (int id)
		{
			var item = FiscalDocument.TryFind (id);
			var qry = from x in TaxpayerBatch.Queryable
					  where x.Taxpayer.Id == item.Issuer.Id
			          select x.Batch;
			var list = from x in qry.Distinct ().ToList ()
					   select new { value = x, text = x };

			return Json (list.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult Recipients (int id)
		{
			var item = FiscalDocument.TryFind (id);
			var qry = from x in item.Customer.Taxpayers
					  select new { value = x.Id, text = x.ToString () };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult PaymentMethods ()
		{
			var items = new ArrayList ();

			items.Add (new { value = (int)PaymentMethod.Unidentified, text = PaymentMethod.Unidentified.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.Cash,         text = PaymentMethod.Cash.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.DebitCard,    text = PaymentMethod.DebitCard.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.CreditCard,   text = PaymentMethod.CreditCard.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.Check,        text = PaymentMethod.Check.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.BankDeposit,  text = PaymentMethod.BankDeposit.GetDisplayName ()});
			items.Add (new { value = (int)PaymentMethod.WireTransfer, text = PaymentMethod.WireTransfer.GetDisplayName ()});

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetIssuer (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			var item = TaxpayerIssuer.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				var batch = item.Batches.FirstOrDefault ();

				if (batch == null) {
					Response.StatusCode = 400;
					return Content (Resources.BatchRangeNotFound);
				}

				entity.Issuer = item;
				entity.IssuerName = item.Name;
				entity.IssuerRegime = item.Regime;
				entity.IssuerAddress = item.Address;
				entity.Batch = batch.Batch;
				entity.Type = batch.Type;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Issuer),
				batch = entity.FormattedValueFor (x => x.Batch),
				type = entity.Type.GetDisplayName ()
			});
		}

		[HttpPost]
		public ActionResult SetCustomer (int id, int value)
		{
			var entity = FiscalDocument.Find (id);
			var item = Customer.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				var recipient = item.Taxpayers.FirstOrDefault ();
				
				if (recipient == null) {
					Response.StatusCode = 400;
					return Content (Resources.TaxpayerNotFound);
				}

				entity.Customer = item;
				entity.Recipient = recipient.Id;
				entity.RecipientName = recipient.Name;
				entity.RecipientAddress = recipient.Address;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer),
				recipient = entity.Recipient,
				recipientText = entity.FormattedValueFor (x => x.Recipient)
			});
		}

		[HttpPost]
		public ActionResult SetRecipient (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			var item = TaxpayerRecipient.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Recipient = item.Id;
				entity.RecipientName = item.Name;
				entity.RecipientAddress = item.Address;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Recipient)
			});
		}

		[HttpPost]
		public ActionResult SetBatch (int id, string value)
		{
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				var batch = entity.Issuer.Batches.FirstOrDefault (x => x.Batch == value);

				if (batch == null) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidBatch);
				}

				entity.Batch = batch.Batch;
				entity.Type = batch.Type;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}
			
			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Batch),
				type = entity.Type.GetDisplayName ()
			});
		}

		[HttpPost]
		public ActionResult SetReference (int id, string value)
		{
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Reference = string.Format ("{0}", value).Trim ();
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Reference)
			});
		}

		[HttpPost]
		public ActionResult SetCurrency (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
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
			var entity = FiscalDocument.Find (id);
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
		public ActionResult SetPaymentMethod (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			PaymentMethod val;
			bool success;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse<PaymentMethod> (value.Trim (), out val);

			if (success) {
				entity.PaymentMethod = val;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				if (val == PaymentMethod.Unidentified || val == PaymentMethod.Cash) {
					entity.PaymentReference = null;
				} else if (string.IsNullOrEmpty (entity.PaymentReference)) {
					entity.PaymentReference = Resources.Unidentified;
				}

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.PaymentMethod,
				paymentReference = entity.FormattedValueFor (x => x.PaymentReference)
			});
		}

		[HttpPost]
		public ActionResult SetPaymentReference (int id, string value)
		{
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (entity.PaymentMethod == PaymentMethod.Unidentified ||
				entity.PaymentMethod == PaymentMethod.Cash) {
				Response.StatusCode = 400;
				return Content (Resources.PaymentReferenceNotRequired);
			}

			entity.PaymentReference = string.Format ("{0}", value).Trim ();
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;

			if (string.IsNullOrEmpty (entity.PaymentReference)) {
				entity.PaymentReference = Resources.Unidentified;
			}

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.PaymentReference)
			});
		}

		[HttpPost]
		public ActionResult SetRetentionRate (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			bool success;
			decimal val;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);

			if (success) {
				entity.RetentionRate = val;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { 
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.RetentionRate),
				total = entity.FormattedValueFor (x => x.Total), 
				total2 = entity.FormattedValueFor (x => x.TotalEx),
				itemsChanged = success
			});
		}

		[HttpPost]
		public ActionResult AddItem (int id, int product)
		{
			var p = Product.Find (product);
			var entity = FiscalDocument.Find (id);
			int pl = entity.Customer.PriceList.Id;
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x).SingleOrDefault ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var item = new FiscalDocumentDetail {
				Document = FiscalDocument.Find (id),
				Product = p,
				ProductCode = p.Code,
				ProductName = p.Name,
				UnitOfMeasurement = p.UnitOfMeasurement,
				Discount = 0,
				TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
				Quantity = 1,
				Price = price.Value,
				ExchangeRate = entity.ExchangeRate,
				Currency = entity.Currency
			};

			if (p.Currency != entity.Currency) {
				item.Price = price.Value * CashHelpers.GetTodayExchangeRate (p.Currency, entity.Currency);
			}

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return Json (new { id = item.Id });
		}

		[HttpPost]
		public ActionResult AddItems (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
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
					if (!x.Product.IsInvoiceable)
						continue;

					decimal max_qty = GetInvoiceableQuantity (x.Id);

					if (max_qty <= 0)
						continue;

					var item = new FiscalDocumentDetail {
						Document = entity,
						Product = x.Product,
						OrderDetail = x,
						ProductCode = x.ProductCode,
						ProductName = x.ProductName,
						UnitOfMeasurement = x.Product.UnitOfMeasurement,
						Discount = x.Discount,
						TaxRate = x.TaxRate,
						IsTaxIncluded = x.IsTaxIncluded,
						Quantity = max_qty,
						Price = x.Price,
						ExchangeRate = entity.ExchangeRate,
						Currency = entity.Currency
					};

					if (x.Currency != entity.Currency) {
						item.Price = x.Price * CashHelpers.GetTodayExchangeRate (x.Currency, entity.Currency);
					}

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
			var entity = FiscalDocumentDetail.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
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
			var item = FiscalDocumentDetail.Find (id);
			return PartialView ("_ItemEditorView", item);
		}

		public ActionResult Items (int id)
		{
			var item = FiscalDocument.Find (id);
			return PartialView ("_Items", item.Details);
		}

		public ActionResult Totals (int id)
		{
			var order = FiscalDocument.Find (id);
			return PartialView ("_Totals", order);
		}

		[HttpPost]
		public ActionResult SetItemProductName (int id, string value)
		{
			var entity = FiscalDocumentDetail.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
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
			var entity = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
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
		public ActionResult SetItemUM (int id, string value)
		{
			var entity = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (val.Length > 0) {
				entity.UnitOfMeasurement = val;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = entity.Id, value = entity.UnitOfMeasurement });
		}

		[HttpPost]
		public ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = FiscalDocumentDetail.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (entity.OrderDetail != null) {
				decimal max_qty = entity.Quantity + GetInvoiceableQuantity (entity.OrderDetail.Id);

				if (max_qty < value)
					value = max_qty;
			}

			if (value > 0) {
				entity.Quantity = value;

				using (var scope = new TransactionScope()) {
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
		public ActionResult SetItemPrice (int id, string value)
		{
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (),
			                            System.Globalization.NumberStyles.Currency,
			                            null, out val);

			if (success && val >= 0) {
				entity.Price = val;

				using (var scope = new TransactionScope()) {
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
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				entity.Discount = val;

				using (var scope = new TransactionScope()) {
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
		public ActionResult SetItemTaxRate (int id, string value)
		{
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);

			// TODO: VAT value range validation
			if (success) {
				entity.TaxRate = val;

				using (var scope = new TransactionScope()) {
					entity.UpdateAndFlush ();
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
			dynamic doc;
			int serial;
			TaxpayerBatch batch;
			var dt = DateTime.Now;
			var entity = FiscalDocument.TryFind (id);

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			// quantity validation
			foreach (var detail in entity.Details) {
				if (detail.Quantity <= 0) 
					return RedirectToAction ("Edit", new { id = entity.Id });

				if (detail.OrderDetail == null)
					continue;

				if (detail.Quantity > detail.OrderDetail.Quantity) 
					return RedirectToAction ("Edit", new { id = entity.Id });
			}

			serial = (from x in FiscalDocument.Queryable
		              where x.Issuer.Id == entity.Issuer.Id &&
							x.Batch == entity.Batch
		              select x.Serial).Max ().GetValueOrDefault () + 1;

			batch = (from x in entity.Issuer.Batches
		             where x.Batch == entity.Batch && 
						x.SerialStart <= serial && 
						x.SerialEnd >= serial
		             select x).SingleOrDefault ();

			if (batch == null) {
				return View ("InvalidBatch");
			}

			entity.Type = batch.Type;
			entity.Serial = serial;
			entity.Provider = entity.Issuer.Provider;
			entity.ApprovalYear = batch.ApprovalYear;
			entity.ApprovalNumber = batch.ApprovalNumber;
			entity.Issued = new DateTime (dt.Year, dt.Month, dt.Day,
			                              dt.Hour, dt.Minute, dt.Second,
			                              DateTimeKind.Unspecified);
			entity.IssuerCertificateNumber = entity.Issuer.Certificates.Single (x => x.IsActive).Id;

			try {
				doc = CFDHelpers.IssueCFD (entity);
			} catch (Exception ex) {
				return View ("Error", ex);
			}

			if (entity.Issuer.Scheme == FiscalScheme.CFDI) {
				var tfd = doc.Complemento [0] as CFDv32.TimbreFiscalDigital;

				entity.StampId = tfd.UUID;
				entity.Stamped = tfd.FechaTimbrado;
				entity.AuthorityDigitalSeal = tfd.selloSAT;
				entity.AuthorityCertificateNumber = tfd.noCertificadoSAT;
				entity.OriginalString = tfd.ToString ();
			} else {
				entity.OriginalString = doc.ToString ();
			}

			entity.IssuerDigitalSeal = doc.sello;
			entity.Version = Convert.ToDecimal (doc.version);
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;
			
			var doc_xml = new FiscalDocumentXml {
				Id = entity.Id,
				Data = doc.ToXmlString ()
			};

			using (var scope = new TransactionScope()) {
				doc_xml.Create ();
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = FiscalDocument.TryFind (id);

			if (entity == null || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			try {
				if (!CFDHelpers.CancelCFD (entity)) {
					return View (Resources.Error, new InvalidOperationException (Resources.WebServiceReturnedFalse));
				}
			} catch (Exception ex) {
				return View (Resources.Error, ex);
			}

			entity.CancellationDate = DateTime.Now;
			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		public ActionResult Download (int id)
		{
			var item = FiscalDocumentXml.TryFind (id);

			if (item == null) {
				return View (Resources.Error, new FileNotFoundException ());
			}
			
			var entity = FiscalDocument.TryFind (id);
			var xml = new MemoryStream (Encoding.UTF8.GetBytes (item.Data));
			var result = new FileStreamResult (xml, "text/xml");

			result.FileDownloadName = string.Format (Resources.FiscalDocumentFilenameFormatString + ".xml",
			                                         entity.Issuer.Id, entity.Batch, entity.Serial);

			return result;
		}

		public ViewResult Reports ()
		{
			var query = from x in FiscalDocument.Queryable
						where x.Version < 3 && x.IsCompleted
						select new { x.Issuer.Id, x.Issuer.Name, x.Issued.Value.Month, x.Issued.Value.Year };
			var query2 = from x in FiscalDocument.Queryable
						where x.Version < 3 && x.IsCompleted && x.IsCancelled
						select new { x.Issuer.Id, x.Issuer.Name, x.CancellationDate.Value.Month, x.CancellationDate.Value.Year };
			var items = from x in query.ToList ().Union (query2.ToList ()).Distinct ()
						orderby x.Year descending, x.Month descending, x.Id ascending
						select new FiscalReport {
							TaxpayerId = x.Id, TaxpayerName = x.Name, Month = x.Month, Year = x.Year
						};

			return View (items.ToList ());
		}

		public ActionResult Report (string taxpayer, int year, int month)
		{
			var ms = new MemoryStream ();
			var sw = new StreamWriter (ms, Encoding.UTF8);
			var start = new DateTime (year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
			var end = start.AddMonths (1);
			var query = from x in FiscalDocument.Queryable
						where x.Issuer.Id == taxpayer && x.Version < 3 && 
							x.IsCompleted && ((x.Issued >= start && x.Issued < end) ||
							(x.IsCancelled && x.CancellationDate >= start && x.CancellationDate < end))
						select x;

			foreach (var row in CFDHelpers.MonthlyReport (query.ToList())) {
				sw.WriteLine (row);
			}

			sw.Flush ();
			ms.Seek (0, SeekOrigin.Begin);
			
			var result = new FileStreamResult (ms, "text/plain");
			result.FileDownloadName = string.Format (Resources.Format_FiscalReportName,
			                                         taxpayer, year, month);

			return result;
		}

		public JsonResult GetSuggestions (int id, string pattern)
		{
			int pl = FiscalDocument.Queryable.Where (x => x.Id == id)
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

		decimal GetInvoiceableQuantity (int id)
		{
			var item = SalesOrderDetail.Find (id);
			decimal quantity = item.Quantity;
			string sql = @"SELECT IFNULL(SUM(d.quantity),0) quantity
                           FROM fiscal_document_detail d INNER JOIN fiscal_document m ON d.document = m.fiscal_document_id
                           WHERE m.cancelled = 0 AND d.order_detail = :detail
						   UNION ALL
						   SELECT IFNULL(SUM(d.quantity),0) quantity
						   FROM customer_refund_detail d INNER JOIN customer_refund m ON d.customer_refund = m.customer_refund_id
						   WHERE m.cancelled = 0 AND d.sales_order_detail = :detail";
			
			IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<CustomerRefundDetail>.Execute (
				delegate(ISession session, object instance) {
				try {
					return session.CreateSQLQuery (sql)
							.SetParameter ("detail", id)
							.List<decimal> ();
				} catch (Exception) {
					return null;
				}
			}, null);
			
			if (quantities != null && quantities.Count > 0) {
				quantity = item.Quantity - quantities.Sum();
			}
			
			return quantity > 0 ? quantity : 0;
		}

		public ActionResult QRCode (int id)
		{
			var item = FiscalDocument.Find (id);
			var data = string.Format (Resources.FiscalDocumentQRCodeFormatString,
			                          item.Issuer.Id, item.Recipient, item.Total, item.StampId);

			return BarcodesController.QRCodeAction (data);
		}
	}
}
