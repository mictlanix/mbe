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
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class ContactsController : Controller
    {
        //
        // GET: /Contacts/Details/5

        public ViewResult Details(int id)
        {
            using (new SessionScope())
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
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                string type = Request.Params["OwnerType"];
				
	            using (new SessionScope())
	            {
	                item.CreateAndFlush();
	            }

	            System.Diagnostics.Debug.WriteLine("New Contact [Id = {0}]", item.Id);
				
                if (type == "Suppliers")
                {
		            using (new SessionScope())
		            {
	                    var supplier = Supplier.Find(owner);
						supplier.Contacts.Add(item);
	                	supplier.Save();
		            }
                }

                if (type == "Customers")
                {
		            using (new SessionScope())
		            {
	                    var customer = Customer.Find(owner);
						customer.Contacts.Add(item);
	                	customer.Save();
		            }
                }

                return RedirectToAction("Details", type, new { id = owner });
            }

            return View(item);
        }

        //
        // GET: /Contacts/Edit/5

        public ActionResult Edit(int id)
        {
            using (new SessionScope())
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
        }

        //
        // POST: /Contacts/Edit/5

        [HttpPost]
        public ActionResult Edit(Contact contact)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                string type = Request.Params["OwnerType"];
				
                contact.Save();

                return RedirectToAction("Details", type, new { id = owner });
            }
            return View(contact);
        }

        //
        // GET: /Contacts/Delete/5

        public ActionResult Delete(int id)
        {
            using (new SessionScope())
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
        }

        //
        // POST: /Contacts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int owner = int.Parse(Request.Params["OwnerId"]);
            string type = Request.Params["OwnerType"];
			
            using (new SessionScope())
            {
	            var item = Contact.Find(id);
	            var customer = item.Customers.FirstOrDefault();
	            var supplier = item.Suppliers.FirstOrDefault();
	
	            if (customer != null)
	            {
	                customer.Contacts.Remove(item);
	                customer.Save();
	            }
	
	            if (supplier != null)
	            {
	                supplier.Contacts.Remove(item);
	                supplier.Save();
	            }
	
	            item.Delete();
			}
			
            return RedirectToAction("Details", type, new { id = owner });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}