using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;
using Castle.ActiveRecord;
using Mictlanix.BE.Web.Mvc;
using NHibernate;
using System.Collections.Generic;

namespace Mictlanix.BE.Web.Controllers.Mvc {

	[Authorize]
	public class VehiclesController : CustomController {
		public ActionResult Index ()
		{
			var query = Vehicle.FindAll ();
			var search = new Search<Vehicle> ();
			search.Limit = WebConfig.PageSize;
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = query.Count ();

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (string Pattern)
		{

			var search = new Search<Vehicle> ();
			search.Limit = WebConfig.PageSize;


			if (!string.IsNullOrEmpty (Pattern)) {
				search.Results = Vehicle.Queryable.Where (x => x.Name.Contains (Pattern)
				|| x.NickName.Contains (Pattern)
				|| x.LicensePlate.Contains (Pattern)
				).ToList ();
				search.Total = search.Results.Count;
			} else {
				search.Results = Vehicle.Queryable.ToList ();
				search.Total = search.Results.Count;
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}
			return View (search);
		}

		public ActionResult New ()
		{
			return PartialView ("_Create", new Vehicle ());
		}

		[HttpPost]
		public ActionResult New (Vehicle item)
		{

			if (Vehicle.Queryable.Any (x => x.LicensePlate == item.LicensePlate))
				ModelState.AddModelError ("", Resources.LicensePlateAlreadyExists);

			if (Vehicle.Queryable.Any (x => x.NickName == item.NickName))
				ModelState.AddModelError ("", string.Format (Resources.AttributeAlreadyExists, Resources.Nickname, item.NickName));

			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			using (var scope = new TransactionScope ()) {
				item.IsActive = true;
				item.CreateAndFlush ();
			}


			return PartialView ("_Refresh");
		}

		public ActionResult Edit (int id)
		{
			return PartialView ("_Edit", Vehicle.Find (id));
		}

		[HttpPost]
		public ActionResult Edit (Vehicle item)
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

			return PartialView ("_Delete", Vehicle.Find (id));
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{

			var item = Vehicle.Find (id);
			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public JsonResult GetSuggestions (string pattern)
		{
			pattern = pattern.TrimEnd (new char [] { '*' });


			var sql = @"
					SELECT v.vehicle_id id, v.nickname nickname
					FROM vehicle v 
					WHERE v.name LIKE :pattern OR v.nickname LIKE :pattern
					LIMIT 15";

			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("nickname", NHibernateUtil.String);

				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
						 select new {
							 id = x.id,
							 name = x.nickname
						 }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);

		}

		public JsonResult List ()
		{
			var qry = (from x in MBEQueryable.IQVehicles
					   orderby x.Name
					   select new {
						   id = x.Id,
						   name = x.NickName + " - " + x.Name
					   }).ToList ();

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}
	}
}