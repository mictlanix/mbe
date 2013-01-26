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
					  orderby x.Id descending
                      select x;

            Search<SalesQuote> search = new Search<SalesQuote>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
		}

        [HttpPost]
        public ActionResult Index(Search<SalesQuote> search)
        {
            if (ModelState.IsValid)
            {
                search = GetQuotations(search);
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Index", search);
            }
            else
            {
                return View(search);
            }
        }

        Search<SalesQuote> GetQuotations(Search<SalesQuote> search)
        {
            var item = Configuration.Store;

            if (search.Pattern == null) {
                var qry = from x in SalesQuote.Queryable
                          where x.Store.Id == item.Id
						  orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            } else {
                var qry = from x in SalesQuote.Queryable
                          where x.Store.Id == item.Id &&
                                x.Customer.Name.Contains(search.Pattern)
						  orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
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

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New SalesQuote [Id = {0}]", item.Id);

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        // GET: /Quotations/Details/

        public ViewResult Details(int id)
        {
            return View(SalesQuote.Find(id));
        }

        // GET: /Quotations/Print/

        public ViewResult Print (int id)
		{
			return View (SalesQuote.TryFind (id));
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

			using (var scope = new TransactionScope ()) {
				quote.UpdateAndFlush ();
			}

			return PartialView ("_MasterView", quote);
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            var p = Product.Find (product);
			var q = SalesQuote.Find (order);
			int pl = q.Customer.PriceList.Id;
			var price = (from x in ProductPrice.Queryable
			             where x.Product.Id == product && x.List.Id == pl
			             select x.Price).SingleOrDefault();

            var item = new SalesQuoteDetail {
                SalesQuote = SalesQuote.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
				Price = price
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();
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

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
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

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
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
            var item = SalesQuoteDetail.Find(id);

			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        [HttpPost]
		public ActionResult Confirm (int id)
		{
			SalesQuote item = SalesQuote.Find (id);

			item.IsCompleted = true;
			
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
        }

        [HttpPost]
		public ActionResult Cancel (int id)
		{
			SalesQuote item = SalesQuote.Find (id);

			item.IsCancelled = true;
			
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("New");
        }
		
        public JsonResult GetSuggestions (int order, string pattern)
		{
			SalesQuote sales_order = SalesQuote.Find (order);
			int pl = sales_order.Customer.PriceList.Id;
			
			var qry = from x in ProductPrice.Queryable
					where x.List.Id == pl && (
						x.Product.Name.Contains (pattern) ||
						x.Product.Code.Contains (pattern) ||
						x.Product.SKU.Contains (pattern))
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
    }
}
