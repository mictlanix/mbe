// 
// TaxpayerRecipientsController.cs
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
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
	public class TaxpayerRecipientsController : Controller
    {
		public ActionResult Index ()
		{
			var search = SearchTaxpayers (new Search<TaxpayerRecipient> {
				Limit = Configuration.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<TaxpayerRecipient> search)
		{
			if (ModelState.IsValid) {
				search = SearchTaxpayers (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		Search<TaxpayerRecipient> SearchTaxpayers (Search<TaxpayerRecipient> search)
		{
			IQueryable<TaxpayerRecipient> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();

			if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in TaxpayerRecipient.Queryable
				        orderby x.Name
				        select x;
			} else {
				query = from x in TaxpayerRecipient.Queryable
				        where x.Id.Contains (pattern) ||
				            x.Name.Contains (pattern)
				        orderby x.Name
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
		public ActionResult Create (TaxpayerRecipient item)
		{
			if (!string.IsNullOrEmpty (item.Id)) {
				var entity = TaxpayerRecipient.TryFind (item.Id);

				if (entity != null) {
					ModelState.AddModelError ("", Resources.TaxpayerRecipientAlreadyExists);
				}
			}

			if (!item.HasAddress) {
				ModelState.Where (x => x.Key.StartsWith ("Address.")).ToList ().ForEach (x => x.Value.Errors.Clear ());
				item.Address = null;
			}

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			item.Id = item.Id.ToUpper ().Trim ();
			item.Name = item.Name.Trim ();
			item.Email = item.Email.Trim ();

			using (var scope = new TransactionScope ()) {
				if (item.HasAddress) {
					item.Address.Create ();
				}

				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		public ActionResult Details (string id)
		{
			var item = TaxpayerRecipient.Find (id);

			return PartialView ("_Details", item);
		}

		public ActionResult Edit (string id)
        {
        	var item = TaxpayerRecipient.Find (id);

			item.HasAddress = (item.Address != null);

			return PartialView ("_Edit", item);
        }

        [HttpPost]
        public ActionResult Edit (TaxpayerRecipient item)
		{
			if(!item.HasAddress) {
				ModelState.Where(x => x.Key.StartsWith("Address.")).ToList().ForEach(x => x.Value.Errors.Clear());
				item.Address = null;
			}
			
			if (!ModelState.IsValid) {
				return PartialView ("_Edit", item);
			}
			
			var entity = TaxpayerRecipient.Find (item.Id);
			var address = entity.Address;

			entity.HasAddress = (address != null);
			entity.Name = item.Name.Trim ();
			entity.Email = item.Email.Trim ();

			using (var scope = new TransactionScope()) {
				if(item.HasAddress) {
					entity.Address = item.Address;
					entity.Address.Create ();
				} else {
					entity.Address = null;
				}

				entity.UpdateAndFlush ();
			}

			if (address != null) {
				try {
					using (var scope = new TransactionScope ()) {
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
            var item = TaxpayerRecipient.Find (id);
			return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (string id)
		{
			var item = TaxpayerRecipient.Find (id);

			try {
				using (var scope = new TransactionScope()) {
					item.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}
			
			if (item.Address != null) {
				try {
					using (var scope = new TransactionScope()) {
						item.Address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}

			return PartialView ("_DeleteSuccesful", item);
        }
    }
}