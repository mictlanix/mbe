// 
// AccountController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class AccountController : Controller
    {
        bool ValidateUser(string username, string password)
        {
            User user = Model.User.Queryable.SingleOrDefault(x => x.UserName == username);
            return user != null && user.Password ==  SecurityHelpers.SHA1(password);
        }

        bool CreateUser(string username, string password, int employee, string email)
        {
            username = username.ToLower();

            if (Model.User.Queryable.Count(x => x.UserName == username) > 0)
            {
                throw new Exception(Business.Essentials.Resources.Message_UserNameAlreadyExists);
            }

            User user = new User
            {
                UserName = username,
                Password = SecurityHelpers.SHA1(password),
                Employee = Employee.Find(employee),
                Email = email
            };

            user.Create();

            return true;
        }

        bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            User user = Model.User.Find(username);
            string pwd = SecurityHelpers.SHA1(oldPassword);

            if (user == null || user.Password != pwd)
                return false;

            user.Password = SecurityHelpers.SHA1(newPassword);
            user.Save();
            
            return true;
        }

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.UserName.ToLower();

                if (ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", Business.Essentials.Resources.Message_InvalidUserPassword);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            return View(new RegisterModel());
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                model.UserName = model.UserName.ToLower();

                try
                {
                    if (CreateUser(model.UserName, model.Password, model.EmployeeId, model.Email))
                    {
                        FormsAuthentication.SetAuthCookie(model.UserName, false);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", Business.Essentials.Resources.Message_UnknownError);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);                    
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;

                try
                {
                    changePasswordSucceeded = ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", Business.Essentials.Resources.Message_ChangePasswordWrong);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess ()
		{
			return View ();
		}
		
		public ActionResult LocalSettings ()
		{
			LocalSettings item = new LocalSettings ();
			
			if (Request.Cookies ["Store"] != null) {
				item.Store = Store.TryFind (int.Parse (Request.Cookies ["Store"].Value));
			}
			
			if (Request.Cookies ["PointOfSale"] != null) {
				item.PointOfSale = PointOfSale.TryFind (int.Parse (Request.Cookies ["PointOfSale"].Value));
			}
			
			if (Request.Cookies ["CashDrawer"] != null) {
				item.CashDrawer = CashDrawer.TryFind (int.Parse (Request.Cookies ["CashDrawer"].Value));
			}
			
			return View (item);
		}
		
		[HttpPost]
		public ActionResult LocalSettings (LocalSettings item)
		{
			item.Store = Store.TryFind (item.StoreId);
			item.PointOfSale = PointOfSale.TryFind (item.PointOfSaleId);
			item.CashDrawer = CashDrawer.TryFind (item.CashDrawerId.GetValueOrDefault ());
			
			if (!ModelState.IsValid) {
				return View (item);
			}
			
			Response.Cookies ["Store"].Value = item.Store.Id.ToString ();
			Response.Cookies ["Store"].Expires = DateTime.Now.AddYears (100);
			
			if (item.PointOfSale != null) {
				Response.Cookies ["PointOfSale"].Value = item.PointOfSale.Id.ToString ();
				Response.Cookies ["PointOfSale"].Expires = DateTime.Now.AddYears (100);
			} else {
				if (Request.Cookies ["PointOfSale"] != null) {
					Response.Cookies ["PointOfSale"].Value = string.Empty;
					Response.Cookies ["PointOfSale"].Expires = DateTime.Now.AddDays (-1d);
				}
			}
			
			if (item.CashDrawer != null) {
				Response.Cookies ["CashDrawer"].Value = item.CashDrawer.Id.ToString ();
				Response.Cookies ["CashDrawer"].Expires = DateTime.Now.AddYears (100);
			} else {
				if (Request.Cookies ["CashDrawer"] != null) {
					Response.Cookies ["CashDrawer"].Value = string.Empty;
					Response.Cookies ["CashDrawer"].Expires = DateTime.Now.AddDays (-1d);
				}
			}
			
			return RedirectToAction ("Index", "Home");
		}
    }
}
