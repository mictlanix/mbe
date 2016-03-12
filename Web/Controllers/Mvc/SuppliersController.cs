// 
// SuppliersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class SuppliersController : CustomController {
		public ViewResult Index ()
		{
			var qry = from x in Supplier.Queryable
				  orderby x.Name
				  select x;

			Search<Supplier> search = new Search<Supplier> ();
			search.Limit = WebConfig.PageSize;
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = qry.Count ();

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<Supplier> search)
		{
			if (ModelState.IsValid) {
				search = GetSuppliers (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<Supplier> GetSuppliers (Search<Supplier> search)
		{
			if (search.Pattern == null) {
				var qry = from x in Supplier.Queryable
					  orderby x.Name
					  select x;

				search.Total = qry.Count ();
				search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			} else {
				var qry = from x in Supplier.Queryable
					  where x.Name.Contains (search.Pattern) ||
					  x.Zone.Contains (search.Pattern) ||
					  x.Code.Contains (search.Pattern)
					  orderby x.Name
					  select x;

				search.Total = qry.Count ();
				search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			}

			return search;
		}

		public ActionResult Details (int id)
		{
			var item = Supplier.Find (id);

			item.Addresses.ToList ();
			item.Agreements.ToList ();
			item.BanksAccounts.ToList ();
			item.Contacts.ToList ();

			ViewBag.OwnerId = item.Id;

			return View (item);
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create", new Supplier ());
		}

		[HttpPost]
		public ActionResult Create (Supplier item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);

			//if (!ModelState.IsValid)
			//{
			//    if (Request.IsAjaxRequest())
			//        return PartialView("_Create", supplier);

			//    return View(supplier);
			//}

			//supplier.Create ();

			//if (Request.IsAjaxRequest())
			//{
			//    //FIXME: localize string
			//    return PartialView("_Success", "Operation successful!");
			//}

			//return View("Index");
		}

		public ActionResult Edit (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("_Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (Supplier item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);

			var entity = Supplier.Find (item.Id);

			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Zone = item.Zone;
			entity.CreditDays = item.CreditDays;
			entity.CreditLimit = item.CreditLimit;
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = Supplier.Find (id);

			try {
				using (var scope = new TransactionScope ()) {
					item.DeleteAndFlush ();
				}
				return PartialView ("_DeleteSuccesful", item);
			} catch (Exception) {
				return PartialView ("DeleteUnsuccessful");
			}
		}

		public ActionResult Addresses (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("../Addresses/_Index", item.Addresses);
		}

		public ActionResult Contacts (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("../Contacts/_Index", item.Contacts);
		}

		public ActionResult BankAccounts (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("../BankAccounts/_Index", item.BanksAccounts);
		}

		public ActionResult SupplierAgreements (int id)
		{
			var item = Supplier.Find (id);
			return PartialView ("../SupplierAgreements/_Index", item.Agreements);
		}

		public JsonResult GetSuggestions (string pattern)
		{
			JsonResult result = new JsonResult ();
			var qry = from x in Supplier.Queryable
				  where x.Name.Contains (pattern)
				  select new { id = x.Id, name = x.Name };

			result = Json (qry.Take (15).ToList ());
			result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

			return result;
		}
	}
}