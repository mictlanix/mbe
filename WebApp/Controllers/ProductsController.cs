// 
// ProductsController.cs
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
using System.Data;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using System.IO;

namespace Business.Essentials.WebApp.Controllers
{
    public class ProductsController : Controller
    {

        // AJAX
        // GET: /Products/GetSuggestions

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Product.Queryable
                      where x.Name.Contains(pattern) ||
                            x.Code.Contains(pattern) ||
                            x.SKU.Contains(pattern)
                      select new { id = x.Id, name = x.Name, code = x.Code, sku = x.SKU, url = string.Format("/Photos/{0}.png", x.Code.Trim()) };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Products/

        public ActionResult Index(string pattern, int? offset, int? limit)
        {
            if (pattern == null)
            {
                return View(new Search<Product> { Limit = 25 });
            }

            return View(GetProducts(new Search<Product> { Pattern = pattern, Offset = offset.Value, Limit = limit.Value }));
        }

        //
        // POST: /Products/

        [HttpPost]
        public ActionResult Index(Search<Product> search)
        {
            if (!ModelState.IsValid)
            {
                return View(search);
            }

            search.Offset = 0;
            return View(GetProducts(search));
        }

        //
        // GET: /Products/Details/5

        public ViewResult Details(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // GET: /Products/Create

        public ActionResult Create()
        {
            return View(new Product { IsInvoiceable = true, TaxRate = 0.16m });
        }

        //
        // POST: /Products/Create

        [HttpPost]
        public ActionResult Create(Product product, HttpPostedFileBase file)
        {
            
            if (ModelState.IsValid)
            {
                Supplier supplier = Supplier.Find(product.SupplierId);
                product.Supplier = supplier;
                
                Category category = Category.Find(product.CategoryId);
                product.Category = category;
                product.Save();
                SavePhoto(product.Photo, file);

                return RedirectToAction("Index");
            }
            return View(product);
        }

        //
        // GET: /Products/Edit/5

        public ActionResult Edit(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // POST: /Products/Edit/5

        [HttpPost]
        public ActionResult Edit(Product product, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                Supplier supplier = Supplier.Find(product.SupplierId);
                product.Supplier = supplier;

                Category category = Category.Find(product.CategoryId);
                product.Category = category;
                product.Save();
                SavePhoto(product.Photo, file);

                return RedirectToAction("Index");
            }
            return View(product);
        }

        //
        // GET: /Products/Delete/5

        public ActionResult Delete(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // POST: /Products/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = Product.Find(id);
            product.Delete();
            return RedirectToAction("Index");
        }

        Search<Product> GetProducts(Search<Product> search)
        {
            var qry = from x in Product.Queryable
                      where x.Name.Contains(search.Pattern) ||
                            x.Code.Contains(search.Pattern) ||
                            x.SKU.Contains(search.Pattern) ||
                            x.Brand.Contains(search.Pattern)
                      orderby x.Name
                      select x;

            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();

            return search;
        }

        void SavePhoto(string fileName, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                var img = System.Drawing.Image.FromStream(file.InputStream);
                var path = Path.Combine(Server.MapPath("~/Photos"), fileName);
                img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                img.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
