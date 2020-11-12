// 
// ContactsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2020 Eddy Zavaleta, Mictlanix, and contributors.
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
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class ContactsController : CustomController {
		public ActionResult CreateCustomerContact (int id)
		{
			return PartialView ("_Create");
		}

		public ActionResult CreateSupplierContact (int id)
		{
			return PartialView ("_Create");
		}

		[HttpPost]
		public ActionResult CreateCustomerContact (int id, Contact item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				var customer = Customer.Find (id);

				item.Id = 0;
				item.CreateAndFlush ();
				customer.Contacts.Add (item);
				customer.Update ();
			}

			return PartialView ("_Refresh");
		}

		[HttpPost]
		public ActionResult CreateSupplierContact (int id, Contact item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				var supplier = Supplier.Find (id);

				item.Id = 0;
				item.CreateAndFlush ();
				supplier.Contacts.Add (item);
				supplier.Update ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Details (int id)
		{
			var item = Contact.Find (id);
			return PartialView ("_Details", item);
		}

		public ActionResult Edit (int id)
		{
			var item = Contact.Find (id);
			return PartialView ("_Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (Contact item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);

			using (var scope = new TransactionScope ()) {
				item.Update ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = Contact.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			try {
				using (var scope = new TransactionScope ()) {
					var item = Contact.Find (id);

					foreach (var x in item.Customers) {
						x.Contacts.Remove (item);
						x.Update ();
					}

					foreach (var x in item.Suppliers) {
						x.Contacts.Remove (item);
						x.Update ();
					}
				}
			} catch (GenericADOException ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}

			try {
				using (var scope = new TransactionScope ()) {
					var item = Contact.Find (id);
					item.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}

			return PartialView ("_Refresh");
		}
	}
}