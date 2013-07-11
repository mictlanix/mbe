// 
// PriceListsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
    public class PriceListsController : Controller
    {
        //
        // GET: /PriceList/

        public ViewResult Index()
        {
            var qry = from x in PriceList.Queryable
					  where x.Id > 0
                      orderby x.Name
                      select x;

            var search = new Search<PriceList>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /PriceList/

        [HttpPost]
        public ActionResult Index(Search<PriceList> search)
        {
            if (ModelState.IsValid) {
                search = GetPriceList (search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<PriceList> GetPriceList(Search<PriceList> search)
		{
			var qry = from x in PriceList.Queryable
					  where x.Id > 0
					  orderby x.Name
					  select x;

			if (!string.IsNullOrEmpty(search.Pattern)) {
                qry = from x in PriceList.Queryable
				      where x.Id > 0 && x.Name.Contains (search.Pattern) 
                      orderby x.Name
                      select x;
			}
			
			search.Total = qry.Count();
			search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
			
			return search;
        }

        //
        // GET: /PriceList/Details/5

        public ViewResult Details(int id)
        {
            var item = PriceList.Find (id);
            return View (item);
        }

        //
        // GET: /PriceList/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /PriceList/Create

        [HttpPost]
        public ActionResult Create(PriceList item)
        {
            if (!ModelState.IsValid)
            	return View(item);
			
			item.LowProfitMargin /= 100m;
			item.HighProfitMargin /= 100m;

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return RedirectToAction("Index");
        }

        //
        // GET: /PriceList/Edit/5

        public ActionResult Edit (int id)
        {
			var item = PriceList.Find (id);
			
			item.LowProfitMargin *= 100m;
			item.HighProfitMargin *= 100m;

            return View (item);
        }

        //
        // POST: /PriceList/Edit/5

        [HttpPost]
        public ActionResult Edit (PriceList item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
			item.LowProfitMargin /= 100m;
			item.HighProfitMargin /= 100m;

			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

			return RedirectToAction("Index");
        }

        //
        // GET: /PriceList/Delete/5

        public ActionResult Delete(int id)
        {
            var item = PriceList.Find(id);
            return View(item);
        }

        //
        // POST: /PriceList/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
			var item = PriceList.Find (id);
            
			using (var scope = new TransactionScope ()) {
            	item.DeleteAndFlush ();
			}

            return RedirectToAction("Index");
        }

        public JsonResult GetSuggestions (string pattern)
        {
            JsonResult result = new JsonResult();
            var qry = from x in PriceList.Queryable
                      where x.Name.Contains(pattern)
                      select new { id = x.Id, name = x.Name };

            result = Json(qry.Take(15).ToList());
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }
    }
}
