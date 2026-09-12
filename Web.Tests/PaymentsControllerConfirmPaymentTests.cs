//
// PaymentsControllerConfirmPaymentTests.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// ConfirmPayment is what actually marks a sales order paid. It confirms every
	// outstanding allocation and then asks BalanceInCashDrawer whether anything is
	// still owed -- and BalanceInCashDrawer counts only confirmed allocations, while
	// SalesOrder.Paid counts all of them. That difference is deliberate here, but it is
	// the same shape of split that mictlanix/mbe#55 is about, so it is worth pinning.
	[TestFixture]
	public class PaymentsControllerConfirmPaymentTests {
		Seed seed;
		PaymentsController controller;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}

			controller = new PaymentsController ();

			using (new SessionScope ()) {
				FakeHttpContext.Attach (controller, FakeHttpContext.Principal (seed.Employee));
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

		int OrderAwaiting (decimal total, decimal applied)
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, total);
				var payment = seed.Payment (PaymentType.Immediate, applied);
				seed.Allocation (order, payment, applied);

				return order.Id;
			}
		}

		[Test]
		public void ConfirmPayment_ConfirmsTheOutstandingAllocations ()
		{
			var id = OrderAwaiting (100m, 40m);

			using (new SessionScope ()) {
				Assert.That (SalesOrder.Find (id).PaymentsToConfirm (), Is.Not.Empty);
				controller.ConfirmPayment (id);
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);
				var allocation = order.Payments.Single ();

				Assert.That (allocation.IsConfirmed, Is.True);
				Assert.That (allocation.Applier, Is.Not.Null, "the confirming employee is recorded");
				Assert.That (order.PaymentsToConfirm (), Is.Empty);
			}
		}

		[Test]
		public void ConfirmPayment_WhenTheOrderIsCovered_MarksItPaid ()
		{
			var id = OrderAwaiting (100m, 100m);

			using (new SessionScope ()) {
				controller.ConfirmPayment (id);
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.IsPaid, Is.True);
				Assert.That (order.BalanceInCashDrawer (), Is.EqualTo (0m));
				Assert.That (order.DeliveryMode, Is.EqualTo (DeliveryMode.PartialDeliveries));
			}
		}

		[Test]
		public void ConfirmPayment_WhenTheOrderIsOnlyPartlyCovered_LeavesItUnpaid ()
		{
			var id = OrderAwaiting (100m, 40m);

			using (new SessionScope ()) {
				controller.ConfirmPayment (id);
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.IsPaid, Is.False);
				Assert.That (order.BalanceInCashDrawer (), Is.EqualTo (60m));
			}
		}

		// The till is allowed to be up to ten centavos short before the order is held open.
		[Test]
		public void ConfirmPayment_ToleratesAShortfallOfTenCentavos ()
		{
			var id = OrderAwaiting (100m, 99.95m);

			using (new SessionScope ()) {
				controller.ConfirmPayment (id);
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.BalanceInCashDrawer (), Is.EqualTo (0.05m));
				Assert.That (order.IsPaid, Is.True, "a shortfall within tolerance still settles the order");
			}
		}

		// Just outside the tolerance, so the order stays open.
		[Test]
		public void ConfirmPayment_DoesNotTolerateMoreThanThat ()
		{
			var id = OrderAwaiting (100m, 99.5m);

			using (new SessionScope ()) {
				controller.ConfirmPayment (id);
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.BalanceInCashDrawer (), Is.EqualTo (0.5m));
				Assert.That (order.IsPaid, Is.False);
			}
		}
	}
}
