using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;

namespace Business.Essentials.WebApp.Controllers
{
    public class ProductsController : Controller
    {
        //
        // GET: /Product/

        public ActionResult Index()
        {
            return View(new List<Product>());
        }

        //
        // POST: /Products/Index

        [HttpPost]
        public ActionResult Index(Search search)
        {
            if (ModelState.IsValid)
            {
                var qry = from x in Product.Queryable
                          where x.Name.Contains(search.Pattern) ||
                                x.Code.Contains(search.Pattern) ||
                                x.SKU.Contains(search.Pattern) ||
                                x.Brand.Contains(search.Pattern)
                          select x;

                return View(qry.Take(250).ToList());
            }

            return View(new List<Product>());
        }

        //
        // GET: /Products/Details/5

        public ViewResult Details(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // GET: /Products/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Products/Create

        [HttpPost]
        public ActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                product.Save();
                return RedirectToAction("Index");
            }
            return View(product);
        }
        //
        // GET: /Products/Edit/5

        public ActionResult Edit(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // POST: /Products/Edit/5

        [HttpPost]
        public ActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                product.Save();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        //
        // GET: /Products/Delete/5

        public ActionResult Delete(int id)
        {
            Product product = Product.Find(id);
            return View(product);
        }

        //
        // POST: /Products/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = Product.Find(id);
            product.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
