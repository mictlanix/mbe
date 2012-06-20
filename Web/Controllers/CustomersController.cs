﻿// 
// CustomersController.cs
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
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
    public class CustomersController : Controller
    {
        public JsonResult GetSuggestions(string pattern)
        {
            JsonResult result = new JsonResult();
            var qry = from x in Customer.Queryable
                      where x.Name.Contains(pattern) ||
                            x.Zone.Contains(pattern)
                      select new { id = x.Id, name = x.Name, hasCredit = (x.CreditDays > 0 && x.CreditLimit > 0) };

            result = Json(qry.Take(15).ToList());
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }

        //
        // GET: /Customer/

        public ViewResult Index()
        {
            var qry = from x in Customer.Queryable
                        orderby x.Name
                        select x;

            return View(qry.ToList());
        }

        //
        // GET: /Customer/Details/5

        public ViewResult Details(int id)
        {
			var item = Customer.Find(id);

			item.Addresses.ToList();
			item.Contacts.ToList();
			
			ViewBag.OwnerId = item.Id;
			
            return View(item);
        }

        //
        // GET: /Customer/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Customer/Create

        [HttpPost]
        public ActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                PriceList priceList = PriceList.Find(customer.PriceListId);
                customer.PriceList = priceList;

                customer.Save();
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        //
        // GET: /Customer/Edit/5

        public ActionResult Edit(int id)
        {
            Customer customer = Customer.Find(id);
            return View(customer);
        }

        //
        // POST: /Customer/Edit/5

        [HttpPost]
        public ActionResult Edit(Customer item)
        {
            item.PriceList = PriceList.TryFind(item.PriceListId);
			
            if (!ModelState.IsValid)
            	return View(item);
			
			var customer = Customer.Find(item.Id);
			
			customer.Name = item.Name;
			customer.Zone = item.Zone;
			customer.PriceList = item.PriceList;
			customer.CreditDays = item.CreditDays;
			customer.CreditLimit = item.CreditLimit;
			customer.Comment = item.Comment;
			
            customer.Update();
			
            return RedirectToAction("Index");
        }

        //
        // GET: /Customer/Delete/5

        public ActionResult Delete(int id)
        {
            Customer customer = Customer.Find(id);
            return View(customer);
        }

        //
        // POST: /Customer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed (int id)
		{
			try {
				using (var scope = new TransactionScope()) {
					var item = Customer.Find (id);
					item.DeleteAndFlush ();
				}

				return RedirectToAction ("Index");
			} catch (GenericADOException) {
				return View ("DeleteUnsuccessful");
			}
		}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}