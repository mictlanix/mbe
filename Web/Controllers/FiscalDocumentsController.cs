// 
// InvoicesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2012 Eddy Zavaleta, Mictlanix, and contributors.
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
			
			var qry = from x in FiscalDocument.Queryable
					  where !(!x.IsCompleted && x.IsCancelled)
					  orderby x.IsCompleted, x.Issued descending
                      select x;

            Search<FiscalDocument> search = new Search<FiscalDocument>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
		}


        [HttpPost]
        public ActionResult Index(Search<FiscalDocument> search)
        {
            if (ModelState.IsValid) {
                search = GetFiscalDocuments(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<FiscalDocument> GetFiscalDocuments(Search<FiscalDocument> search)
        {
            if (search.Pattern == null) {
                var qry = from x in FiscalDocument.Queryable
                          where !(!x.IsCompleted && x.IsCancelled)
                          orderby x.IsCompleted, x.Issued descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            } else {
                var qry = from x in FiscalDocument.Queryable
                          where !(!x.IsCompleted && x.IsCancelled) &&
                          x.Customer.Name.Contains(search.Pattern)
                          orderby x.IsCompleted, x.Issued descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
        }
		
        public ViewResult Reports ()
		{
			// FIXME: ugly performance
			var items = (from x in FiscalDocument.Queryable
			             where x.IsCompleted
						 select new FiscalReport {
							  TaxpayerId = x.Issuer.Id,
							  TaxpayerName = x.Issuer.Name,
							  Month = x.Issued.Value.Month,
							  Year = x.Issued.Value.Year
						  }).ToList().Distinct ()
						.OrderByDescending (x => x.Year)
						.OrderByDescending (x => x.Month);

			return View (items.ToList ());
		}
		
        public ViewResult New()
        {
            return View();
        }

        [HttpPost]
		public ActionResult New (FiscalDocument item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.BillTo = Address.TryFind (item.BillToId);
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			if (item.PaymentMethod == PaymentMethod.Unidentified ||
				item.PaymentMethod == PaymentMethod.Cash) {
				item.PaymentReference = null;
			} else if (string.IsNullOrEmpty(item.PaymentReference)) {
				item.PaymentReference = Resources.Unidentified;
			}

			var batch = item.Issuer.Documents.First (x => x.Batch == item.Batch);

			// Fiscal doc's info
			item.Type = batch.Type;
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Updater = item.Creator;
			item.Store = Configuration.Store;
			item.IssuedLocation = item.Store.Location;

			// Address info
			item.BillTo = new Address (item.BillTo);
			item.IssuedFrom = new Address (item.Issuer.Address);
			item.IssuedAt = new Address (item.Store.Address);

			item.IssuedFrom.TaxpayerRegime = item.Issuer.Regime;

			using (var scope = new TransactionScope()) {
				item.BillTo.Create ();
				item.IssuedFrom.Create ();
				item.IssuedAt.Create ();
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New FiscalDocument [Id = {0}]", item.Id);

			if (item.Id == 0) {
				return View ("UnknownError");
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}
		
        public ActionResult Edit(int id)
        {
            FiscalDocument item = FiscalDocument.Find(id);
			
            if (Request.IsAjaxRequest())
                return PartialView("_MasterEditView", item);
            else
                return View(item);
        }

        [HttpPost]
		public ActionResult Edit (FiscalDocument item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.BillTo = Address.TryFind (item.BillToId);

			if (!ModelState.IsValid) {
				return View (item);
			}
			
			if (item.PaymentMethod == PaymentMethod.Unidentified ||
			    item.PaymentMethod == PaymentMethod.Cash) {
				item.PaymentReference = null;
			} else if (string.IsNullOrEmpty(item.PaymentReference)) {
				item.PaymentReference = Resources.Unidentified;
			}
			
			var document = FiscalDocument.Find (item.Id);
			var batch = item.Issuer.Documents.First (x => x.Batch == item.Batch);

			// updated info
			document.Type = batch.Type;
			document.Batch = item.Batch;
			document.Issuer = item.Issuer;
			document.Customer = item.Customer;
			document.ModificationTime = DateTime.Now;
			document.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			document.PaymentMethod = item.PaymentMethod;
			document.PaymentReference = item.PaymentReference;

			// Address info
			document.BillTo.Copy (item.BillTo);
			document.IssuedFrom.Copy (document.Issuer.Address);
			document.IssuedFrom.TaxpayerRegime = document.Issuer.Regime;

			using (var scope = new TransactionScope()) {
				document.BillTo.Update ();
				document.IssuedFrom.Update ();
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

			var doc = CFDv2Helpers.SignCFD (item);
			
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
			
			int serial = (from x in FiscalDocument.Queryable
                          where x.Batch == item.Batch
                          select x.Serial).Max ().GetValueOrDefault () + 1;

			var batch = (from x in item.Issuer.Documents
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
			item.CertificateNumber = item.Issuer.CertificateNumber;

			dynamic doc = CFDv2Helpers.IssueCFD (item);

			item.Issued = doc.fecha;
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
			var doc = CFDv2Helpers.InvoiceToCFD (item);
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
			
			foreach (var row in CFDv2Helpers.MonthlyReport(qry.ToList())) {
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
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product &&
			             x.List.Id == f.Customer.PriceList.Id
			             select x.Price).SingleOrDefault();

			var item = new FiscalDocumentDetail {
                Document = FiscalDocument.Find (id),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
				UnitOfMeasurement = p.UnitOfMeasurement,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
				Price = price
            };

			if (p.IsTaxIncluded) {
				item.Price = item.Price / (1m + item.TaxRate);
			}
			
			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New FiscalDocumentDetail [Id = {0}]", item.Id);

			return Json (new { id = item.Id });
		}
		
		[HttpPost]
		public JsonResult EditDetailProductName (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.ProductName = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = id, value = detail.ProductName });
		}
		
		[HttpPost]
		public JsonResult EditDetailProductCode (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.ProductCode = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = id, value = detail.ProductCode });
		}
		
		[HttpPost]
		public JsonResult EditDetailUM (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.UnitOfMeasurement = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = id, value = detail.UnitOfMeasurement });
		}

		[HttpPost]
		public JsonResult EditDetailPrice (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
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

			return Json (new {id = id, value = detail.Price.ToString ("C4"), total = detail.Total.ToString ("c") });
		}
		
		[HttpPost]
		public JsonResult EditDetailTaxRate (int id, string value)
		{
			FiscalDocumentDetail detail = FiscalDocumentDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;
			
			// FIXME: Allow to configure tax rates
			if (success && (val == 0m || val == 0.16m)) {
				detail.TaxRate = val;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
			}

			return Json (new { id = id, value = detail.TaxRate.ToString ("p") });
		}
		
        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal value)
        {
            FiscalDocumentDetail detail = FiscalDocumentDetail.Find(id);

            if (value > 0)
            {
                detail.Quantity = value;

				using (var scope = new TransactionScope()) {
					detail.Update ();
				}
            }

            return Json(new { id = id, value = detail.Quantity, total = detail.Total.ToString("c") });
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

			return Json (new { id = id, value = detail.Discount.ToString ("p"), total = detail.Total.ToString ("c") });
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
					
					decimal quantity = GetInvoiceableQuantity (x.Id);
					
					if (quantity == 0)
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
						Quantity = quantity,
						Price = x.Price
					};
					
					if (x.Product.IsTaxIncluded) {
						item.Price = item.Price / (1m + item.TaxRate);
					}
					
					item.Create ();
					count++;
				}
			}
			
			return Json (new { id = id, result = count });
		}

		public ActionResult GetBatchSelector (string id)
		{
			var qry = from x in TaxpayerDocument.Queryable
					  where x.Taxpayer.Id == id
					  select new { x.Batch, x.Type };
			var list = from x in qry.ToList ()
					   select new SelectListItem {
							Value = x.Batch,
							Text = string.Format ("{0} - {1}", x.Batch, x.Type.GetDisplayName ())
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
					where x.List.Id == pl ||
						x.Product.Name.Contains (pattern) ||
						x.Product.Code.Contains (pattern) ||
						x.Product.SKU.Contains (pattern)
					orderby x.Product.Name
					select new {
						id = x.Product.Id,
						name = x.Product.Name, 
						code = x.Product.Code, 
						sku = x.Product.SKU, 
						url = Url.Content(x.Product.Photo),
						price = x.Price
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

		decimal GetInvoiceableQuantity(int id)
		{
			var item = SalesOrderDetail.Find (id);
			string sql = @"SELECT IFNULL(SUM(d.quantity),0) quantity
                           FROM fiscal_document_detail d INNER JOIN fiscal_document m ON d.document = m.fiscal_document_id
                           WHERE m.completed <> 0 AND m.cancelled = 0 AND d.order_detail = :detail ";
			
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
				return item.Quantity - quantities[0];
			}
			
			return item.Quantity;
		}
    }
}
