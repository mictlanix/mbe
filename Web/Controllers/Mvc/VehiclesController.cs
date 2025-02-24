using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;
using Castle.ActiveRecord;
using System;
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

		public ActionResult ServiceOrders ()
		{
			var search = SearchServiceOrders (new Search<ServiceOrder> () {
				Limit = WebConfig.PageSize
			});

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_ServiceOrders", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult ServiceOrders (Search<ServiceOrder> search)
		{
			search = SearchServiceOrders (search);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_ServiceOrders", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult AddItem (int order, int product)
		{
			var entity = ServiceOrder.TryFind (order);
			var p = Product.TryFind (product);


			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			var item = new ServiceOrderDetail {
				ServiceOrder = entity,
				SparePart = p,
				Quantity = p.MinimumOrderQuantity,
				Comment = p.Comment,
				Date = DateTime.Now,
			};

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return Json (new {
				id = item.Id
			});
		}

		public ActionResult Item (int id)
		{
			var entity = ServiceOrderDetail.Find (id);
			return PartialView ("_ItemEditorView", entity);
		}

		Search<ServiceOrder> SearchServiceOrders (Search<ServiceOrder> search)
		{
			var item = WebConfig.Store;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			IQueryable<ServiceOrder> query = from x in ServiceOrder.Queryable
							 select x;

			if (int.TryParse (pattern, out int id) && id > 0) {
				query = query.Where (x => x.Id == id);
			} else if (string.IsNullOrEmpty (pattern)) {
				query = from x in query
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					select x;
			} else {
				query = from x in query
					where x.Vehicle.Name.Contains (pattern) ||
						x.Vehicle.NickName.Contains (pattern) ||
						(x.Notifier.FirstName + " " + x.Notifier.LastName).Contains (pattern)
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending
					select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult CreateServiceOrder ()
		{
			return PartialView ("_CreateServiceOrder", new ServiceOrder ());
		}

		[HttpPost]
		public ActionResult CreateServiceOrder (ServiceOrder order)
		{

			if (!ModelState.IsValid) {
				return PartialView ("_CreateServiceOrder", order);
			}
			order.CreationTime = DateTime.Now;
			order.ModificationTime = DateTime.Now;
			order.Creator = CurrentUser.Employee;
			order.Updater = order.Creator;
			order.Notifier = Employee.Find (order.NotifierId);
			order.Vehicle = Vehicle.Find (order.VehicleId);
			order.Date = DateTime.Now;

			using (var scope = new TransactionScope ()) {

				order.SaveAndFlush ();
			}

			return PartialView ("_RefreshServiceOrders");
		}

		public ActionResult DeleteServiceOrder (int id)
		{

			return PartialView ("_DeleteServiceOrder", ServiceOrder.Find (id));
		}

		[HttpPost, ActionName ("DeleteServiceOrder")]
		public ActionResult DeleteServiceOrderConfirmed (int id)
		{

			var item = ServiceOrder.Find (id);
			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return PartialView ("_RefreshServiceOrders");
		}

		public ActionResult EditServiceOrder (int id)
		{
			var item = ServiceOrder.Find (id);
			return View (item);
		}

		public ActionResult Items (int id)
		{
			var item = ServiceOrder.Find (id);
			return PartialView ("_Items", item.Details);
		}

		[HttpPost]
		public ActionResult SetItemQuantity (int id, decimal value)
		{
			var entity = ServiceOrderDetail.Find (id);

			if (entity.ServiceOrder.IsCompleted || entity.ServiceOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value < entity.SparePart.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MinimumQuantityRequired, entity.SparePart.MinimumOrderQuantity));
			}

			entity.Quantity = value;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Quantity),
			});
		}

		public ActionResult RemoveItem (int id)
		{
			var entity = ServiceOrderDetail.Find (id);

			if (entity.ServiceOrder.IsCompleted || entity.ServiceOrder.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		[HttpPost]
		public ActionResult SetServiceReport (int id, string value)
		{
			var entity = ServiceOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.ServiceDescription = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.ServiceDescription
			});
		}

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = ServiceOrder.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Comment = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.Comment
			});
		}

		[HttpPost]
		public ActionResult ServiceOrderCancel (int id)
		{
			var entity = ServiceOrder.Find (id);

			if (entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("ServiceOrders");
		}

		[HttpPost]
		public virtual ActionResult Confirm (int id)
		{
			var entity = ServiceOrder.TryFind (id);

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;


			using (var scope = new TransactionScope ()) {

				var warehouse = WebConfig.PointOfSale.Warehouse;
					var dt = DateTime.Now;

					foreach (var x in entity.Details) {
						//x.Warehouse = warehouse;
						x.Update ();

						InventoryHelpers.ChangeNotification (TransactionType.InventoryIssue, entity.Id,
							dt, warehouse, null, x.SparePart, -x.Quantity);
					}

				
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}
		public JsonResult GetSuggestions (string pattern)
		{
			pattern = pattern.TrimEnd (new char [] { '*' });


			var sql = @"
					SELECT v.vehicle_id id, v.nickname nickname,
					IFNULL( so.services, 0) service  FROM vehicle v 
					LEFT JOIN (SELECT vso.vehicle, COUNT(*) services 
							FROM vehicle_service_order vso 
							WHERE vso.cancelled = 0 AND vso.completed = 0
							GROUP BY vso.vehicle) so
					ON v.vehicle_id = so.vehicle
					WHERE v.name LIKE :pattern OR v.nickname LIKE :pattern
					LIMIT 15";

			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("nickname", NHibernateUtil.String);
				query.AddScalar ("service", NHibernateUtil.Int32);

				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
				     select new {
					     id = x.id,
					     name = x.nickname,
					     service = x.service
				     }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);

		}

		public JsonResult GetSparePartSuggestions (int id, string pattern)
		{


			pattern = pattern.TrimEnd (new char [] { '*' });


			var sql = @"SELECT	p.product_id		id,
						p.name			name,
						p.photo			url,
						SUM(lst.quantity)	quantity,
						w.name			warehouse
					FROM product p 
					LEFT JOIN lot_serial_tracking lst ON p.product_id = lst.product
					LEFT JOIN warehouse w ON w.warehouse_id = lst.warehouse
					WHERE (
						(p.name LIKE :pattern)
						OR (p.code LIKE :pattern)
						OR (p.sku LIKE :pattern)
						OR (p.brand LIKE :pattern)
						OR (p.model LIKE :pattern))
						AND p.deactivated = FALSE
						AND p.salable = FALSE
						AND p.purchasable = TRUE
						AND p.invoiceable = FALSE
					GROUP BY lst.product
					ORDER BY p.product_id DESC
					LIMIT 15";

			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("name", NHibernateUtil.String);
				query.AddScalar ("warehouse", NHibernateUtil.String);
				query.AddScalar ("url", NHibernateUtil.String);
				query.AddScalar ("quantity", NHibernateUtil.Decimal);

				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
				     select new {
					     id = x.id,
					     name = x.name,
					     url = x.url,
					     quantity = x.quantity,
				     }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);
		}
	}
}