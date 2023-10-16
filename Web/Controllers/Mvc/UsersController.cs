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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class UsersController : CustomController {
		public ActionResult Index ()
		{
			var qry = from x in Model.User.Queryable
				  select x;

			return View (qry.ToList ());
		}

		public ViewResult Details (string id)
		{
			User user = Model.User.Find (id);
			return View (user);
		}

		public ActionResult Edit (string id)
		{
			User user = Model.User.Find (id);

			if (user.UserSettings == null) {
				var storeId = int.Parse (WebConfig.DefaultStore);
				var store = Store.TryFind (storeId);

				var pointOfSaleId = int.Parse (WebConfig.DefaultPointOfSale);
				var pointOfSale = PointOfSale.TryFind (pointOfSaleId);

				user.UserSettings = new UserSettings () {
					UserName = user.UserName,
					Store = store,
					PointOfSale = pointOfSale
				};
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

						user.UserSettings = new UserSettings () {
							UserName = user.UserName,
							Store = store,
							PointOfSale = pointOfSale,
							CashDrawer = cashDrawer
						};
					} else{
						user.UserSettings.Store = Store.Find (item.UserSettings.StoreId);
						user.UserSettings.PointOfSale = PointOfSale.Find (item.UserSettings.PointOfSaleId);

						if (item.UserSettings.CashDrawerId.HasValue) {
							user.UserSettings.CashDrawer = CashDrawer.Find (item.UserSettings.CashDrawerId);
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

				user.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");

		}

		public ActionResult Delete (string id)
		{
			User item = Model.User.Find (id);
			return View (item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (string id)
		{
			var item = Model.User.Find (id);
			var settings = Model.UserSettings.TryFind (id);

			using (var scope = new TransactionScope ()) {
				foreach (var x in item.Privileges.ToList ()) {
					x.Delete ();
				}

				if (settings != null && WebConfig.UserSettingsMode == UserSettingsMode.Managed) {
					settings.Delete ();
				}

				scope.Flush ();
				item.DeleteAndFlush ();
			}

			return RedirectToAction ("Index");
		}
	}
}