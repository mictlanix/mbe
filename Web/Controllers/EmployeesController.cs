// 
// EmployeesController.cs
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
    public class EmployeesController : Controller
    {

        //
        // GET: /Employee/

        public ViewResult Index()
        {
            var search = new Search<Employee> {
            	Limit = Configuration.PageSize
			};

			return View (GetEmployees(search));
        }

        // POST: /Employees/

        [HttpPost]
        public ActionResult Index(Search<Employee> search)
        {
            if (ModelState.IsValid) {
                search = GetEmployees(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            } else {
                return View(search);
            }
        }

        Search<Employee> GetEmployees(Search<Employee> search)
        {
			var qry = from x in Employee.Queryable
					  where x.Id > 0
					  orderby x.FirstName
					  select x;

			if (!string.IsNullOrEmpty(search.Pattern)) {
                qry = from x in Employee.Queryable
					  where x.Id > 0 && (
							x.FirstName.Contains(search.Pattern) ||
                      		x.LastName.Contains(search.Pattern))
                      orderby x.FirstName
                      select x;
            }
			
			search.Total = qry.Count();
			search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();

            return search;
        }

        //
        // GET: /Employee/Details/5

        public ViewResult Details (int id)
        {
            var item = Employee.Find(id);
            return View (item);
        }

        //
        // GET: /Employee/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Employee/Create

        [HttpPost]
        public ActionResult Create (Employee item)
        {
            if (!ModelState.IsValid)
            	return View (item);

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return RedirectToAction ("Index");
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit (int id)
        {
            var item = Employee.Find (id);
            return View (item);
        }

        //
        // POST: /Employee/Edit/5

        [HttpPost]
        public ActionResult Edit(Employee item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction ("Index");
        }

        //
        // GET: /Employee/Delete/5

        public ActionResult Delete (int id)
        {
            var item = Employee.Find (id);
            return View (item);
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed (int id)
		{
			try {
				using (var scope = new TransactionScope()) {
					var item = Employee.Find (id);
					item.DeleteAndFlush ();
				}

				return RedirectToAction ("Index");
			} catch (GenericADOException) {
				return View ("DeleteUnsuccessful");
			}
		}

        // AJAX
        // GET: /Employees/GetSuggestions

        public JsonResult GetSuggestions (string pattern)
        {
            var qry = from x in Employee.Queryable
                      where x.FirstName.Contains(pattern) ||
                            x.LastName.Contains(pattern)
                      select new { id = x.Id, name = x.FirstName + " " + x.LastName };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}

