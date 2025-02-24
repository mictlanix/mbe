using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class ProductionSitesController : CustomController {
		public ViewResult Index ()
		{
			Search<ProductionSite> search = SearchWarehouses (new Search<ProductionSite> {
				Limit=WebConfig.PageSize,
			});
			search.Limit = WebConfig.PageSize;

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<ProductionSite> search)
		{
			if (ModelState.IsValid) {
				search = SearchWarehouses (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		Search<ProductionSite> SearchWarehouses (Search<ProductionSite> search)
		{
			var query = MBEQueryable.IQProductionSites;

			if (search.Pattern != null) {
				query = from x in query
					where x.Name.Contains (search.Pattern) || x.Code.Contains(search.Pattern)
					  select x;
			}

				search.Total = query.Count ();
				search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}

		public ActionResult View (int id)
		{
			var item = MBEQueryable.IQProductionSites.Single (x => x.Id == id);
			return PartialView ("_View", item);
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create");
		}

		[HttpPost]
		public ActionResult Create (ProductionSite item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			item.Store = Store.Find (item.StoreId);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		public ActionResult Edit (int id)
		{
			var item = MBEQueryable.IQProductionSites.Single (x => x.Id == id);
			return PartialView ("_Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (Warehouse item)
		{
			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);

			var entity = MBEQueryable.IQProductionSites.Single (x => x.Id == item.Id);

			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Store = Store.Find (item.StoreId);
			entity.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = MBEQueryable.IQProductionSites.Single (x => x.Id == id);
			item.IsDisabled = true;
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = ProductionSite.Find (id);

			try {
				using (var scope = new TransactionScope ()) {
					item.IsDisabled = true;
					item.UpdateAndFlush ();
				}

				return PartialView ("_DeleteSuccesful", item);
			} catch (TransactionException) {
				return PartialView ("DeleteUnsuccessful");
			}
		}

		public JsonResult GetSuggestions (string pattern)
		{
			var qry = from x in MBEQueryable.IQProductionSites
				  where x.Code.Contains (pattern) ||
					x.Name.Contains (pattern)
				  select new { id = x.Id, name = x.Name };

			return Json (qry.Take (15).ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult List ()
		{
			var qry = from x in MBEQueryable.IQProductionSites
				  orderby x.Name
				  select new {
					  value = x.Id,
					  text = x.Name
				  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}
	}
}