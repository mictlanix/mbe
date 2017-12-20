// 
// AddressesController.cs
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
	public class AddressesController : CustomController {
		public ActionResult CreateCustomerAddress (int id)
		{
			return PartialView ("_Create");
		}

		public ActionResult CreateSupplierAddress (int id)
		{
			return PartialView ("_Create");
		}

		[HttpPost]
		public ActionResult CreateCustomerAddress (int id, Address item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				var customer = Customer.Find (id);

				item.Id = 0;
				item.CreateAndFlush ();
				customer.Addresses.Add (item);
				customer.Update ();
			}

			return PartialView ("_Refresh");
		}

		[HttpPost]
		public ActionResult CreateSupplierAddress (int id, Address item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope ()) {
				var supplier = Supplier.Find (id);

				item.Id = 0;
				item.CreateAndFlush ();
				supplier.Addresses.Add (item);
				supplier.Update ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Details (int id)
		{
			var item = Address.Find (id);
			return PartialView ("_Details", item);
		}

		public ActionResult Edit (int id)
		{
			var item = Address.Find (id);
			return PartialView ("_Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (Address item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);

			var entity = Address.Find (item.Id);

			if (entity.Equals (item))
				return PartialView ("_Refresh");

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();

				foreach (var x in entity.Customers) {
					x.Addresses.Remove (entity);
					x.Addresses.Add (item);
					x.Update ();
				}

				foreach (var x in entity.Suppliers) {
					x.Addresses.Remove (entity);
					x.Addresses.Add (item);
					x.Update ();
				}

				entity.Customers.Clear ();
				entity.Suppliers.Clear ();
			}

			try {
				using (var scope = new TransactionScope ()) {
					entity.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = Address.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			try {
				using (var scope = new TransactionScope ()) {
					var item = Address.Find (id);

					foreach (var x in item.Customers) {
						x.Addresses.Remove (item);
						x.Update ();
					}

					foreach (var x in item.Suppliers) {
						x.Addresses.Remove (item);
						x.Update ();
					}
				}
			} catch (GenericADOException ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}

			try {
				using (var scope = new TransactionScope ()) {
					var item = Address.Find (id);
					item.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}

			return PartialView ("_Refresh");
		}

		public JsonResult GetSuggestions (int customer)
		{
			var qry = from x in Address.Queryable
				  from y in x.Customers
				  where y.Id == customer
				  orderby x.Street
				  select new { id = x.Id, name = x.ToString () };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult PostalCodes (string pattern)
		{
			var query = from x in SatPostalCode.Queryable
				    where x.Id.Contains (pattern)
                                    select x;
			var items = from x in query.Take (15).ToList ()
				    select new { id = x.Id, name = x.ToString () };

			return Json (items, JsonRequestBehavior.AllowGet);
		}
	}
}