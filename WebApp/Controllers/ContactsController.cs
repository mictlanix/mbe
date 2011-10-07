using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class ContactsController : Controller
    {
        //
        // GET: /Contacts/Details/5

        public ViewResult Details(int id)
        {
            Contact contact = Contact.Find(id);
            var customer = contact.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = contact.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }
            return View(contact);
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
        public ActionResult Create(Contact contact)
        {
            if (ModelState.IsValid)
            {
                int owner = int.Parse(Request.Params["OwnerId"]);
                string type = Request.Params["OwnerType"];

                if (type == "Suppliers")
                {
                    var supplier = Supplier.Find(owner);
                    contact.Suppliers.Add(supplier);
                }

                if (type == "Customers")
                {
                    var customer = Customer.Find(owner);
                    contact.Customers.Add(customer);
                }

                contact.Create();

                return RedirectToAction("Details", type, new { id = owner });
            }

            return View(contact);
        }

        //
        // GET: /Contacts/Edit/5

        public ActionResult Edit(int id)
        {
            Contact contact = Contact.Find(id);
            var customer = contact.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = contact.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(contact);
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
                Contact item = Contact.Find(contact.Id);

                contact.Suppliers = item.Suppliers;
                contact.Customers = item.Customers;
                contact.Save();

                return RedirectToAction("Details", type, new { id = owner });
            }
            return View(contact);
        }

        //
        // GET: /Contacts/Delete/5

        public ActionResult Delete(int id)
        {
            Contact contact = Contact.Find(id);
            var customer = contact.Customers.FirstOrDefault();

            if (customer == null)
            {
                var supplier = contact.Suppliers.First();
                ViewBag.OwnerId = supplier.Id;
                ViewBag.OwnerType = "Suppliers";
            }
            else
            {
                ViewBag.OwnerId = customer.Id;
                ViewBag.OwnerType = "Customers";
            }

            return View(contact);
        }

        //
        // POST: /Contacts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int owner = int.Parse(Request.Params["OwnerId"]);
            string type = Request.Params["OwnerType"];
            Contact contact = Contact.Find(id);

            contact.Delete();
            return RedirectToAction("Details", type, new { id = owner });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}