using System;
using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using System.Collections.Generic;
using static NHibernate.Engine.Query.CallableParser;
using Castle.Core.Internal;


namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class DeliveryItinerariesController : CustomController {

		public ActionResult Index ()
		{
			var search = SearchDeliveryItineraries (new Search<DeliveriesItinerary> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<DeliveriesItinerary> search)
		{
			if (ModelState.IsValid) {
				search = SearchDeliveryItineraries (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}
		public ActionResult Deliveries ()
		{
			var date = DateTime.Now;

			var prev = 1;
			var next = 2;
			//date = new DateTime (2024, 08, 01);

			var pivot = date;

			var pickups = MBEQueryable.IQStores.Select (x => x.Address).ToList ();

			var query = DeliveryOrderDetail.Queryable.Where (x => !x.DeliveryOrder.IsCancelled
					&& x.DeliveryOrder.IsCompleted && !pickups.Contains (x.DeliveryOrder.ShipTo) && !x.DeliveryOrder.Store.IsDisabled)
				.OrderByDescending (y => y.Id);

			var pages = new List<DeliveriesOnDay> ();
			var items = query.Where (x => x.DeliveryOrder.Date.Value.Date >= date.AddDays (-prev).Date && x.DeliveryOrder.Date.Value.Date <= date.AddDays (next).Date).ToList ();


			pages.Add (new DeliveriesOnDay {
				Title = Resources.PreviousDeliveryOrders, Selected = false,
				Details = query.Where (x => x.DeliveryOrder.Date.Value.Date < date.AddDays (-prev)).Take (WebConfig.PageSize).ToList ()
			});


			pivot = date.AddDays (-prev);

			for (int i = 0; i < prev + next + 1; i++) {
				pages.Add (new DeliveriesOnDay {
					Title = pivot.Date.ToShortDateString (),
					Selected = false,
					Date = pivot.Date,
					Details = items.Where (y => y.DeliveryOrder.Date.Value.Date == pivot.Date)
					//.OrderBy (y => y.Quantity - y.DeliveriesItineraryDetails.Sum (z => (decimal?) z.SentQuantity ?? 0) > 0 ? 1:0 )
					.OrderByDescending (z => z.OrderDetail.SalesOrder.Priority).ToList ()
				});
				pivot = pivot.AddDays (1);
			}

			pages.Where (x => x.Date.Date == date.Date).Single ().Title = Resources.TodayDeliveryOrders;
			pages.Where (x => x.Date.Date == date.Date).Single ().Selected = true;
			pages.Where (x => x.Date.Date == date.AddDays (1).Date).Single ().Title = Resources.TomorrowDeliveryOrders;



			pages.Add (new DeliveriesOnDay {
				Title = Resources.FollowingDeliveryOrders, Selected = false,
				Details = query.Where (x => x.DeliveryOrder.Date.Value.Date > date.AddDays (next)).Take (WebConfig.PageSize).ToList ()
			});

			return View (pages);
		}


		Search<DeliveryOrder> SearchDeliveries (Search<DeliveryOrder> search)
		{
			IQueryable<DeliveryOrder> qry;
			DateTime date;
			DateTime.TryParse (search.Pattern, out date);

			if (search.Pattern == null) {
				qry = from x in DeliveryOrder.Queryable
				      orderby x.Id descending
				      select x;
			} else {
				qry = from x in DeliveryOrder.Queryable
				      where x.Date == date.Date
				      orderby x.Id descending
				      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		Search<DeliveriesItinerary> SearchDeliveryItineraries (Search<DeliveriesItinerary> search)
		{
			IQueryable<DeliveriesItinerary> qry;
			DateTime date;
			DateTime.TryParse (search.Pattern, out date);

			if (search.Pattern == null) {
				qry = from x in DeliveriesItinerary.Queryable
				      orderby x.Id descending
				      select x;
			} else {
				qry = from x in DeliveriesItinerary.Queryable
				      where x.DueDate.Date == date.Date
				      orderby x.Id descending
				      select x;
			}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult Details (int id)
		{
			var item = DeliveriesItinerary.Find (id);

			return View (item);
		}

		public ActionResult Create ()
		{
			//if (!CashHelpers.ValidateExchangeRate ()) {
			//    return View ("InvalidExchangeRate");
			//}

			return PartialView ("_Create");
		}

		[HttpPost]
		public ActionResult Create (DeliveriesItinerary item)
		{
			item.VehicleOperator = VehicleOperator.TryFind (item.VehicleOperatorId);
			item.Vehicle = Vehicle.TryFind (item.VehicleId);

			//if (item.DueDate.Date < DateTime.Now.Date) {
			//	ModelState.AddModelError ("DueDate", Resources.Validation_Date);
			//}

			//var itinerary_invalid = DeliveriesItinerary.Queryable
			//	.Where(x => x.DueDate.Date == item.DueDate.Date
			//	&& x.VehicleOperator == item.VehicleOperator
			//	&& x.Vehicle == item.Vehicle).Count() > 0;

			//if (itinerary_invalid) {
			//	ModelState.AddModelError ("", Resources.ItemAlreadyAdded);
			//}

			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			item.Creator = CurrentUser.Employee;
			item.Updater = item.Creator;
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", new DeliveriesItinerary { Id = item.Id });
		}

		public ActionResult Edit (int id)
		{
			var item = DeliveriesItinerary.Find (id);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_MasterEditView", item);
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("Details", new {
					id = item.Id
				});
			}

			return View (item);
		}


		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = DeliveriesItinerary.Find (id);
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
		public ActionResult SetItemDetailQuantity (int id, string value)
		{
			var entity = DeliveriesItineraryDetail.Find (id);
			decimal val = 0;
			decimal.TryParse (value, out val);

			if (entity.DeliveriesItinerary.IsCompleted || entity.DeliveriesItinerary.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}
			var remaining_delivery_detail = GetRemainingToDeliver (entity.DeliveryOrderDetail) + entity.SentQuantity;
			entity.SentQuantity = val > remaining_delivery_detail ? remaining_delivery_detail : val;
			entity.DeliveriesItinerary.Updater = CurrentUser.Employee;
			entity.DeliveriesItinerary.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.SentQuantity
			});
		}

		/// <summary>
		/// DeliveryItineraries/SetItemDetailQuantity/1
		/// </summary>
		/// <param name="delivery_order_detail_id"></param>
		/// <param name="id"></param>
		/// <returns></returns>

		[HttpPost]
		public ActionResult AddDeliveryOrderDetail (int delivery_order_detail_id, int id)
		{
			var entity = DeliveriesItinerary.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var delivery_order_detail = DeliveryOrderDetail.Find (delivery_order_detail_id);

			var detail = new DeliveriesItineraryDetail {
				DeliveriesItinerary = entity,
				DeliveryOrderDetail = delivery_order_detail,
				SentQuantity = GetRemainingToDeliver (delivery_order_detail)
			};

			using (var scope = new TransactionScope ()) {
				entity.CreateAndFlush ();
			}

			return Json (new {
				id = entity.Id
			});
		}

		[HttpPost]
		public ActionResult AddDeliveryOrder (int value, int id)
		{
			var entity = DeliveriesItinerary.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var delivery_order = DeliveryOrder.Find (value);

			if (delivery_order == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			if (delivery_order.IsCancelled || !delivery_order.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var details = delivery_order.Details.Select (x => new DeliveriesItineraryDetail {
				DeliveriesItinerary = entity,
				DeliveryOrderDetail = x,
				SentQuantity = GetRemainingToDeliver (x)
			}).ToList ();

			details = details.Where (x => x.SentQuantity > 0).ToList ();

			if (details.Count == 0) {
				Response.StatusCode = 400;
				return Content (Resources.AlreadyFullyDelivered);
			}

			details = details.Where (x => !entity.Details.Select (y => y.DeliveryOrderDetail).Contains (x.DeliveryOrderDetail)).ToList ();

			using (var scope = new TransactionScope ()) {
				details.Where (x => x.SentQuantity > 0).ForEach (y => y.CreateAndFlush ());
			}

			return Json (new {
				id = delivery_order.Id
			});
		}


		public ActionResult GetDetails (int id)
		{
			return PartialView ("_Items", DeliveriesItinerary.Find (id).Details);
		}

		[HttpPost]
		public JsonResult RemoveDetail (int id)
		{
			var item = DeliveriesItineraryDetail.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		// TODO: Remove inventory stuff
		[HttpPost]
		public ActionResult Confirm (int id)
		{
			DeliveriesItinerary item = DeliveriesItinerary.Find (id);


			var dt = DateTime.Now;
			var employee = CurrentUser.Employee;

			var verify = item.Details.Any (x => x.SentQuantity > GetRemainingToDeliver (x.DeliveryOrderDetail) + x.DeliveryOrderDetail.Quantity);

			if (verify) {
				return RedirectToAction ("Edit", item);
			}

			using (var scope = new TransactionScope ()) {

				item.IsCompleted = true;
				item.ModificationTime = DateTime.Now;
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var item = DeliveriesItinerary.Find (id);

			item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		public JsonResult GetSuggestions (int id, string pattern)
		{
			var sql = @"SELECT
					di.deliveries_itinerary_id	id,
					di.date				date,
					v.name				vehicle_name,
					v.nickname			vehicle_nickname,
					e.nickname			operator_nickname,
					e.first_name			operator_first_name,
					e.last_name			operator_last_name
					FROM deliveries_itinerary di 
					JOIN vehicle_operator vo ON vo.vehicle_operator_id = di.vehicle_operator
					JOIN vehicle v ON v.vehicle_id = di.vehicle
					JOIN employee e ON vo.driver = e.employee_id
					WHERE di.completed = FALSE AND di.cancelled = FALSE
					AND (v.name LIKE :pattern || di.date LIKE :pattern || v.nickname LIKE :pattern || e.nickname LIKE :pattern)
					ORDER BY di.deliveries_itinerary_id desc;
					LIMIT 15";


			var raw = (IList<dynamic>) ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);
				query.AddScalar ("id", NHibernateUtil.Int32);
				query.AddScalar ("date", NHibernateUtil.DateTime);
				query.AddScalar ("vehicle_name", NHibernateUtil.String);
				query.AddScalar ("operator_nickname", NHibernateUtil.String);
				query.AddScalar ("operator_first_name", NHibernateUtil.String);
				query.AddScalar ("operator_last_name", NHibernateUtil.String);


				query.SetParameter ("pattern", "%" + pattern + "%");
				return query.DynamicList ();
			}, null);

			var items = (from x in raw
				     select new {
					     id = x.id,
					     name = x.id + " - " + x.date + " - " + x.vehicle_name + " / " + x.operator_nickname
				     }).ToList ();


			return Json (items, JsonRequestBehavior.AllowGet);

		}

		private decimal GetRemainingToDeliver (DeliveryOrderDetail detail)
		{

			if (detail.DeliveryOrder.IsCancelled || !detail.DeliveryOrder.IsCompleted)
				return 0;

			var items = DeliveriesItineraryDetail.Queryable
				.Where (x => !x.DeliveriesItinerary.IsCancelled && x.DeliveryOrderDetail == detail).ToList ();
			return detail.Quantity - items.Sum (x => (decimal?) x.SentQuantity ?? 0);
		}

	}
}
