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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class ProductsController : Controller
    {
        //
        // GET: /Products/

        public ActionResult Index ()
        {
            var qry = from x in Product.Queryable
                      orderby x.Name
                      select x;

            var search = new Search<Product>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View (search);
        }

        //
        // POST: /Products/

        [HttpPost]
        public ActionResult Index (Search<Product> search)
        {
            if (ModelState.IsValid) {
                search = GetProducts(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView ("_Index", search);
            } else {
                return View (search);
            }
        }

        //
        // GET: /Products/Details/5

        public ActionResult Details(int id)
        {
            Product product = Product.Find(id);

            if (Request.IsAjaxRequest()) {
                return PartialView("_Details", product);
            } else {
                return View(product);
            }
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
        public ActionResult Create (Product item, HttpPostedFileBase file)
		{
			if (!ModelState.IsValid)
				return View (item);
            
			item.IsTaxIncluded = Configuration.IsTaxIncluded;
			item.Supplier = Supplier.Find (item.SupplierId);
			item.Category = Category.Find (item.CategoryId);
			item.Photo = SavePhoto (file) ?? Configuration.DefaultPhotoFile;

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

        //
        // GET: /Products/Edit/5

        public ActionResult Edit(int id)
        {
            Product item = Product.Find (id);
            return View (item);
        }

        //
        // POST: /Products/Edit/5

        [HttpPost]
        public ActionResult Edit (Product item, HttpPostedFileBase file)
		{
			if (!ModelState.IsValid)
				return View (item);
            
			var entity = Product.Find (item.Id);

			entity.Brand = item.Brand;
			entity.Code = item.Code;
			entity.Comment = item.Comment;
			entity.IsInvoiceable = item.IsInvoiceable;
			entity.IsPerishable = item.IsPerishable;
			entity.IsSeriable = item.IsSeriable;
			entity.IsTaxIncluded = item.IsTaxIncluded;
			entity.Location = item.Location;
			entity.Model = item.Model;
			entity.Name = item.Name;
			entity.SKU = item.SKU;
			entity.TaxRate = item.TaxRate;
			entity.UnitOfMeasurement = item.UnitOfMeasurement;
			entity.IsTaxIncluded = Configuration.IsTaxIncluded;
			entity.Supplier = Supplier.Find (item.SupplierId);
			entity.Category = Category.Find (item.CategoryId);
			entity.Photo = SavePhoto (file) ?? item.Photo;

			using (var scope = new TransactionScope ()) {
            	entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

        //
        // GET: /Products/Delete/5

        public ActionResult Delete(int id)
        {
            Product item = Product.Find(id);
            return View(item);
        }

        //
        // POST: /Products/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed (int id)
        {
			try {
				using (var scope = new TransactionScope()) {
					var item = Product.Find (id);
					item.DeleteAndFlush ();
				}

				return RedirectToAction ("Index");
			} catch (GenericADOException) {
				return View ("DeleteUnsuccessful");
			}
        }

        Search<Product> GetProducts(Search<Product> search)
        {
			var qry = from x in Product.Queryable
					  orderby x.Name
					  select x;

            if (!string.IsNullOrEmpty(search.Pattern)) {
                qry = from x in Product.Queryable
                      where x.Name.Contains(search.Pattern) ||
                            x.Code.Contains(search.Pattern) ||
                            x.SKU.Contains(search.Pattern) ||
                            x.Brand.Contains(search.Pattern)
                      orderby x.Name
                      select x;
                
            }

			search.Total = qry.Count();
			search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
			
            return search;
        }

        string SavePhoto (HttpPostedFileBase file)
		{
			if (file == null || file.ContentLength == 0)
				return null;
			
			using (var stream = file.InputStream) {
				using (var img = Image.FromStream (stream)) {
					var hash = string.Format ("{0}.png", HashFromImage (img));
					var path = Path.Combine (Server.MapPath (Configuration.PhotosPath), hash);
					
					img.Save (path, ImageFormat.Png);
					
					return Path.Combine (Configuration.PhotosPath, hash);
				}
			}
		}
		
		string HashFromStream (Stream stream)
		{
			string hash;
			byte[] bytes = null;

			using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
				bytes = sha1.ComputeHash (stream);
				hash = BitConverter.ToString (bytes).Replace ("-", "").ToLower ();
			}

			return hash;
		}
		
        string HashFromImage(Image img)
        {
            string hash;
            byte[] bytes = null;

            using (MemoryStream ms = new MemoryStream ())
            {
                img.Save (ms, img.RawFormat);
                bytes = ms.ToArray ();
            }

            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                bytes = sha1.ComputeHash (bytes);
                hash = BitConverter.ToString (bytes).Replace ("-", "").ToLower ();
            }

            return hash;
        }

		// AJAX
		// GET: /Products/GetSuggestions

		public JsonResult GetSuggestions (string pattern)
		{
			ArrayList items = new ArrayList (15);
			var qry = from x in Product.Queryable
                      where x.Name.Contains (pattern) ||
							x.Code.Contains (pattern) ||
							x.SKU.Contains (pattern)
					  orderby x.Name
                      select x;
			
			foreach (var x in qry.Take(15)) {
				var item = new { 
                    id = x.Id,
                    name = x.Name, 
                    code = x.Code, 
                    sku = x.SKU, 
                    url = Url.Content (x.Photo)
				};
                
				items.Add (item);
			}
			
			return Json (items, JsonRequestBehavior.AllowGet);
		}
    }
}
