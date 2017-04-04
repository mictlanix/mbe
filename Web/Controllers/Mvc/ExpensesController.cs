using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;
using Castle.ActiveRecord;

namespace Mictlanix.BE.Web.Controllers.Mvc
{

	[Authorize]
    public class ExpensesController : Controller
    {
        public ActionResult Index()
        {
            var query = Expense.FindAll();
            var search = new Search<Expense>();
            search.Limit = WebConfig.PageSize;
            search.Results = query.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = query.Count();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Index", search);
            }

            return View(search);
        }

        [HttpPost]
        public ActionResult Index(string Pattern) {

            var search = new Search<Expense>();
            search.Limit = WebConfig.PageSize;


            if (!string.IsNullOrEmpty(Pattern)){
                search.Results = Expense.Queryable.Where(x => x.Name.Contains(Pattern)).ToList();
                search.Total = search.Results.Count;
            }
            else {
                search.Results = Expense.Queryable.ToList();
                search.Total = search.Results.Count;
            }

            if (Request.IsAjaxRequest()) { 
            return PartialView("_Index", search);
            }
            return View(search);
        }

        public ActionResult Create() {
            return PartialView("_Create", new Expense());
        }

        [HttpPost]
        public ActionResult Create(Expense item) {

            if (!ModelState.IsValid)
                return PartialView("_Create", item);

            using (var scope = new TransactionScope()) {

                item.CreateAndFlush();
            }

            return PartialView("_Refresh");
        }

        public ActionResult Edit(int id) {
            return PartialView("_Edit", Expense.Find(id));
        }

        [HttpPost]
        public ActionResult Edit(Expense item) {
            if (ModelState.IsValid) {
                using (var scope = new TransactionScope()) {
                    item.UpdateAndFlush();
                }
            }
            return PartialView("_Refresh");
        }

        public ActionResult Delete(int id) {

            return PartialView("_Delete", Expense.Find(id));
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id) {

            var item = Expense.Find(id);
            using (var scope = new TransactionScope()) {
                item.DeleteAndFlush();
            }

            return PartialView("_Refresh");
        }
    }
}