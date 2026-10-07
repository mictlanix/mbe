using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class EntitiesEditorController : CustomController {
		// This screen overwrites order data, so it is for administrators only.
		// Read from the database rather than the login cookie, so a revoked
		// administrator loses access straight away.
		protected override void OnActionExecuting (ActionExecutingContext filterContext)
		{
			var user = Model.User.TryFind (User.Identity.Name);

			if (user == null || !user.IsAdministrator) {
				filterContext.Result = RedirectToAction ("Index", "Home");
				return;
			}

			base.OnActionExecuting (filterContext);
		}

		// Step 1: ask for the order.
		public ActionResult RestoreDeliveryMode ()
		{
			return View ();
		}

		// Step 2: show the order and exactly what will change, or why it can't.
		// Nothing is written here.
		public ActionResult RestoreDeliveryModePreview (int? id)
		{
			var order = id.HasValue ? SalesOrder.TryFind (id.Value) : null;

			if (order == null) {
				ViewBag.Error = Resources.SalesOrderNotFound;
				return View ("RestoreDeliveryMode");
			}

			ViewBag.Error = WhyNotRestorable (order);
			return View (order);
		}

		// Step 3: apply it. The rule is checked again, since the order may have
		// changed since the preview.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult RestoreDeliveryMode (int id)
		{
			var order = SalesOrder.TryFind (id);

			if (order == null) {
				ViewBag.Error = Resources.SalesOrderNotFound;
				return View ("RestoreDeliveryMode");
			}

			ViewBag.Error = WhyNotRestorable (order);

			if (ViewBag.Error != null) {
				return View ("RestoreDeliveryModePreview", order);
			}

			var previous = order.DeliveryMode.GetDisplayName ();

			using (var scope = new TransactionScope ()) {
				order.DeliveryMode = DeliveryMode.ToBeDefined;
				order.UpdateAndFlush ();
				(new Incidence {
					Comment = $"{previous} → {DeliveryMode.ToBeDefined.GetDisplayName ()}",
					PreviousState = previous,
					Reference = order.Id,
					ModificationTime = DateTime.Now,
					Updater = CurrentUser.Employee,
					SourceType = SourceType.SalesOrder
				}).CreateAndFlush ();
			}

			return View ("RestoreDeliveryModeDone", order);
		}

		// Only a counter pick-up is undone. A PartialDeliveries order may already
		// have delivery orders, and ToBeDefined would hide the button to add more.
		static string WhyNotRestorable (SalesOrder order)
		{
			switch (order.DeliveryMode) {
			case DeliveryMode.PickUp:
				return null;
			case DeliveryMode.ToBeDefined:
				return string.Format (Resources.RestoreDeliveryModeNothingToChange,
						      DeliveryMode.ToBeDefined.GetDisplayName ());
			default:
				return string.Format (Resources.RestoreDeliveryModeOnlyPickUp,
						      order.DeliveryMode.GetDisplayName (), DeliveryMode.PickUp.GetDisplayName ());
			}
		}
	}
}
