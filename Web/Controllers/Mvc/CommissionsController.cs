using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	public class CommissionsController : Controller {
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

		// GET: Commissions/Details/5
		public ActionResult Commissions (int id)
		{
			return View ();
		}

		// GET: Commissions/Create
		public ActionResult Create ()
		{
			return View ();
		}

		// POST: Commissions/Create
		[HttpPost]
		public ActionResult Create (FormCollection collection)
		{
			try {
				// TODO: Add insert logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}

		// GET: Commissions/Edit/5
		public ActionResult Edit (int id)
		{
			return View ();
		}

		// POST: Commissions/Edit/5
		[HttpPost]
		public ActionResult Edit (int id, FormCollection collection)
		{
			try {
				// TODO: Add update logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}

		// GET: Commissions/Delete/5
		public ActionResult Delete (int id)
		{
			return View ();
		}

		// POST: Commissions/Delete/5
		[HttpPost]
		public ActionResult Delete (int id, FormCollection collection)
		{
			try {
				// TODO: Add delete logic here

				return RedirectToAction ("Index");
			} catch {
				return View ();
			}
		}
	}
}
