// 
// SuppliersController.cs
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
using System.Text;
using Castle.ActiveRecord;
using NHibernate.Exceptions;
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
			using (var session = new SessionScope()) {
				var item = Taxpayer.Find (id);
				item.Documents.ToList ();
				
				return View (item);
			}
		}

        //
        // GET: /Taxpayer/Create

        public ActionResult Create ()
		{
			var item = new Taxpayer { 
				Address = new Address {
					TaxpayerId = "XXXXXXXXXXXX",
					TaxpayerName = "XXX"
				},
				CertificateNumber = 0
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
			
			item.Address.TaxpayerId = item.Id;
			item.Address.TaxpayerName = item.Name;
			
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
			
			// FIXME: use transaction
			using (var session = new SessionScope()) {
				item.Address.CreateAndFlush ();
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
			
			item.KeyPassword = null;
			
			return View (item);
		}

        //
        // POST: /Taxpayer/Edit/5

        [HttpPost]
        public ActionResult Edit (Taxpayer item, IEnumerable<HttpPostedFileBase> files)
		{
			if (!ModelState.IsValid)
				return View (item);
			
			item.Address.TaxpayerId = item.Id;
			item.Address.TaxpayerName = item.Name;
			
			var taxpayer = Taxpayer.Find (item.Id);
			
			// update info
			taxpayer.Name = item.Name;
			taxpayer.CertificateNumber = item.CertificateNumber;
			taxpayer.Address.Copy (item.Address);
			
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
			
			// FIXME: use transaction
			taxpayer.Address.Update ();
			taxpayer.Update ();
            
			return RedirectToAction ("Index");
		}

        //
        // GET: /Taxpayer/Delete/5

        public ActionResult Delete(string id)
        {
            Taxpayer item = Taxpayer.Find(id);
            return View(item);
        }

        //
        // POST: /Taxpayer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
			try {
				using (new SessionScope()) {
					var item = Taxpayer.Find (id);
					item.Delete ();
				}

				return RedirectToAction ("Index");
			} catch (GenericADOException) {
				return View ("DeleteUnsuccessful");
			}
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