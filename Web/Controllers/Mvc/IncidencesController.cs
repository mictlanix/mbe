//
// IncidencesController.cs
//
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (C) 2026 Eddy Zavaleta, Mictlanix, and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	// Read-only view of the audit trail (mictlanix/mbe#63). Ten code paths write
	// incidences; this is the only thing that reads them back.
	[Authorize]
	public class IncidencesController : CustomController {
		// Also the target of links that open the history of one record, e.g.
		// ?Source=CustomerPayment&Reference=123.
		public ActionResult Index (IncidenceSearch search)
		{
			if (!GetAccessPrivilege (SystemObjects.Incidences).AllowRead) {
				return RedirectToAction ("Index", "Home");
			}

			return View (Find (search));
		}

		[HttpPost]
		[ActionName ("Index")]
		public ActionResult Search (IncidenceSearch search)
		{
			if (!GetAccessPrivilege (SystemObjects.Incidences).AllowRead) {
				return RedirectToAction ("Index", "Home");
			}

			search = Find (search);

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
			}

			return View (search);
		}

		public ActionResult Details (int id)
		{
			if (!GetAccessPrivilege (SystemObjects.Incidences).AllowRead) {
				return RedirectToAction ("Index", "Home");
			}

			return PartialView ("_Details", Incidence.Find (id));
		}

		static IncidenceSearch Find (IncidenceSearch search)
		{
			IQueryable<Incidence> query = Incidence.Queryable;

			search.Limit = WebConfig.PageSize;

			if (search.Source.HasValue) {
				var source = search.Source.Value;
				query = query.Where (x => x.SourceType == source);
			}

			if (search.Reference.HasValue) {
				var reference = search.Reference.Value;
				query = query.Where (x => x.Reference == reference);
			}

			if (search.StartDate.HasValue) {
				var start = search.StartDate.Value.Date;
				query = query.Where (x => x.ModificationTime >= start);
			}

			if (search.EndDate.HasValue) {
				var end = search.EndDate.Value.Date.AddDays (1);
				query = query.Where (x => x.ModificationTime < end);
			}

			search.Total = query.Count ();
			search.Results = query.OrderByDescending (x => x.Id)
					      .Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}
	}
}
