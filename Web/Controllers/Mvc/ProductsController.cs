// 
// ProductsController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using NHibernate;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class ProductsController : CustomController {
		public ActionResult Index ()
		{
			var search = SearchProducts (new Search<Product> {
				Limit = WebConfig.PageSize
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
			item.TaxRate = WebConfig.DefaultVAT;
			item.IsTaxIncluded = WebConfig.IsTaxIncluded;
			item.PriceType = WebConfig.DefaultPriceType;
			item.Photo = WebConfig.DefaultPhotoFile;

			using (var scope = new TransactionScope ()) {
				item.Create ();

				foreach (var l in PriceList.Queryable.ToList ()) {
					var price = new ProductPrice {
						Product = item,
						List = l,
						Value = WebConfig.DefaultPrice
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

			entity.Photo = SavePhoto (file) ?? WebConfig.DefaultPhotoFile;

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
				using (var scope = new TransactionScope ()) {

					foreach (var x in item.Prices) {
						x.DeleteAndFlush ();
					}

					item.DeleteAndFlush ();
				}
				return PartialView ("_DeleteSuccesful", item);
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}
		}

		public ActionResult Merge ()
		{
			return View ();
		}

		[HttpPost]
		public ActionResult Merge (int product, int duplicate)
		{
			var prod = Product.TryFind (product);
			var dup = Product.TryFind (duplicate);
			string sql = @"UPDATE customer_discount SET product = :product WHERE product = :duplicate;
							UPDATE customer_refund_detail SET product = :product WHERE product = :duplicate;
							UPDATE delivery_order_detail SET product = :product WHERE product = :duplicate;
							UPDATE fiscal_document_detail SET product = :product WHERE product = :duplicate;
							UPDATE inventory_issue_detail SET product = :product WHERE product = :duplicate;
							UPDATE inventory_receipt_detail SET product = :product WHERE product = :duplicate;
							UPDATE inventory_transfer_detail SET product = :product WHERE product = :duplicate;
							UPDATE lot_serial_rqmt SET product = :product WHERE product = :duplicate;
							UPDATE lot_serial_tracking SET product = :product WHERE product = :duplicate;
							UPDATE purchase_order_detail SET product = :product WHERE product = :duplicate;
							UPDATE sales_order_detail SET product = :product WHERE product = :duplicate;
							UPDATE sales_quote_detail SET product = :product WHERE product = :duplicate;
							UPDATE supplier_return_detail SET product = :product WHERE product = :duplicate;
							DELETE FROM product_label WHERE product = :duplicate;
							DELETE FROM product_price WHERE product = :duplicate;
							DELETE FROM product WHERE product_id = :duplicate;";

			ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				int ret;

				using (var tx = session.BeginTransaction ()) {
					var query = session.CreateSQLQuery (sql);

					query.AddScalar ("product", NHibernateUtil.Int32);
					query.AddScalar ("duplicate", NHibernateUtil.Int32);

					query.SetInt32 ("product", product);
					query.SetInt32 ("duplicate", duplicate);

					ret = query.ExecuteUpdate ();

					tx.Commit ();
				}

				return ret;
			}, null);

			return View (new Pair<Product, Product> { First = prod, Second = dup });
		}

		string SavePhoto (HttpPostedFileBase file)
		{
			if (file == null || file.ContentLength == 0)
				return null;

			using (var stream = file.InputStream) {
				using (var img = Image.FromStream (stream)) {
					var hash = string.Format ("{0}.png", HashFromImage (img));
					var path = Path.Combine (Server.MapPath (WebConfig.PhotosPath), hash);

					img.Save (path, ImageFormat.Png);

					return Path.Combine (WebConfig.PhotosPath, hash);
				}
			}
		}

		string HashFromStream (Stream stream)
		{
			string hash;
			byte [] bytes = null;

			using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider ()) {
				bytes = sha1.ComputeHash (stream);
				hash = BitConverter.ToString (bytes).Replace ("-", "").ToLower ();
			}

			return hash;
		}

		string HashFromImage (Image img)
		{
			string hash;
			byte [] bytes = null;

			using (MemoryStream ms = new MemoryStream ()) {
				img.Save (ms, img.RawFormat);
				bytes = ms.ToArray ();
			}

			using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider ()) {
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
					    id = x.Id,
					    name = x.Name,
					    code = x.Code,
					    model = x.Model,
					    sku = x.SKU,
					    url = Url.Content (x.Photo)
				    };

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Labels (int id)
		{
			var item = Product.Find (id);
			return PartialView ("_Labels", item.Labels);
		}

		[HttpPost]
		public ActionResult SetLabels (int id, int [] value)
		{
			var entity = Product.Find (id);

			if (value == null) {
				var param = Request.Params ["value[]"];
				if (!string.IsNullOrWhiteSpace (param)) {
					value = param.Split (',').Select (x => Convert.ToInt32 (x)).ToArray ();
				}
			}

			if (entity == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			using (var scope = new TransactionScope ()) {
				entity.Labels.Clear ();
				if (value != null) {
					foreach (int v in value) {
						entity.Labels.Add (Label.Find (v));
					}
				}
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = value });
		}

		public JsonResult UnitsOfMeasurement ()
		{
			var qry = from x in Enum.GetValues (typeof (UnitOfMeasurement)).Cast<UnitOfMeasurement> ()
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
