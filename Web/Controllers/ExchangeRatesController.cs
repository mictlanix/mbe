// 
// ExchangeRatesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
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
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{ 
	public class ExchangeRatesController : Controller
    {
        public ViewResult Index ()
        {
            var qry = from x in ExchangeRate.Queryable
					  orderby x.Date descending
                      select x;

			var search = new Search<ExchangeRate>();
            search.Limit = Configuration.PageSize;
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
			
			if (!string.IsNullOrEmpty(search.Pattern)) {
				qry = from x in ExchangeRate.Queryable
					  orderby x.Date descending
					  select x;

			}
			
			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return PartialView ("_Index", search);
        }

        public ActionResult Create ()
        {
			return View (new ExchangeRate { Date = DateTime.Now, Base = Configuration.BaseCurrency });
        } 

        [HttpPost]
		public ActionResult Create (ExchangeRate item)
        {
            if (!ModelState.IsValid)
            	return View (item);

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

            return RedirectToAction ("Index");
        }

        public ActionResult Delete (int id)
        {
			var item = ExchangeRate.Find (id);
            return View (item);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed (int id)
        {
			var item = ExchangeRate.Find (id);
			
			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

            return RedirectToAction ("Index");
        }
    }
}