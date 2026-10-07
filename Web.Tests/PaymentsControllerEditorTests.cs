//
// PaymentsControllerEditorTests.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Security;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// The payment correction screen (SystemObjects.PaymentsEditor) fixes historical
	// payment data. mictlanix/mbe#48: it demanded a cash drawer, an open cash session
	// and IsAdministrator on top of the privilege, and the "edit only once" rule lived
	// only in the view.
	//
	// No cash drawer cookie is set anywhere in this fixture, and no session is opened.
	[TestFixture]
	public class PaymentsControllerEditorTests {
		Seed seed;

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
				seed.Dispose ();
			}

			FakeHttpContext.Detach ();
		}

		PaymentsController LogIn (AccessRight rights, bool administrator = false)
		{
			var controller = new PaymentsController ();

			using (new SessionScope ()) {
				var user = seed.User (SystemObjects.PaymentsEditor, rights, administrator);
				var principal = new CustomPrincipal (user.UserName, user.Email, administrator,
								     seed.Employee, new List<AccessPrivilege> ());

				FakeHttpContext.Attach (controller, principal);
			}

			return controller;
		}

		[Test]
		public void Payments_OpensForAPrivilegedNonAdministratorWithoutADrawer ()
		{
			var controller = LogIn (AccessRight.Read);

			using (new SessionScope ()) {
				Assert.That (controller.Payments (), Is.InstanceOf<ViewResult> ());
				Assert.That (controller.Payments (new Search<CustomerPayment> ()), Is.InstanceOf<ViewResult> ());
			}
		}

		// Administrators hold every right on every module, row or no row.
		[Test]
		public void Payments_OpensForAnAdministratorWithoutThePrivilege ()
		{
			var controller = LogIn (AccessRight.None, administrator: true);

			using (new SessionScope ()) {
				Assert.That (controller.Payments (), Is.InstanceOf<ViewResult> ());
				Assert.That (controller.Payments (new Search<CustomerPayment> ()), Is.InstanceOf<ViewResult> ());
			}
		}

		[Test]
		public void Payments_RedirectsANonAdministratorWithoutThePrivilege ()
		{
			var controller = LogIn (AccessRight.None);

			using (new SessionScope ()) {
				Assert.That (controller.Payments (), Is.InstanceOf<RedirectToRouteResult> ());
				Assert.That (controller.Payments (new Search<CustomerPayment> ()), Is.InstanceOf<RedirectToRouteResult> ());
			}
		}

		[Test]
		public void EditPayment_AppliesTheFirstCorrection ()
		{
			var controller = LogIn (AccessRight.Read | AccessRight.Update);
			int id;

			using (new SessionScope ()) {
				id = seed.Payment (PaymentType.Immediate, 100m).Id;
			}

			using (new SessionScope ()) {
				var result = controller.EditPayment (Correction (id, 90m));
				Assert.That (((PartialViewResult) result).ViewName, Is.EqualTo ("_RefreshPayment"));
			}

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.Find (id).Amount, Is.EqualTo (90m));
			}
		}

		// mictlanix/mbe#42: the snapshot was taken from the instance already holding the
		// new values -- a second Find in the same session returns that same object -- so
		// the audit trail recorded the correction as its own previous state. The scope
		// mirrors the per-request one Global.asax opens.
		[Test]
		public void EditPayment_RecordsThePaymentAsItWasBeforeTheCorrection ()
		{
			var controller = LogIn (AccessRight.Read | AccessRight.Update);
			int id;

			using (new SessionScope ()) {
				id = seed.Payment (PaymentType.Immediate, 100m).Id;
			}

			using (new SessionScope (FlushAction.Never)) {
				controller.EditPayment (Correction (id, 90m));
			}

			using (new SessionScope ()) {
				var incidence = Incidence.Queryable.Single (x => x.SourceType == SourceType.CustomerPayment && x.Reference == id);

				Assert.That (incidence.PreviousState, Does.Contain ("\"Amount\":100"));
				Assert.That (incidence.PreviousState, Does.Not.Contain ("\"Amount\":90"));
				Assert.That (CustomerPayment.Find (id).Amount, Is.EqualTo (90m));
			}
		}

		// The view hides the edit link once a payment has been changed; replaying the
		// POST must not get around that.
		[Test]
		public void EditPayment_RejectsASecondCorrection ()
		{
			var controller = LogIn (AccessRight.Read | AccessRight.Update);
			int id;

			using (new SessionScope ()) {
				var payment = seed.Payment (PaymentType.Immediate, 100m);
				payment.ModificationTime = payment.CreationTime.AddMinutes (1);
				payment.UpdateAndFlush ();
				id = payment.Id;
			}

			using (new SessionScope ()) {
				var result = controller.EditPayment (Correction (id, 90m));
				Assert.That (((PartialViewResult) result).ViewName, Is.EqualTo ("_EditPayment"));
				Assert.That (controller.ModelState.IsValid, Is.False);
			}

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.Find (id).Amount, Is.EqualTo (100m));
			}
		}

		static CustomerPayment Correction (int id, decimal amount)
		{
			return new CustomerPayment {
				Id = id,
				Amount = amount,
				Method = PaymentMethod.Cash,
				PaymentType = PaymentType.Immediate
			};
		}
	}
}
