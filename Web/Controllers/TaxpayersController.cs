// 
// SuppliersController.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Castle.ActiveRecord;
using NHibernate;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
    public class TaxpayersController : Controller
    {
		public ActionResult Index ()
        {
            var qry = from x in Taxpayer.Queryable
                      orderby x.Name
                      select x;

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", qry.ToList ());
			}

            return View (qry.ToList ());
        }

		public ActionResult Details (string id)
		{
			var entity = Taxpayer.Find (id);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Details", entity);
			}

			return View (entity);
		}

        public ActionResult Create ()
		{
			return PartialView ("_Create");
		}

        [HttpPost]
        public ActionResult Create (Taxpayer item)
		{
			if (!string.IsNullOrEmpty (item.Id)) {
				var entity = Taxpayer.TryFind (item.Id);

				if(entity != null) {
					ModelState.AddModelError ("", Resources.CustomerTaxpayerAlreadyExists);
				}
			}
			
			if (!item.HasAddress) {
				ModelState.Where(x => x.Key.StartsWith("Address.")).ToList().ForEach(x => x.Value.Errors.Clear());
				item.Address = null;
			}
			
			if (!ModelState.IsValid) {
				return PartialView ("_Create", item);
			}

			item.Id = item.Id.ToUpper ();
			item.Name = string.IsNullOrWhiteSpace (item.Name) ? null : item.Name.Trim ();
			item.Regime = item.Regime.Trim ();

			using (var scope = new TransactionScope()) {
				if(item.HasAddress) {
					item.Address.Create ();
				}
				
				item.CreateAndFlush ();
			}
			
			return PartialView ("_Refresh");
		}

        public ActionResult Edit (string id)
		{
			var item = Taxpayer.Find (id);
			item.HasAddress = (item.Address != null);
			return PartialView ("_Edit", item);
		}

        [HttpPost]
		public ActionResult Edit (Taxpayer item)
		{
			if(!item.HasAddress) {
				ModelState.Where(x => x.Key.StartsWith("Address.")).ToList().ForEach(x => x.Value.Errors.Clear());
				item.Address = null;
			}
			
			if (!ModelState.IsValid) {
				return PartialView ("_Edit", item);
			}
			
			var entity = Taxpayer.Find (item.Id);
			var address = entity.Address;
			
			entity.HasAddress = (address != null);
			entity.Name = string.IsNullOrWhiteSpace (item.Name) ? null : item.Name.Trim ();
			entity.Regime = item.Regime.Trim ();
			entity.Scheme = item.Scheme;
			entity.Provider = item.Provider;
			
			using (var scope = new TransactionScope()) {
				if(item.HasAddress) {
					entity.Address = item.Address;
					entity.Address.Create ();
				} else {
					entity.Address = null;
				}
				
				entity.UpdateAndFlush ();
			}
			
			if(address != null) {
				try {
					using (var scope = new TransactionScope()) {
						address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}
			
			return PartialView ("_Refresh");
		}

        public ActionResult Delete(string id)
		{
			var item = Taxpayer.Find (id);
			return PartialView ("_Delete", item);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
		{
			var item = Taxpayer.Find (id);
			
			try {
				using (var scope = new TransactionScope ()) {
					item.DeleteAndFlush ();
				}
			} catch (GenericADOException ex) {
				System.Diagnostics.Debug.WriteLine (ex);
				return PartialView ("DeleteUnsuccessful");
			}
			
			if (item.Address != null) {
				try {
					using (var scope = new TransactionScope()) {
						item.Address.DeleteAndFlush ();
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex);
				}
			}
			
			return PartialView ("_Refresh");
        }

		public ActionResult AddCertificate (string id)
		{
			var item = new TaxpayerCertificate {
				TaxpayerId = id
			};

			return View (item);
		}

		[HttpPost]
		public ActionResult AddCertificate (TaxpayerCertificate item, IEnumerable<HttpPostedFileBase> files)
		{
			if (!ModelState.IsValid)
				return View (item);

			foreach (var file in files) {
				if (file != null && file.ContentLength > 0) {
					var name = file.FileName.ToLower ();
					
					if (name.EndsWith (".cer")) {
						item.CertificateData = FileToBytes (file);
					} else if (name.EndsWith (".key")) {
						item.KeyData = FileToBytes (file);
						item.KeyPassword = Encoding.UTF8.GetBytes (item.KeyPassword2);
					}
				}
			}
			
			if (!CFDHelpers.PrivateKeyTest (item.KeyData, item.KeyPassword)) {
				ModelState.AddModelError ("KeyPassword", Resources.Validation_InvalidPassword);
				return View (item);
			}

			string sn = string.Empty;
			var cert = new X509Certificate2 ();
			cert.Import (item.CertificateData);

			foreach (var b in cert.GetSerialNumber()) {
				sn = (char)b + sn;
			}

			item.Id = sn.PadLeft (20, '0');
			var entity = TaxpayerCertificate.Queryable.SingleOrDefault (x => x.Id == item.Id);

			if (entity == null) {
				entity = new TaxpayerCertificate ();
			}

			entity.Id = item.Id;
			entity.CertificateData = item.CertificateData;
			entity.KeyData = item.KeyData;
			entity.KeyPassword = item.KeyPassword;
			entity.NotBefore = cert.NotBefore;
			entity.NotAfter = cert.NotAfter;
			entity.Taxpayer = Taxpayer.Find (item.TaxpayerId);

			using (var scope = new TransactionScope ()) {
				foreach(var x in entity.Taxpayer.Certificates) {
					x.IsActive = false;
					x.Update ();
				}
				
				entity.IsActive = true;
				entity.SaveAndFlush ();
			}

			return RedirectToAction ("Details", new { id = item.TaxpayerId });
		}

        public JsonResult GetSuggestions (string pattern)
        {
			var query = from x in Taxpayer.Queryable
                      	where x.Id.Contains (pattern) ||
							x.Name.Contains (pattern)
			            select new { id = x.Id, name = x.ToString () };

			return Json(query.Take (15).ToList (), JsonRequestBehavior.AllowGet);
		}
		
		byte[] FileToBytes (HttpPostedFileBase file)
		{
			using (var stream = file.InputStream) {
				var data = new byte[file.ContentLength];
				stream.Read (data, 0, file.ContentLength);
				return data;
			}
		}
    }
}