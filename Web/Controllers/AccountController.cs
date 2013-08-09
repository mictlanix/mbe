// 
// AccountController.cs
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
using System.Web.Routing;
using System.Web.Security;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class AccountController : Controller
    {
        bool ValidateUser(string username, string password)
        {
            var item = Model.User.Queryable.SingleOrDefault(x => x.UserName == username);
            return item != null && item.Password ==  SecurityHelpers.SHA1 (password);
        }

        bool CreateUser (string username, string password, string email)
		{
			username = username.ToLower ();

			if (Model.User.Queryable.Count (x => x.UserName == username) > 0) {
				throw new Exception (Mictlanix.BE.Resources.Message_UserNameAlreadyExists);
			}

			var item = new User {
                UserName = username,
                Password = SecurityHelpers.SHA1 (password),
				Email = email.ToLower()
            };

			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

            return true;
        }

        bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            User item = Model.User.Find(username);
            string pwd = SecurityHelpers.SHA1 (oldPassword);

            if (item == null || item.Password != pwd)
                return false;

            item.Password = SecurityHelpers.SHA1 (newPassword);
			
			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();
			}
        
            return true;
        }

        public ActionResult LogOn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
				return View(model);
            
			// force lowercase for usernames
            model.UserName = model.UserName.ToLower();

            if (ValidateUser(model.UserName, model.Password)) {
                FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")) {
                    return Redirect(returnUrl);
                } else {
                    return RedirectToAction("Index", "Home");
                }
            } else {
                ModelState.AddModelError("", Mictlanix.BE.Resources.Message_InvalidUserPassword);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
				return View(model);
            
			// force lowercase for usernames
            model.UserName = model.UserName.ToLower();

            try {
				// Attempt to register the user
                if (CreateUser(model.UserName, model.Password, model.Email)) {
                    FormsAuthentication.SetAuthCookie(model.UserName, false);
                    return RedirectToAction("Index", "Home");
                } else {
                    ModelState.AddModelError("", Mictlanix.BE.Resources.Message_UnknownError);
                }
            } catch (Exception ex) {
                ModelState.AddModelError("", ex.Message);                    
            }

			// If we got this far, something failed, redisplay form
			return View(model);
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

		[HttpPost]
		[Authorize]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;

                try {
                    changePasswordSucceeded = ChangePassword (User.Identity.Name, model.OldPassword, model.NewPassword);
                } catch (Exception) {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded) {
                    return RedirectToAction ("ChangePasswordSuccess");
                } else {
                    ModelState.AddModelError ("", Mictlanix.BE.Resources.Message_ChangePasswordWrong);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

		[Authorize]
        public ActionResult ChangePasswordSuccess ()
		{
			return View ();
		}

		[Authorize]
		public ActionResult LocalSettings ()
		{
			LocalSettings item = new LocalSettings {
				Store = Configuration.Store,
				PointOfSale = Configuration.PointOfSale,
				CashDrawer = Configuration.CashDrawer
			};
			
			return View (item);
		}
		
		[HttpPost]
		[Authorize]
		public ActionResult LocalSettings (LocalSettings item)
		{
			item.Store = Store.TryFind (item.StoreId);
			item.PointOfSale = PointOfSale.TryFind (item.PointOfSaleId);
			item.CashDrawer = CashDrawer.TryFind (item.CashDrawerId.GetValueOrDefault ());
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			Response.Cookies.Add (new HttpCookie (Configuration.StoreCookieKey) {
				Value = item.Store.Id.ToString (),
				Expires = DateTime.Now.AddYears (100)
			});
			
			if (item.PointOfSale != null) {
				Response.Cookies.Add(new HttpCookie(Configuration.PointOfSaleCookieKey) {
					Value = item.PointOfSale.Id.ToString (),
					Expires = DateTime.Now.AddYears (100)
				});
			} else {
				if (Request.Cookies [Configuration.PointOfSaleCookieKey] != null) {
					Response.Cookies.Add(new HttpCookie(Configuration.PointOfSaleCookieKey) {
						Value = string.Empty,
						Expires = DateTime.Now.AddDays (-1d)
					});
				}
			}
			
			if (item.CashDrawer != null) {
				Response.Cookies.Add(new HttpCookie(Configuration.CashDrawerCookieKey) {
					Value = item.CashDrawer.Id.ToString (),
					Expires = DateTime.Now.AddYears (100)
				});
			} else {
				if (Request.Cookies [Configuration.CashDrawerCookieKey] != null) {
					Response.Cookies.Add(new HttpCookie(Configuration.CashDrawerCookieKey) {
						Value = string.Empty,
						Expires = DateTime.Now.AddDays (-1d)
					});
				}
			}
			
			return RedirectToAction ("Index", "Home");
		}
    }
}