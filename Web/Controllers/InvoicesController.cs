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
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class InvoicesController : Controller
    {
        public ViewResult Index ()
		{
			var item = Configuration.Store;
			
			if (item == null) {
				return View ("InvalidStore");
			}
			
			var qry = from x in SalesInvoice.Queryable
					  orderby x.Id descending
                      select x;

			return View (qry.ToList ());
		}

        public ViewResult New()
        {
            return View();
        }

        [HttpPost]
		public ActionResult New (SalesInvoice item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.BillTo = Address.TryFind (item.BillToId);
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			// Invoice creation info
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Updater = item.Creator;
			item.Store = Configuration.Store;
			
			// Address info
			item.BillTo = new Address (item.BillTo);
			item.IssuedFrom = new Address (item.Issuer.Address);
			
			// FIXME: use transaction
			using (var session = new SessionScope()) {
				item.BillTo.CreateAndFlush ();
				item.IssuedFrom.CreateAndFlush ();
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New SalesInvoice [Id = {0}]", item.Id);

			if (item.Id == 0) {
				return View ("UnknownError");
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ViewResult Details (int id)
		{
			SalesInvoice item = SalesInvoice.Find (id);

			return View (item);
		}
		
		public ViewResult Print (int id)
		{
			return View (SalesInvoice.Find (id));
		}
		
		public ActionResult GetMaster (int id)
		{
			return PartialView ("_MasterView", SalesInvoice.TryFind (id));
		}
		
        public ActionResult Edit(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);
			
            if (Request.IsAjaxRequest())
                return PartialView("_MasterEditView", item);
            else
                return View(item);
        }

        [HttpPost]
        public ActionResult Edit (SalesInvoice item)
		{
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			item.Customer = Customer.TryFind (item.CustomerId);
			item.BillTo = Address.TryFind (item.BillToId);
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			var invoice = SalesInvoice.Find (item.Id);
			
			invoice.Issuer = item.Issuer;
			invoice.Customer = item.Customer;
			invoice.Batch = item.Batch;
			invoice.ModificationTime = DateTime.Now;
			invoice.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			
			// Address info
			invoice.BillTo.Copy (item.BillTo);
			invoice.IssuedFrom.Copy (invoice.Issuer.Address);
			
			// FIXME: use transaction
			using (var session = new SessionScope()) {
				invoice.BillTo.Save ();
				invoice.IssuedFrom.Save ();
				invoice.Save ();
			}

			return PartialView ("_MasterView", invoice);
		}

        [HttpPost]
		public ActionResult Confirm (int id)
		{
			SalesInvoice item;
			TaxpayerDocument batch;
			
			using (var session = new SessionScope()) {
				item = SalesInvoice.Find (id);
				batch = item.Issuer.Documents.Single (x => x.Batch == item.Batch);
			}
			
			int serial = (from x in SalesInvoice.Queryable
                          where x.Batch == item.Batch
                          select x.Serial).Max ().GetValueOrDefault () + 1;
			
			item.Serial = serial;
			item.ApprovalNumber = batch.ApprovalNumber;
			item.ApprovalYear = batch.ApprovalYear;
			item.CertificateNumber = item.Issuer.CertificateNumber;
			item.IsCompleted = true;
			
			var doc = CFDv2Helpers.IssueCFD (item);
			
			item.Issued = doc.fecha;
			item.OriginalString = CFDv2Helpers.OriginalString (doc);
			item.DigitalSeal = doc.sello;
			
			// save to database
			item.Save ();
			
			// save to filesystem
			var filename = string.Format (Resources.Format_FiscalDocumentPath,
	                                      Server.MapPath (Configuration.FiscalFilesPath),
                                          item.Issuer.Id, item.Batch, item.Serial);
			var xml_content = CFDv2Helpers.SerializeToXmlString (doc);
			System.IO.File.WriteAllText (filename, xml_content);

			return RedirectToAction ("Index");
		}

        [HttpPost]
		public ActionResult Sign (int id)
		{
			SalesInvoice item = SalesInvoice.Find (id);
			
			if (!item.IsCompleted)
				return RedirectToAction ("Index");

			var doc = CFDv2Helpers.SignCFD (item);
			
			item.OriginalString = CFDv2Helpers.OriginalString (doc);
			item.DigitalSeal = doc.sello;
			
			// save to database
			item.Save ();
			
			// save to filesystem
			var filename = string.Format (Resources.Format_FiscalDocumentPath,
	                                      Server.MapPath (Configuration.FiscalFilesPath),
                                          item.Issuer.Id, item.Batch, item.Serial);
			var xml_content = CFDv2Helpers.SerializeToXmlString (doc);
			System.IO.File.WriteAllText (filename, xml_content);

			return RedirectToAction ("Index");
		}
		
		public ActionResult Download (int id)
		{
			var item = SalesInvoice.Find (id);
			var doc = CFDv2Helpers.InvoiceToCFD (item);
			var stream = CFDv2Helpers.SerializeToXmlStream (doc);
			var result = new FileStreamResult (stream, "text/xml");
			
			result.FileDownloadName = string.Format (Resources.Format_FiscalDocumentName,
			                                         item.Issuer.Id, item.Batch, item.Serial);
			
			return result;
		}
		
        [HttpPost]
        public ActionResult Cancel(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("Index");
        }
		
		public ActionResult GetDetail (int id)
		{
			return PartialView ("_DetailEditView", SalesInvoiceDetail.Find (id));
		}
		
        [HttpPost]
        public JsonResult AddDetail (int invoice, int product)
		{
			var p = Product.Find (product);

			var item = new SalesInvoiceDetail
            {
                Invoice = SalesInvoice.Find (invoice),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
				UnitOfMeasurement = p.UnitOfMeasurement,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };
			
			switch (item.Invoice.Customer.PriceList.Id) {
			case 1:
				item.Price = p.Price1;
				break;
			case 2:
				item.Price = p.Price2;
				break;
			case 3:
				item.Price = p.Price3;
				break;
			case 4:
				item.Price = p.Price4;
				break;
			}
			
			if (p.IsTaxIncluded) {
				item.Price = item.Price / (1m + item.TaxRate);
			}
			
			using (var session = new SessionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New SalesInvoiceDetail [Id = {0}]", item.Id);

			return Json (new { id = item.Id });
		}
		
		[HttpPost]
		public JsonResult EditDetailProductName (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.ProductName = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.ProductName });
		}
		
		[HttpPost]
		public JsonResult EditDetailProductCode (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.ProductCode = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.ProductCode });
		}
		
		[HttpPost]
		public JsonResult EditDetailUM (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			string val = string.Format ("{0}", value).Trim ();
			
			if (val.Length > 0) {
				detail.UnitOfMeasurement = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.UnitOfMeasurement });
		}

		[HttpPost]
		public JsonResult EditDetailPrice (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.Trim (),
			                            System.Globalization.NumberStyles.Currency,
			                            null, out val);

			if (success && val >= 0) {
				detail.Price = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.Price.ToString ("c"), total = detail.Total.ToString ("c") });
		}
		
		[HttpPost]
		public JsonResult EditDetailTaxRate (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;
			
			// FIXME: Allow to configure tax rates
			if (success && (val == 0m || val == 0.16m)) {
				detail.TaxRate = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.TaxRate.ToString ("p") });
		}
		
        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal value)
        {
            SalesInvoiceDetail detail = SalesInvoiceDetail.Find(id);

            if (value > 0)
            {
                detail.Quantity = value;
                detail.Save();
            }

            return Json(new { id = id, value = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount (int id, string value)
		{
			SalesInvoiceDetail detail = SalesInvoiceDetail.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char[] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				detail.Discount = val;
				detail.Save ();
			}

			return Json (new { id = id, value = detail.Discount.ToString ("p"), total = detail.Total.ToString ("c") });
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
		
        public ActionResult GetTotals(int id)
        {
            var order = SalesInvoice.Find(id);
            return PartialView("_Totals", order);
        }

        [HttpPost]
        public JsonResult RemoveDetail (int id)
		{
			SalesInvoiceDetail item = SalesInvoiceDetail.Find (id);
			item.Delete ();
			return Json (new { id = id, result = true });
		}
		
		public JsonResult GetSuggestions (int id, string pattern)
		{
			SalesInvoice invoice = SalesInvoice.Find (id);
			int pl = invoice.Customer.PriceList.Id;
			ArrayList items = new ArrayList (15);

			var qry = from x in Product.Queryable
                      where x.Name.Contains (pattern) ||
                            x.Code.Contains (pattern) ||
                            x.SKU.Contains (pattern)
					  orderby x.Name
                      select x;

			foreach (var x in qry.Take(15)) {
				var item = new { 
                    id = x.Id,
                    name = x.Name, 
                    code = x.Code, 
                    sku = x.SKU, 
                    url = Url.Content (x.Photo),
                    price = (pl == 1 ? x.Price1 : (pl == 2 ? x.Price2 : (pl == 3 ? x.Price3 : x.Price4))).ToString ("c")
                };
                
				items.Add (item);
			}

			return Json (items, JsonRequestBehavior.AllowGet);
		}
    }
}
