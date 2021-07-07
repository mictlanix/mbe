using System;
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc
{

	[Authorize]
    public class ExpenseVoucherController : CustomController
    {
      public ActionResult Index(){

			Search<ExpenseVoucher> search = new Search<ExpenseVoucher>(){
				Limit = WebConfig.PageSize,
				Results = ExpenseVoucher.Queryable.Where(x => !x.IsCancelled && x.CashSession.CashDrawer.Store == WebConfig.Store).OrderByDescending(x => x.Date).ToList()
			};
			search.Total = search.Results.Count();
            return View(search);
        }

		[HttpPost]
		public ActionResult Index(Search<ExpenseVoucher> search) {

			var pattern = (search.Pattern ?? string.Empty).Trim();
			var query = ExpenseVoucher.Queryable.Where(x => !x.IsCancelled && x.CashSession.CashDrawer.Store == WebConfig.Store);
			int id = 0;

			if (int.TryParse(pattern, out id)){
				query = query.Where(x => x.Id == id);
			}
			else if (!string.IsNullOrEmpty(pattern)) {
				query = query.Where(x => x.Details.Any(y => y.Comment.Contains(pattern) || x.Comment.Contains(pattern)));
			}

			search.Results = query.Skip(search.Offset).Take(search.Limit).ToList();
			search.Total = search.Results.Count();

			if (Request.IsAjaxRequest()) {
				return PartialView("_Index", search);
			}

			return View(search);
		}

		[HttpPost]
		public ActionResult New() {

			var cashsession = GetSession();

			if (cashsession == null) {
				return RedirectToAction("OpenSession","Payments");
			}

			ExpenseVoucher item = new ExpenseVoucher();

			item.Date = DateTime.Now;
			item.Creator = cashsession.Cashier;
			item.Updater = cashsession.Cashier;
			item.Store = WebConfig.Store;
			item.CashSession = cashsession;
			item.ModificationTime = DateTime.Now;
			item.CreationTime = DateTime.Now;
			item.Date = DateTime.Now;


			using (var scope = new TransactionScope()) {
				item.CreateAndFlush();
			}

				return RedirectToAction("Edit", new { id = item.Id });
		}

      public ActionResult Edit(int id) {

            ExpenseVoucher item = ExpenseVoucher.Find(id);
            if (item.IsCompleted || item.IsCancelled) {
					return RedirectToAction("View", new { id = item.Id });
            }

            return View(item);
		}

		public JsonResult GetSuggestions(int expensevoucher, string pattern) {


			var item = ExpenseVoucher.Find(expensevoucher);

			var query = Expense.Queryable.Where(x => x.Name.Contains(pattern) || x.Comment.Contains(pattern));
			var items = query.Take(15).ToList().Select(x => new { id = x.Id, name = x.Name, comment = x.Comment });

			return Json(items, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetComment(int id, string value)
		{

			if (GetSession() == null) {
				return Content(Resources.InvalidCashDrawer);
			}

			var entity = ExpenseVoucher.Find(id);
			string val = (value ?? string.Empty).Trim();

			if (entity.IsCompleted || entity.IsCancelled)
			{
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Comment = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope())
			{
				entity.UpdateAndFlush();
			}

			return Json(new
			{
				id = id,
				value = entity.Comment
			});
		}

		[HttpPost]
		public ActionResult SetCashier(int id, int value)
		{
			var entity = ExpenseVoucher.Find(id);
			var item = Employee.TryFind(value);

			if (GetSession() == null)
			{
				return Content(Resources.InvalidCashDrawer);
			}

			if (entity.IsCompleted || entity.IsCancelled)
			{
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null)
			{
				entity.Creator = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope())
				{
					entity.UpdateAndFlush();
				}
			}

			return Json(new
			{
				id = id,
				value = entity.Creator.ToString()
			});
		}

		[HttpPost]
		public ActionResult SetPurchaseOrder (int id, int value)
		{
			var entity = ExpenseVoucher.Find (id);
			var item = PurchaseOrder.TryFind (value);

			if (GetSession () == null) {
				return Content (Resources.InvalidCashDrawer);
			}

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;
				entity.PurchaseOrder = item;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}

				return Json (new {
					id = id,
					value = item.Id + " - " + AbastosInventoryHelpers.GetLotCode (item)
				});
			} else {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}
		}

		[HttpPost]
		public ActionResult Cancel(int id)
		{
			var entity = ExpenseVoucher.Find(id);

			if (entity.IsCancelled || entity.IsCompleted)
			{
				return RedirectToAction("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope())
			{
				entity.UpdateAndFlush();
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		public ActionResult AddItem(int expensevoucher, int expense) {

			ExpenseVoucher entity = ExpenseVoucher.Find(expensevoucher);
			Expense item = Expense.Find(expense);

			if (entity.IsCancelled || entity.IsCompleted) {
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			ExpenseVoucherDetail detail = new ExpenseVoucherDetail { ExpenseVoucher = entity, Expense = item, Comment = item.Comment };

			using (var scope = new TransactionScope()) {
				detail.CreateAndFlush();
			}

				return Json(new { id = detail.Id }, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Item(int id)
		{
			var entity = ExpenseVoucherDetail.Find(id);
			return PartialView("_ItemEditorView", entity);
		}

		[HttpPost]
		public ActionResult RemoveItem(int id) {

			var entity = ExpenseVoucherDetail.Find(id);

			if (entity.ExpenseVoucher.IsCancelled || entity.ExpenseVoucher.IsCompleted) {
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope()) {
				entity.DeleteAndFlush();
			}

			return Json(new { id = id, result = true });
		}

		[HttpPost]
		public ActionResult SetItemAmount(int id, decimal value) {

			ExpenseVoucherDetail item = ExpenseVoucherDetail.Find(id);

			if (item.ExpenseVoucher.IsCancelled || item.ExpenseVoucher.IsCompleted) {
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value > 0) {
				using (var scope = new TransactionScope()) {
					item.Amount = value;
					item.UpdateAndFlush();
				}
			}

			return Json(new { id = item.Id, value = item.FormattedValueFor(x => x.Amount) });
		}

		[HttpPost]
		public ActionResult SetItemComment(int id, string value) {

			ExpenseVoucherDetail item = ExpenseVoucherDetail.Find(id);

			if (item.ExpenseVoucher.IsCancelled || item.ExpenseVoucher.IsCompleted)
			{
				Response.StatusCode = 400;
				return Content(Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (!string.IsNullOrWhiteSpace(value)) {
				using (var scope = new TransactionScope()) {
					item.Comment = value.Length > 500 ? value.Substring(0,500):value;
					item.UpdateAndFlush();
				}
			}

			return Json(new { id = id, value = item.Comment });
		}

		[HttpPost]
		public ActionResult Confirm(int id) {
			var entity = ExpenseVoucher.Find(id);

			if (entity.IsCancelled || entity.IsCompleted || GetSession() == null) {
				return RedirectToAction("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			using (var scope = new TransactionScope()) {

				entity.UpdateAndFlush();
			}

			return RedirectToAction("Index");
		}

		public ActionResult Totals(int id) {
			var entity = ExpenseVoucher.Find(id);
			return PartialView("_Totals", entity);
		}

		public ActionResult Print(int id) {
			var model = ExpenseVoucher.Find (id);
			if (!model.IsCancelled && model.IsCompleted) {
				return PdfTicketView ("Print", model);
			}
			return RedirectToAction ("Index");
		}

		public ViewResult View(int id) {

			return View(ExpenseVoucher.Find(id));
		}

		public ActionResult Pdf(int id) {
			return PdfView("Print", ExpenseVoucher.Find(id));
		}

		CashSession GetSession() {
			var item = WebConfig.CashDrawer;
			if (item == null)
				return null;

			return CashSession.Queryable.Where(x => x.End == null)
					.SingleOrDefault(x => x.CashDrawer.Id == item.Id);
		}
	}
}