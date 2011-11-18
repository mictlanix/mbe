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
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;

namespace Business.Essentials.WebApp.Controllers
{
    public class AccountController : Controller
    {
        bool ValidateUser(string username, string password)
        {
            User user = Model.User.Queryable.SingleOrDefault(x => x.UserName == username);
            return user != null && user.Password == SHA1(password);
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
                Password = SHA1(password),
                Employee = Employee.Find(employee),
                Email = email
            };

            user.Create();

            return true;
        }

        bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            User user = Model.User.Find(username);
            string pwd = SHA1(oldPassword);

            if (user == null || user.Password != pwd)
                return false;

            user.Password = SHA1(newPassword);
            user.Save();
            
            return true;
        }


        protected string SHA1(string text)
        {
            byte[] bytes = Encoding.Default.GetBytes("" + text);
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

            return BitConverter.ToString(sha1.ComputeHash(bytes)).Replace("-", "");
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

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }
    }
}
