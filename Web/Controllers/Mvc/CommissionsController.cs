using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class CommissionsController : CustomController {
		// GET: Commissions
		public ActionResult Index ()
		{
			return View ();
		}

		public ActionResult CommissionProducts ()
		{
			var products = CommissionProduct.Queryable
				.OrderBy (x => x.Commission)
				.ThenBy (x => x.Product.Name)
				.ToList ();

			var search = new Search<CommissionProduct> ();
			search.Results = products;
			return View (search);
		}

		public JsonResult LabelList ()
		{
			var qry = from x in Commission.Queryable
				  orderby x.Name
				  select new {
					  value = x.Id,
					  text = x.Name
				  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SetCommissionLabel (int id, int value)
		{

			var permissions = GetAccessPrivilege (SystemObjects.CommissionsBySalesPerson);
			if (permissions == null || !permissions.AllowUpdate) {
				Response.StatusCode = 400;
				return Content (Resources.NoAccessRights);
			}

			var Label = Commission.Queryable.Single (x => x.Id == value);
			var product = Product.Queryable.SingleOrDefault (x => x.Id == id);

			if (product == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			var commissionProduct = CommissionProduct.Queryable.SingleOrDefault (x => x.Product == product);

			if (commissionProduct != null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemAlreadyAdded);
			}

			var entity = new CommissionProduct {
				Commission = Label,
				Product = product
			};
				using (var scope = new TransactionScope ()) {
					entity.SaveAndFlush ();
				}

			return Json (new {
				id = entity.Id
			});
		}
	}
}
