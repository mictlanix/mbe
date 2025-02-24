using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class VehicleOperatorsController : CustomController {
		// GET: VehicleOperators
		public ActionResult Index ()
		{

			var query = VehicleOperator.FindAll ();
			var search = new Search<VehicleOperator> ();
			search.Limit = WebConfig.PageSize;
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = query.Count ();

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_index", search);
			}

			return View (search);
		}

		public ActionResult New ()
		{
			var item = new VehicleOperator {
				IssueLicenceDate = DateTime.Now.AddYears (-1),
				ExpirationLicenceDate = DateTime.Now.AddYears (2)
			};
			return PartialView ("_Create", item);
		}

		[HttpPost]
		public ActionResult New (VehicleOperator item)
		{

			if (VehicleOperator.Queryable.Any (x => x.LicenseDriverNumber == item.LicenseDriverNumber))
				ModelState.AddModelError ("", Resources.LicensePlateAlreadyExists);

			if (VehicleOperator.Queryable.Any (x => x.Operator == item.Operator))
				ModelState.AddModelError ("", string.Format (Resources.AttributeAlreadyExists, Resources.VehicleOperator, item.Operator.Name));

			//if (item.IssueLicenceDate > DateTime.Now || item.ExpirationLicenceDate < DateTime.Now) {
			//	ModelState.AddModelError ("", Resources.Validation_WrongDateRange);
			//}

			if (!ModelState.IsValid)
				return PartialView ("_Create", item);


			item.CreationTime = DateTime.Now;
			item.ModificationTime = DateTime.Now;
			item.Creator = CurrentUser.Employee;
			item.Updater = CurrentUser.Employee;
			item.Operator = Employee.Find (item.OperatorId);
			item.IsActive = true;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}


			return PartialView ("_Refresh");
		}

		public ActionResult Edit (int id)
		{
			return PartialView ("_Edit", VehicleOperator.Find (id));
		}

		[HttpPost]
		public ActionResult Edit (VehicleOperator item)
		{
			if (ModelState.IsValid) {
				using (var scope = new TransactionScope ()) {
					item.UpdateAndFlush ();
				}
			}
			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = VehicleOperator.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{

			var item = VehicleOperator.Find (id);
			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public JsonResult GetSuggestions (string pattern)
		{

			pattern = pattern.TrimEnd (new char [] { ' ', '*' });


			//var sql = @"SELECT
			//		ve.vehicle_operator_id id,
			//		e.nickname nickname,
			//		e.last_name last_name FROM employee e
			//		JOIN vehicle_operator ve ON e.employee_id = ve.driver
			//		WHERE e.active = TRUE AND ve.active = TRUE
			//		AND (	e.nickname like :pattern
			//			OR e.last_name like :pattern
			//			OR e.first_name like :pattern
			//		)
			//		ORDER BY e.employee_id DESC
			//		LIMIT 15";
			var sql = @"SELECT	vo.vehicle_operator_id id,
						e.nickname nickname,
						e.last_name last_name
					FROM vehicle_operator vo 
					LEFT JOIN employee e
						ON vo.driver = e.employee_id
					WHERE e.active = TRUE AND vo.active = TRUE
					AND (e.nickname LIKE :pattern OR e.first_name LIKE :pattern)
					ORDER BY e.employee_id DESC
					LIMIT 15;";


			var raw = (IList<dynamic>) ActiveRecordMediator<VehicleOperator>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("nickname", NHibernateUtil.String);
				query.AddScalar ("last_name", NHibernateUtil.String);

				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
				     select new {
						id = x.id,
						name = x.nickname,
						//last_name = x.last_name
				     }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);
		}
	}
}