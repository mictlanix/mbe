// 
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
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class CustomersController : Controller
    {
        //
        // GET: /Customer/

        public ViewResult Index()
        {
            var qry = from x in Customer.Queryable
                      orderby x.Name
                      select x;

            Search<Customer> search = new Search<Customer>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /Customers/

        [HttpPost]
        public ActionResult Index(Search<Customer> search)
        {
            if (ModelState.IsValid) {
                search = GetCustomers(search);
            }

            if (Request.IsAjaxRequest()){
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<Customer> GetCustomers(Search<Customer> search)
        {
            if (search.Pattern == null) {
                var qry = from x in Customer.Queryable
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            } else {
                var qry = from x in Customer.Queryable
                          where x.Name.Contains(search.Pattern) ||
                          x.Zone.Contains(search.Pattern)
                          orderby x.Name
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
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
        public ActionResult Create(Customer item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
            PriceList priceList = PriceList.Find(item.PriceListId);
            item.PriceList = priceList;

			using (var scope = new TransactionScope()) {
            	item.CreateAndFlush ();
			}
            
            return RedirectToAction("Index");


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

			using (var scope = new TransactionScope()) {
            	customer.UpdateAndFlush();
			}
			
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
    }
}