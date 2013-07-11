// 
// WarehousesController.cs
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
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class WarehousesController : Controller
    {
        //
        // GET: /Warehouses/

        public ViewResult Index()
        {
            var qry = from x in Warehouse.Queryable
                      orderby x.Name
                      select x;

            Search<Warehouse> search = new Search<Warehouse>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /Warehouses/

        [HttpPost]
        public ActionResult Index(Search<Warehouse> search)
        {
            if (ModelState.IsValid) {
                search = GetWarehouses(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<Warehouse> GetWarehouses(Search<Warehouse> search)
        {
            if (search.Pattern == null) {
                var qry = from x in Warehouse.Queryable
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            } else {
                var qry = from x in Warehouse.Queryable
                          where x.Name.Contains(search.Pattern) 
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
        }

        //
        // GET: /Warehouses/Details/5

        public ViewResult Details (int id)
        {
            var item = Warehouse.Find (id);
            return View (item);
        }

        //
        // GET: /Warehouses/Create

        public ActionResult Create ()
        {
            return View ();
        }

        //
        // POST: /Warehouses/Create

        [HttpPost]
        public ActionResult Create (Warehouse item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
            item.Store = Store.Find (item.StoreId);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Index");
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit (int id)
        {
            var item = Warehouse.Find (id);
            return View (item);
        }

        //
        // POST: /Warehouses/Edit/5

        [HttpPost]
        public ActionResult Edit (Warehouse item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
			var entity = Warehouse.Find (item.Id);

			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
        }

        //
        // GET: /Warehouses/Delete/5

        public ActionResult Delete (int id)
        {
            return View (Warehouse.Find (id));
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
			try {
				using (var scope = new TransactionScope()) {
					var item = Warehouse.Find (id);
					item.DeleteAndFlush ();
				}

				return RedirectToAction ("Index");
			} catch (TransactionException) {
				return View ("DeleteUnsuccessful");
			}
        }

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Warehouse.Queryable
                      where x.Code.Contains(pattern) ||
                            x.Name.Contains(pattern)
                      select new { id = x.Id, name = x.Name};

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult List()
        {
            var qry = from x in Warehouse.Queryable
                      select new { id = x.Id, name = x.Name };

            var dict = qry.ToDictionary(x => x.id.ToString(), x => x.name);

            //dict.Add("selected", id.ToString());

            return Json(dict, JsonRequestBehavior.AllowGet);
        }
    }
}