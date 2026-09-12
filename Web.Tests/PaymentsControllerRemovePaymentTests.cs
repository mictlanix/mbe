//
// PaymentsControllerRemovePaymentTests.cs
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
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// PaymentsController.RemovePayment is the only way an applied payment comes off a
	// sales order -- there is no reversal, the row is hard-deleted. docs/specs/02-sales.md
	// used to describe an "Unapply" action that set sales_order_payment.cancelled; no such
	// action was ever written, and the column is gone as of mictlanix/mbe#55.
	//
	// These tests exercise the two rules the action actually enforces: when removal is
	// refused, and when removing the allocation also deletes the customer payment behind it.
	[TestFixture]
	public class PaymentsControllerRemovePaymentTests {
		Seed seed;
		PaymentsController controller;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}

			controller = new PaymentsController ();
			FakeHttpContext.Attach (controller, null);
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				seed.Dispose ();
			}

			FakeHttpContext.Detach ();
		}

		[Test]
		public void RemovePayment_OnPaidOrder_IsRefusedAndKeepsTheAllocation ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order (paid: true);
				var payment = seed.Payment (PaymentType.Immediate, 100m);
				id = seed.Allocation (order, payment, 100m).Id;
			}

			using (new SessionScope ()) {
				var result = controller.RemovePayment (id);

				Assert.That (result, Is.InstanceOf<ContentResult> ());
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (400));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (id), Is.Not.Null,
					     "a paid order must keep its allocation");
			}
		}

		[Test]
		public void RemovePayment_OnCancelledOrder_IsRefusedAndKeepsTheAllocation ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order (cancelled: true);
				var payment = seed.Payment (PaymentType.Immediate, 100m);
				id = seed.Allocation (order, payment, 100m).Id;
			}

			using (new SessionScope ()) {
				var result = controller.RemovePayment (id);

				Assert.That (result, Is.InstanceOf<ContentResult> ());
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (400));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (id), Is.Not.Null,
					     "a cancelled order must keep its allocation");
			}
		}

		[Test]
		public void RemovePayment_WhenTheAllocationIsConfirmed_IsRefusedAndKeepsIt ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				var payment = seed.Payment (PaymentType.Immediate, 100m);
				id = seed.Allocation (order, payment, 100m, confirmed: true).Id;
			}

			using (new SessionScope ()) {
				var result = controller.RemovePayment (id);

				Assert.That (result, Is.InstanceOf<ContentResult> ());
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (400));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (id), Is.Not.Null,
					     "a confirmed allocation must not be removable");
			}
		}

		// An Immediate payment exists only to settle the order it was taken for, so
		// removing the allocation takes the customer_payment with it.
		[Test]
		public void RemovePayment_WithAnImmediatePayment_AlsoDeletesTheCustomerPayment ()
		{
			int id, payment_id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				var payment = seed.Payment (PaymentType.Immediate, 100m);
				payment_id = payment.Id;
				id = seed.Allocation (order, payment, 100m).Id;
			}

			using (new SessionScope ()) {
				var result = controller.RemovePayment (id);

				Assert.That (result, Is.InstanceOf<JsonResult> ());
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (200));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (id), Is.Null,
					     "the allocation should be gone");
				Assert.That (CustomerPayment.TryFind (payment_id), Is.Null,
					     "an Immediate payment should be deleted with its allocation");
			}
		}

		// A payment in advance outlives the order it was applied to -- the money is still
		// the customer's, so only the allocation goes.
		[Test]
		public void RemovePayment_WithAPaymentInAdvance_KeepsTheCustomerPayment ()
		{
			int id, payment_id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				var payment = seed.Payment (PaymentType.PaymentInAdvance, 100m);
				payment_id = payment.Id;
				id = seed.Allocation (order, payment, 100m).Id;
			}

			using (new SessionScope ()) {
				var result = controller.RemovePayment (id);

				Assert.That (result, Is.InstanceOf<JsonResult> ());
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (id), Is.Null,
					     "the allocation should be gone");
				Assert.That (CustomerPayment.TryFind (payment_id), Is.Not.Null,
					     "a payment in advance must survive losing an allocation");
			}
		}
	}
}
