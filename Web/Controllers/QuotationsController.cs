// 
// QuotationsController.cs
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
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
    public class QuotationsController : Controller
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

			var search = SearchQuotations (new Search<SalesQuote> {
				Limit = Configuration.PageSize
			});

			return View (search);
		}

        [HttpPost]
        public ActionResult Index(Search<SalesQuote> search)
		{
			if (ModelState.IsValid) {
				search = SearchQuotations (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
        }

		Search<SalesQuote> SearchQuotations (Search<SalesQuote> search)
		{
			IQueryable<SalesQuote> qry;
            var item = Configuration.Store;

            if (search.Pattern == null) {
                qry = from x in SalesQuote.Queryable
                      where x.Store.Id == item.Id
					  orderby x.Id descending
                      select x;
            } else {
                qry = from x in SalesQuote.Queryable
                      where x.Store.Id == item.Id &&
                            x.Customer.Name.Contains(search.Pattern)
					  orderby x.Id descending
                      select x;
			}

			search.Total = qry.Count();
			search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();

            return search;
        }

        public ViewResult New ()
		{
			var item = Configuration.Store;

			if (item == null) {
				return View ("InvalidStore");
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

            return View (new SalesQuote {
				CustomerId = Configuration.DefaultCustomer,
				Customer = Customer.Find (Configuration.DefaultCustomer)
			});
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

			return RedirectToAction ("Edit", new { id = item.Id });
		}

        public ViewResult Details(int id)
        {
            return View(SalesQuote.Find(id));
        }

        public ViewResult Print (int id)
		{
			return View (SalesQuote.TryFind (id));
        }

        public ActionResult Edit (int id)
		{
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

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
			             select x.Value).SingleOrDefault();

            var item = new SalesQuoteDetail {
                SalesQuote = SalesQuote.Find (order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
                Quantity = 1,
				Price = price,
				ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate(),
				Currency = Configuration.DefaultCurrency
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();
            }

            return Json(new { id = item.Id });
		}

		[HttpPost]
		public JsonResult EditDetailPrice (int id, string value)
		{
			var detail = SalesQuoteDetail.Find (id);
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

			return Json (new { id = id, value = detail.Price.ToString ("C4"), total = detail.Total.ToString ("c") });
		}

		[HttpPost]
		public ActionResult EditDetailCurrency (int id, string value)
		{
			var detail = SalesQuoteDetail.Find (id);
			CurrencyCode val;
			bool success;

			success = Enum.TryParse<CurrencyCode> (value.Trim (), out val);

			if (success) {
				decimal rate = CashHelpers.GetTodayExchangeRate (val);

				if (rate == 0) {
					Response.StatusCode = 400;
					return Content (Resources.Message_InvalidExchangeRate);
				}

				detail.Currency = val;
				detail.ExchangeRate = CashHelpers.GetTodayExchangeRate (val);

				using (var scope = new TransactionScope ()) {
					detail.Update ();
				}
			}

			return Json (new { id = id, value = detail.Currency.ToString (), rate = detail.ExchangeRate, total = detail.Total.ToString ("c") });
		}

        [HttpPost]
		public JsonResult EditDetailQuantity (int id, decimal value)
        {
            var detail = SalesQuoteDetail.Find (id);

			if (value > 0) {
				detail.Quantity = value;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
            }

			return Json(new { id = id, value = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount (int id, string value)
        {
            var detail = SalesQuoteDetail.Find (id);
            bool success;
            decimal discount;

            success = decimal.TryParse (value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1) {
                detail.Discount = discount;

				using (var scope = new TransactionScope ()) {
					detail.UpdateAndFlush ();
				}
            }

			return Json(new { id = id, value = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
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
    }
}
