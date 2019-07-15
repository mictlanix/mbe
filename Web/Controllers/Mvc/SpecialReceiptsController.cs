using System;
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;
using Newtonsoft.Json.Linq;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
    [Authorize]
	public class SpecialReceiptsController : CustomController {
		public ViewResult Index ()
		{
			if (WebConfig.Store == null) {
				return View ("InvalidStore");
			}

			var search = SearchSpecialReceipts (new Search<SpecialReceipt> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<SpecialReceipt> search)
		{
			if (ModelState.IsValid) {
				search = SearchSpecialReceipts(search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<SpecialReceipt> SearchSpecialReceipts(Search<SpecialReceipt> search)
		{
			IQueryable<SpecialReceipt> query;
			var pattern = (search.Pattern ?? string.Empty).Trim ();
			int id = 0;

			query = SpecialReceipt.Queryable.Where (x => !x.IsCancelled)
						.OrderBy (x => x.IsCompleted || x.IsCancelled ? 1 : 0)
						.OrderByDescending (x => x.Id);

			if (int.TryParse (pattern, out id) && id > 0) {
				query = query.Where (y => y.Id == id || y.Serial == id);
			} else if (!string.IsNullOrEmpty(pattern)) {
				query = query.Where (x => x.CustomerName.Contains (pattern));
			}

			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = search.Results.Count ();

			return search;
		}

		public ViewResult View (int id)
		{
			var item = SpecialReceipt.Find (id);
			return View (item);
		}

		public ActionResult Print (int id)
		{
			var item = SpecialReceipt.Find (id);

			return PdfView (WebConfig.DeliveryOrderTemplate, item);
		}

		public ActionResult PrintFormat (int id)
		{
			var item = SpecialReceipt.Find (id);
			return PdfView (WebConfig.DeliveryOrderTemplate, item);
		}

		[HttpPost]
		public ActionResult New (string value)
		{
			var dt = DateTime.Now;
			var item = new SpecialReceipt ();

            item.Store = WebConfig.Store;


			try {
				item.Serial = (from x in SpecialReceipt.Queryable
					       where x.Store.Id == item.Store.Id
					       select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}

			item.Store = WebConfig.Store;

            string json = @"{'elementoAColar': '','unidadDeTransporte': '474URA','volumenM3': '','tipo': '','resistenciaConcreto': '','edadGarantia':'',
                'TMA':'','revenimientoCM':'','tiro':'','pedidoM3':'','porSurtirM3':'','impermeabilizante':false,'retardanteDeFraguado':false,'aceletente':false,
                  'fibras':false,'aditivosEspeciales':false,'observaciones':'','salidaPlanta': '','llegadaObra':'','inicioDescarga':'','finDescarga':'','salidaObra':''}";
            

			item.Serial = 0;
			item.Date = DateTime.Now;
			item.CreationTime = DateTime.Now;
			item.Creator = CurrentUser.Employee;
            item.SalesPerson = CurrentUser.Employee;
			item.ModificationTime = item.CreationTime;
			item.Updater = item.Creator;
			item.CustomerName = string.Empty;
			item.CustomerShipTo = string.Empty;
			item.Comment = string.Empty;
            item.JSON = json;


			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

            return RedirectToAction("Edit", new { id = item.Id });
		}

		public ActionResult Edit (int id)
		{
			var item = SpecialReceipt.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("View", new { id = item.Id });
			}

			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

			return View (item);
		}

        [HttpPost]
		public ActionResult SetSalesPerson (int id, int value)
		{
			var entity = SpecialReceipt.Find (id);
			var item = Employee.TryFind (value);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			if (item != null) {
				entity.SalesPerson = item;
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = DateTime.Now;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = id,
				value = entity.SalesPerson.ToString ()
			});
		}

		[HttpPost]
		public ActionResult SetDate (int id, DateTime? value)
		{
			var entity = SpecialReceipt.Find (id);

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			if (value != null) {
				entity.Date = value.Value;
			} else {
				entity.Date = null;
			}
				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			

			return Json (new {
				id = id,
				value = entity.FormattedValueFor (x => x.Date)
			});
		}

        [HttpPost]
		public ActionResult SetCustomerName (int id, string value)
		{
			var entity = SpecialReceipt.Find (id);
			string val = (value ?? string.Empty).Trim ();

			if (entity.IsCompleted || entity.IsCancelled) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyCompletedOrCancelled);
			}

			entity.CustomerName = (value.Length == 0) ? null : val;
			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return Json (new { id = id, value = value });
		}


        [HttpPost]
        public ActionResult SetShipTo(int id, string value)
        {
            var entity = SpecialReceipt.Find(id);
            string val = (value ?? string.Empty).Trim();

            if (entity.IsCompleted || entity.IsCancelled)
            {
                Response.StatusCode = 400;
                return Content(Resources.ItemAlreadyCompletedOrCancelled);
            }

            entity.CustomerShipTo = (value.Length == 0) ? null : val;
            entity.Updater = CurrentUser.Employee;
            entity.ModificationTime = DateTime.Now;

            using (var scope = new TransactionScope())
            {
                entity.UpdateAndFlush();
            }

            return Json(new { id = id, value = value });
        }

		 [HttpPost]
        public ActionResult UpdateJSON(int id, string value, string attribute)
        {
            var entity = SpecialReceipt.Find(id);
            string val = (value ?? string.Empty).Trim();

            if (entity.IsCompleted || entity.IsCancelled)
            {
                Response.StatusCode = 400;
                return Content(Resources.ItemAlreadyCompletedOrCancelled);
            }

            JObject json = JObject.Parse(entity.JSON);
            json[attribute] = val;

            entity.JSON = json.ToString();
            entity.Updater = CurrentUser.Employee;
            entity.ModificationTime = DateTime.Now;

            using (var scope = new TransactionScope())
            {
                entity.UpdateAndFlush();
            }

            return Json(new { id = id, value = value });
        }

		[HttpPost]
		public ActionResult SetComment (int id, string value)
		{
			var entity = DeliveryOrder.Find (id);
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
		public ActionResult Confirm (int id)
		{
			var entity = SpecialReceipt.TryFind (id);

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = DateTime.Now;
			entity.IsCompleted = true;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		[HttpPost]
		public ActionResult Cancel (int id)
		{
			var entity = SpecialReceipt.TryFind (id);

			if (entity == null || entity.IsCancelled || entity.IsDelivered) {
				return RedirectToAction ("Index");
			}

			entity.IsCancelled = true;
			entity.ModificationTime = DateTime.Now;
			entity.Updater = CurrentUser.Employee;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return RedirectToAction ("Index");
		}

		public ViewResult Delivery (int id)
		{

			var entity = SpecialReceipt.TryFind (id);

			return View (entity);
		}

		[HttpPost]
		public ActionResult Delivered (int id)
		{

			var specialReceipt = SpecialReceipt.Find (id);

			if (!(specialReceipt.IsCancelled || specialReceipt.IsDelivered)) {
				using (var scope = new TransactionScope ()) {
					specialReceipt.IsDelivered = true;
					specialReceipt.Updater = CurrentUser.Employee;
					specialReceipt.ModificationTime = DateTime.Now;

					specialReceipt.UpdateAndFlush ();
				}
			}

			return RedirectToAction ("Index");
		}
	}
}