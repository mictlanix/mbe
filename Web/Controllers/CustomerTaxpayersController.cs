// 
// AddressesController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
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
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
	public class CustomerTaxpayersController : Controller
    {
		public ActionResult Create (int owner)
        {
			ViewBag.OwnerId = owner;
			return PartialView ("_Create");
		}

        [HttpPost]
		public ActionResult Create (int owner, CustomerTaxpayer item)
		{
			var entity = CustomerTaxpayer.TryFind (item.Id);

			if(entity != null) {
				ModelState.AddModelError ("", Resources.CustomerTaxpayerAlreadyExists);
			}

			if(!item.HasAddress) {
				ModelState.Where(x => x.Key.StartsWith("Address.")).ToList().ForEach(x => x.Value.Errors.Clear());
				item.Address = null;
			}

			if (!ModelState.IsValid) {
				ViewBag.OwnerId = owner;
				return PartialView ("_Create", item);
			}

			using (var scope = new TransactionScope()) {
				item.Customer = Customer.Find (owner);

				if(item.HasAddress) {
					item.Address.Create ();
				}

                item.Create ();
				item.Customer.Taxpayers.Add (item);
				item.Customer.Update ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Details (string id)
		{
			var item = CustomerTaxpayer.Find (id);
			return PartialView ("_Details", item);
		}

		public ActionResult Edit (string id)
        {
        	var item = CustomerTaxpayer.Find (id);

			item.HasAddress = (item.Address != null);

			return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit (CustomerTaxpayer item)
		{
			if(!item.HasAddress) {
				ModelState.Where(x => x.Key.StartsWith("Address.")).ToList().ForEach(x => x.Value.Errors.Clear());
				item.Address = null;
			}
			
			if (!ModelState.IsValid) {
				return PartialView ("_Edit", item);
			}
			
			var entity = CustomerTaxpayer.Find (item.Id);
			var address = entity.Address;

			entity.HasAddress = (address != null);
			entity.Name = item.Name;
			entity.Email = item.Email;

			using (var scope = new TransactionScope()) {
				if(item.HasAddress) {
					entity.Address = item.Address;
					entity.Address.Create ();
				}

				entity.UpdateAndFlush ();
			}

			if(address != null) {
				try {
					using (var scope = new TransactionScope()) {
						address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}

			return PartialView ("_Refresh");
        }

		public ActionResult Delete (string id)
        {
            var item = CustomerTaxpayer.Find (id);
			return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (string id)
		{
			var item = CustomerTaxpayer.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					item.Customer.Taxpayers.Remove (item);
					item.Customer.Update ();
					item.DeleteAndFlush ();
				}
			} catch (GenericADOException ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}
			
			if(item.Address != null) {
				try {
					using (var scope = new TransactionScope()) {
						item.Address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}

			return PartialView ("_Refresh");
        }
    }
}