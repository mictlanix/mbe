// 
// AddressesController.cs
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
    public class AddressesController : Controller
    {
        //
        // GET: /Address/Details/5

        public ViewResult Details(int id)
        {
            Address address = Address.Find(id);
            var customer = address.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = address.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(address);
        }

        //
        // GET: /Address/Create

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
        // POST: /Address/Create

        [HttpPost]
        public ActionResult Create(Address address)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                string type = Request.Params["OwnerType"];
				
	            using (var session = new SessionScope())
	            {
	                address.CreateAndFlush();
	            }
	
	            System.Diagnostics.Debug.WriteLine("New Address [Id = {0}]", address.Id);
				
                if (type == "Suppliers")
                {
                    var supplier = Supplier.Find(owner);
					supplier.Addresses.Add(address);
					supplier.Save();
                }

                if (type == "Customers")
                {
                    var customer = Customer.Find(owner);
					customer.Addresses.Add(address);
					customer.Save();
                }
				
				address.Save();
				
                return RedirectToAction("Details", type, new { id = owner });
            }

            return View(address);
        }

        //
        // GET: /Address/Edit/5

        public ActionResult Edit(int id)
        {
            Address address = Address.Find(id);
            var customer = address.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = address.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(address);
        }

        //
        // POST: /Address/Edit/5

        [HttpPost]
        public ActionResult Edit(Address address)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                string type = Request.Params["OwnerType"];

                address.Save();

                return RedirectToAction("Details", type, new { id = owner });
            }
            return View(address);
        }

        //
        // GET: /Address/Delete/5

        public ActionResult Delete(int id)
        {
            Address address = Address.Find(id);
            var customer = address.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = address.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(address);
        }

        //
        // POST: /Address/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var address = Address.Find(id);
			var customer = address.Customers.FirstOrDefault();
            var supplier = address.Suppliers.FirstOrDefault();
            string type = Request.Params["OwnerType"];
            int owner = int.Parse(Request.Params["OwnerId"]);

            if (customer != null)
            {
				customer.Addresses.Remove(address);
				customer.Save();
            }
			
            if (supplier != null)
            {
				supplier.Addresses.Remove(address);
				supplier.Save();
            }
			
            address.Delete();
			
            return RedirectToAction("Details", type, new { id = owner });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}