// 
// InvoicesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class InvoicesController : Controller
    {
        public ViewResult Index()
        {
            var qry = from x in SalesInvoice.Queryable
					  orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        public ViewResult New()
        {
            return View();
        }

        [HttpPost]
        public ActionResult New (SalesInvoice item)
		{
			item.Customer = Customer.TryFind (item.CustomerId);
			item.BillTo = Address.TryFind (item.BillToId);
			item.Issuer = Taxpayer.TryFind (item.IssuerId);
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			// Invoice creation info
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Updater = item.Creator;
			
			// Bill to info
			item.BillToTaxId = item.BillTo.TaxpayerId;
			item.BillToName = item.BillTo.TaxpayerName;
			item.Street = item.BillTo.Street;
			item.ExteriorNumber = item.BillTo.ExteriorNumber;
			item.InteriorNumber = item.BillTo.InteriorNumber;
			item.Neighborhood = item.BillTo.Neighborhood;
			item.Borough = item.BillTo.Borough;
			item.State = item.BillTo.State;
			item.Country = item.BillTo.Country;
			item.ZipCode = item.BillTo.ZipCode;
			
			using (var session = new SessionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New SalesInvoice [Id = {0}]", item.Id);

			if (item.Id == 0) {
				return View ("UnknownError");
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ViewResult Details(int id)
        {
            SalesInvoice order = SalesInvoice.Find(id);

            return View(order);
        }

        public ActionResult Edit(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            if (Request.IsAjaxRequest())
                return PartialView("_Edit", item);
            else
                return View(item);
        }

        [HttpPost]
        public ActionResult Edit (SalesInvoice item)
		{
			var invoice = SalesInvoice.Find (item.Id);
			
			invoice.Customer = Customer.TryFind (item.CustomerId);
			invoice.BillTo = Address.TryFind (item.BillToId);
			invoice.Issuer = Taxpayer.TryFind (item.IssuerId);
			
			item.ModificationTime = DateTime.Now;
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			
			// Bill to info
			invoice.BillToTaxId = invoice.BillTo.TaxpayerId;
			invoice.BillToName = invoice.BillTo.TaxpayerName;
			invoice.Street = invoice.BillTo.Street;
			invoice.ExteriorNumber = invoice.BillTo.ExteriorNumber;
			invoice.InteriorNumber = invoice.BillTo.InteriorNumber;
			invoice.Neighborhood = invoice.BillTo.Neighborhood;
			invoice.Borough = invoice.BillTo.Borough;
			invoice.State = invoice.BillTo.State;
			invoice.Country = invoice.BillTo.Country;
			invoice.ZipCode = invoice.BillTo.ZipCode;
			
			invoice.Save ();

			return PartialView ("_MasterInfo", invoice);
		}

        [HttpPost]
        public ActionResult Confirm(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            item.IsCompleted = true;
            item.Save();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Cancel(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("Index");
        }
		
        public ViewResult Print(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            if(item.IsCompleted)
                return View("_SalesTicket", item);
            else
                return View("_SalesNote", item);
        }
		
		public ActionResult GetDetail (int id)
		{
			return PartialView ("_Detail", SalesInvoiceDetail.Find (id));
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
			                            System.Globalization.NumberStyles.AllowCurrencySymbol,
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
                    url = x.Photo,
                    price = (pl == 1 ? x.Price1 : (pl == 2 ? x.Price2 : (pl == 3 ? x.Price3 : x.Price4))).ToString ("c")
                };
                
				items.Add (item);
			}

			return Json (items, JsonRequestBehavior.AllowGet);
		}
    }
}
