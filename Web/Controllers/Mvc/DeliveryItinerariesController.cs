using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Castle.ActiveRecord;
using Castle.Core.Internal;
using NHibernate;



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
			var items = query.Where (x => x.DeliveryOrder.Date.Date >= date.AddDays (-prev).Date && x.DeliveryOrder.Date.Date <= date.AddDays (next).Date).ToList ();


			pages.Add (new DeliveriesOnDay {
				Title = Resources.PreviousDeliveryOrders, Selected = false,
				Details = query.Where (x => x.DeliveryOrder.Date.Date < date.AddDays (-prev)).Take (WebConfig.PageSize).ToList ()
			});


			pivot = date.AddDays (-prev);

			for (int i = 0; i < prev + next + 1; i++) {
				pages.Add (new DeliveriesOnDay {
					Title = pivot.Date.ToShortDateString (),
					Selected = false,
					Date = pivot.Date,
					Details = items.Where (y => y.DeliveryOrder.Date.Date == pivot.Date)
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
				Details = query.Where (x => x.DeliveryOrder.Date.Date > date.AddDays (next)).Take (WebConfig.PageSize).ToList ()
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
			var qry = DeliveriesItinerary.Queryable.Where(x => !x.IsCancelled);
			DateTime date;
			DateTime.TryParse (search.Pattern, out date);
			var pattern = string.IsNullOrEmpty (search.Pattern) ? string.Empty : search.Pattern.Trim ();
			var warehouse = UserSettings.Find (CurrentUser.Identity.Name).PointOfSale.Warehouse;

			if (!string.IsNullOrEmpty (pattern)) {
				Int32.TryParse (pattern, out int result);
				if (result > 0) {
					qry = DeliveriesItinerary.Queryable.Where (x => x.Id == result);
				} else {
					//qry = qry.Where (x => x.VehicleOperator.Operator.FirstName.Contains (pattern) || x.VehicleOperator.Operator.LastName.Contains (pattern));
					qry = qry.Where (x => x.DeliveryOrders.Select (y => y.Customer.Name).Contains (pattern));
				}
			}

			qry = qry.OrderByDescending (x => x.Id);

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

		[HttpPost]
		public ActionResult New ()
		{

			var dt = DateTime.Now;
			var user = CurrentUser.Employee;

			var itinerary = new DeliveriesItinerary {
				Creator = user,
				Updater = user,
				CreationTime = dt,
				ModificationTime = dt,
				Date = dt,
				Warehouse = UserSettings.Find (CurrentUser.Identity.Name).PointOfSale.Warehouse
			};
			using (var scope = new TransactionScope ()) {
				itinerary.CreateAndFlush ();
			}
			return RedirectToAction ("Edit", new { id = itinerary.Id });
		}

		public ActionResult Edit (int id)
		{
			var item = DeliveriesItinerary.Find (id);
			if (!item.IsCancelled && !item.IsCompleted) {
				return View (item);
			}
			return RedirectToAction ("View", new { id = id });
		}

		public ActionResult View (int id)
		{
			var item = DeliveriesItinerary.Find (id);

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
		public ActionResult SetVehicle (int id, int value)
		{
			var entity = DeliveriesItinerary.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var vehicle = MBEQueryable.IQVehicles.SingleOrDefault (x => x.Id == value);

			if (vehicle == null) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.ItemNotFound, Resources.Vehicle));
			}

			entity.Vehicle = vehicle;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = vehicle.NickName + " - " + vehicle.Name
			});
		}

		[HttpPost]
		public ActionResult SetVehicleOperator (int id, int value)
		{
			var entity = DeliveriesItinerary.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var vehicleoperator = MBEQueryable.IQVehicleOperators.SingleOrDefault (x => x.Id == value);

			if (vehicleoperator == null) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.ItemNotFound, Resources.VehicleOperator));
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.VehicleOperator = vehicleoperator;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = id,
				value = entity.VehicleOperator.Operator.Nickname
			});
		}

		[HttpPost]
		public ActionResult SetDate (int id, DateTime? value)
		{
			var entity = DeliveriesItinerary.Find (id);
			var dt = DateTime.Now;

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			//if (!value.HasValue || value.Value.Date < dt.Date) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.InvalidDate);
			//}

			if (value != null) {
				entity.Date = value.Value;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Date)
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
			var remaining_delivery_detail = GetRemainingQuantityToDeliver (entity.DeliveryOrderDetail) + entity.SentQuantity;
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
				SentQuantity = GetRemainingQuantityToDeliver (delivery_order_detail)
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

			if (delivery_order.IsPickedUpInStore) {
				Response.StatusCode = 400;
				return Content (Resources.CounterDelivery);
			}

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
				SentQuantity = GetRemainingQuantityToDeliver (x)
			}).ToList ();

			details = details.Where (x => x.SentQuantity > 0).ToList ();

			if (details.Count == 0) {
				Response.StatusCode = 400;
				return Content (Resources.AlreadyFullyDelivered);
			}

			details = details.Where (x => !entity.Details.Select (y => y.DeliveryOrderDetail)
						.Contains (x.DeliveryOrderDetail)).ToList ();

			using (var scope = new TransactionScope ()) {
				details.Where (x => x.SentQuantity > 0)
					.ForEach (y => y.CreateAndFlush ());
			}

			return Json (new {
				id = delivery_order.Id
			});
		}

		[HttpPost]
		public ActionResult AddDeliveryOrdersOfTheDay (int id)
		{
			var entity = DeliveriesItinerary.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var settings = UserSettings.Queryable.Where(x => x.UserName == CurrentUser.Identity.Name).Single();
			var warehouse = settings.PointOfSale.Warehouse;

			GetDeliveriesItineraryDetails (entity, warehouse);

			return Json (new {
				id = entity.Id
			});
		}


		public ActionResult GetDetails (int id)
		{
			return PartialView ("_Items", DeliveriesItinerary.Find (id).Details);
		}

		public ActionResult GetAddresses (int id)
		{
			var entity = DeliveriesItinerary.Find (id);
			var addresses = entity.DeliveryOrders.Select(x =>
				new DeliveryItineraryAddressViewModel { DeliveryOrder = x, Itinerary = entity }).ToList();
			return PartialView ("_Addresses", addresses);
		}

		[HttpPost]
		public ActionResult RemoveDetail (int id)
		{
			var item = DeliveriesItineraryDetail.Find (id);
			if (item.DeliveriesItinerary.IsCancelled || item.DeliveriesItinerary.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		[HttpPost]
		public ActionResult RemoveDeliveryOrder (int id, int order)
		{
			var itinerary = DeliveriesItinerary.Find (id);
			if (itinerary.IsCancelled || itinerary.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var items = itinerary.Details.Where (x => x.DeliveryOrderDetail.DeliveryOrder.Id == order).ToList ();

			using (var scope = new TransactionScope ()) {
				items.ForEach (x => {
					x.DeleteAndFlush ();
				});
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		[HttpPost]
		public ActionResult RemoveAll (int id)
		{
			var itinerary = DeliveriesItinerary.Find (id);
			if (itinerary.IsCancelled || itinerary.IsCompleted) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var items = itinerary.Details.ToList ();

			using (var scope = new TransactionScope ()) {
				items.ForEach (x => {
					x.DeleteAndFlush ();
				});
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		[HttpPost]
		public ActionResult Confirm (int id)
		{
			DeliveriesItinerary item = DeliveriesItinerary.Find (id);


			var dt = DateTime.Now;
			var employee = CurrentUser.Employee;

			var verify = item.Details.Any (x => x.SentQuantity > GetRemainingQuantityToDeliver (x.DeliveryOrderDetail) + x.DeliveryOrderDetail.Quantity);

			if (verify) {
				ModelState.AddModelError ("", Resources.QuantitiesHaveChanged);
				return RedirectToAction ("Edit", item);
			}

			if (item.Vehicle == null) {
				Response.StatusCode = 400;
				return Content (Resources.ChooseVehicle);
			}

			if (item.VehicleOperator == null) {
				Response.StatusCode = 400;
				return Content (Resources.ChooseVehicleOperator);
			}

			//if (item.Date.Date < dt.Date) {
			//	Response.StatusCode = 400;
			//	return Content (Resources.InvalidDate);
			//}

			using (var scope = new TransactionScope ()) {

				item.IsCompleted = true;
				item.ModificationTime = DateTime.Now;
				item.UpdateAndFlush ();
			}

			if(Request.IsAjaxRequest()) {
				return Json (new {
					id = id,
					result = true
				});
			}

			return RedirectToAction ("Index");
		}

		//[HttpPost]
		public ActionResult Cancel (int id)
		{
			var item = DeliveriesItinerary.Find (id);

			item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			if (Request.IsAjaxRequest()) {
				return PartialView ("_Item", item);
			}

			return RedirectToAction ("Index");
		}

		public ActionResult Print (int id)
		{
			var item = DeliveriesItinerary.Queryable.Where(x => x.Id == id).Single();

			if (!item.IsCompleted) {
				return RedirectToAction ("Edit", new { id });
			}

			return PdfTicketView ("ItineraryTicket", item);

		}

		public ActionResult PrintDeliveryNotes (int id)
		{
			var item = DeliveriesItinerary.Queryable.Where (x => x.Id == id).Single ();

			if (!item.IsCompleted) {
				return RedirectToAction ("Edit", new { id });
			}

			return PdfTicketView ("DeliveryNotesTicket", item);

		}

		public ActionResult DeliveriesSummary ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			var search = new DateRange {
				StartDate = DateTime.Now.Date.AddDays (-7),
				EndDate = DateTime.Now.Date
			};

			return View ("DeliveriesSummaryReport",search);
		}

		[HttpPost]
		public ActionResult DeliveriesSummary (DateRange dateRange)
		{

			var query = DeliveryOrder.Queryable.Where (x => !x.IsCancelled
							&& x.IsCompleted
							&& x.Date >= dateRange.StartDate
							&& x.Date <= dateRange.EndDate
						).SelectMany(x => x.Details.Where(y => y.OrderDetail.Warehouse == WebConfig.PointOfSale.Warehouse));

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_DeliveriesSummaryReport", query);
			} else {
				return View (query);
			}
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

		private decimal GetRemainingQuantityToDeliver (DeliveryOrderDetail detail)
		{

			if (detail.DeliveryOrder.IsCancelled || !detail.DeliveryOrder.IsCompleted)
				return 0;

			var items = DeliveriesItineraryDetail.Queryable
				.Where (x => !x.DeliveriesItinerary.IsCancelled && x.DeliveryOrderDetail == detail).ToList ();
			return detail.Quantity - items.Sum (x => (decimal?) x.SentQuantity ?? 0);
		}

		private List<DeliveryOrderDetail> GetDeliveryOrderDetails (DateRange date, Warehouse w)
		{
			var orders = DeliveryOrder.Queryable.Where (x => x.Date >= date.StartDate && x.Date <= date.EndDate).ToList();

			var items = orders.Where (x =>
				x.Details.Any (y => y.OrderDetail.Warehouse == w)
				&& !x.IsPickedUpInStore
				).ToList ();
			var details = items.SelectMany (x => x.Details)
				.Where(x => x.OrderDetail.Product.IsStockable);

			return details.ToList ();
		}

		private void GetDeliveriesItineraryDetails (DeliveriesItinerary entity, Warehouse w) {

			var range = new DateRange (entity.Date, entity.Date);

			var details = GetDeliveryOrderDetails (range, entity.Warehouse);

			List<DeliveriesItineraryDetail> to_deliver = new List<DeliveriesItineraryDetail>();
			foreach (var detail in details) {

				if(entity.Details.Any(x => x.DeliveryOrderDetail == detail)) {
					continue; // already added
				}

				var quantity_to_send = GetRemainingQuantityToDeliver(detail);

				if(quantity_to_send <= 0) {
					continue; // already fully delivered
				}

				using(var scope = new TransactionScope ()) {
					// create the itinerary detail
					// if the quantity is 0, it will not be created
					// so we can avoid creating empty details
					(new DeliveriesItineraryDetail {
						DeliveryOrderDetail = detail,
						SentQuantity = quantity_to_send,
						DeliveriesItinerary = entity,
						Comment = detail.OrderDetail.Comment
					}).CreateAndFlush();
				}

			}
		}

	}
}
