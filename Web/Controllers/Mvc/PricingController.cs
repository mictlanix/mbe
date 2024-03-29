// 
// PricingController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using Newtonsoft.Json;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class PricingController : CustomController {
		public ActionResult Index ()
		{
			var qry = from x in Product.Queryable
				  where !x.IsDeactivated
				  orderby x.Name
				  select x;

			var search = new Search<Product> ();
			search.Limit = WebConfig.PageSize;
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = qry.Count ();

			var list = PriceList.Queryable.ToList ();

			if (!CurrentUser.IsInRole ("PriceLists.Update")) {
				list.Remove (list.Single (x => x.Id == 0));
			}

			ViewBag.PriceLists = list;

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<Product> search)
		{
			if (!ModelState.IsValid)
				return View (search);

			var qry = from x in Product.Queryable
				  where !x.IsDeactivated
				  orderby x.Name
				  select x;

			if (!string.IsNullOrEmpty (search.Pattern)) {
				qry = from x in Product.Queryable
				      where (x.Name.Contains (search.Pattern) ||
						    x.Code.Contains (search.Pattern) ||
						    x.Model.Contains (search.Pattern) ||
						    x.SKU.Contains (search.Pattern) ||
						    x.Brand.Contains (search.Pattern))
						&& !x.IsDeactivated
				      orderby x.Name
				      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			ViewBag.PriceLists = PriceList.Queryable.ToList ();

			return PartialView ("_Index", search);
		}

		[HttpPost]
		public JsonResult SetPrice (int product, int list, string value)
		{
			decimal val;
			bool success;
			var p = Product.TryFind (product);
			var l = PriceList.TryFind (list);
			var item = ProductPrice.Queryable.SingleOrDefault (x => x.Product.Id == product && x.List.Id == list);

			if (item == null) {
				item = new ProductPrice {
					Product = p,
					List = l
				};
			}

			success = decimal.TryParse (value.Trim (),
						    System.Globalization.NumberStyles.Currency,
						    null, out val);

			if (success && val >= 0) {
				item.Value = val;

				using (var scope = new TransactionScope ()) {
					item.SaveAndFlush ();
				}
			}

			return Json (new { id = item.Id, value = item.FormattedValueFor (x => x.Value) });
		}

		[HttpPost]
		public JsonResult SetCurrency (int id, string value)
		{
			bool success;
			CurrencyCode val;
			var item = Product.TryFind (id);

			success = Enum.TryParse<CurrencyCode> (value.Trim (), out val);

			if (success && val >= 0) {
				item.Currency = val;

				using (var scope = new TransactionScope ()) {
					item.SaveAndFlush ();
				}
			}

			return Json (new { id = item.Id, value = item.Currency.ToString () });
		}

		[HttpPost]
		public JsonResult SetMOQ (int id, decimal value)
		{
			var entity = Product.Find (id);

			if (value > 0) {
				entity.MinimumOrderQuantity = value;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new { id = id, value = entity.FormattedValueFor (x => x.MinimumOrderQuantity) });
		}

		[HttpPost]
		public JsonResult SetTaxRate (int id, decimal value)
		{
			var entity = Product.Find (id);

			entity.TaxRate = value;
			entity.IsTaxIncluded = WebConfig.IsTaxIncluded;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = entity.FormattedValueFor (x => x.TaxRate) });
		}

		[HttpPost]
		public JsonResult SetPriceType (int id, string value)
		{
			bool success;
			PriceType val;
			var item = Product.Find (id);

			success = Enum.TryParse<PriceType> (value.Trim (), out val);

			if (success && val >= 0) {
				item.PriceType = val;

				using (var scope = new TransactionScope ()) {
					item.UpdateAndFlush ();
				}
			}

			return Json (new { id = id, value = item.PriceType.GetDisplayName () });
		}

		[HttpPost]
		public ActionResult ToogleTaxIncluded (int id)
		{
			var item = Product.Find (id);

			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			item.IsTaxIncluded = !item.IsTaxIncluded;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return PartialView ("_IsTaxIncluded", item);
		}

		public JsonResult PriceTypes ()
		{
			var qry = from x in Enum.GetValues (typeof (PriceType)).Cast<PriceType> ()
				  select new {
					  value = (int) x,
					  text = x.GetDisplayName ()
				  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult TaxRates ()
		{
			var rates = from x in WebConfig.TaxRates
				    select new { value = x.Value, text = x.Key };

			return Json (rates, JsonRequestBehavior.AllowGet);
		}

		public JsonResult RetentionRates ()
		{
			var rates = from x in WebConfig.RetentionRates
				    select new { value = x.Value, text = x.Key };

			return Json (rates, JsonRequestBehavior.AllowGet);
		}

		public JsonResult LocalRetentionRates ()
		{
			var rates = from x in WebConfig.LocalRetentionRates
				    select new { value = x.Value, text = x.Key };

			return Json (rates, JsonRequestBehavior.AllowGet);
		}
	}
}