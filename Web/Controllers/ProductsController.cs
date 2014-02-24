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
			var search = SearchProducts (new Search<Product> {
				Limit = Configuration.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<Product> search)
		{
			if (ModelState.IsValid) {
				search = SearchProducts (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		Search<Product> SearchProducts (Search<Product> search)
		{
			IQueryable<Product> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();

			if (string.IsNullOrEmpty (pattern)) {
				query = from x in Product.Queryable
						orderby x.Name
						select x;
			} else {
				query = from x in Product.Queryable
						where x.Name.Contains (pattern) ||
							x.Code.Contains (pattern) ||
							x.Model.Contains (pattern) ||
							x.SKU.Contains (pattern) ||
							x.Brand.Contains (pattern)
						orderby x.Name
						select x;

			}
			
			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create", new Product ());
		}

		[HttpPost]
		public ActionResult Create (Product item)
		{
			item.Supplier = Supplier.TryFind (item.SupplierId);

			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}
            
			item.MinimumOrderQuantity = 1;
			item.TaxRate = Configuration.DefaultVAT;
			item.IsTaxIncluded = Configuration.IsTaxIncluded;
			item.PriceType = Configuration.DefaultPriceType;
			item.Photo = Configuration.DefaultPhotoFile;

			using (var scope = new TransactionScope ()) {
				item.Create ();

				foreach (var l in PriceList.Queryable.ToList ()) {
					var price = new ProductPrice {
						Product = item,
						List = l,
						Value = Configuration.DefaultPrice
					};
					price.Create ();
				}

				scope.Flush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		[HttpPost]
		public ActionResult SetPhoto (int id, HttpPostedFileBase file)
		{
			var entity = Product.Find (id);

			entity.Photo = SavePhoto (file) ?? Configuration.DefaultPhotoFile;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, url = Url.Content (entity.Photo) });
		}

		public ActionResult View (int id)
		{
			var entity = Product.Find (id);
			return PartialView ("_View", entity);
		}

		public ActionResult Edit (int id)
		{
			var entity = Product.Find (id);
			return PartialView ("_Edit", entity);
		}

		[HttpPost]
		public ActionResult Edit (Product item)
		{
			item.Supplier = Supplier.TryFind (item.SupplierId);

			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);
            
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
			entity.Supplier = item.Supplier;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = Product.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = Product.Find (id);

			try {
				using (var scope = new TransactionScope()) {

					foreach (var x in item.Prices) {
						x.DeleteAndFlush ();
					}

					item.DeleteAndFlush ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}

			return PartialView ("_DeleteSuccesful", item);
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

		string HashFromImage (Image img)
		{
			string hash;
			byte[] bytes = null;

			using (MemoryStream ms = new MemoryStream ()) {
				img.Save (ms, img.RawFormat);
				bytes = ms.ToArray ();
			}

			using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
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
			var items = new Dictionary<Label, bool> ();

			foreach (var label in Label.Queryable.OrderBy (x => x.Name).ToList ()) {
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

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult Brands (string pattern)
		{
			IQueryable<string> query;

			if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in Product.Queryable
						where x.Brand != null && x.Brand != string.Empty
						orderby x.Brand
						select x.Brand;
			} else {
				query = from x in Product.Queryable
						where x.Brand.Contains (pattern)
						orderby x.Brand
						select x.Brand;
			}

			var items = from x in query.Distinct ().ToList ()
						select new { id = x, name = x };

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Models (string pattern)
		{
			IQueryable<string> query;

			if (string.IsNullOrWhiteSpace (pattern)) {
				query = from x in Product.Queryable
						where x.Model != null && x.Model != string.Empty
			            orderby x.Model
			            select x.Model;
			} else {
				query = from x in Product.Queryable
						where x.Model.Contains (pattern)
						orderby x.Model
						select x.Model;
			}

			var items = from x in query.Distinct ().ToList ()
						select new { id = x, name = x };

			return Json (items, JsonRequestBehavior.AllowGet);
		}
	}
}
