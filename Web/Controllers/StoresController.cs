// 
// PointsOfSaleController.cs
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
	[Authorize]
    public class StoresController : Controller
    {
        public ViewResult Index ()
        {
			var search = SearchStores (new Search<Store> {
				Limit = Configuration.PageSize
			});

			return View (search);
        }

        [HttpPost]
        public ActionResult Index (Search<Store> search)
		{
			if (ModelState.IsValid) {
				search = SearchStores (search);
			}

            if (Request.IsAjaxRequest()) {
                return PartialView ("_Index", search);
            }

			return View (search);
        }

		Search<Store> SearchStores (Search<Store> search)
		{
			IQueryable<Store> query;

			if (string.IsNullOrEmpty (search.Pattern)) {
                query = from x in Store.Queryable
                        orderby x.Name
                        select x;
            } else {
				query = from x in Store.Queryable
						where x.Name.Contains(search.Pattern) 
						orderby x.Name
						select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

            return search;
        }

        public ViewResult Details(int id)
        {
            Store store = Store.Find(id);
            return View(store);
        }

        public ActionResult Create()
        {
			var item = new Store { 
				Address = new Address ()
			};

            return View (item);
        }

        [HttpPost]
        public ActionResult Create (Store item)
        {
			if (!ModelState.IsValid) {
				return View (item);
			}

			using (var scope = new TransactionScope ()) {
				item.ReceiptMessage = string.Format("{0}", item.ReceiptMessage).Trim ();
				item.Address.Create ();
				item.CreateAndFlush ();
			}
			
            return RedirectToAction ("Index");
        }

        public ActionResult Edit (int id)
        {
            Store item = Store.Find(id);
            return View (item);
        }

        [HttpPost]
        public ActionResult Edit (Store item)
        {
            if (!ModelState.IsValid)
				return View (item);

			var entity = Store.Find (item.Id);
			var address = entity.Address;
			
			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Taxpayer = item.Taxpayer;
			entity.Logo = item.Logo;
			entity.Location = item.Location;
			entity.ReceiptMessage = string.Format("{0}", item.ReceiptMessage).Trim ();
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope()) {
				if (!address.Equals (item.Address)) {
					entity.Address = item.Address;
					entity.Address.Create ();
				}

				entity.UpdateAndFlush ();
			}
			
			if (!address.Equals (item.Address)) {
				try {
					using (var scope = new TransactionScope()) {
						address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}
			
			return RedirectToAction ("Index");
        }

        public ActionResult Delete (int id)
        {
            return View (Store.Find (id));
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
			try {
				using (var scope = new TransactionScope()) {
					var item = Store.Find (id);
					item.Delete ();
					item.Address.Delete ();
				}

				return RedirectToAction ("Index");
			} catch (TransactionException) {
				return View ("DeleteUnsuccessful");
			}
        }

        public JsonResult GetSuggestions (string pattern)
        {
            var qry = from x in Store.Queryable
                      where x.Name.Contains(pattern) ||
                      		x.Code.Contains(pattern)
                      select new { id = x.Id, name = x.Name };

            return Json (qry.Take (15).ToList (), JsonRequestBehavior.AllowGet);
        }
    }
}