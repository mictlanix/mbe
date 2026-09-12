//
// CustomerPaymentAllocationTests.cs
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
using Mictlanix.BE.Model;
using NUnit.Framework;

namespace Mictlanix.BE.Model.Tests {
	// Covers the allocation arithmetic shared by CustomerPayment and SalesOrder.
	// See mictlanix/mbe#55: sales_order_payment.cancelled used to be filtered by
	// CustomerPayment.Allocated and by nothing else, so a flagged row was invisible
	// to the payment but still counted against the order. The column is gone; these
	// tests pin down that every reader now counts the same rows.
	[TestFixture]
	public class CustomerPaymentAllocationTests {
		static SalesOrderPayment Allocation (decimal amount, decimal change = 0m)
		{
			return new SalesOrderPayment { Amount = amount, Change = change };
		}

		[Test]
		public void Allocated_WithNoAllocations_IsZero ()
		{
			var payment = new CustomerPayment { Amount = 100m };

			Assert.That (payment.Allocated, Is.EqualTo (0m));
		}

		[Test]
		public void Allocated_SumsAmountAndChangeOfEveryAllocation ()
		{
			var payment = new CustomerPayment {
				Amount = 500m,
				Allocations = new List<SalesOrderPayment> {
					Allocation (100m),
					Allocation (150m, 20m),
					Allocation (50m, 5m)
				}
			};

			Assert.That (payment.Allocated, Is.EqualTo (325m));
		}

		[Test]
		public void Balance_IsAmountLessAllocated ()
		{
			var payment = new CustomerPayment {
				Amount = 500m,
				Allocations = new List<SalesOrderPayment> {
					Allocation (200m),
					Allocation (100m, 25m)
				}
			};

			Assert.That (payment.Allocated, Is.EqualTo (325m));
			Assert.That (payment.Balance, Is.EqualTo (175m));
		}

		[Test]
		public void Balance_WhenFullyAllocated_IsZero ()
		{
			var payment = new CustomerPayment {
				Amount = 300m,
				Allocations = new List<SalesOrderPayment> {
					Allocation (300m)
				}
			};

			Assert.That (payment.Balance, Is.EqualTo (0m));
		}

		[Test]
		public void Paid_SumsAllocationAmountsOnly ()
		{
			var order = new SalesOrder {
				Payments = new List<SalesOrderPayment> {
					Allocation (100m, 10m),
					Allocation (200m, 30m)
				}
			};

			Assert.That (order.Paid, Is.EqualTo (300m));
			Assert.That (order.Change, Is.EqualTo (40m));
		}

		// The invariant mbe#55 is about. Allocated deliberately includes Change and
		// Paid deliberately does not, but both must span the *same* rows. If a filter
		// is ever reintroduced on one side only, this is what breaks -- that asymmetry
		// is what would let the same money be spent twice.
		[Test]
		public void AllocatedAndPaid_SpanTheSameAllocationRows ()
		{
			var rows = new List<SalesOrderPayment> {
				Allocation (100m),
				Allocation (150m, 20m),
				Allocation (50m, 5m)
			};

			var payment = new CustomerPayment { Amount = 500m, Allocations = rows };
			var order = new SalesOrder { Payments = rows };

			Assert.That (payment.Allocated, Is.EqualTo (order.Paid + order.Change));
		}

		// mbe#55 removed SalesOrderPayment.IsCancelled along with the column it mapped.
		// Reintroducing it is a decision that has to be made deliberately, because every
		// one of the ~60 sites that sum allocations would have to honour it at once.
		[Test]
		public void SalesOrderPayment_DoesNotExposeACancelledFlag ()
		{
			var members = typeof (SalesOrderPayment).GetProperties ()
				.Select (x => x.Name).ToList ();

			Assert.That (members, Does.Not.Contain ("IsCancelled"));
		}
	}
}
