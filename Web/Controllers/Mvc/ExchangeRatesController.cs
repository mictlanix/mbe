// 
// ExchangeRatesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class ExchangeRatesController : CustomController {
		public ViewResult Index ()
		{
			var qry = from x in ExchangeRate.Queryable
				  orderby x.Date descending
				  select x;

			var search = new Search<ExchangeRate> ();
			search.Limit = WebConfig.PageSize;
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = qry.Count ();

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<ExchangeRate> search)
		{
			if (!ModelState.IsValid)
				return View (search);

			var qry = from x in ExchangeRate.Queryable
				  orderby x.Date descending
				  select x;

			//if (!string.IsNullOrEmpty(search.Pattern)) {
			//	qry = from x in ExchangeRate.Queryable
			//		  orderby x.Date descending
			//		  select x;

			//}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return PartialView ("_Index", search);
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create", new ExchangeRate { Date = DateTime.Now, Target = WebConfig.BaseCurrency });
		}

		[HttpPost]
		public ActionResult Create (ExchangeRate item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			var qry = from x in ExchangeRate.Queryable
				  where x.Date == item.Date.Date &&
						x.Base == item.Base &&
						x.Target == item.Target
				  select x;

			if (qry.Count () > 0) {
				ModelState.AddModelError ("Date", Resources.ExchangeRateAlreadyExists);
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		public ActionResult Delete (int id)
		{
			var item = ExchangeRate.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = ExchangeRate.Find (id);

			try {
				using (var scope = new TransactionScope ()) {
					item.DeleteAndFlush ();
				}
				return PartialView ("_DeleteSuccesful", item);
			} catch (Exception) {
				return PartialView ("DeleteUnsuccessful");
			}
		}

		public JsonResult Currencies ()
		{
			var qry = from x in Enum.GetValues (typeof (CurrencyCode)).Cast<CurrencyCode> ()
				  select new {
					  value = (int) x,
					  text = x.ToString ()
				  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}
	}
}