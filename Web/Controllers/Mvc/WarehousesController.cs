// 
// WarehousesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
	[Authorize]
    public class WarehousesController : CustomController
    {
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

        public ActionResult View (int id)
        {
            var item = Warehouse.Find (id);
            return PartialView ("_View", item);
        }

        public ActionResult Create ()
        {
            return PartialView ("_Create");
        }

        [HttpPost]
        public ActionResult Create (Warehouse item)
        {
            if (!ModelState.IsValid)
                return PartialView ("_Create", item);
            
            item.Store = Store.Find (item.StoreId);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

            return PartialView ("_CreateSuccesful", item);
        }

        public ActionResult Edit (int id)
        {
            var item = Warehouse.Find (id);
            return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit (Warehouse item)
        {
            if (!ModelState.IsValid)
                return PartialView ("_Edit", item);
            
			var entity = Warehouse.Find (item.Id);

			entity.Code = item.Code;
            entity.Name = item.Name;
            entity.Store = Store.Find (item.StoreId);
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

            return PartialView ("_Refresh");
        }

        public ActionResult Delete (int id)
        {
            var item = Warehouse.Find (id);
            return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = Warehouse.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					item.DeleteAndFlush ();
				}

                return PartialView ("_DeleteSuccesful", item);
			} catch (TransactionException) {
                return PartialView ("DeleteUnsuccessful");
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

        public JsonResult List ()
        {
            var qry = from x in Warehouse.Queryable
					  orderby x.Name
					  select new { 
						value = x.Id,
						text = x.Name
					  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
        }
    }
}