//
// PaymentsControllerApplyCreditNoteTests.cs
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#77: ApplyCreditNotes and ApplyCreditNote allocated credit notes
	// to an order but never marked it paid, even when the credit covered the whole
	// balance. Like AccountsReceivables.ApplyPayment, they now set paid and
	// balance_zeroed_time when the applied amount zeroes the balance.
	//
	// ApplyCreditNotes draws on every open credit note of the order's customer, and
	// Seed's customer has real ones, so these tests use a customer of their own.
	[TestFixture]
	public class PaymentsControllerApplyCreditNoteTests {
		Seed seed;
		PaymentsController controller;
		Customer customer;
		readonly List<CreditNote> notes = new List<CreditNote> ();
		readonly List<CustomerRefund> refunds = new List<CustomerRefund> ();

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();

				var price_list = PriceList.Queryable.First ();
				customer = new Customer {
					Code = Seed.Marker + "-CN",
					Name = "Credit Note Customer " + Seed.Marker,
					CreditLimit = 0m,
					CreditDays = 0,
					Comment = Seed.Marker,
					PriceList = price_list,
					PriceListId = price_list.Id
				};

				customer.CreateAndFlush ();
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
				// Credit notes and refunds point at Seed's orders and payments.
				foreach (var item in notes) {
					Remove (CreditNote.TryFind (item.Id));
				}

				foreach (var item in refunds) {
					Remove (CustomerRefund.TryFind (item.Id));
				}

				seed.Dispose ();
				Remove (Customer.TryFind (customer.Id));
			}

			notes.Clear ();
			refunds.Clear ();
			FakeHttpContext.Detach ();
		}

		SalesOrder Order (decimal total)
		{
			var order = seed.Order ();

			order.Customer = customer;
			order.CustomerName = customer.Name;
			order.UpdateAndFlush ();
			seed.Detail (order, total);

			return order;
		}

		// A credit note is left over from a refund on an earlier order.
		CreditNote NewCreditNote (decimal amount)
		{
			var now = DateTime.Now;
			var origin = Order (amount);
			var refund = new CustomerRefund {
				Store = seed.Store,
				SalesOrder = origin,
				SalesPerson = seed.Employee,
				Customer = customer,
				Date = now,
				Currency = CurrencyCode.MXN,
				ExchangeRate = 1m,
				IsCompleted = true,
				Creator = seed.Employee,
				Updater = seed.Employee,
				CreationTime = now,
				ModificationTime = now
			};

			refund.CreateAndFlush ();
			refunds.Add (refund);

			var payment = seed.Payment (PaymentType.CreditNote, amount);

			payment.Customer = customer;
			payment.UpdateAndFlush ();

			var note = new CreditNote {
				SalesOrder = origin,
				CustomerRefund = refund,
				CustomerPayment = payment,
				Customer = customer,
				Refunded = 0m,
				Date = now
			};

			note.CreateAndFlush ();
			notes.Add (note);

			return note;
		}

		// Returns the order and credit note payment ids.
		int [] OrderWithCreditNote (decimal total, decimal credit)
		{
			using (new SessionScope ()) {
				var order = Order (total);
				var note = NewCreditNote (credit);

				return new [] { order.Id, note.CustomerPayment.Id };
			}
		}

		// The actions create the allocations, so Seed has to be told about them.
		void Track (int order)
		{
			using (new SessionScope ()) {
				seed.Track (SalesOrderPayment.Queryable.Where (x => x.SalesOrder.Id == order).ToArray ());
			}
		}

		void ApplyAll (int order, decimal value)
		{
			using (new SessionScope ()) {
				controller.ApplyCreditNotes (order, value);
			}

			Track (order);
		}

		void ApplyOne (int order, int credit_note, decimal value)
		{
			using (new SessionScope ()) {
				controller.ApplyCreditNote (order, credit_note, value);
			}

			Track (order);
		}

		[Test]
		public void ApplyCreditNotes_CoveringTheBalance_MarksTheOrderPaid ()
		{
			var ids = OrderWithCreditNote (total: 100m, credit: 150m);

			ApplyAll (ids [0], 100m);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (ids [0]);

				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (200));
				Assert.That (order.Balance, Is.EqualTo (0m));
				Assert.That (order.IsPaid, Is.True);
				Assert.That (order.BalanceZeroedTime, Is.Not.Null);
			}
		}

		[Test]
		public void ApplyCreditNotes_LessThanTheBalance_LeavesTheOrderUnpaid ()
		{
			var ids = OrderWithCreditNote (total: 100m, credit: 150m);

			ApplyAll (ids [0], 40m);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (ids [0]);

				Assert.That (order.Balance, Is.EqualTo (60m));
				Assert.That (order.IsPaid, Is.False);
				Assert.That (order.BalanceZeroedTime, Is.Null);
			}
		}

		[Test]
		public void ApplyCreditNote_CoveringTheBalance_MarksTheOrderPaid ()
		{
			var ids = OrderWithCreditNote (total: 100m, credit: 150m);

			ApplyOne (ids [0], ids [1], 300m);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (ids [0]);

				Assert.That (FakeHttpContext.StatusCode (controller), Is.EqualTo (200));
				Assert.That (order.Balance, Is.EqualTo (0m));
				Assert.That (order.IsPaid, Is.True);
				Assert.That (order.BalanceZeroedTime, Is.Not.Null);
			}
		}

		[Test]
		public void ApplyCreditNote_LessThanTheBalance_LeavesTheOrderUnpaid ()
		{
			var ids = OrderWithCreditNote (total: 100m, credit: 150m);

			ApplyOne (ids [0], ids [1], 40m);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (ids [0]);

				Assert.That (order.Balance, Is.EqualTo (60m));
				Assert.That (order.IsPaid, Is.False);
				Assert.That (order.BalanceZeroedTime, Is.Null);
			}
		}

		static void Remove (ActiveRecordBase item)
		{
			if (item == null) {
				return;
			}

			try {
				item.DeleteAndFlush ();
			} catch (Exception e) {
				Console.WriteLine ("cleanup failed: " + e.Message);
			}
		}
	}
}
