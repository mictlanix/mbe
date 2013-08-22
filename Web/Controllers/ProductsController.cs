// 
// ProductsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
	[Authorize]
    public class ProductsController : Controller
    {
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

        [HttpPost]
        public ActionResult Index (Search<Product> search)
        {
            if (ModelState.IsValid) {
                search = GetProducts (search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView ("_Index", search);
            } else {
                return View (search);
            }
        }

        public ActionResult Details(int id)
        {
            Product product = Product.Find(id);

            if (Request.IsAjaxRequest()) {
                return PartialView("_Details", product);
            } else {
                return View(product);
            }
        }

        public ActionResult Create()
        {
            return View(new Product ());
        }

        [HttpPost]
        public ActionResult Create (Product item, HttpPostedFileBase file)
		{
			item.Supplier = Supplier.TryFind (item.SupplierId);

			if (!ModelState.IsValid)
				return View (item);
            
			item.TaxRate = Configuration.DefaultVAT;
			item.IsTaxIncluded = Configuration.IsTaxIncluded;
			item.PriceType = Configuration.DefaultPriceType;
			item.Photo = SavePhoto (file) ?? Configuration.DefaultPhotoFile;

			using (var scope = new TransactionScope ()) {
            	item.CreateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

        public ActionResult Edit(int id)
        {
            Product item = Product.Find (id);
            return View (item);
        }

        [HttpPost]
        public ActionResult Edit (Product item, HttpPostedFileBase file)
		{
			item.Supplier = Supplier.TryFind (item.SupplierId);

			if (!ModelState.IsValid)
				return View (item);
            
			var entity = Product.Find (item.Id);

			entity.Brand = item.Brand;
			entity.Code = item.Code;
			entity.Comment = item.Comment;
			entity.IsStockable = item.IsStockable;
			entity.IsPerishable = item.IsPerishable;
			entity.IsSeriable = item.IsSeriable;
			entity.IsPurchasable = item.IsPurchasable;
			entity.IsSalable = item.IsSalable;
			entity.IsInvoiceable = item.IsInvoiceable;
			entity.Location = item.Location;
			entity.Model = item.Model;
			entity.Name = item.Name;
			entity.SKU = item.SKU;
			entity.UnitOfMeasurement = item.UnitOfMeasurement;
			entity.Photo = SavePhoto (file) ?? item.Photo;

			using (var scope = new TransactionScope ()) {
            	entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

        public ActionResult Delete(int id)
        {
            var item = Product.Find(id);
            return View(item);
        }

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
							x.Model.Contains(search.Pattern) ||
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

		public JsonResult GetSuggestions (string pattern)
		{
			var query = from x in Product.Queryable
						where x.Name.Contains (pattern) ||
							x.Code.Contains (pattern) ||
							x.Model.Contains (pattern) ||
							x.SKU.Contains (pattern) ||
							x.Brand.Contains (pattern)
						orderby x.Name
						select x;

			var items = from x in query.Take (15).ToList ()
				select new {
				id = x.Id, name = x.Name, code = x.Code, model = x.Model,
				sku = x.SKU, url = Url.Content (x.Photo)
			};

			return Json (items, JsonRequestBehavior.AllowGet);
		}
		
		public ActionResult Labels (int id)
		{
			var item = Product.Find (id);
			return PartialView ("_Labels", item.Labels);
		}

		public ActionResult EditLabels (int id)
		{
			var item = Product.Find (id);
			var items = new Dictionary<Label, bool>();

			foreach(var label in Label.Queryable.OrderBy (x => x.Name).ToList ()) {
				items.Add (label, item.Labels.Contains (label));
			}

			return PartialView ("_EditLabels", items);
		}

		[HttpPost]
		public ActionResult AddLabel (int id, int value)
		{
			var entity = Product.Find (id);

			using (var scope = new TransactionScope ()) {
				entity.Labels.Add (Label.Find (value));
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Labels", entity.Labels);
		}

		[HttpPost]
		public ActionResult RemoveLabel (int id, int value)
		{
			var entity = Product.Find (id);

			using (var scope = new TransactionScope ()) {
				entity.Labels.Remove (Label.Find (value));
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Labels", entity.Labels);
		}

		public JsonResult UnitsOfMeasurement ()
		{
			var qry = from x in Enum.GetValues (typeof(UnitOfMeasurement)).Cast<UnitOfMeasurement> ()
					  select new {
						  value = x.GetDisplayName (),
						  text = x.GetDisplayName ()
					  };

			return Json (qry.ToList(), JsonRequestBehavior.AllowGet);
		}
    }
}
