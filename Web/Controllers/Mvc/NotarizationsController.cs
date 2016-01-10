// 
// NotarizationsController.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using Castle.ActiveRecord;
using NHibernate;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
	[Authorize]
	public class NotarizationsController : CustomController
    {
		public ActionResult Index ()
		{
			var search = Search (new Search<Notarization> {
				Limit = WebConfig.PageSize
			});

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		[HttpPost]
		public ActionResult Index (Search<Notarization> search)
		{
			if (ModelState.IsValid) {
				search = Search (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<Notarization> Search (Search<Notarization> search)
		{
			IQueryable<Notarization> query;
			var pattern = string.Format("{0}", search.Pattern).Trim ();

			if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in Notarization.Queryable
						orderby x.Date descending
				        select x;
			} else {
				query = from x in Notarization.Queryable
				        where x.NotaryOffice.Contains (pattern) ||
				            x.DocumentDescription.Contains (pattern)
						orderby x.Date descending
				        select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Create ()
        {
			return PartialView ("_Create");
		}

        [HttpPost]
		public ActionResult Create (Notarization item)
		{
			item.Requester = Employee.TryFind (item.RequesterId);

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		public ActionResult View (int id)
		{
			var item = Notarization.Find (id);
			return PartialView ("_View", item);
		}

		public ActionResult Edit (int id)
        {
        	var item = Notarization.Find (id);
			return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit (Notarization item)
		{
			item.Requester = Employee.TryFind (item.RequesterId);

			if (!ModelState.IsValid) {
				return PartialView ("_Edit", item);
			}
			
			var entity = Notarization.Find (item.Id);

			entity.Date = item.Date;
			entity.Requester = item.Requester;
			entity.NotaryOffice = item.NotaryOffice;
			entity.DocumentDescription = item.DocumentDescription;
			entity.DeliveryDate = item.DeliveryDate;
			entity.PaymentDate = item.PaymentDate;
			entity.Amount = item.Amount;
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
        }

		public ActionResult Delete (int id)
        {
            var item = Notarization.Find (id);
			return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = Notarization.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					item.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}

			return PartialView ("_DeleteSuccesful", item);
        }
    }
}