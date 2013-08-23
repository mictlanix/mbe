// 
// EmployeesController.cs
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
	[Authorize]
    public class EmployeesController : Controller
    {
        public ViewResult Index ()
        {
			var search = SearchEmployees (new Search<Employee> {
				Limit = Configuration.PageSize
			});

			return View (search);
        }

        [HttpPost]
        public ActionResult Index (Search<Employee> search)
        {
            if (ModelState.IsValid) {
				search = SearchEmployees (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
        }

		Search<Employee> SearchEmployees (Search<Employee> search)
		{
			IQueryable<Employee> query;
			var pattern = string.Format("{0}", search.Pattern).Trim ();

			if (string.IsNullOrEmpty (pattern)) {
				query = from x in Employee.Queryable
						orderby x.FirstName
						select x;
			} else {
				query = from x in Employee.Queryable
						where x.FirstName.Contains (pattern) ||
							x.LastName.Contains (pattern)
						orderby x.FirstName
						select x;
            }
			
			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

            return search;
        }

		public ActionResult Details (int id)
        {
            var item = Employee.Find (id);
			return PartialView ("_Details", item);
        }

        public ActionResult Create()
        {
			return PartialView ("_Create");
        }

        [HttpPost]
        public ActionResult Create (Employee item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return PartialView ("_Refresh");
        }

        public ActionResult Edit (int id)
        {
            var item = Employee.Find (id);
			return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit(Employee item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Edit", item);
			}
            
			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
        }

        public ActionResult Delete (int id)
        {
            var item = Employee.Find (id);
			return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed (int id)
		{
			var item = Employee.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					item.DeleteAndFlush ();
				}
			} catch (Exception) {
				return PartialView ("DeleteUnsuccessful");
			}

			return PartialView ("_Refresh");
		}

        public JsonResult GetSuggestions (string pattern)
        {
            var query = from x in Employee.Queryable
						where x.IsActive && (
							x.FirstName.Contains (pattern) ||
							x.LastName.Contains (pattern))
						select new { id = x.Id, name = x.FirstName + " " + x.LastName };

            return Json (query.ToList (), JsonRequestBehavior.AllowGet);
        }
		
		public JsonResult SalesPeople (string pattern)
		{
			var query = from x in Employee.Queryable
						where x.IsActive && x.IsSalesPerson && (
							x.FirstName.Contains (pattern) ||
							x.LastName.Contains (pattern))
						select new { id = x.Id, name = x.FirstName + " " + x.LastName };

			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}
    }
}

