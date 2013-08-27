// 
// PointsOfSaleController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
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
		public ActionResult Index ()
        {
			var search = SearchStores (new Search<Store> {
				Limit = Configuration.PageSize
			});

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
        }

        [HttpPost]
        public ActionResult Index (Search<Store> search)
		{
			if (ModelState.IsValid) {
				search = SearchStores (search);
			}

            if (Request.IsAjaxRequest ()) {
                return PartialView ("_Index", search);
            }

			return View (search);
        }

		Search<Store> SearchStores (Search<Store> search)
		{
			IQueryable<Store> query;
			var pattern = string.Format("{0}", search.Pattern).Trim ();

			if (string.IsNullOrEmpty (pattern)) {
                query = from x in Store.Queryable
                        orderby x.Name
                        select x;
            } else {
				query = from x in Store.Queryable
						where x.Name.Contains (pattern) 
						orderby x.Name
						select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

            return search;
        }

		public ActionResult Details (int id)
        {
            var entity = Store.Find (id);
			return PartialView ("_Details", entity);
        }

        public ActionResult Create ()
        {
			return PartialView ("_Create");
        }

        [HttpPost]
        public ActionResult Create (Store item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				item.ReceiptMessage = string.Format("{0}", item.ReceiptMessage).Trim ();
				item.Address.Create ();
				item.CreateAndFlush ();
			}
			
			return PartialView ("_Refresh");
        }

        public ActionResult Edit (int id)
		{
			var entity = Store.Find (id);
			return PartialView ("_Edit", entity);
        }

        [HttpPost]
        public ActionResult Edit (Store item)
        {
			item.Taxpayer = Taxpayer.TryFind (item.TaxpayerId);

			if (!ModelState.IsValid || item.Taxpayer == null) {
				return PartialView ("_Edit", item);
			}

			var entity = Store.Find (item.Id);
			var address = entity.Address;
			
			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Taxpayer = Taxpayer.Find (item.TaxpayerId);
			entity.Logo = item.Logo;
			entity.Location = item.Location;
			entity.ReceiptMessage = string.Format("{0}", item.ReceiptMessage).Trim ();

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
			
			return PartialView ("_Refresh");
        }

        public ActionResult Delete (int id)
		{
			var entity = Store.Find (id);
			return PartialView ("_Delete", entity);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
			var entity = Store.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					entity.Delete ();
					entity.Address.Delete ();
				}
			} catch (TransactionException) {
				return PartialView ("DeleteUnsuccessful");
			}

			return PartialView ("_Refresh");
        }

        public JsonResult GetSuggestions (string pattern)
        {
            var query = from x in Store.Queryable
						where x.Code.Contains (pattern) ||
							x.Name.Contains (pattern)
                      	select new { id = x.Id, name = x.Name };

            return Json (query.Take (15).ToList (), JsonRequestBehavior.AllowGet);
        }
    }
}