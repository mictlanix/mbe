using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;
using Castle.ActiveRecord;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
	public class MethodPaymentsOptionsController : CustomController {

		public ActionResult Index ()
		{
			var query = PaymentMethodCharge.Queryable;
			var search = new Search<PaymentMethodCharge> ();
			search.Limit = WebConfig.PageSize;
			search.Results = query.ToList ();
			search.Total = query.Count ();
			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}
			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<PaymentMethodCharge> search) {

			IQueryable<PaymentMethodCharge> query = PaymentMethodCharge.Queryable;
			var pattern = string.Format ("{0}", search.Pattern).Trim ();
			int id = 0;

			if (int.TryParse (pattern, out id)) {
				query = query.Where (x => x.Id == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => x.Name.Contains (pattern));
			}

			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = query.Count ();

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}
			return View (search);
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create", new PaymentMethodCharge ());
		}

		[HttpPost]
		public ActionResult Create (PaymentMethodCharge item) {

			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			item.Warehouse = Warehouse.Find (item.WarehouseId);
			item.CommissionByManage /= 100m;
			item.BankPaymentsCharge /= 100m;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Edit (int id) {
			return PartialView ("_Edit", PaymentMethodCharge.Find (id));
		}

		[HttpPost]
		public ActionResult Edit (PaymentMethodCharge item) {
			if (ModelState.IsValid) {
				using (var scope = new TransactionScope ()) {
					item.Warehouse = Warehouse.Find (item.WarehouseId);
					item.UpdateAndFlush ();
				}
			}
			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id) {
			return PartialView ("_Delete", PaymentMethodCharge.Find (id));
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id) {
			var item = PaymentMethodCharge.Find (id);
			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}
			return PartialView ("_Refresh");
		}
	}
}