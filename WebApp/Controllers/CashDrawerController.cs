// 
// CashDrawerController.cs
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
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class CashDrawerController : Controller
    {
        //
        // GET: /cashDrawer/

        public ViewResult Index()
        {
            var qry = from x in CashDrawer.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /cashDrawer/Details/5

        public ViewResult Details(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /cashDrawer/Create

        [HttpPost]
        public ActionResult Create(CashDrawer cashDrawer)
        {
            if (ModelState.IsValid)
            {
                cashDrawer.Save();
                return RedirectToAction("Index");
            }

            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Edit/5

        public ActionResult Edit(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // POST: /cashDrawer/Edit/5

        [HttpPost]
        public ActionResult Edit(CashDrawer cashDrawer)
        {
            if (ModelState.IsValid)
            {
                cashDrawer.Save();
                return RedirectToAction("Index");
            }
            return View(cashDrawer);
        }

        //
        // GET: /cashDrawer/Delete/5

        public ActionResult Delete(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            return View(cashDrawer);
        }

        //
        // POST: /cashDrawer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            CashDrawer cashDrawer = CashDrawer.Find(id);
            cashDrawer.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}