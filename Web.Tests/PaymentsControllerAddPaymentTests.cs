//
// PaymentsControllerAddPaymentTests.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// The apply side of PaymentsController. AddPayment creates both the customer_payment
	// and the sales_order_payment that links it to the order, and decides three things
	// worth pinning down: whether the reference is acceptable, what happens when the
	// customer hands over more than the order is worth, and which PaymentType the
	// resulting payment gets.
	[TestFixture]
	public class PaymentsControllerAddPaymentTests {
		Seed seed;
		PaymentsController controller;
		SalesOrder order;

		[SetUp]
		public void SetUp ()
		{
			CashSession session;

			using (new SessionScope ()) {
				seed = new Seed ();
				session = seed.OpenCashSession ();
				order = seed.Order ();
				seed.Detail (order, 100m);
			}

			controller = new PaymentsController ();

			using (new SessionScope ()) {
				FakeHttpContext.Attach (controller,
						        FakeHttpContext.Principal (seed.Employee),
						        session.CashDrawer.Id);
			}
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				seed.Track (SalesOrderPayment.Queryable
					    .Where (x => x.SalesOrder.Id == order.Id).ToArray ());
				seed.Dispose ();
			}

			FakeHttpContext.Detach ();
		}

		// Added in 6d94ea4. The column is varchar(50); a longer reference used to reach
		// the database and fail there.
		[Test]
		public void AddPayment_WithAnOverlongReference_IsRefused ()
		{
			using (new SessionScope ()) {
				var result = controller.AddPayment (order.Id, (int) PaymentMethod.Cash, 50m,
								    new string ('x', 51), null, false);

				Assert.That (result, Is.InstanceOf<ContentResult> ());
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (400));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrder.Find (order.Id).Payments, Is.Empty,
					     "a rejected reference must not leave a payment behind");
			}
		}

		[Test]
		public void AddPayment_ForPartOfTheBalance_LeavesTheRestOutstanding ()
		{
			using (new SessionScope ()) {
				var result = controller.AddPayment (order.Id, (int) PaymentMethod.Cash, 40m,
								    null, null, false);

				Assert.That (result, Is.InstanceOf<JsonResult> ());
			}

			using (new SessionScope ()) {
				var saved = SalesOrder.Find (order.Id);

				Assert.That (saved.Paid, Is.EqualTo (40m));
				Assert.That (saved.Balance, Is.EqualTo (60m));
				Assert.That (saved.Change, Is.EqualTo (0m));
			}
		}

		// Cash over the balance is given back as change rather than banked, so the
		// order is settled exactly and the surplus is recorded separately.
		[Test]
		public void AddPayment_WithTooMuchCash_RecordsTheSurplusAsChange ()
		{
			using (new SessionScope ()) {
				controller.AddPayment (order.Id, (int) PaymentMethod.Cash, 150m, null, null, false);
			}

			using (new SessionScope ()) {
				var saved = SalesOrder.Find (order.Id);

				Assert.That (saved.Paid, Is.EqualTo (100m), "the order takes only what it is owed");
				Assert.That (saved.Change, Is.EqualTo (50m), "the surplus is change");
				Assert.That (saved.Balance, Is.EqualTo (0m));
			}
		}

		// A card cannot give change, so the charge itself is reduced to the balance.
		[Test]
		public void AddPayment_WithTooMuchOnACard_CapsTheChargeInstead ()
		{
			using (new SessionScope ()) {
				controller.AddPayment (order.Id, (int) PaymentMethod.CreditCard, 150m, null, null, false);
			}

			using (new SessionScope ()) {
				var saved = SalesOrder.Find (order.Id);
				var allocation = saved.Payments.Single ();

				Assert.That (saved.Paid, Is.EqualTo (100m));
				Assert.That (saved.Change, Is.EqualTo (0m), "a card payment gives no change");
				Assert.That (allocation.Payment.Amount, Is.EqualTo (100m),
					     "the charge itself is capped, not refunded");
			}
		}

		// PaymentType is derived from the order's terms, not passed in.
		[Test]
		public void AddPayment_OnAnImmediateOrder_CreatesAnImmediatePayment ()
		{
			using (new SessionScope ()) {
				controller.AddPayment (order.Id, (int) PaymentMethod.Cash, 100m, null, null, false);
			}

			using (new SessionScope ()) {
				var allocation = SalesOrder.Find (order.Id).Payments.Single ();

				Assert.That (allocation.Payment.PaymentType, Is.EqualTo (PaymentType.Immediate));
				Assert.That (allocation.Payment.CashSession, Is.Not.Null,
					     "a payment taken at the till belongs to the open session");
			}
		}
	}
}
