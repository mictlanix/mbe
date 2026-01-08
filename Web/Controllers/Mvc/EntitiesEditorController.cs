using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class EntitiesEditorController : CustomController {

		public ActionResult Index ()
		{
			return View ();
		}
		// GET: Correction

		public ActionResult RestoreDeliverableOrderStatus ()
		{
			return View ();
		}

		[HttpPost]
		public ActionResult RestoreDeliverableOrderStatus (int order_id)
		{
			var str = "";
			if (!CurrentUser.IsAdministrator) {
				str = $"{Resources.No} {Resources.Administrator}";
				return View ("RestoredOrderDeliveryStatus", (object) str);
			}

			var order = SalesOrder.Queryable.Where (x => x.Id == order_id).SingleOrDefault ();
			if (order == null) {
				str = Resources.ItemNotFound;
				return View ("RestoredOrderDeliveryStatus", (object) str);
			}


			using (var scope = new TransactionScope ()) {
				order.DeliveryMode = DeliveryMode.ToBeDefined;
				order.UpdateAndFlush ();
				str = "Completado";
				(new Incidence {
					Comment = str,
					Reference = order.Id,
					ModificationTime = DateTime.Now,
					Updater = CurrentUser.Employee,
					SourceType = SourceType.SalesOrder

				}).CreateAndFlush ();
			}

			var review = SalesOrder.Queryable.Where (x => x.Id == order_id).SingleOrDefault ().DeliveryMode;
			return View ("RestoredOrderDeliveryStatus", (object)str );
		}

		public ActionResult ViewSalesOrder (int id)
		{
			var salesOrder = SalesOrder.Queryable.Where (x => x.Id == id).SingleOrDefault ();
			return View (salesOrder);
		}

	}
}
