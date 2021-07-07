using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class AbastosInventoryManagerController : Controller {
		public ActionResult Index (){
			if (!CashHelpers.ValidateExchangeRate ()) {
				return View ("InvalidExchangeRate");
			}

			var search = SearchPurchaseOrders (new Search<PurchaseOrder> {
				Limit = WebConfig.PageSize
			});

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<PurchaseOrder> search) {
			if (ModelState.IsValid) {
				search = SearchPurchaseOrders (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			} else {
				return View (search);
			}
		}

		protected Search<PurchaseOrder> SearchPurchaseOrders (Search<PurchaseOrder> search) {
			IQueryable<PurchaseOrder> qry = from x in PurchaseOrder.Queryable
							orderby x.Id descending
							select x;

			//if (search.Pattern == null) {
			//	qry = from x in qry
			//	      where !x.IsCancelled
			//	      select x;
			//} else {
			//	qry = from x in qry
			//	      where x.Supplier.Name.Contains (search.Pattern)
			//	      select x;
			//}

			search.Total = qry.Count ();
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}
	}
}
