using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class CorrectionController : CustomController {
		// GET: Correction
		public ActionResult Index ()
		{
			return View ();
		}

		// GET: Correction/Details/5
		public ActionResult Details (int id)
		{
			return View ();
		}

		// GET: Correction/Create
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

		// POST: Correction/Create
		[HttpPost]
		public ActionResult Create (FormCollection collection)
		{
			try {
				// TODO: Add insert logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}

		// GET: Correction/Edit/5
		public ActionResult Edit (int id)
		{
			return View ();
		}

		// POST: Correction/Edit/5
		[HttpPost]
		public ActionResult Edit (int id, FormCollection collection)
		{
			try {
				// TODO: Add update logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}

		// GET: Correction/Delete/5
		public ActionResult Delete (int id)
		{
			return View ();
		}

		// POST: Correction/Delete/5
		[HttpPost]
		public ActionResult Delete (int id, FormCollection collection)
		{
			try {
				// TODO: Add delete logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}
	}
}
