using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{ 
    public class CategoriesController : Controller
    {

        // AJAX
        // GET: /Categories/GetSuggestions

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Category.Queryable
                      where x.Name.Contains(pattern) 
                      select new { id = x.Id, name = x.Name};

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Categories/

        public ViewResult Index()
        {
            var qry = from x in Category.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Categories/Details/5

        public ViewResult Details(int id)
        {
            Category category = Category.Find(id);
            return View(category);
        }

        //
        // GET: /Categories/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Categories/Create

        [HttpPost]
        public ActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Save();
                return RedirectToAction("Index");  
            }

            return View(category);
        }
        
        //
        // GET: /Categories/Edit/5
 
        public ActionResult Edit(int id)
        {
            Category category = Category.Find(id);
            return View(category);
        }

        //
        // POST: /Categories/Edit/5

        [HttpPost]
        public ActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Save();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        //
        // GET: /Categories/Delete/5
 
        public ActionResult Delete(int id)
        {
            Category category = Category.Find(id);
            return View(category);
        }

        //
        // POST: /Categories/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = Category.Find(id);
            category.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}