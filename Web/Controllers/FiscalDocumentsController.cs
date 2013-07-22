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
			IQueryable<FiscalDocument> qry;

            if (string.IsNullOrEmpty (search.Pattern)) {
                qry = from x in FiscalDocument.Queryable
                      where !(!x.IsCompleted && x.IsCancelled)
                      orderby x.IsCompleted, x.Issued descending
                      select x;
            } else {
                qry = from x in FiscalDocument.Queryable
                      where !(!x.IsCompleted && x.IsCancelled) &&
                            x.Customer.Name.Contains(search.Pattern)
                      orderby x.IsCompleted, x.Issued descending
                      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

            return search;
        }
		
        public ViewResult New()
        {
			var item = new FiscalDocument {
				Issuer = Taxpayer.TryFind (Configuration.DefaultIssuer),
				Batch = Configuration.DefaultBatch,
				ExchangeRate = 1
			};

			return View (item);
        }

        [HttpPost]
		public ActionResult New (FiscalDocument item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.Recipient = CustomerTaxpayer.TryFind (item.RecipientId);
			
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
			
			if (item.PaymentMethod == PaymentMethod.Unidentified ||
			    item.PaymentMethod == PaymentMethod.Cash) {
				item.PaymentReference = null;
			} else if (string.IsNullOrEmpty(item.PaymentReference)) {
				item.PaymentReference = Resources.Unidentified;
			}

			var batch = item.Issuer.Batches.First (x => x.Batch == item.Batch);

			// Fiscal doc's info
			item.Type = batch.Type;
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Updater = item.Creator;
			item.Currency = Configuration.DefaultCurrency;
			item.ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate();

			using (var scope = new TransactionScope()) {
				item.IssuerAddress.Create ();
				item.RecipientAddress.Create ();
				item.IssuedAt.Create ();
				item.CreateAndFlush ();
			}

			if (item.Id == 0) {
				return View ("UnknownError");
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}
		
        public ActionResult Edit (int id)
        {
            FiscalDocument item = FiscalDocument.Find(id);
			
            if (Request.IsAjaxRequest())
                return PartialView("_MasterEditView", item);
            else
                return View (item);
        }

        [HttpPost]
		public ActionResult Edit (FiscalDocument item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.Recipient = CustomerTaxpayer.TryFind (item.RecipientId);

			if (!ModelState.IsValid) {
				return View (item);
			}

			var document = FiscalDocument.Find (item.Id);
			
			// Issuer
			item.IssuerName = item.Issuer.Name;
			item.IssuerRegime = item.Issuer.Regime;
			item.IssuerAddress = item.Issuer.Address;
			
			// Recipient
			item.RecipientName = item.Recipient.Name;
			item.RecipientAddress = item.Recipient.Address;

			if (item.PaymentMethod == PaymentMethod.Unidentified ||
			    item.PaymentMethod == PaymentMethod.Cash) {
				item.PaymentReference = null;
			} else if (string.IsNullOrEmpty (item.PaymentReference)) {
				item.PaymentReference = Resources.Unidentified;
			}

			var batch = item.Issuer.Batches.First (x => x.Batch == item.Batch);
			bool changed = document.Currency != item.Currency || document.ExchangeRate != item.ExchangeRate;

			// updated info
			document.Type = batch.Type;
			document.Batch = item.Batch;
			document.Issuer = item.Issuer;
			document.Customer = item.Customer;
			document.ModificationTime = DateTime.Now;
			document.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			document.PaymentMethod = item.PaymentMethod;
			document.PaymentReference = item.PaymentReference;
			document.Currency = item.Currency;
			document.ExchangeRate = item.ExchangeRate;
			document.Reference = item.Reference;

			using (var scope = new TransactionScope()) {
				document.IssuerAddress.Update ();
				document.RecipientAddress.Update ();

				if (changed) {
					foreach (var x in document.Details) {
						x.Currency = document.Currency;
						x.ExchangeRate = document.ExchangeRate;
						x.Update ();
					}
				}

				document.UpdateAndFlush ();
			}

			return PartialView ("_MasterView", document);
		}
		
		public ActionResult DiscardChanges (int id)
		{
			return PartialView ("_MasterView", FiscalDocument.TryFind (id));
		}

        public ViewResult Details (int id)
		{
			FiscalDocument item = FiscalDocument.Find (id);

			return View (item);
		}
		
		public ViewResult Print (int id)
		{
			var item = FiscalDocument.Find (id);

			if (item.Version == 2.0m) {
				return View ("Printv20", item);
			} else {
				return View ("Printv22", item);
			}
		}

        [HttpPost]
		public ActionResult Sign (int id)
		{
			FiscalDocument item = FiscalDocument.Find (id);
			
			if (!item.IsCompleted)
				return RedirectToAction ("Index");

			var doc = CFDHelpers.SignCFD (item);
			
			item.OriginalString = doc.ToString ();
			item.DigitalSeal = doc.sello;
			
			// save to database
			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

			// save to filesystem
			var filename = string.Format (Resources.Format_FiscalDocumentPath,
	                                      Server.MapPath (Configuration.FiscalFilesPath),
                                          item.Issuer.Id, item.Batch, item.Serial);
			System.IO.File.WriteAllText (filename, doc.ToXmlString ());

			return RedirectToAction ("Index");
		}

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
			item.ApprovalNumber = batch.ApprovalNumber;
			item.ApprovalYear = batch.ApprovalYear;
			item.CertificateNumber = item.Issuer.Certificates.Single (x => x.IsActive).Id;
			
			var dt = DateTime.Now;
			item.Issued = new DateTime (dt.Year, dt.Month, dt.Day,
			                            dt.Hour, dt.Minute, dt.Second,
			                            DateTimeKind.Unspecified);

			dynamic doc = CFDHelpers.SignCFD (item);

			item.OriginalString = doc.ToString ();
			item.DigitalSeal = doc.sello;
			item.Version = Convert.ToDecimal (doc.version);

			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;
			item.IsCompleted = true;
			
			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();
			}
			
			// save to filesystem
			var filename = string.Format (Resources.Format_FiscalDocumentPath,
	                                      Server.MapPath (Configuration.FiscalFilesPath),
                                          item.Issuer.Id, item.Batch, item.Serial);
			System.IO.File.WriteAllText (filename, doc.ToXmlString ());

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
			var item = FiscalDocument.Find (id);
			var doc = CFDHelpers.InvoiceToCFD (item);
			var result = new FileStreamResult (doc.ToXmlStream (), "text/xml");
			
			result.FileDownloadName = string.Format (Resources.Format_FiscalDocumentName,
			                                         item.Issuer.Id, item.Batch, item.Serial);
			
			return result;
		}
		
		public ActionResult Report (string taxpayer, int year, int month)
		{
			var ms = new MemoryStream ();
			var result = new FileStreamResult (ms, "text/plain");
			StreamWriter sw = new StreamWriter (ms, Encoding.UTF8);
			var start = new DateTime (year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
			var end = start.AddMonths (1);
			var qry = from x in FiscalDocument.Queryable
					  where x.IsCompleted && ((x.Issued >= start && x.Issued < end) ||
				(x.IsCancelled && x.CancellationDate >= start && x.CancellationDate < end))
					  select x;
			
			foreach (var row in CFDHelpers.MonthlyReport(qry.ToList())) {
				sw.WriteLine (row);
			}
			
			sw.Flush ();
			ms.Seek (0, SeekOrigin.Begin);

			result.FileDownloadName = string.Format (Resources.Format_FiscalReportName,
			                                         taxpayer, year, month);
			
			return result;
		}
		
		public ActionResult GetDetail (int id)
		{
			return PartialView ("_DetailEditView", FiscalDocumentDetail.Find (id));
		}

        [HttpPost]
		public JsonResult AddDetail (int id, int product)
		{
			var p = Product.Find (product);
			var f = FiscalDocument.Find (id);
			int pl = f.Customer.PriceList.Id;
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x.Value).SingleOrDefault();

			var item = new FiscalDocumentDetail {
                Document = FiscalDocument.Find (id),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
				UnitOfMeasurement = p.UnitOfMeasurement,
                Discount = 0,
                TaxRate = p.TaxRate,
				IsTaxIncluded = false,
                Quantity = 1,
				Price = price,
				ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate(),
				Currency = Configuration.DefaultCurrency
            };

			if (p.IsTaxIncluded) {
				item.Price = item.Price / (1 + item.TaxRate);
			}

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			return Json (new { id = item.Id });
		}
		
		[HttpPost]
		public JsonResult EditDetailProductName (int id, string value)
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
		public JsonResult EditDetailProductCode (int id, string value)
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
		public JsonResult EditDetailUM (int id, string value)
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
		public JsonResult EditDetailQuantity (int id, decimal value)
		{
			var detail = FiscalDocumentDetail.Find (id);

			if (detail.OrderDetail != null) {
				decimal max_qty = detail.Quantity + GetInvoiceableQuantity (detail.OrderDetail.Id);

				if (max_qty < value)
					value =  max_qty;
			}

			if (value > 0) {
				detail.Quantity = value;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { 
				id = detail.Id,
				value = detail.FormattedValueFor (x => x.Quantity),
				subtotal = detail.FormattedValueFor (x => x.Subtotal), 
				subtotal2 = detail.FormattedValueFor (x => x.SubtotalEx)
			});
		}

		[HttpPost]
		public JsonResult EditDetailPrice (int id, string value)
		{
			var detail = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.Trim (),
			                            System.Globalization.NumberStyles.Currency,
			                            null, out val);

			if (success && val >= 0) {
				detail.Price = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new {
				id = detail.Id,
				value = detail.FormattedValueFor (x => x.Price),
				subtotal = detail.FormattedValueFor (x => x.Subtotal), 
				subtotal2 = detail.FormattedValueFor (x => x.SubtotalEx)
			});
		}

		[HttpPost]
		public JsonResult EditDetailDiscount (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				detail.Discount = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { 
				id = detail.Id,
				value = detail.FormattedValueFor (x => x.Discount),
				subtotal = detail.FormattedValueFor (x => x.Subtotal), 
				subtotal2 = detail.FormattedValueFor (x => x.SubtotalEx)
			});
		}
		
		[HttpPost]
		public JsonResult EditDetailTaxRate (int id, string value)
		{
			var detail = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;
			
			if (success && (val == 0m || val == Configuration.VAT)) {
				detail.TaxRate = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { 
				id = detail.Id,
				value = detail.FormattedValueFor (x => x.TaxRate)
			});
		}

		public ActionResult GetDetails (int id)
		{
			var item = FiscalDocument.Find (id);
			return PartialView ("_ItemsBlock", item.Details);
		}
		
		[HttpPost]
		public JsonResult AddOrder (int id, int order)
		{
			var doc_entity = FiscalDocument.Find (id);
			var order_entity = SalesOrder.Find (order);
			var count = 0;

			using (var scope = new TransactionScope()) {
				foreach(var x in order_entity.Details) {
					if (!x.Product.IsInvoiceable)
						continue;
					
					decimal max_qty = GetInvoiceableQuantity (x.Id);
					
					if (max_qty <= 0)
						continue;
					
					var item = new FiscalDocumentDetail {
						Document = doc_entity,
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
						ExchangeRate = doc_entity.ExchangeRate,
						Currency = doc_entity.Currency
					};

					if (x.IsTaxIncluded) {
						item.Price = item.Price / (1 + item.TaxRate);
					}

					if (x.Currency != doc_entity.Currency) {
						item.Price = x.Price * x.ExchangeRate;
					}

					item.Create ();
					count++;
				}
			}
			
			return Json (new { id = id, result = count });
		}

		public ActionResult GetBatchSelector (string id)
		{
			var qry = (from x in TaxpayerBatch.Queryable
					  where x.Taxpayer.Id == id
					  select x.Batch).Distinct ();
			var list = from x in qry.ToList ()
					   select new SelectListItem {
							Value = x,
							Text = x
					   };

			ViewBag.Items = list.ToList ();
			
			return PartialView ("_BatchSelector");
		}
		
        public ActionResult GetTotals (int id)
        {
            var order = FiscalDocument.Find(id);
            return PartialView ("_Totals", order);
        }

        [HttpPost]
        public JsonResult RemoveDetail (int id)
		{
			var item = FiscalDocumentDetail.Find (id);

			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}
		
		public JsonResult GetSuggestions (int id, string pattern)
		{
			int pl = FiscalDocument.Queryable.Where(x => x.Id == id)
					.Select(x => x.Customer.PriceList.Id)
					.Single();
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
						price = x.Value
					};
			
			return Json (qry.Take(15), JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetOrders (string pattern)
		{
			int id = 0;
			int.TryParse(pattern, out id);
			var qry = from x in SalesOrder.Queryable
					  where x.IsCompleted && !x.IsCancelled &&
						    x.IsPaid && x.Id == id
					  select new { id = x.Id, name = string.Format("{0:00000} ({1})", x.Id, x.Date) };
			
			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		decimal GetInvoiceableQuantity (int id)
		{
			var item = SalesOrderDetail.Find (id);
			decimal quantity = item.Quantity;
			string sql = @"SELECT IFNULL(SUM(d.quantity),0) quantity
                           FROM fiscal_document_detail d INNER JOIN fiscal_document m ON d.document = m.fiscal_document_id
                           WHERE m.cancelled = 0 AND d.order_detail = :detail ";
			
			IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<CustomerReturnDetail>.Execute(
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
		
		// FIXME: ugly performance
		public ViewResult Reports ()
		{
			var items = (from x in FiscalDocument.Queryable
			             where x.Issuer.Scheme == FiscalScheme.CFD && 
		             			x.IsCompleted
			             select new FiscalReport {
							TaxpayerId = x.Issuer.Id,
							TaxpayerName = x.Issuer.Name,
							Month = x.Issued.Value.Month,
							Year = x.Issued.Value.Year
						}).ToList().Distinct ()
						.OrderByDescending (x => x.Year).OrderByDescending (x => x.Month);
			
			return View (items.ToList ());
		}
    }
}
