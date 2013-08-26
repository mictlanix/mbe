// 
// InvoicesController.cs
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
			var item = Configuration.Store;
			
			if (item == null) {
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

            if (string.IsNullOrEmpty (search.Pattern)) {
                query = from x in FiscalDocument.Queryable
                      	where !(!x.IsCompleted && x.IsCancelled)
						orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Issued descending
                      	select x;
            } else {
                query = from x in FiscalDocument.Queryable
						where !(!x.IsCompleted && x.IsCancelled) && (
							x.Issuer.Id.Contains (search.Pattern) ||
							x.Recipient.Id.Contains (search.Pattern) ||
							x.RecipientName.Contains (search.Pattern) ||
							x.Customer.Name.Contains (search.Pattern))
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
			var view = string.Format ("Print{0:00}", item.Version * 10);

			return View (view, item);
		}

        public ViewResult New()
		{
			var store = Configuration.Store;

			if (store == null) {
				return View ("InvalidStore");
			}

			var item = new FiscalDocument {
				Issuer = Taxpayer.TryFind (store.Taxpayer)
			};

			return View (item);
        }

        [HttpPost]
		public ActionResult New (FiscalDocument item)
		{
			TaxpayerBatch batch = null;

			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.Recipient = CustomerTaxpayer.TryFind (item.RecipientId);

			if (item.Issuer != null) {
				batch = item.Issuer.Batches.FirstOrDefault ();
			}

			if (batch == null) {
				ModelState.AddModelError ("IssuerId", Resources.BatchRangeNotFound);
			}

			if (!ModelState.IsValid) {
				return View (item);
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
			item.RecipientName = item.Recipient.Name;
			item.RecipientAddress = item.Recipient.Address;


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

			return RedirectToAction ("Edit", new { id = item.Id });
		}
		
        public ActionResult Edit (int id)
        {
            var item = FiscalDocument.Find(id);
			
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			return View (item);
        }

		/* Edit


			entity.PaymentMethod = item.PaymentMethod;
			entity.PaymentReference = item.PaymentReference;
			entity.Currency = item.Currency;
			entity.ExchangeRate = item.ExchangeRate;
			entity.Reference = item.Reference;

		}*/

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
			var item = Taxpayer.TryFind (value);

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
					entity.Update ();
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

			if (item != null) {
				var recipient = item.Taxpayers.FirstOrDefault ();
				
				if (recipient == null) {
					Response.StatusCode = 400;
					return Content (Resources.TaxpayerNotFound);
				}

				entity.Customer = item;
				entity.Recipient = recipient;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Customer),
				recipient = entity.Recipient.Id,
				recipientText = entity.FormattedValueFor (x => x.Recipient)
			});
		}
		
		[HttpPost]
		public ActionResult SetRecipient (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			var item = CustomerTaxpayer.TryFind (value);

			if (item != null) {
				entity.Recipient = item;
				entity.RecipientName = item.Name;
				entity.RecipientAddress = item.Address;
				entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope()) {
					entity.Update ();
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
					entity.Update ();
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

			entity.Reference = string.Format("{0}", value).Trim ();
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
				entity.Update ();
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
					entity.Update ();
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

			if (entity.PaymentMethod == PaymentMethod.Unidentified ||
			    entity.PaymentMethod == PaymentMethod.Cash) {
				Response.StatusCode = 400;
				return Content (Resources.PaymentReferenceNotRequired);
			}

			entity.PaymentReference = string.Format("{0}", value).Trim ();
			entity.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			entity.ModificationTime = DateTime.Now;

			if (string.IsNullOrEmpty (entity.PaymentReference)) {
				entity.PaymentReference = Resources.Unidentified;
			}

			using (var scope = new TransactionScope()) {
				entity.Update ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.PaymentReference)
			});
		}

		[HttpPost]
		public JsonResult AddItem (int id, int product)
		{
			var p = Product.Find (product);
			var entity = FiscalDocument.Find (id);
			int pl = entity.Customer.PriceList.Id;
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x).SingleOrDefault();

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
			SalesOrder sales_order = null;
			int sales_order_id = 0;

			if (int.TryParse (value, out sales_order_id)) {
				sales_order = SalesOrder.TryFind (sales_order_id);
			}

			if (sales_order == null) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotFound);
			}

			var entity = FiscalDocument.Find (id);
			var count = 0;

			using (var scope = new TransactionScope()) {
				foreach(var x in sales_order.Details) {
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
		public JsonResult RemoveItem (int id)
		{
			var item = FiscalDocumentDetail.Find (id);

			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
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
			var order = FiscalDocument.Find(id);
			return PartialView ("_Totals", order);
		}

		[HttpPost]
		public JsonResult SetItemProductName (int id, string value)
		{
			var detail = FiscalDocumentDetail.Find (id);
			string val = (value ?? string.Empty).Trim ();
			
			if (val.Length > 0) {
				detail.ProductName = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = detail.Id, value = detail.ProductName });
		}
		
		[HttpPost]
		public JsonResult SetItemProductCode (int id, string value)
		{
			var detail = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.ProductCode = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = detail.Id, value = detail.ProductCode });
		}
		
		[HttpPost]
		public JsonResult SetItemUM (int id, string value)
		{
			var detail = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.UnitOfMeasurement = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = detail.Id, value = detail.UnitOfMeasurement });
		}

		[HttpPost]
		public JsonResult SetItemQuantity (int id, decimal value)
		{
			var entity = FiscalDocumentDetail.Find (id);

			if (entity.OrderDetail != null) {
				decimal max_qty = entity.Quantity + GetInvoiceableQuantity (entity.OrderDetail.Id);

				if (max_qty < value)
					value =  max_qty;
			}

			if (value > 0) {
				entity.Quantity = value;

				using (var scope = new TransactionScope()) {
					entity.Update ();
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
			var entity = FiscalDocumentDetail.Find (id);
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
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				entity.Discount = val;

				using (var scope = new TransactionScope()) {
					entity.Update ();
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
			var entity = FiscalDocumentDetail.Find (id);
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

		/*
		[HttpPost]
		public ActionResult Sign (int id)
		{
			FiscalDocument item = FiscalDocument.Find (id);

			if (!item.IsCompleted)
				return RedirectToAction ("Index");

			var doc = CFDHelpers.SignCFD (item);

			item.OriginalString = doc.ToString ();
			item.IssuerDigitalSeal = doc.sello;

			// save to orginal xml
			var doc_xml = new FiscalDocumentXml {
				Id = item.Id,
				Data = doc.ToXmlString ()
			};

			// save to database
			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}
		*/

		[HttpPost]
		public ActionResult Confirm (int id)
		{
			var item = FiscalDocument.TryFind (id);

			if (item == null || item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("Index");
			}

			// quantity validation
			foreach (var detail in item.Details) {
				if (detail.Quantity <= 0) 
					return RedirectToAction ("Edit", new { id = item.Id });

				if (detail.OrderDetail == null)
					continue;

				if (detail.Quantity > detail.OrderDetail.Quantity) 
					return RedirectToAction ("Edit", new { id = item.Id });
			}

			int serial = (from x in FiscalDocument.Queryable
			              where x.Issuer.Id == item.Issuer.Id &&
			              x.Batch == item.Batch
			              select x.Serial).Max ().GetValueOrDefault () + 1;

			var batch = (from x in item.Issuer.Batches
			             where x.Batch == item.Batch && 
			             x.SerialStart <= serial && 
			             x.SerialEnd >= serial
			             select x).SingleOrDefault ();

			if (batch == null) {
				return View ("InvalidBatch");
			}

			item.Type = batch.Type;
			item.Serial = serial;
			item.IssuerCertificateNumber = item.Issuer.Certificates.Single (x => x.IsActive).Id;

			var dt = DateTime.Now;
			item.Issued = new DateTime (dt.Year, dt.Month, dt.Day,
			                            dt.Hour, dt.Minute, dt.Second,
			                            DateTimeKind.Unspecified);

			dynamic doc = CFDHelpers.StampCFD (item);

			if (item.Issuer.Scheme == FiscalScheme.CFDI) {
				var tfd = doc.Complemento [0] as CFDv32.TimbreFiscalDigital;

				item.StampId = tfd.UUID;
				item.Stamped = tfd.FechaTimbrado;
				item.AuthorityDigitalSeal = tfd.selloSAT;
				item.AuthorityCertificateNumber = tfd.noCertificadoSAT;
				item.OriginalString = tfd.ToString ();
			} else {
				doc = CFDHelpers.SignCFD (item);
				item.ApprovalNumber = batch.ApprovalNumber;
				item.ApprovalYear = batch.ApprovalYear;
				item.OriginalString = doc.ToString ();
			}

			item.IssuerDigitalSeal = doc.sello;
			item.Version = Convert.ToDecimal (doc.version);
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
			item.IsCompleted = true;
			
			// save to orginal xml
			var doc_xml = new FiscalDocumentXml {
				Id = item.Id,
				Data = doc.ToXmlString ()
			};

			using (var scope = new TransactionScope()) {
				doc_xml.Create ();
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var item = FiscalDocument.Find (id);

			item.CancellationDate = DateTime.Now;
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
			item.IsCancelled = true;

			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction("Index");
		}

		public ActionResult Download (int id)
		{
			Stream xml = null;
			var entity = FiscalDocument.TryFind (id);
			var item = FiscalDocumentXml.TryFind (id);

			if (item == null) {
				xml = CFDHelpers.InvoiceToCFD (entity).ToXmlStream ();
			} else {
				xml = new MemoryStream (Encoding.UTF8.GetBytes (item.Data));
			}

			var result = new FileStreamResult (xml, "text/xml");
			result.FileDownloadName = string.Format (Resources.FiscalDocumentFilenameFormatString + ".xml",
			                                         entity.Issuer.Id, entity.Batch, entity.Serial);

			return result;
		}
		
		public ViewResult Reports ()
		{
			var qry = from x in FiscalDocument.Queryable
					  where x.Issuer.Scheme == FiscalScheme.CFD && x.IsCompleted
					  orderby x.Issued descending
					  select new { x.Issuer.Id, x.Issuer.Name, x.Issued.Value.Month, x.Issued.Value.Year };
			var items = from x in qry.ToList ().Distinct ()
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
						where x.Issuer.Id == taxpayer &&
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
                           WHERE m.cancelled = 0 AND d.order_detail = :detail ";
			
			IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<CustomerRefundDetail>.Execute(
				delegate(ISession session, object instance) {
				try {
					return session.CreateSQLQuery(sql)
							.SetParameter("detail", id)
							.SetMaxResults(1)
							.List<decimal>();
				} catch (Exception) {
					return null;
				}
			}, null);
			
			if (quantities != null && quantities.Count > 0) {
				quantity = item.Quantity - quantities[0];
			}
			
			return quantity > 0 ? quantity : 0;
		}
		
		public ActionResult QRCode (int id)
		{
			var item = FiscalDocument.Find (id);
			var data = string.Format ("?re={0}&rr={1}&tt={2:0000000000.000000}&id={3}",
			                          item.Issuer.Id, item.Recipient.Id, item.Total, item.StampId);

			return BarcodesController.QRCodeAction (data);
		}
    }
}
