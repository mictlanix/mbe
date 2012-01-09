// 
// SuppliersController.cs
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
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate.Exceptions;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class SuppliersController : Controller
    {
        public JsonResult GetSuggestions(string pattern)
        {
            JsonResult result = new JsonResult();
            var qry = from x in Supplier.Queryable
                      where x.Name.Contains(pattern)
                      select new { id = x.Id, name = x.Name };

            result = Json(qry.Take(15).ToList());
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }

        //
        // GET: /Suppliers/

        public ViewResult Index()
        {
            var qry = from x in Supplier.Queryable
                      orderby x.Name
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Supplier/Details/5

        public ViewResult Details(int id)
        {
			Supplier item;
			
			using(new SessionScope())
			{
	            item = Supplier.Find(id);
				item.Addresses.ToList();
				item.Agreements.ToList();
				item.BanksAccounts.ToList();
				item.Contacts.ToList();
			}

            ViewBag.OwnerId = item.Id;
            
            return View(item);
        }

        //
        // GET: /Supplier/Create

        public ActionResult Create()
        {
            if (Request.IsAjaxRequest())
            {
                return PartialView("_Create");
            }

            return View();
        }

        //
        // POST: /Supplier/Create

        [HttpPost]
        public ActionResult Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.Save();
                return RedirectToAction("Index");
            }

            return View(supplier);

            //if (!ModelState.IsValid)
            //{
            //    if (Request.IsAjaxRequest())
            //        return PartialView("_Create", supplier);

            //    return View(supplier);
            //}

            //supplier.Save();

            //if (Request.IsAjaxRequest())
            //{
            //    //FIXME: localize string
            //    return PartialView("_Success", "Operation successful!");
            //}

            //return View("Index");
        }

        //
        // GET: /Supplier/Edit/5

        public ActionResult Edit(int id)
        {
            Supplier supplier = Supplier.Find(id);
            return View(supplier);
        }

        //
        // POST: /Supplier/Edit/5

        [HttpPost]
        public ActionResult Edit(Supplier item)
        {
            if (!ModelState.IsValid)
            	return View(item);
            
			var supplier = Supplier.Find(item.Id);
			
			supplier.Code = item.Code;
			supplier.Name = item.Name;
			supplier.Zone = item.Zone;
			supplier.CreditDays = item.CreditDays;
			supplier.CreditLimit = item.CreditLimit;
			//TODO: Add Comment Property
			supplier.Comment = item.Comment;

            supplier.Update();
            
			return RedirectToAction("Index");
        }

        //
        // GET: /Supplier/Delete/5

        public ActionResult Delete(int id)
        {
            Supplier supplier = Supplier.Find(id);
            return View(supplier);
        }

        //
        // POST: /Supplier/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
			try {
				using (new SessionScope()) {
					var item = Supplier.Find (id);
					item.Delete ();
				}

				return RedirectToAction ("Index");
			} catch (GenericADOException) {
				return View ("DeleteUnsuccessful");
			}
        }

        //
        // GET: /Supplier/Delete/5

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}