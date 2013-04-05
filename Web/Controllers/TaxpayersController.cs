// 
// SuppliersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class TaxpayersController : Controller
    {
        //
        // GET: /Suppliers/

        public ViewResult Index()
        {
            var qry = from x in Taxpayer.Queryable
                      orderby x.Name
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Taxpayer/Details/5

        public ViewResult Details (string id)
		{
			var item = Taxpayer.Find (id);
			item.Batches.ToList ();
			
			return View (item);
		}

        //
        // GET: /Taxpayer/Create

        public ActionResult Create ()
		{
			var item = new Taxpayer { 
				Address = new Address ()
			};
			
			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Create", item);
			}

			return View (item);
		}

        //
        // POST: /Taxpayer/Create

        [HttpPost]
        public ActionResult Create (Taxpayer item, IEnumerable<HttpPostedFileBase> files)
		{
			if (!ModelState.IsValid)
				return View (item);

			// FIXME: taxpayer's certificates management
			/*
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
			*/
			
			using (var scope = new TransactionScope()) {
				item.Address.Create ();
				item.CreateAndFlush ();
			}
			
			return RedirectToAction ("Index");
		}
		
		byte[] FileToBytes (HttpPostedFileBase file)
		{
			using (var stream = file.InputStream) {
				var data = new byte[file.ContentLength];
				stream.Read (data, 0, file.ContentLength);
				return data;
			}
		}
		
        //
        // GET: /Taxpayer/Edit/5

        public ActionResult Edit (string id)
		{
			var item = Taxpayer.Find (id);

			return View (item);
		}

        //
        // POST: /Taxpayer/Edit/5

        [HttpPost]
		public ActionResult Edit (Taxpayer item, IEnumerable<HttpPostedFileBase> files)
		{
			if (!ModelState.IsValid)
				return View (item);

			var taxpayer = Taxpayer.Find (item.Id);
			
			// update info
			// FIXME: address updated
			taxpayer.Name = item.Name;
			taxpayer.Regime = item.Regime;
			taxpayer.Address = item.Address;

			// FIXME: taxpayer's certificates management
			/*
			foreach (var file in files) {
				if (file != null && file.ContentLength > 0) {
					var name = file.FileName.ToLower ();
					
					if (name.EndsWith (".cer")) {
						taxpayer.CertificateData = FileToBytes (file);
					} else if (name.EndsWith (".key")) {
						taxpayer.KeyData = FileToBytes (file);
						taxpayer.KeyPassword = Encoding.UTF8.GetBytes (item.KeyPassword2);
					}
				}
			}
			*/

			using (var scope = new TransactionScope()) {
				taxpayer.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

        //
        // GET: /Taxpayer/Delete/5

        public ActionResult Delete(string id)
        {
            return View (Taxpayer.Find (id));
        }

        //
        // POST: /Taxpayer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
			try {
				using (var scope = new TransactionScope()) {
					var item = Taxpayer.Find (id);
					item.DeleteAndFlush ();
				}

				return RedirectToAction ("Index");
			} catch (TransactionException) {
				return View ("DeleteUnsuccessful");
			}
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
			
			//FIXME: validate wrong passphrase
			if (!CFDv2Helpers.PrivateKeyTest (item.KeyData, item.KeyPassword)) {
				return View (item);
			}

			string sn = string.Empty;
			var cert = new X509Certificate2();
			cert.Import(item.CertificateData);

			foreach (var b in cert.GetSerialNumber()) {
				sn = (char)b + sn;
			}

			//FIXME: Update on existing certificate number
			item.Id = ulong.Parse (sn);
			item.NotBefore = cert.NotBefore;
			item.NotAfter = cert.NotAfter;
			item.Taxpayer = Taxpayer.Find (item.TaxpayerId);
			item.IsActive = true;

			using (var scope = new TransactionScope()) {
				foreach(var x in item.Taxpayer.Certificates) {
					x.IsActive = false;
					x.Update();
				}
				
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Details", new { id = item.TaxpayerId });
		}

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Taxpayer.Queryable
                      where x.Id.Contains(pattern) ||
							x.Name.Contains(pattern)
                      select new { id = x.Id, name = string.Format ("{1} ({0})", x.Id, x.Name) };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}