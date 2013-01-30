﻿// 
// PointsOfSaleController.cs
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
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class StoresController : Controller
    {
        //
        // GET: /Store/

        public ViewResult Index()
        {
            var qry = from x in Store.Queryable
                      orderby x.Name
                      select x;

            Search<Store> search = new Search<Store>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /Stores/

        [HttpPost]
        public ActionResult Index(Search<Store> search)
        {
            if (ModelState.IsValid) {
                search = GetStores(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<Store> GetStores(Search<Store> search)
        {
            if (search.Pattern == null) {
                var qry = from x in Store.Queryable
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            } else {
                var qry = from x in Store.Queryable
                          where x.Name.Contains(search.Pattern) 
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
        }

        //
        // GET: /Store/Details/5

        public ViewResult Details(int id)
        {
            Store store = Store.Find(id);
            return View(store);
        }

        //
        // GET: /store/Create

        public ActionResult Create()
        {
			var item = new Store { 
				Address = new Address {
					TaxpayerId = "XXXXXXXXXXXX",
					TaxpayerName = "XXX"
				}
			};

            return View (item);
        }

        //
        // POST: /store/Create

        [HttpPost]
        public ActionResult Create(Store item)
        {
            if (!ModelState.IsValid)
				return View (item);
			
			using (var scope = new TransactionScope()) {
				item.ReceiptMessage = item.ReceiptMessage.Trim();
				item.Address.Create ();
				item.CreateAndFlush ();
			}
			
            return RedirectToAction ("Index");
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit (int id)
        {
            Store item = Store.Find(id);
            return View (item);
        }

        //
        // POST: /store/Edit/5

        [HttpPost]
        public ActionResult Edit (Store item)
        {
            if (!ModelState.IsValid)
				return View (item);

			using (var scope = new TransactionScope()) {
				item.ReceiptMessage = item.ReceiptMessage == null ? null : item.ReceiptMessage.Trim();
				item.Address.Update ();
				item.UpdateAndFlush ();
			}
			
			return RedirectToAction ("Index");
        }

        //
        // GET: /store/Delete/5

        public ActionResult Delete (int id)
        {
            return View (Store.Find (id));
        }

        //
        // POST: /store/Delete/5

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

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Store.Queryable
                      where x.Name.Contains(pattern) ||
                      x.Code.Contains(pattern)
                      select new { id = x.Id, name = x.Name };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}