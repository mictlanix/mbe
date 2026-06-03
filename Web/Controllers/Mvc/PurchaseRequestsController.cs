using System;
using System.Collections.Generic;
using System.Linq;
//using System.Web.Http.Results;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Utils;
using Mysqlx;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;

namespace Mictlanix.BE.Web.Controllers.Mvc {

	[Authorize]
	public class PurchaseRequestsController : CustomController {
		public ActionResult Index ()
		{

			var items = PurchaseRequest.Queryable.Where (x => !x.IsCancelled
					&& (x.Creator == CurrentUser.Employee || x.Updater == CurrentUser.Employee)).OrderByDescending (x => x.Date);

			Search<PurchaseRequest> search = new Search<PurchaseRequest> () {
				Limit = WebConfig.PageSize
			};

			search.Total = items.Count ();
			search.Results = items.Take (WebConfig.PageSize).ToList ();

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<PurchaseRequest> search)
		{

			var pattern = (search.Pattern ?? string.Empty).Trim ();
			var query = PurchaseRequest.Queryable.Where (x => !x.IsCancelled && (x.Creator == CurrentUser.Employee || x.Updater == CurrentUser.Employee));
			int id = 0;

			if (int.TryParse (pattern, out id)) {
				query = PurchaseRequest.Queryable.Where (x => x.Id == id);
			} else if (!string.IsNullOrEmpty (pattern)) {
				query = query.Where (x => x.Updater.Name.Contains (pattern) || x.Comment.Contains (pattern));
				if (pattern.Contains (Resources.WilcardStringPatternForSearch)) {
					query = PurchaseRequest.Queryable.OrderByDescending (x => x.Id);
				}
			}

			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = search.Results.Count ();

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		//public ActionResult Create ()
		//{
		//	var warehouse = UserSettings.Find (CurrentUser.Employee.Nickname).PointOfSale.Warehouse;
		//	var item = new PurchaseRequest {
		//		Warehouse = warehouse,
		//		WarehouseId = warehouse.Id
		//	};
		//	return PartialView ("_Create", item);
		//}

		//[HttpPost]
		//public ActionResult Create (PurchaseRequest item)
		//{
		//	//if (!(item.Quantity > 0)) {
		//	//	ModelState.AddModelError (Resources.QuantityShort, Resources.Validation_CannotBeZeroOrNegative);
		//	//}

		//	//if (item.Product == null && item.Comment == string.Empty) {
		//	//	ModelState.AddModelError (Resources.QuantityShort, Resources.ProductInfoRequired);
		//	//}

		//	if (!ModelState.IsValid) {
		//		return PartialView ("_Create", item);
		//	}

		//	item.Creator = CurrentUser.Employee;
		//	item.Updater = CurrentUser.Employee;
		//	item.CreationTime = DateTime.Now;
		//	item.ModificationTime = item.CreationTime;
		//	item.Date = item.CreationTime;
		//	item.Warehouse = Warehouse.Find (item.WarehouseId);
		//	//item.Product = item.ProductId.HasValue ? Product.TryFind (item.ProductId) : null;
		//	//item.Customer = item.CustomerId.HasValue ? Customer.TryFind (item.CustomerId) : null;

		//	using (var scope = new TransactionScope ()) {
		//		item.CreateAndFlush ();
		//	}

		//	return PartialView ("_Refresh");
		//}

		[HttpPost]
		public ActionResult New ()
		{
			var dt = DateTime.Now;
			var item = new PurchaseRequest ();

			item.Date = dt;

			item.Creator = CurrentUser.Employee;
			item.CreationTime = dt;
			item.Updater = item.Creator;
			item.Warehouse = WebConfig.PointOfSale.Warehouse;
			item.ModificationTime = dt;
			item.Serial = PurchaseRequest.Queryable
				.Where (x => x.Warehouse.Store == item.Warehouse.Store)
				.Select (y => (int?) y.Serial).Max () + 1 ?? 1;

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return RedirectToAction ("Edit", new { id = item.Id });
		}

		public ActionResult Edit (int id)
		{
			var item = PurchaseRequest.Find (id);
			if (item.IsCompleted && !item.IsCancelled)
				return RedirectToAction ("View");

			return View ("Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (PurchaseRequest item)
		{
			var purchase_request = PurchaseRequest.Find (item.Id);

			if (!ModelState.IsValid) {
				return View ("_Edit", item);
			}

			using (var scope = new TransactionScope ()) {
				purchase_request.UpdateAndFlush ();
			}

			//return PartialView ("_Refresh");
			return View ("Index");
		}

		[HttpPost]
		public ActionResult AddDetail (int id, int product_id)
		{

			var entity = PurchaseRequest.Find (id);
			var item = Product.Find (product_id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}


			using (var scope = new TransactionScope ()) {

				var detail = new PurchaseRequestDetail {
					PurchaseRequest = entity,
					Product = item,
					ProductName = item.Name,
					Quantity = item.MinimumOrderQuantity,
					Warehouse = entity.Warehouse,
					Customer = Customer.Find (WebConfig.DefaultCustomer),
					ToPurchase = true
				};

				detail.CreateAndFlush ();
				return Json (new { id = detail.Id });
			}

		}

		public JsonResult RemoveDetail (int id)
		{
			var item = PurchaseRequestDetail.Find (id);
			if (item.PurchaseRequest.IsCancelled || item.PurchaseRequest.IsCompleted) {
				return Json (new {
					id,
					ErrorMessage = Resources.ItemAlreadyCompletedOrCancelled
				});
			}

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

			return Json (new {
				id = id,
				result = true
			});
		}

		public JsonResult GetSuggestions (string pattern)
		{
			var query = MBEQueryable.IQProducts.Where (x => (x.Name.Contains (pattern)
								|| x.Code.Contains (pattern) || x.SKU.Contains (pattern)
								|| x.Brand.Contains (pattern) || x.BarCodeNumber.Contains (pattern)
								//|| (x.Supplier != null && x.Supplier.Name.Contains(pattern))
								) && x.IsPurchasable);
			var items = query.Take (15).ToList ().Select (x => new {
				id = x.Id, name = x.Name, comment = x.Comment,
				supplier = x.Supplier == null ? string.Format (Resources.AttribValueMissing, Resources.Supplier) : x.Supplier.Name
				, code = x.Code, model = x.Model, brand = x.Brand
			});

			return Json (items, JsonRequestBehavior.AllowGet);
		}

		public JsonResult Warehouses ()
		{
			var query = MBEQueryable.IQWarehouses.Select (x => new { value = x.Id, text = x.Name });
			return Json (query.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetWarehouse (int id, int value)
		{
			var entity = PurchaseRequest.Find (id);
			var item = MBEQueryable.IQWarehouses.Where (x => x.Id == value).Single ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.Warehouse = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.Warehouse.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = PurchaseRequest.Find (id);
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

		public ActionResult Detail (int id)
		{
			return PartialView ("_ItemEditorView", PurchaseRequestDetail.Find (id));
		}

		[HttpPost]
		public ActionResult SetDetailQuantity (int id, decimal value)
		{
			var entity = PurchaseRequestDetail.Find (id);

			if (entity.PurchaseRequest.IsCompleted || entity.PurchaseRequest.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (value < entity.Product.MinimumOrderQuantity) {
				Response.StatusCode = 400;
				return Content (string.Format (Resources.MinimumQuantityRequired, entity.Product.MinimumOrderQuantity));
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

		[HttpPost]
		public ActionResult SetDetailCustomer (int id, int value)
		{
			var entity = PurchaseRequestDetail.Find (id);

			if (entity.PurchaseRequest.IsCompleted || entity.PurchaseRequest.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			var customer = Customer.TryFind (value);

			entity.Customer = customer;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Customer.Name),
			});
		}

		[HttpPost]
		public ActionResult SetDetailProductName (int id, string value)
		{
			var entity = PurchaseRequestDetail.Find (id);

			if (entity.PurchaseRequest.IsCompleted || entity.PurchaseRequest.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.ProductName = value;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.ProductName),
			});
		}

		[HttpPost]
		public JsonResult SetDetailWarehouse (int id, int value)
		{
			var detail = PurchaseRequestDetail.Find (id);

			detail.Warehouse = Warehouse.Find (value);

			using (var scope = new TransactionScope ()) {
				detail.UpdateAndFlush ();
			}

			return Json (new {
				id = detail.Id,
				value = detail.Warehouse.Name
			});
		}

		[HttpPost]
		public ActionResult ToogleToPurchase (int id)
		{
			var item = PurchaseRequestDetail.Find (id);

			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			if (item.PurchaseRequest.IsApproved || item.PurchaseRequest.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			item.ToPurchase = !item.ToPurchase;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return PartialView ("_ItemDisplayView", item);
		}

		[HttpPost]
		public virtual ActionResult Confirm (int id)
		{
			var entity = PurchaseRequest.TryFind (id);

			if (entity.Details.Count <= 0 || entity.Details.Any (x => x.Quantity <= 0)) {
				return RedirectToAction ("Edit", new { id = entity.Id });
			}

			if (entity == null || entity.IsCompleted || entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;
			entity.Serial = entity.Serial > 0 ? entity.Serial :
				PurchaseRequest.Queryable.Where (x => x.Warehouse == WebConfig.PointOfSale.Warehouse).Select (x => (int?) x.Serial).Max () ?? 0 + 1;

			if (!WebConfig.PurchaseRequestApprovalRequired) {
				entity.IsApproved = true;
			}


			using (var scope = new TransactionScope ()) {

				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = PurchaseRequest.Find (id);

			if (entity.IsCancelled) {
				return RedirectToAction ("Index");
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		public ActionResult Print (int id)
		{
			var model = PurchaseRequest.Find (id);
			if (!model.IsCancelled && model.IsCompleted) {
				return PdfTicketView ("Print", model);
			}
			return RedirectToAction ("Index");
		}

		public ActionResult View (int id)
		{
			var item = PurchaseRequest.Find (id);

			if (!item.IsCompleted) {
				return RedirectToAction ("Edit", new { id = item.Id });
			}

			return View (item);
		}

		public ViewResult PurchaseRequestsApproval ()
		{
			var search = SearchPurchseRequestsForApproval (new Search<PurchaseRequest> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult PurchaseRequestsApproval (Search<PurchaseRequest> search)
		{
			if (ModelState.IsValid) {
				search = SearchPurchseRequestsForApproval (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		public ActionResult PurchaseRequestApproval (int id)
		{

			var item = PurchaseRequest.TryFind (id);
			return View (item);
		}

		[HttpPost]
		public ActionResult PurchaseRequestApproval (int id, bool approval, string value)
		{

			var item = PurchaseRequest.TryFind (id);
			var incidence = new Incidence ();
			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			//if (item.Date < DateTime.Now) {
			//	Response.StatusCode = 400;
			//	return Content (string.Format (Resources.Validation_DateGreaterThan, Resources.Date, DateTime.Now.Date));
			//}

			using (var scope = new TransactionScope ()) {

				if (approval) {
					item.IsApproved = true;
					item.UpdateAndFlush ();
				} else {
					incidence.SourceType = SourceType.PurchaseRequest;
					incidence.Reference = item.Id;
					incidence.Updater = CurrentUser.Employee;
					incidence.ModificationTime = DateTime.Now;
					incidence.PreviousState = JsonConvert.SerializeObject (item.GetSerializable ());
					incidence.Comment = value;
					incidence.CreateAndFlush ();

					item.IsApproved = false;
					item.IsCompleted = false;
					item.UpdateAndFlush ();
				}
			}

			if (Request.IsAjaxRequest ()) {
				return Json (new { id = id, done = true });
			}

			return RedirectToAction ("PurchaseRequestsApproval");
		}

		Search<PurchaseRequest> SearchPurchseRequestsForApproval (Search<PurchaseRequest> search)
		{
			IQueryable<PurchaseRequest> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			query = PurchaseRequest.Queryable.Where (x => !x.IsCancelled
									 && x.IsCompleted
						);

			if (int.TryParse (pattern, out id) && id > 0) {
				query = PurchaseRequest.Queryable.Where (y => y.Id == id || y.Serial == id);
			} else {
				query = query.Where (x => x.Creator.FirstName.Contains (pattern));
			}

			query = query.OrderBy (x => x.IsApproved ? 1 : 0)
					.OrderByDescending (x => x.Id);

			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = search.Results.Count ();

			return search;
		}

		CashSession GetSession ()
		{
			var item = WebConfig.CashDrawer;
			if (item == null)
				return null;

			return CashSession.Queryable.Where (x => x.End == null)
					.SingleOrDefault (x => x.CashDrawer.Id == item.Id);
		}

		public PurchaseRequestStatus GetRequestStatus (int id)
		{
			var request = PurchaseRequest.Find (id);

			if (request.IsCompleted) {
				if (request.IsCancelled) {
					return PurchaseRequestStatus.Cancelled;
				} else {

					var purchases_details = PurchaseOrderDetail.Queryable.Where (x => !x.Order.IsCancelled &&
					request.Details.Contains (x.PurchaseRequestDetail)).ToList ();

					if (purchases_details.Count () <= 0) {
						return PurchaseRequestStatus.OnRequest;
					} else {
						var purchases = purchases_details.Select (y => y.Order).ToList ();
						var receptions = InventoryReceipt.Queryable.
							Where (x => purchases.Contains (x.Order)).ToList ();
						if (receptions.Count () <= 0) {
							return PurchaseRequestStatus.OnPurchase;
						} else {
							if (receptions.Where (x => x.IsCompleted).Count () <= 0) {
								return PurchaseRequestStatus.OnReception;
							} else {
								return PurchaseRequestStatus.OnStock;
							}
						}
					}
				}
			}

			return PurchaseRequestStatus.OnEdition;

		}
	}
}