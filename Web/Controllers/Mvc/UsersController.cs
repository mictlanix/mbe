// 
// UsersController.cs
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
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Castle.ActiveRecord;
using Castle.Core.Internal;
using Gma.QrCodeNet.Encoding.DataEncodation;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class UsersController : CustomController {
		public ActionResult Index ()
		{
			var qry = from x in Model.User.Queryable
					  select x;

			var search = SearchUsers (new Search<User> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<User> search)
		{
			if (ModelState.IsValid) {
				search = SearchUsers (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		public ViewResult Details (string id)
		{
			User user = Model.User.Find (id);
			return View (user);
		}

		Search<User> SearchUsers (Search<User> search)
		{
			var query = from x in Model.User.Queryable
						select x;
			if (!string.IsNullOrEmpty (search.Pattern)) {
				query = from x in query
						where x.UserName.Contains (search.Pattern)
						|| x.Email.Contains (search.Pattern)
						|| x.Employee.FirstName.Contains (search.Pattern)
						|| x.Employee.LastName.Contains (search.Pattern)
						select x;
			}
			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Edit (string id)
		{
			var user = Model.User.Find (id);
			//var storeId = user.UserSettings == null || user.UserSettings.Store == null ?  int.Parse (WebConfig.DefaultStore) : user.UserSettings.Store.Id;

			//Int32.TryParse(user.UserSettings.Store.Id, out storeId);
			//var store = MBEQueryable.IQStores.SingleOrDefault(x => x.Id == storeId);

			//var pointOfSaleId = user.UserSettings == null ? int.Parse (WebConfig.DefaultPointOfSale) : user.UserSettings.PointOfSale.Id;
			//var pointOfSale = MBEQueryable.IQPointsOfSales.SingleOrDefault(x => x.Id == pointOfSaleId);

			if (user.UserSettings == null) {
				var defaultStoreId = int.Parse (WebConfig.DefaultStore);
				var defaultStore = MBEQueryable.IQStores.Single (x => x.Id == defaultStoreId);
				var defaultPointOfSaleId = int.Parse (WebConfig.DefaultPointOfSale); ;
				var defaultPointOfSale = MBEQueryable.IQPointsOfSales.Single (x => x.Id == defaultPointOfSaleId);
				var defaultCashDrawerId = (int?) null;
				user.UserSettings = new UserSettings {
					UserName = user.UserName,
					StoreId = defaultStoreId,
					Store = defaultStore,
					PointOfSale = defaultPointOfSale,
					PointOfSaleId = defaultPointOfSaleId,
					CashDrawerId = defaultCashDrawerId,
				};
			} else {
				user.UserSettings.StoreId = user.UserSettings.Store.Id;
				user.UserSettings.PointOfSaleId = user.UserSettings.PointOfSale != null ? user.UserSettings.PointOfSale.Id : (int?) null;
				user.UserSettings.CashDrawerId = user.UserSettings.CashDrawer != null ? user.UserSettings.CashDrawer.Id : (int?) null;
			}

			return View (user);
		}

		[HttpPost]
		public ActionResult Edit (User item)
		{
			if (!ModelState.IsValid) {
				return View (item);
			}

			using (var scope = new TransactionScope ()) {
				var user = Model.User.Find (item.UserName);

				var incidence = new Incidence {
					ModificationTime = DateTime.Now,
					SourceType = SourceType.UserSettings,
					Updater = CurrentUser.Employee,
					PreviousState = user.UserName.ToString (),
					Reference = user.EmployeeId,
				};
				incidence.CreateAndFlush ();

				user.Employee = Employee.Find (item.EmployeeId);
				user.Email = item.Email;
				user.IsAdministrator = item.IsAdministrator;

				if (WebConfig.UserSettingsMode == UserSettingsMode.Managed) {
					if (user.UserSettings == null) {
						var store = Store.TryFind (item.UserSettings.StoreId);
						var pointOfSale = PointOfSale.TryFind (item.UserSettings.PointOfSaleId);
						CashDrawer cashDrawer = null;

						if (item.UserSettings.CashDrawerId.HasValue) {
							cashDrawer = CashDrawer.Find (item.UserSettings.CashDrawerId);
						}

						user.UserSettings = new UserSettings {
							UserName = user.UserName,
							Store = store,
							PointOfSale = pointOfSale,
							CashDrawer = cashDrawer
						};
					} else {
						user.UserSettings.Store = Store.Find (item.UserSettings.StoreId);
						user.UserSettings.PointOfSale = item.UserSettings.PointOfSaleId.HasValue ?
							PointOfSale.TryFind (item.UserSettings.PointOfSaleId.Value) : null;

						if (item.UserSettings.CashDrawerId.HasValue) {
							user.UserSettings.CashDrawer = CashDrawer.TryFind (item.UserSettings.CashDrawerId);
						}
					}
				}

				foreach (var i in Enum.GetValues (typeof (SystemObjects))) {
					var obj = (SystemObjects) i;
					string prefix = Enum.GetName (typeof (SystemObjects), i);
					var privilege = user.Privileges.SingleOrDefault (x => x.Object == obj);

					if (privilege == null) {
						privilege = new AccessPrivilege { User = user, Object = obj };
					}

					foreach (var j in Enum.GetValues (typeof (AccessRight))) {
						AccessRight right = (AccessRight) j;
						string name = prefix + Enum.GetName (typeof (AccessRight), j);
						string value = Request.Params [name];

						if (value == null)
							continue;

						if (value.Contains ("true"))
							privilege.Privileges |= right;
						else
							privilege.Privileges &= ~right;
					}

					privilege.Save ();
				}

				if (WebConfig.UserSettingsMode == UserSettingsMode.Managed) {
					user.UserSettings.Save ();
				}

				//var session = FormsAuthentication.GetAuthCookie()

				user.SessionVersion++;
				user.UpdateAndFlush ();

			}

			return RedirectToAction ("Index");

		}

		public ActionResult Delete (string id)
		{
			User item = Model.User.TryFind (id);
			if (item == null) {
				return RedirectToAction ("Index");
			}
			return View (item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (string id)
		{
			using (var scope = new TransactionScope ()) {
				var settings = UserSettings.TryFind (id);

				if (settings != null) {
					settings.Delete ();
				}

				var privileges = (from x in AccessPrivilege.Queryable
								  where x.User.UserName == id
								  select x).ToList ();

				privileges.ForEach (privilege => { privilege.Delete (); });
				scope.Flush ();

				var item = Model.User.Find (id);

				item.DeleteAndFlush ();
			}

			return RedirectToAction ("Index");
		}
	}
}