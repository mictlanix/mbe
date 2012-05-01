// 
// QuotationsController.cs
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
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class QuotationsController : Controller
    {
        //
        // GET: /Quotations/

        public ViewResult Index ()
		{
			var item = Configuration.Store;
			
			if (item == null) {
				return View ("InvalidStore");
			}
			
			var qry = from x in SalesQuote.Queryable
                      where x.Store.Id == item.Id
                      select x;

			return View (qry.ToList ());
		}

        // GET: /Quotations/Print/

        public ViewResult Print (int id)
		{
			return View ("_SalesTicket", SalesQuote.TryFind (id));
        }

        // GET: /Quotations/Details/

        public ViewResult Details(int id)
        {
            return View(SalesQuote.Find(id));
        }

        //
        // GET: /Quotations/New

        public ViewResult New ()
		{
			var item = Configuration.Store;
			
			if (item == null) {
				return View ("InvalidStore");
			}

            return View(new SalesQuote { CustomerId = 1, Customer = Customer.Find(1) });
        }

        [HttpPost]
		public ActionResult New (SalesQuote item)
		{
			item.Store = Configuration.Store;
			
			if (item.Store == null) {
				return View ("InvalidStore");
			}
			
			// Store and Serial
			item.Store = item.Store;
			try {
				item.Serial = (from x in SalesQuote.Queryable
	            			   where x.Store.Id == item.Store.Id
	                      	   select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}
			
			item.Customer = Customer.Find (item.CustomerId);
			item.SalesPerson = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Date = DateTime.Now;
			//FIXME: choose date from UI
			item.DueDate = item.Date.AddDays (30);

			using (var session = new SessionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New SalesQuote [Id = {0}]", item.Id);

			if (item.Id == 0) {
				return View ("UnknownError");
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ActionResult Edit (int id)
		{
			SalesQuote item = SalesQuote.Find (id);

			if (Request.IsAjaxRequest ())
				return PartialView ("_MasterEditView", item);
            else
                return View(item);
        }

		public ActionResult DiscardChanges (int id)
		{
			return PartialView ("_MasterView", SalesQuote.TryFind (id));
		}
		
        [HttpPost]
		public ActionResult Edit (SalesQuote item)
		{
			item.Customer = Customer.TryFind (item.CustomerId);

			if (!ModelState.IsValid) {
				return PartialView ("_MasterEditView", item);
			}
			
			var quote = SalesQuote.Find (item.Id);
			quote.DueDate = item.DueDate;
			quote.Save ();

			return PartialView ("_MasterView", quote);
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            var p = Product.Find(product);

            var item = new SalesQuoteDetail
            {
                SalesQuote = SalesQuote.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            switch (item.SalesQuote.Customer.PriceList.Id)
            {
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

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New SalesQuoteDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SalesQuoteDetail detail = SalesQuoteDetail.Find(id);

            if (quantity > 0)
            {
                detail.Quantity = quantity;
                detail.Save();
            }

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount(int id, string value)
        {
            SalesQuoteDetail detail = SalesQuoteDetail.Find(id);
            bool success;
            decimal discount;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1)
            {
                detail.Discount = discount;
                detail.Save();
            }

            return Json(new { id = id, discount = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
        }

        public ActionResult GetTotals(int id)
        {
            var order = SalesQuote.Find(id);
            return PartialView("_Totals", order);
        }

        public ActionResult GetDetail (int id)
		{
			return PartialView ("_DetailEditView", SalesQuoteDetail.Find(id));
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            SalesQuoteDetail item = SalesQuoteDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        [HttpPost]
		public ActionResult Confirm (int id)
		{
			SalesQuote item = SalesQuote.Find (id);

			item.IsCompleted = true;
			item.Save ();

			return RedirectToAction ("Index");
        }

        [HttpPost]
		public ActionResult Cancel (int id)
		{
			SalesQuote item = SalesQuote.Find (id);

			item.IsCancelled = true;
			item.Save ();

			return RedirectToAction ("New");
        }
		
        public JsonResult GetSuggestions (int order, string pattern)
		{
			SalesQuote sales_order = SalesQuote.Find (order);
			int pl = sales_order.Customer.PriceList.Id;
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
                    url = Url.Content(x.Photo),
                    price = (pl == 1 ? x.Price1 : (pl == 2 ? x.Price2 : (pl == 3 ? x.Price3 : x.Price4))).ToString ("c")
                };
                
				items.Add (item);
			}

			return Json (items, JsonRequestBehavior.AllowGet);
		}
    }
}
