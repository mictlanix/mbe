//
// PaymentsControllerApplyInAdvanceTests.cs
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
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#47: ApplyInAdvancePayment checked the requested amount against
	// the customer's prepayment funds but never against the order's balance, so a
	// large prepayment could be applied past the order total. Its siblings
	// (ApplyCreditNotes, ApplyCreditNote, AccountsReceivables.ApplyPayment) clamp.
	//
	// The action draws on every open prepayment of the order's customer, so these
	// rely on Seed's customer having none of its own.
	[TestFixture]
	public class PaymentsControllerApplyInAdvanceTests {
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

		// Returns the order and prepayment ids.
		int [] OrderWithPrepayment (decimal total, decimal prepaid)
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, total);
				var payment = seed.Payment (PaymentType.PaymentInAdvance, prepaid);

				return new [] { order.Id, payment.Id };
			}
		}

		// The action creates the allocations, so Seed has to be told about them.
		void Apply (int order, decimal value)
		{
			using (new SessionScope ()) {
				controller.ApplyInAdvancePayment (order, value);
			}

			using (new SessionScope ()) {
				seed.Track (SalesOrderPayment.Queryable.Where (x => x.SalesOrder.Id == order).ToArray ());
			}
		}

		[Test]
		public void ApplyInAdvancePayment_ClampsToTheOrderBalance ()
		{
			var ids = OrderWithPrepayment (total: 100m, prepaid: 500m);

			Apply (ids [0], 300m);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (ids [0]);

				Assert.That (order.Payments.Sum (x => x.Amount), Is.EqualTo (100m), "only what the order is worth");
				Assert.That (order.Balance, Is.EqualTo (0m));
				Assert.That (CustomerPayment.Find (ids [1]).Balance, Is.EqualTo (400m), "the rest stays available");
			}
		}

		[Test]
		public void ApplyInAdvancePayment_AppliesLessThanTheBalanceAsAsked ()
		{
			var ids = OrderWithPrepayment (total: 100m, prepaid: 500m);

			Apply (ids [0], 40m);

			using (new SessionScope ()) {
				Assert.That (SalesOrder.Find (ids [0]).Balance, Is.EqualTo (60m));
				Assert.That (CustomerPayment.Find (ids [1]).Balance, Is.EqualTo (460m));
			}
		}

		[Test]
		public void ApplyInAdvancePayment_RefusesMoreThanTheFunds ()
		{
			var ids = OrderWithPrepayment (total: 1000m, prepaid: 50m);

			Apply (ids [0], 80m);

			using (new SessionScope ()) {
				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (400));
				Assert.That (SalesOrder.Find (ids [0]).Payments, Is.Empty);
			}
		}
	}
}
