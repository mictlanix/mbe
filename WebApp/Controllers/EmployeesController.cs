using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{
    public class EmployeesController : Controller
    {

        // AJAX
        // GET: /Categories/GetSuggestions

        public JsonResult GetSuggestions(string pattern)
        {
            var qry = from x in Employee.Queryable
                      where x.FirstName.Contains(pattern) ||
                            x.LastName.Contains(pattern)
                      select new { id = x.Id, name = x.FirstName + " " + x.LastName };

            return Json(qry.Take(15).ToList(), JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Employee/

        public ViewResult Index()
        {
            var qry = from x in Employee.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Employee/Details/5

        public ViewResult Details(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
        }

        //
        // GET: /Employee/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Employee/Create

        [HttpPost]
        public ActionResult Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Create();
                return RedirectToAction("Index");
            }

            return View(employee);
        }

        //
        // GET: /Warehouses/Edit/5

        public ActionResult Edit(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
        }

        //
        // POST: /Employee/Edit/5

        [HttpPost]
        public ActionResult Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Update();
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        //
        // GET: /Employee/Delete/5

        public ActionResult Delete(int id)
        {
            Employee employee = Employee.Find(id);
            return View(employee);
        }

        //
        // POST: /Warehouses/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Employee employee = Employee.Find(id);
            employee.Delete();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

