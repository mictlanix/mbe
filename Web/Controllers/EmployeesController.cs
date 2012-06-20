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

namespace Mictlanix.BE.Web.Controllers
{
    public class EmployeesController : Controller
    {

        // AJAX
        // GET: /Categories/GetSuggestions

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Employee.Queryable
                      where x.FirstName.Contains(pattern) ||
                            x.LastName.Contains(pattern)
                      select new { id = x.Id, name = x.FirstName + " " + x.LastName };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Employee/

        public ViewResult Index()
        {
            var qry = from x in Employee.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Employee/Details/5

        public ViewResult Details(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
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
        public ActionResult Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Create();
                return RedirectToAction("Index");
            }

            return View(employee);
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
        }

        //
        // POST: /Employee/Edit/5

        [HttpPost]
        public ActionResult Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Update();
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        //
        // GET: /Employee/Delete/5

        public ActionResult Delete(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

