// 
// ContactsController.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Controllers
{
    public class ContactsController : Controller
    {
        //
        // GET: /Contacts/Details/5

        public ViewResult Details(int id)
        {
            Contact item = Contact.Find(id);
            var customer = item.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = item.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }
			
            return View(item);
        }

        //
        // GET: /Contacts/Create

        public ActionResult CreateForSupplier(int id)
        {
            ViewBag.OwnerId = id;
            ViewBag.OwnerType = "Suppliers";
            return View("Create");
        }

        public ActionResult CreateForCustomer(int id)
        {
            ViewBag.OwnerId = id;
            ViewBag.OwnerType = "Customers";
            return View("Create");
        }

        //
        // POST: /Contacts/Create

        [HttpPost]
        public ActionResult Create(Contact item)
        {
            if (!ModelState.IsValid)
            	return View(item);
            
            int owner = int.Parse(Request.Params["OwnerId"]);
            string type = Request.Params["OwnerType"];

			using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();

	            System.Diagnostics.Debug.WriteLine("New Contact [Id = {0}]", item.Id);
				
                if (type == "Suppliers") {
                    var supplier = Supplier.Find(owner);
					supplier.Contacts.Add(item);
                	supplier.Update ();
                }

                if (type == "Customers") {
                    var customer = Customer.Find(owner);
					customer.Contacts.Add(item);
                	customer.Update ();
                }
			}

            return RedirectToAction("Details", type, new { id = owner });
        }

        //
        // GET: /Contacts/Edit/5

        public ActionResult Edit(int id)
        {
        	var item = Contact.Find(id);
            var customer = item.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = item.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }
			
        	return View(item);
        }

        //
        // POST: /Contacts/Edit/5

        [HttpPost]
        public ActionResult Edit(Contact item)
        {
            if (!ModelState.IsValid)
            	return View (item);
            
            int owner = int.Parse(Request.Params["OwnerId"]);
            string type = Request.Params["OwnerType"];

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}
            
			return RedirectToAction("Details", type, new { id = owner });
        }

        //
        // GET: /Contacts/Delete/5

        public ActionResult Delete(int id)
        {
            var item = Contact.Find(id);
            var customer = item.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = item.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(item);
        }

        //
        // POST: /Contacts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int owner = int.Parse(Request.Params["OwnerId"]);
            string type = Request.Params["OwnerType"];
			
            using (var scope = new TransactionScope()) {
	            var item = Contact.Find (id);
	            var customer = item.Customers.FirstOrDefault ();
	            var supplier = item.Suppliers.FirstOrDefault ();
	
	            if (customer != null) {
	                customer.Contacts.Remove (item);
	                customer.Update ();
	            }
	
	            if (supplier != null) {
	                supplier.Contacts.Remove (item);
	                supplier.Update ();
	            }
	
				item.DeleteAndFlush ();
			}
			
            return RedirectToAction("Details", type, new { id = owner });
		}
    }
}