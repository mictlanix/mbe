// 
// ProductionOrdersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2014 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
	[Authorize]
	public class ProductionOrdersController : CustomController
	{
		public ViewResult Index ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			var search = SearchSalesOrders (new Search<SalesOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<SalesOrder> search)
		{
			if (ModelState.IsValid) {
				search = SearchSalesOrders (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		Search<SalesOrder> SearchSalesOrders (Search<SalesOrder> search)
		{
			IQueryable<SalesOrder> query;
			var item = WebConfig.Store;

			if (string.IsNullOrEmpty (search.Pattern)) {
				query = from x in SalesOrder.Queryable
						where x.Store.Id == item.Id && x.IsCompleted && !x.IsCancelled
						orderby x.Date descending
				        select x;
			} else {
				query = from x in SalesOrder.Queryable
						where x.Store.Id == item.Id && x.IsCompleted && !x.IsCancelled && (
				              x.Customer.Name.Contains (search.Pattern) ||
				              x.SalesPerson.Nickname.Contains (search.Pattern))
				        orderby x.Date descending
				        select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}
		
		public ViewResult Print (int id)
		{
			var item = SalesOrder.Find (id);
			return View (item);
		}
		
		[HttpPost]
		public ActionResult Confirm (int id)
		{
			var item = SalesOrder.Find (id);
			
			item.Updater = CurrentUser.Employee;
			item.ModificationTime = DateTime.Now;
			item.IsDelivered = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}
			
			return RedirectToAction ("Index");
		}
    }
}
