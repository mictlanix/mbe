// 
// ReturnsCustomerController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
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
using Castle.ActiveRecord;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class ReturnsCustomerController : Controller
    {
        //
        // GET: /Returns/

        public ActionResult Index()
        {
            var qry = from x in ReturnCustomer.Queryable
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        [HttpPost]
        public ActionResult Index(int id)
        {
            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && x.IsPaid &&
                            x.Id == id
                      select x;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Index", qry.ToList());
            }
            else
            {
                return View(new SalesOrder());

            }
        }


        //
        // GET: /Returns/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Returns/Create

        public ActionResult Return(int id)
        {
            SalesOrder sales = SalesOrder.Find(id);

            ReturnCustomer item = new ReturnCustomer();
            item.CreationTime = DateTime.Now;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.SalesOrder = sales;
            item.SalesPerson = sales.SalesPerson;
            item.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.ModificationTime = DateTime.Now;

            item.Create();

            return View(item);
        } 

        //
        // POST: /Returns/Create

        [HttpPost]
        public ActionResult Return(FormCollection collection)
        {
            return PartialView();
        }
        
        //
        // GET: /Returns/Edit/5
 
        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /Returns/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Returns/Delete/5
 
        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Returns/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
