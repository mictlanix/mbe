//
// IncidencesTests.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Security;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#63: the audit trail had ten writers and no reader.
	//
	// Rows are written under SourceType.PurchaseOrder, which nothing in the
	// application writes, so a filter on it sees only what these tests created.
	[TestFixture]
	public class IncidencesTests {
		const SourceType Unused = SourceType.PurchaseOrder;

		Seed seed;
		readonly List<int> created = new List<int> ();

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				foreach (var id in created) {
					var item = Incidence.TryFind (id);

					if (item != null) {
						item.DeleteAndFlush ();
					}
				}

				seed.Dispose ();
			}

			created.Clear ();
			FakeHttpContext.Detach ();
		}

		int Write (int reference, string comment = null, string content = null, DateTime? time = null,
			   SourceType source = Unused)
		{
			using (new SessionScope ()) {
				var item = new Incidence {
					SourceType = source,
					Reference = reference,
					Updater = seed.Employee,
					PreviousState = content,
					Comment = comment,
					ModificationTime = time ?? DateTime.Now
				};

				item.CreateAndFlush ();
				created.Add (item.Id);

				return item.Id;
			}
		}

		IncidencesController LogIn (AccessRight rights, bool administrator = false)
		{
			var controller = new IncidencesController ();

			using (new SessionScope ()) {
				var user = seed.User (SystemObjects.Incidences, rights, administrator);
				var principal = new CustomPrincipal (user.UserName, user.Email, administrator,
								     seed.Employee, new List<AccessPrivilege> ());

				FakeHttpContext.Attach (controller, principal);
			}

			return controller;
		}

		static IncidenceContent Parse (string content, SourceType source = SourceType.CustomerPayment)
		{
			return IncidenceContent.Parse (new Incidence { SourceType = source, PreviousState = content });
		}

		// ---- reading content ----

		// Before #42 the snapshot was taken after ModificationTime was set to the time
		// of the correction; a true previous state of a once-only edit has it equal to
		// CreationTime.
		[Test]
		public void Content_FlagsAPaymentSnapshotTakenAfterTheChange ()
		{
			var content = Parse ("{\"Id\":1,\"CreationTime\":\"2026-10-01T11:20:13\",\"ModificationTime\":\"2026-10-06T14:44:56.692859-06:00\",\"Amount\":90.0}");

			Assert.That (content.IsPostChangeSnapshot, Is.True);
		}

		[Test]
		public void Content_TrustsAPaymentSnapshotTakenBeforeTheChange ()
		{
			var content = Parse ("{\"Id\":1,\"CreationTime\":\"2026-10-01T11:20:13\",\"ModificationTime\":\"2026-10-01T11:20:13\",\"Amount\":100.0}");

			Assert.That (content.IsPostChangeSnapshot, Is.False);
		}

		// Only payments are edited once; other snapshots legitimately differ.
		[Test]
		public void Content_FlagsOnlyPaymentSnapshots ()
		{
			var content = Parse ("{\"CreationTime\":\"2026-10-01T11:20:13\",\"ModificationTime\":\"2026-10-06T14:44:56\"}",
					     SourceType.Customer);

			Assert.That (content.IsPostChangeSnapshot, Is.False);
		}

		[Test]
		public void Content_ListsFieldsShowingNestedRecordsByNameAndSkippingNulls ()
		{
			var content = Parse ("{\"Amount\":90.0,\"Customer\":null,\"Store\":{\"Id\":1,\"Name\":\"FERRETERIA\"},\"Allocations\":[]}");

			Assert.That (content.IsStructured, Is.True);
			Assert.That (content.Fields.Select (x => x.Key + "=" + x.Value),
				     Is.EqualTo (new [] { Resources.Amount + "=90", Resources.Store + "=FERRETERIA" }));
		}

		// A snapshot is the serialized model, so its fields read like the model's own
		// screens: enum names rather than numbers, the app's date format, Si/No.
		[Test]
		public void Content_FormatsFieldsThroughTheModelProperty ()
		{
			var content = Parse ("{\"Currency\":0,\"ModificationTime\":\"2026-10-06T14:44:56.692859-06:00\",\"HasCredit\":false,\"Method\":999}");
			var fields = content.Fields.ToDictionary (x => x.Key, x => x.Value);

			Assert.That (fields [Resources.Currency], Is.EqualTo (CurrencyCode.MXN.GetDisplayName ()));
			Assert.That (fields [Resources.ModificationTime], Is.EqualTo ("2026-10-06 14:44:56"));
			Assert.That (fields ["HasCredit"], Is.EqualTo (Resources.No));
			Assert.That (fields [Resources.PaymentMethod], Is.EqualTo ("999"), "a value the enum does not define stays a number");
		}

		[Test]
		public void Content_KeepsPlainTextAsText ()
		{
			var content = Parse ("Product created.", SourceType.Product);

			Assert.That (content.IsStructured, Is.False);
			Assert.That (content.Text, Is.EqualTo ("Product created."));
		}

		// ---- latest comments ----

		[Test]
		public void LatestComments_ReturnsTheNewestCommentPerRecord ()
		{
			var reference = 900000001;

			Write (reference, "first");
			Write (reference, "second");
			Write (reference, "");
			Write (reference + 1, "other record");
			Write (reference, "wrong source", source: SourceType.Pricing);

			using (new SessionScope ()) {
				var comments = Incidence.LatestComments (Unused, new [] { reference, reference + 2 });

				Assert.That (comments.Count, Is.EqualTo (1));
				Assert.That (comments [reference], Is.EqualTo ("second"));
			}
		}

		[Test]
		public void LatestComments_WithNoRecords_IsEmpty ()
		{
			using (new SessionScope ()) {
				Assert.That (Incidence.LatestComments (Unused, new int [0]), Is.Empty);
			}
		}

		// ---- the screen ----

		[Test]
		public void Index_RedirectsWithoutThePrivilege ()
		{
			var controller = LogIn (AccessRight.None);

			using (new SessionScope ()) {
				Assert.That (controller.Index (new IncidenceSearch ()), Is.InstanceOf<RedirectToRouteResult> ());
				Assert.That (controller.Search (new IncidenceSearch ()), Is.InstanceOf<RedirectToRouteResult> ());
				Assert.That (controller.Details (0), Is.InstanceOf<RedirectToRouteResult> ());
			}
		}

		[Test]
		public void Index_OpensForAnAdministratorWithoutThePrivilege ()
		{
			var controller = LogIn (AccessRight.None, administrator: true);

			using (new SessionScope ()) {
				Assert.That (controller.Index (new IncidenceSearch ()), Is.InstanceOf<ViewResult> ());
			}
		}

		[Test]
		public void Index_FiltersBySourceAndReference_NewestFirst ()
		{
			var reference = 900000011;
			var older = Write (reference, "older");
			var newer = Write (reference, "newer");
			Write (reference + 1, "other record");

			var controller = LogIn (AccessRight.Read);

			using (new SessionScope ()) {
				var result = (ViewResult) controller.Index (new IncidenceSearch { Source = Unused, Reference = reference });
				var search = (IncidenceSearch) result.Model;

				Assert.That (search.Total, Is.EqualTo (2));
				Assert.That (search.Results.Select (x => x.Id), Is.EqualTo (new [] { newer, older }));
			}
		}

		// The end date is inclusive: anything during that day is in range.
		[Test]
		public void Index_FiltersByDateRange ()
		{
			var reference = 900000021;
			var day = new DateTime (2020, 3, 15);
			Write (reference, "before", time: day.AddSeconds (-1));
			var start = Write (reference, "start", time: day);
			var end = Write (reference, "end", time: day.AddDays (1).AddHours (23));
			Write (reference, "after", time: day.AddDays (2));

			var controller = LogIn (AccessRight.Read);

			using (new SessionScope ()) {
				var result = (ViewResult) controller.Index (new IncidenceSearch {
					Source = Unused,
					StartDate = day,
					EndDate = day.AddDays (1)
				});
				var search = (IncidenceSearch) result.Model;

				Assert.That (search.Results.Select (x => x.Id), Is.EquivalentTo (new [] { start, end }));
			}
		}

		[Test]
		public void Details_ShowsOneIncidence ()
		{
			var id = Write (900000031, "detail");
			var controller = LogIn (AccessRight.Read);

			using (new SessionScope ()) {
				var result = (PartialViewResult) controller.Details (id);

				Assert.That (result.ViewName, Is.EqualTo ("_Details"));
				Assert.That (((Incidence) result.Model).Id, Is.EqualTo (id));
			}
		}
	}
}
