// 
// WarehousesController.cs
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
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
    public class WarehousesController : Controller
    {
        //
        // GET: /Warehouses/

        public ViewResult Index()
        {
            var qry = from x in Warehouse.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Warehouses/Details/5

        public ViewResult Details(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // GET: /Warehouses/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Warehouses/Create

        [HttpPost]
        public ActionResult Create(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.Save();
                return RedirectToAction("Index");
            }

            return View(warehouse);
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // POST: /Warehouses/Edit/5

        [HttpPost]
        public ActionResult Edit(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.Save();
                return RedirectToAction("Index");
            }
            return View(warehouse);
        }

        //
        // GET: /Warehouses/Delete/5

        public ActionResult Delete(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            return View(warehouse);
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Warehouse warehouse = Warehouse.Find(id);
            warehouse.Delete();
            return RedirectToAction("Index");
        }



        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Warehouse.Queryable
                      where x.Code.Contains(pattern) ||
                            x.Name.Contains(pattern)
                      select new { id = x.Id, name = x.Name};

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult List()
        {
            var qry = from x in Warehouse.Queryable
                      select new { id = x.Id, name = x.Name };

            var dict = qry.ToDictionary(x => x.id.ToString(), x => x.name);

            //dict.Add("selected", id.ToString());

            return Json(dict, JsonRequestBehavior.AllowGet);
        }
    }
}