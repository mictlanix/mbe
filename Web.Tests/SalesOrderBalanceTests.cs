//
// SalesOrderBalanceTests.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// SalesOrder.Balance is the reader mictlanix/mbe#55 warns about. It queries
	// CreditNote and CustomerRefund, so it cannot be exercised without a database --
	// which is why it went uncovered when the rest of the allocation arithmetic was
	// pinned down in Model.Tests.
	//
	// The hazard the issue describes is an asymmetry: CustomerPayment.Allocated once
	// filtered out cancelled allocations and roughly sixty other readers, Balance among
	// them, did not. A row hidden from one side and counted by the other lets the same
	// money settle two different orders. These tests hold the two sides together.
	[TestFixture]
	public class SalesOrderBalanceTests {
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
		}

		[Test]
		public void Balance_WithNoPayments_IsTheWholeTotal ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				id = order.Id;
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.Total, Is.EqualTo (100m));
				Assert.That (order.Paid, Is.EqualTo (0m));
				Assert.That (order.Balance, Is.EqualTo (order.Total));
			}
		}

		[Test]
		public void Balance_FallsByTheAmountApplied ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				var payment = seed.Payment (PaymentType.PaymentInAdvance, 40m);
				seed.Allocation (order, payment, 40m);
				id = order.Id;
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.Paid, Is.EqualTo (40m));
				Assert.That (order.Balance, Is.EqualTo (60m));
			}
		}

		[Test]
		public void Balance_WhenTheTotalIsCovered_IsZero ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				var payment = seed.Payment (PaymentType.PaymentInAdvance, 100m);
				seed.Allocation (order, payment, 100m);
				id = order.Id;
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.Balance, Is.EqualTo (0m));
			}
		}

		// The invariant that matters. Both sides are reading the same
		// sales_order_payment row, through different entities and different queries.
		// If a filter is ever reintroduced on one reader and not the other, the two
		// figures drift apart and this fails.
		// sales_order_payment carries a unique index on (sales_order, customer_payment),
		// so one payment settles a given order at most once -- two payments are needed to
		// put two allocations on one order.
		[Test]
		public void OrderAndPayments_AgreeOnWhatHasBeenApplied ()
		{
			int order_id, first_id, second_id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 250m);

				var first = seed.Payment (PaymentType.PaymentInAdvance, 100m);
				var second = seed.Payment (PaymentType.PaymentInAdvance, 100m);

				seed.Allocation (order, first, 90m);
				seed.Allocation (order, second, 60m);

				order_id = order.Id;
				first_id = first.Id;
				second_id = second.Id;
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (order_id);
				var first = CustomerPayment.Find (first_id);
				var second = CustomerPayment.Find (second_id);

				Assert.That (order.Paid, Is.EqualTo (150m));
				Assert.That (first.Allocated, Is.EqualTo (90m));
				Assert.That (second.Allocated, Is.EqualTo (60m));

				Assert.That (first.Allocated + second.Allocated,
					     Is.EqualTo (order.Paid + order.Change),
					     "the payments and the order must count the same allocations");
				Assert.That (order.Balance, Is.EqualTo (order.Total - order.Paid));

				// What is left on each payment is still the customer's to spend.
				Assert.That (first.Balance, Is.EqualTo (10m));
				Assert.That (second.Balance, Is.EqualTo (40m));
			}
		}
	}
}
