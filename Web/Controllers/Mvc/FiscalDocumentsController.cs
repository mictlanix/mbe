// 
// FiscalDocumentsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
using MimeKit;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	[RoutePrefix ("FiscalDocuments")]
	public class FiscalDocumentsController : CustomController {
		public ViewResult Index ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchFiscalDocuments (new Search<FiscalDocument> {
				Limit = WebConfig.PageSize
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
			IQueryable<FiscalDocument> query = from x in FiscalDocument.Queryable
							   where !(!x.IsCompleted && x.IsCancelled)
							   orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Issued descending
							   select x;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id) && id > 0) {
				query = from x in query
					where (x.Id == id || x.Serial == id)
					select x;
			} else if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in query
					select x;
			} else {
				query = from x in FiscalDocument.Queryable
					where ( x.Issuer.Id.Contains (pattern) ||
						x.Recipient.Contains (pattern) ||
						x.RecipientName.Contains (pattern) ||
						x.Customer.Name.Contains (pattern))
					select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = FiscalDocument.Find (id);

			switch (item.Type) {
			case FiscalDocumentType.PaymentReceipt:
				return View ("ViewPayment", item);

			case FiscalDocumentType.CreditNote:
			case FiscalDocumentType.AdvancePaymentsApplied:
				return View ("ViewOutcome", item);

			case FiscalDocumentType.SalesSummaryInvoice:
				return View ("ViewSalesSummaryInvoice", item);

			default:
				return View (item);
			}
		}

		public ViewResult Print (int id)
		{
			var view = "Print";
			var model = FiscalDocument.Find (id);

			if (model.IsCompleted) {
				var batch = TaxpayerBatch.Queryable.First (x => x.Batch == model.Batch);
				var template = Newtonsoft.Json.JsonConvert.DeserializeObject<Template> (batch.Template);
				view = string.Format ("Print{0:00}{1}", model.Version * 10, template.Name);
			}

			return View (view, model);
		}

		public ActionResult Pdf (int id)
		{
			var view = "Print";
			var header = "_PageHead";
			var footer = "_PageFoot";
			var model = FiscalDocument.Find (id);
			var template = new Template ();

			if (model.IsCompleted) {
				var batch = TaxpayerBatch.Queryable.First (x => x.Taxpayer.Id == model.Issuer.Id && x.Batch == model.Batch);
				var filename = string.Format (Resources.FiscalDocumentFilenameFormatString, model.Issuer.Id, model.Batch, model.Serial);

				template = Newtonsoft.Json.JsonConvert.DeserializeObject<Template> (batch.Template);
				view = string.Format ("Print{0:00}{1}", model.Version * 10, template.Name);

				Response.AppendHeader ("Content-Disposition", string.Format ("inline; filename={0}.pdf", filename));
			}

			header = view + header;
			footer = view + footer;

			if (ViewEngines.Engines.FindPartialView (ControllerContext, header).View == null) {
				header = null;
			}

			if (ViewEngines.Engines.FindPartialView (ControllerContext, footer).View == null) {
				footer = null;
			}

			ViewBag.Logo = template.Logo;
			ViewBag.ExtraInfo = template.ExtraInfo;

			return PdfView (view, model, new jsreport.Types.Phantom {
				Header = header,
				HeaderHeight = template.HeaderHeight + " mm",
				Footer = footer,
				FooterHeight = template.FooterHeight + " mm"
			});
		}

		public ActionResult PdfTicket (int id) {

			var model = FiscalDocument.Find (id);

			if (model.IsCancelled || !model.IsCompleted) {
				return RedirectToAction ("Index");
			}
			return PdfTicketView ("PrintSalesOrders", model);
		}

		[HttpGet]
		[Route (@"New/{type}")]
		public virtual ActionResult New (FiscalDocumentType type)
		{
			var store = WebConfig.Store;

			if (store == null) {
				return View ("InvalidStore");
			}

			var item = new FiscalDocument {
				Type = type,
				Issuer = store.Taxpayer
			};

			return PartialView ("_Create", item);
		}

		[HttpPost]
		[Route (@"New")]
		public virtual ActionResult New (FiscalDocument item)
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
				batch = item.Issuer.Batches.FirstOrDefault (x => x.Type == item.Type);
			}

			if (batch == null) {
				ModelState.AddModelError ("IssuerId", Resources.BatchRangeNotFound);
			}

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			// Store
			item.Store = WebConfig.Store;
			item.IssuedAt = item.Store.Address;
			item.IssuedLocation = item.Store.LocationId;

			// Issuer
			item.IssuerName = item.Issuer.Name;
			item.IssuerRegime = item.Issuer.Regime;
			item.IssuerRegimeName = item.Issuer.Regime.Description;

			// Fiscal doc's info
			item.Batch = batch.Batch;
			item.Type = batch.Type;
			item.Currency = WebConfig.BaseCurrency;
			item.ExchangeRate = 1m;
			item.Terms = item.Customer.HasCredit ? PaymentTerms.NetD : PaymentTerms.Immediate;
			item.PaymentMethod = PaymentMethod.ToBeDefined;
			item.Usage = SatCfdiUsage.TryFind (WebConfig.DefaultCfdiUsage);
			item.CreationTime = DateTime.Now;
			item.Creator = CurrentUser.Employee;
			item.ModificationTime = item.CreationTime;
			item.Updater = item.Creator;

			if (item.Type == FiscalDocumentType.PaymentReceipt) {
				item.Terms = PaymentTerms.Immediate;
				item.PaymentMethod = PaymentMethod.Cash;
				item.Usage = SatCfdiUsage.TryFind ("P01");
			}

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", new FiscalDocument { Id = item.Id });
		}

		public virtual ActionResult Edit (int id)
		{
			var item = FiscalDocument.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new { id = item.Id });
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			switch (item.Type) {
				case FiscalDocumentType.PaymentReceipt:
					return View ("EditPayment", item);
				case FiscalDocumentType.CreditNote:
				case FiscalDocumentType.AdvancePaymentsApplied:
					return View("EditOutcome", item);
				case FiscalDocumentType.SalesSummaryInvoice:
					return View ("EditSalesSummaryInvoice", item);
				default:
					return View (item);
			}

		}

		public JsonResult Batches (int id)
		{
			var item = FiscalDocument.TryFind (id);
			var qry = from x in TaxpayerBatch.Queryable
					  where x.Taxpayer.Id == item.Issuer.Id && x.Type == item.Type
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
				var batch = item.Batches.FirstOrDefault (x => x.Type == entity.Type);

				if (batch == null) {
					Response.StatusCode = 400;
					return Content (Resources.BatchRangeNotFound);
				}

				entity.Issuer = item;
				entity.IssuerName = item.Name;
				entity.IssuerRegime = item.Regime;
				entity.IssuerRegimeName = item.Regime.Description;
				entity.Batch = batch.Batch;
				entity.Type = batch.Type;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
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
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
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
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = Enum.TryParse (value.Trim (), out val);

			if (success) {
				entity.Terms = val;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				if(entity.Terms == PaymentTerms.NetD) {
					entity.PaymentMethod = PaymentMethod.ToBeDefined;
				}

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id,
				value = entity.Terms,
				paymentMethod = entity.PaymentMethod,
				paymentMethodText = entity.PaymentMethod.GetDisplayName (),
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

			success = Enum.TryParse (value.Trim (), out val);

			if (success) {
				if (entity.Terms == PaymentTerms.NetD) {
					entity.PaymentMethod = PaymentMethod.ToBeDefined;
				} else {
					entity.PaymentMethod = val;
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.PaymentMethod
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

			entity.PaymentReference = string.Format ("{0}", value).Trim ();
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			if (string.IsNullOrEmpty (entity.PaymentReference)) {
				entity.PaymentReference = null;
			}

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.PaymentReference)
			});
		}

		[HttpPost]
		public ActionResult SetPaymentDate (int id, DateTime? value)
		{
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value != null) {
				entity.PaymentDate = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.PaymentDate)
			});
		}

		[HttpPost]
		public ActionResult SetPaymentAmount (int id, string value)
		{
			var entity = FiscalDocument.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (decimal.TryParse (value.Trim (), NumberStyles.Currency, null, out decimal val)) {
				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidPaymentAmount);
				}

				entity.PaymentAmount = val;

				if (entity.PaymentAmount < entity.Paid) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidPaymentAmountSum);
				}

				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.PaymentAmount),
				itemsChanged = true
			});
		}

		[HttpPost]
		public ActionResult SetUsage (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			var item = SatCfdiUsage.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Usage = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Id)
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

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);

			if (success) {
				entity.RetentionRate = val;

				using (var scope = new TransactionScope ()) {
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
				ProductService = p.ProductService,
				UnitOfMeasurement = p.UnitOfMeasurement,
				UnitOfMeasurementName = p.UnitOfMeasurement.Name,
				DiscountRate = 0,
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

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return Json (new { id = item.Id });
		}

		[HttpPost]
		public ActionResult AddRelation (int id, int relation)
		{
			var item = new FiscalDocumentRelation {
				Document = FiscalDocument.Find (id),
				Relation = FiscalDocument.Find (relation)
			};

			if (item.Document.IsCompleted || item.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			item.Installment = 1;
			item.ExchangeRate = 1m;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return Json (new { id = item.Id });
		}

		[HttpPost]
		public ActionResult RemoveRelation (int id)
		{
			var entity = FiscalDocumentRelation.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}

		public ActionResult Relation (int id)
		{
			var item = FiscalDocumentRelation.Find (id);

			if (item.Document.Type == FiscalDocumentType.PaymentReceipt) {
				return PartialView ("_RelationEditorView", item);
			}

			return PartialView ("_SimpleRelationEditorView", item);
		}

		public ActionResult Relations (int id)
		{
			var item = FiscalDocument.Find (id);

			if (item.Type == FiscalDocumentType.PaymentReceipt) {
				return PartialView ("_Relations", item.Relations);
			}

			return PartialView ("_SimpleRelations", item.Relations);
		}

		public ActionResult PaymentTotals (int id)
		{
			var order = FiscalDocument.Find (id);
			return PartialView ("_PaymentTotals", order);
		}

		[HttpPost]
		public ActionResult SetRelationInstallment (int id, int value)
		{
			var entity = FiscalDocumentRelation.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value <= 0m) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.Validation_CannotBeZeroOrNegative, ""));
			}

			entity.Installment = value;
			entity.Document.Updater = CurrentUser.Employee;
			entity.Document.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Installment)
			});
		}

		[HttpPost]
		public ActionResult SetRelationPreviousBalance (int id, string value)
		{
			var entity = FiscalDocumentRelation.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (), out val);

			if (success) {
				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidPreviousBalance);
				}

				entity.PreviousBalance = val;
				entity.Document.Updater = CurrentUser.Employee;
				entity.Document.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.PreviousBalance)
			});
		}

		[HttpPost]
		public ActionResult SetRelationAmount (int id, string value)
		{
			var entity = FiscalDocumentRelation.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (decimal.TryParse (value.Trim (), NumberStyles.Currency, null, out decimal val)) {
				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidPaymentAmount);
				}

				entity.Document.Relations.Single (x => x.Id == entity.Id).Amount = val;

				if (entity.Document.PaymentAmount < entity.Document.Paid) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidPaymentAmountSum);
				}

				entity.Amount = val;
				entity.Document.Updater = CurrentUser.Employee;
				entity.Document.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Amount),
				itemsChanged = true
			});
		}

		[HttpPost]
		public ActionResult SetRelationExchangeRate (int id, string value)
		{
			var entity = FiscalDocumentRelation.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.Trim (), out val);

			if (success) {
				if (val <= 0m) {
					Response.StatusCode = 400;
					return Content (Resources.InvalidExchangeRate);
				}

				entity.ExchangeRate = val;
				entity.Document.Updater = CurrentUser.Employee;
				entity.Document.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.ExchangeRate)
			});
		}

		[HttpPost]
		public ActionResult AddItems (int id, string value)
		{
			var entity = FiscalDocument.Find (id);
			SalesOrder sales_order = null;
			List<SalesOrder> sales_orders = new List<SalesOrder> ();
			int sales_order_id = 0;
			int count = 0;

			//string pattern = @"^((\d+-\d+)|\d+)(( )?,( )?((\d+( )?-( )?\d+)|\d+))*$";
			//var matches = Regex.Match (value, pattern);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			//if (!matches.Success) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.Validation_DigitsOnly);
			//}

			

			if (int.TryParse (value, out sales_order_id)) {
				sales_order = SalesOrder.TryFind (sales_order_id);
			}

			if (sales_order == null) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderNotFound);
			}

			if (!sales_order.IsCompleted || sales_order.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.SalesOrderIsNotInvoiceable);
			}

			using (var scope = new TransactionScope ()) {
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
						ProductService = x.Product.ProductService,
						UnitOfMeasurement = x.Product.UnitOfMeasurement,
						UnitOfMeasurementName = x.Product.UnitOfMeasurement.Name,
						DiscountRate = x.DiscountRate,
						TaxRate = x.TaxRate,
						IsTaxIncluded = x.IsTaxIncluded,
						Quantity = max_qty,
						Price = x.Price,
						ExchangeRate = entity.ExchangeRate,
						Currency = entity.Currency,
						Comment = x.Comment
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
		public virtual ActionResult RemoveItem (int id)
		{
			var entity = FiscalDocumentDetail.Find (id);

			if (entity.Document.Type == FiscalDocumentType.SalesSummaryInvoice) {
				Response.StatusCode = 400;
				return Content (Resources.Message_DeleteUnsuccessful);
			}

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}

		public virtual ActionResult Item (int id)
		{
			var item = FiscalDocumentDetail.Find (id);
			return PartialView ("_ItemEditorView", item);
		}

		public virtual ActionResult ItemSingleDetail (int id)
		{
			var item = FiscalDocument.Find (id);
			return PartialView ("_SingleItemDisplayView", item.Details);
		}

		public virtual ActionResult Items (int id)
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

			using (var scope = new TransactionScope ()) {
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

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = entity.Id, value = entity.ProductCode });
		}

		[HttpPost]
		public ActionResult SetItemComment (int id, string value)
		{
			var entity = FiscalDocumentDetail.Find (id);

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Comment = string.IsNullOrWhiteSpace (value) ? null : value.Trim ();

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = entity.Comment });
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
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
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
			var entity = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			if (entity.Document.IsCompleted || entity.Document.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);

			// TODO: VAT value range validation
			if (success) {
				entity.TaxRate = val;

				using (var scope = new TransactionScope ()) {
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
			int serial;
			TaxpayerBatch batch;
			var dt = DateTime.Now;
			var entity = FiscalDocument.TryFind (id);

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			// quantity validation
			foreach (var detail in entity.Details) {
				detail.Document = entity;

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
				 where x.Batch == entity.Batch
				 select x).SingleOrDefault ();

			if (batch == null) {
				return View ("InvalidBatch");
			}

			entity.Type = batch.Type;
			entity.Serial = serial;
			entity.Provider = entity.Issuer.Provider;
			entity.Issued = new DateTime (dt.Year, dt.Month, dt.Day,
						      dt.Hour, dt.Minute, dt.Second,
						      DateTimeKind.Unspecified);
			entity.IssuerCertificateNumber = entity.Issuer.Certificates.Single (x => x.IsActive).Id;

			CFDv33.Comprobante doc;

			try {
				doc = CFDHelpers.IssueCFD (entity);
			} catch (Exception ex) {
				return View ("Error", ex);
			}

			foreach(var complemento in doc.Complemento) {
				if(complemento is CFDv33.TimbreFiscalDigital tfd) {
					entity.StampId = tfd.UUID;
					entity.Stamped = tfd.FechaTimbrado;
					entity.AuthorityDigitalSeal = tfd.SelloSAT;
					entity.AuthorityCertificateNumber = tfd.NoCertificadoSAT;
					entity.OriginalString = tfd.ToString ();
					break;
				}
			}

			entity.IssuerDigitalSeal = doc.Sello;
			entity.Version = Convert.ToDecimal (doc.Version);

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			var doc_xml = new FiscalDocumentXml {
				Id = entity.Id,
				Data = doc.ToXmlString ()
			};

			using (var scope = new TransactionScope ()) {
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

			if(entity.IsCompleted) {
				try {
					if (!CFDHelpers.CancelCFD (entity)) {
						return View (Resources.Error, new InvalidOperationException (Resources.WebServiceReturnedFalse));
					}
				} catch (Exception ex) {
					return View (Resources.Error, ex);
				}
			}


			entity.CancellationDate = DateTime.Now;
			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				foreach (var order in FiscalDocumentDetailSalesOrderDetail.Queryable.Where (x => x.FiscalDocumentDetail.Document == entity).ToList ()) {
					order.Delete ();
				}
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
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

		public JsonResult GetRelations (int id, string pattern)
		{
			var entity = FiscalDocument.Find (id);
			IQueryable<FiscalDocument> query;
			int serial = 0;

			pattern = (pattern ?? string.Empty).Trim ();

			if (int.TryParse (pattern, out serial) && serial > 0) {
				query = from x in FiscalDocument.Queryable
					where x.Type != FiscalDocumentType.PaymentReceipt && x.Type != FiscalDocumentType.CreditNote &&
						x.IsCompleted && !x.IsCancelled &&
						x.Recipient == entity.Recipient &&
						x.Issuer.Id == entity.Issuer.Id &&
						(x.Id == serial || x.Serial == serial)
					orderby x.Issued descending
					select x;
			} else {
				query = from x in FiscalDocument.Queryable
					where x.Type != FiscalDocumentType.PaymentReceipt && x.Type != FiscalDocumentType.CreditNote &&
						x.IsCompleted && !x.IsCancelled &&
						x.Recipient == entity.Recipient &&
						x.Issuer.Id == entity.Issuer.Id &&
						x.StampId.Contains (pattern)
					orderby x.Issued descending
					select x;
			}

			var items = from x in query.Take (15).ToList ()
				    select new {
					    id = x.Id,
					    stamp = x.StampId,
					    batch = x.Batch,
					    serial = x.FormattedValueFor (o => o.Serial),
					    currency = x.Currency.GetDisplayName ()
				    };

			return Json (items.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetReplacementRelations (int id, string pattern)
		{
			var entity = FiscalDocument.Find (id);
			IQueryable<FiscalDocument> query;
			int serial = 0;

			pattern = (pattern ?? string.Empty).Trim ();

			if (int.TryParse (pattern, out serial) && serial > 0) {
				query = from x in FiscalDocument.Queryable
					where x.Type < FiscalDocumentType.CreditNote &&
						x.IsCompleted && x.IsCancelled &&
						x.Recipient == entity.Recipient &&
						x.Issuer.Id == entity.Issuer.Id &&
						(x.Id == serial || x.Serial == serial)
					orderby x.Issued descending
					select x;
			} else {
				query = from x in FiscalDocument.Queryable
					where x.Type < FiscalDocumentType.CreditNote &&
						x.IsCompleted && x.IsCancelled &&
						x.Recipient == entity.Recipient &&
						x.Issuer.Id == entity.Issuer.Id &&
						x.StampId.Contains (pattern)
					orderby x.Issued descending
					select x;
			}

			var items = from x in query.Take (15).ToList ()
				    select new {
					    id = x.Id,
					    stamp = x.StampId,
					    batch = x.Batch,
					    serial = x.FormattedValueFor (o => o.Serial),
					    currency = x.Currency.GetDisplayName ()
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

			IList<decimal> quantities = (IList<decimal>) ActiveRecordMediator<CustomerRefundDetail>.Execute (
				delegate (ISession session, object instance) {
					try {
						return session.CreateSQLQuery (sql)
								.SetParameter ("detail", id)
								.List<decimal> ();
					} catch (Exception) {
						return null;
					}
				}, null);

			if (quantities != null && quantities.Count > 0) {
				quantity = item.Quantity - quantities.Sum ();
			}

			return quantity > 0 ? quantity : 0;
		}

		[AllowAnonymous]
		public ActionResult QRCode (int id)
		{
			var item = FiscalDocument.Find (id);
			var data = string.Format (Resources.FiscalDocumentQRCodeFormatString,
						  item.Issuer.Id, item.Recipient, item.Total, item.StampId);

			return BarcodesController.QRCodeAction (data);
		}

		[AllowAnonymous]
		public ActionResult QRCode33 (int id)
		{
			var item = FiscalDocument.Find (id);
			var data = string.Format (Resources.FiscalDocumentQRCode33FormatString,
						  item.Issuer.Id, item.Recipient, item.Total, item.StampId,
						  item.IssuerDigitalSeal.Substring (item.IssuerDigitalSeal.Length - 8));

			return BarcodesController.QRCodeAction (data);
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

		public ActionResult SendEmail (int id)
		{
			var model = FiscalDocument.Find (id);

			return PartialView ("_SendEmail", model);
		}

		[HttpPost]
		public ActionResult SendEmail (int id, string email)
		{
			var model = FiscalDocument.Find (id);
			var xml = FiscalDocumentXml.Find (id);
			var batch = TaxpayerBatch.Queryable.First (x => x.Taxpayer.Id == model.Issuer.Id && x.Batch == model.Batch);
			var filename = string.Format (Resources.FiscalDocumentFilenameFormatString, model.Issuer.Id, model.Batch,
						      model.Serial);
			var subject = string.Format (Resources.FiscalDocumentEmailSubjectFormatString, model.Issuer.Id, model.Batch,
						     model.Serial);
			var message = string.Format (Resources.FiscalDocumentEmailBodyFormatString, model.Issuer.Id, model.Recipient,
						     model.Total, model.StampId, model.IssuerDigitalSeal.Substring (model.IssuerDigitalSeal.Length - 8));
			var attachments = new List<MimePart> ();
			var template = Newtonsoft.Json.JsonConvert.DeserializeObject<Template> (batch.Template);
			var view = string.Format ("Print{0:00}{1}", model.Version * 10, template.Name);
			var header = view + "_PageHead";
			var footer = view + "_PageFoot";

			if (ViewEngines.Engines.FindPartialView (ControllerContext, header).View == null) {
				header = null;
			}

			if (ViewEngines.Engines.FindPartialView (ControllerContext, footer).View == null) {
				footer = null;
			}

			ViewBag.Logo = template.Logo;
			ViewBag.ExtraInfo = template.ExtraInfo;

			attachments.Add (new MimePart {
				Content = new MimeContent (GetPdf (view, model, new jsreport.Types.Phantom {
					Header = header,
					HeaderHeight = template.HeaderHeight + " mm",
					Footer = footer,
					FooterHeight = template.FooterHeight + " mm"
				}), ContentEncoding.Default),
				ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = filename + ".pdf"
			});
			attachments.Add (new MimePart {
				Content = new MimeContent (new MemoryStream (Encoding.UTF8.GetBytes (xml.Data)), ContentEncoding.Default),
				ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = filename + ".xml"
			});

						var emails = email.Split (',');
						var emailRecipient = emails [0];
						var emailsCC = WebConfig.DefaultEmailCC.Concat (emails.Skip (1));

			SendEmailWithAttachments (WebConfig.DefaultSender, emailRecipient, emailsCC, subject, message, attachments);

			return PartialView ("_SendEmailSuccesful");
		}

		public JsonResult Terms ()
		{
			var items = new ArrayList {
				new { value = (int) PaymentTerms.Immediate, text = Resources.SinglePayment },
				new { value = (int) PaymentTerms.NetD, text = Resources.InstallmentPayments }
			};

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult AllPaymentMethods ()
		{
			var items = new ArrayList {
				new { value = (int) PaymentMethod.ToBeDefined, text = PaymentMethod.ToBeDefined.GetDisplayName () },
				new { value = (int) PaymentMethod.Cash, text = PaymentMethod.Cash.GetDisplayName () },
				new { value = (int) PaymentMethod.Check, text = PaymentMethod.Check.GetDisplayName () },
				new { value = (int) PaymentMethod.EFT, text = PaymentMethod.EFT.GetDisplayName () },
				new { value = (int) PaymentMethod.CreditCard, text = PaymentMethod.CreditCard.GetDisplayName () },
				new { value = (int) PaymentMethod.ElectronicPurse, text = PaymentMethod.ElectronicPurse.GetDisplayName () },
				new { value = (int) PaymentMethod.ElectronicMoney, text = PaymentMethod.ElectronicMoney.GetDisplayName () },
				new { value = (int) PaymentMethod.FoodVouchers, text = PaymentMethod.FoodVouchers.GetDisplayName () },
				new { value = (int) PaymentMethod.DebitCard, text = PaymentMethod.DebitCard.GetDisplayName () },
				new { value = (int) PaymentMethod.ServiceCard, text = PaymentMethod.ServiceCard.GetDisplayName () },
                new { value = (int) PaymentMethod.AdvancePayments, text = PaymentMethod.AdvancePayments.GetDisplayName () }
            };

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult PaymentMethods ()
		{
			var items = new ArrayList {
				new { value = (int) PaymentMethod.Cash, text = PaymentMethod.Cash.GetDisplayName () },
				new { value = (int) PaymentMethod.Check, text = PaymentMethod.Check.GetDisplayName () },
				new { value = (int) PaymentMethod.EFT, text = PaymentMethod.EFT.GetDisplayName () },
				new { value = (int) PaymentMethod.CreditCard, text = PaymentMethod.CreditCard.GetDisplayName () },
				new { value = (int) PaymentMethod.ElectronicPurse, text = PaymentMethod.ElectronicPurse.GetDisplayName () },
				new { value = (int) PaymentMethod.ElectronicMoney, text = PaymentMethod.ElectronicMoney.GetDisplayName () },
				new { value = (int) PaymentMethod.FoodVouchers, text = PaymentMethod.FoodVouchers.GetDisplayName () },
				new { value = (int) PaymentMethod.DebitCard, text = PaymentMethod.DebitCard.GetDisplayName () },
				new { value = (int) PaymentMethod.ServiceCard, text = PaymentMethod.ServiceCard.GetDisplayName () }
			};

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Usages ()
		{
			var query = from x in SatCfdiUsage.Queryable
				    select new { value = x.Id, text = x.Description };

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult GetSalesOrdersDetails (int id, DateRange datetime)
		{

			var sales_orders_details = SalesOrderDetail.Queryable.Where (x => !x.SalesOrder.IsCancelled && x.SalesOrder.IsCompleted && !FiscalDocumentDetailSalesOrderDetail.Queryable.Any (y => y.SalesOrderDetail == x)
										 && x.SalesOrder.Customer.Id == WebConfig.DefaultCustomer && x.SalesOrder.CreationTime > datetime.StartDate && x.SalesOrder.CreationTime < datetime.EndDate).ToList ();
			ViewBag.items = sales_orders_details;
			ViewBag.id = id;
			return PartialView ("_SalesOrderSelector");
		}

		[HttpPost]
		public JsonResult AddSalesOrderDetail (int id, int sales_order_detail)
		{

			var invoice = FiscalDocument.Find (id);

			if (invoice == null || invoice.Type != FiscalDocumentType.SalesSummaryInvoice || FiscalDocumentDetailSalesOrderDetail.Queryable.Any(x => x.SalesOrderDetail.Id == sales_order_detail)) {
				return Json(null, JsonRequestBehavior.AllowGet);
			}

			var detail = SalesOrderDetail.Find (sales_order_detail);		
			FiscalDocumentDetail item = invoice.Details.SingleOrDefault (x => x.Product == detail.Product);

			using (var scope = new TransactionScope ()) {


				if (item == null) {
					item = new FiscalDocumentDetail {
						Product = detail.Product,
						ProductName = detail.Product.Name,
						ProductCode = detail.Product.Code,
						UnitOfMeasurement = detail.Product.UnitOfMeasurement,
						UnitOfMeasurementName = detail.Product.UnitOfMeasurement.Name,
						ProductService = detail.Product.ProductService,
						Document = invoice,
						Quantity = 1
					};
					item.Create ();
				}

				var fiscal_detail =new FiscalDocumentDetailSalesOrderDetail { FiscalDocumentDetail = item, SalesOrderDetail = detail };
				fiscal_detail.Create ();
				var items =	FiscalDocumentDetailSalesOrderDetail.Queryable.Where (x => x.FiscalDocumentDetail == item).ToList();
				item.Price = items.Sum (y => (decimal?) y.SalesOrderDetail.Subtotal) ?? 0;
				item.Update ();
				

				return Json (new {
					id = fiscal_detail.Id,
					detail = true
				});

			}

		}

		public ActionResult ItemsSalesOrderDetails (int id)
		{
			var list = FiscalDocumentDetailSalesOrderDetail.Queryable.Where (x => x.FiscalDocumentDetail.Document.Id == id).ToList ();
			return PartialView ("_ItemsSalesOrders", list);
		}

		public ActionResult ItemSalesOrderDetail (int id)
		{
			var entity = FiscalDocumentDetailSalesOrderDetail.Find (id);
			return PartialView ("_ItemEditorViewSalesOrder", entity);
		}

		public ActionResult DocumentDetails (int id) {
			var entity = FiscalDocument.Find (id);
			return PartialView ("_DocumentDetails", entity.Details);
		}

		[HttpPost]
		public ActionResult RemoveItemSalesOrderDetail (int id)
		{
			var entity = FiscalDocumentDetailSalesOrderDetail.Find (id);
			var invoice_detail = entity.FiscalDocumentDetail;
			var exist = invoice_detail.Total > 0;

			if (invoice_detail.Document.IsCancelled || invoice_detail.Document.IsCompleted) {
					Response.StatusCode = 400;
					return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}			


			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
				var orders = FiscalDocumentDetailSalesOrderDetail.Queryable.Where (x => x.FiscalDocumentDetail == entity.FiscalDocumentDetail).ToList();
				invoice_detail.Price = orders.Sum(x => x.SalesOrderDetail.Subtotal);
				exist = invoice_detail.Price > 0;
				if (invoice_detail.Price > 0) {
					invoice_detail.UpdateAndFlush ();
				} else {
					invoice_detail.DeleteAndFlush ();
				}
			}

			return Json (new { id = id, result = true, exist = exist });
		}

	}
}
