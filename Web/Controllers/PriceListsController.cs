// 
// PriceListsController.cs
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
    public class PriceListsController : Controller
    {
        //
        // GET: /PriceList/

        public ViewResult Index()
        {
            var qry = from x in PriceList.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /PriceList/Details/5

        public ViewResult Details(int id)
        {
            PriceList priceList = PriceList.Find(id);
            return View(priceList);
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
            
			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return RedirectToAction("Index");
        }

        //
        // GET: /PriceList/Edit/5

        public ActionResult Edit (int id)
        {
            PriceList item = PriceList.Find (id);
            return View (item);
        }

        //
        // POST: /PriceList/Edit/5

        [HttpPost]
        public ActionResult Edit (PriceList item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

			return RedirectToAction("Index");
        }

        //
        // GET: /PriceList/Delete/5

        public ActionResult Delete(int id)
        {
            PriceList item = PriceList.Find(id);
            return View(item);
        }

        //
        // POST: /PriceList/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            PriceList item = PriceList.Find (id);
            
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
