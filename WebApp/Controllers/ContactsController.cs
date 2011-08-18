using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class ContactsController : Controller
    {
        //
        // GET: /Contact/

        public ViewResult Index()
        {
            var qry = from x in Contact.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Contacts/Details/5

        public ViewResult Details(int id)
        {
            Contact contact = Contact.Find(id);
            return View(contact);
        }

        //
        // GET: /Contacts/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Contacts/Create

        [HttpPost]
        public ActionResult Create(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.Save();
                return RedirectToAction("Index");
            }

            return View(contact);
        }

        //
        // GET: /Contacts/Edit/5

        public ActionResult Edit(int id)
        {
            Contact contact = Contact.Find(id);
            return View(contact);
        }

        //
        // POST: /Contacts/Edit/5

        [HttpPost]
        public ActionResult Edit(Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.Save();
                return RedirectToAction("Index");
            }
            return View(contact);
        }

        //
        // GET: /Contacts/Delete/5

        public ActionResult Delete(int id)
        {
            Contact contact = Contact.Find(id);
            return View(contact);
        }

        //
        // POST: /Contacts/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Contact contact = Contact.Find(id);
            contact.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}