//
// PaymentCorrectionTests.cs
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
using System.Threading;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Services;
using Mictlanix.BE.Web.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// The shared check and audit record behind every payment correction
	// (mictlanix/mbe#66, #67). Each test pays one order of 100 with one allocation,
	// then asks what taking money off it would do.
	[TestFixture]
	public class PaymentCorrectionTests {
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

		SalesOrderDetail PaidOrder (decimal allocated = 100m)
		{
			var order = seed.Order (paid: true);
			var line = seed.Detail (order, 100m);
			var payment = seed.Payment (PaymentType.Immediate, allocated);

			seed.Allocation (order, payment, allocated);

			return line;
		}

		static PaymentCorrection Check (int order, decimal change)
		{
			return PaymentCorrection.Check (new [] {
				new KeyValuePair<SalesOrder, decimal> (SalesOrder.Find (order), change)
			});
		}

		[Test]
		public void Check_WorksOutTheBalanceAfterWithoutWriting ()
		{
			int id;

			using (new SessionScope ()) {
				id = PaidOrder ().SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var change = Check (id, -40m).Orders.Single ();

				Assert.That (change.BalanceBefore, Is.EqualTo (0m));
				Assert.That (change.BalanceAfter, Is.EqualTo (40m));
				Assert.That (change.Unpays, Is.True);
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrder.Find (id).Balance, Is.EqualTo (0m));
			}
		}

		[Test]
		public void Check_BlocksUnpayingAnOrderWithAnActivePueInvoice ()
		{
			int id;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				seed.Invoice (line, PaymentTerms.Immediate);
				id = line.SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -100m);

				Assert.That (check.IsBlocked, Is.True);
				Assert.That (check.Blocks.Single (), Does.Contain ("TEST000001"));
			}
		}

		[Test]
		public void Check_IgnoresACancelledPueInvoice ()
		{
			int id;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				seed.Invoice (line, PaymentTerms.Immediate, cancelled: true);
				id = line.SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -100m);

				Assert.That (check.IsBlocked, Is.False);
				Assert.That (check.Invoiced, Is.Null);
			}
		}

		[Test]
		public void Check_BlocksUnpayingAnOrderWhosePpdInvoiceHasAnActiveReceipt ()
		{
			int id;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				var invoice = seed.Invoice (line, PaymentTerms.NetD);
				seed.PaymentReceipt (invoice, cancelled: true);
				seed.PaymentReceipt (invoice);
				id = line.SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -100m);

				Assert.That (check.IsBlocked, Is.True);
				Assert.That (check.Blocks.Single (), Does.Contain ("TEST000001").And.Contain ("TEST000003"));
				Assert.That (check.Blocks.Single (), Does.Not.Contain ("TEST000002"));
			}
		}

		[Test]
		public void Check_LetsAPpdInvoiceWithEveryReceiptCancelledThroughAndRecordsIt ()
		{
			int id;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				var invoice = seed.Invoice (line, PaymentTerms.NetD);
				seed.PaymentReceipt (invoice, cancelled: true);
				id = line.SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -100m);

				Assert.That (check.IsBlocked, Is.False);
				Assert.That (check.Invoiced, Is.EqualTo (id + ": TEST000001"));
			}
		}

		// A correction that leaves the order unpaid either way -- installment 1 of 3 --
		// proceeds, and the receipt that no longer matches is recorded for billing.
		[Test]
		public void Check_LetsAPartlyPaidPpdOrderThroughAndRecordsItsReceipts ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order (paid: false);
				var line = seed.Detail (order, 100m);
				seed.Allocation (order, seed.Payment (PaymentType.CreditPayment, 30m), 30m);
				seed.PaymentReceipt (seed.Invoice (line, PaymentTerms.NetD));
				id = order.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -30m);

				Assert.That (check.IsBlocked, Is.False);
				Assert.That (check.Unpaid, Is.Empty);
				Assert.That (check.Invoiced, Is.EqualTo (id + ": TEST000001 (TEST000002)"));
			}
		}

		[Test]
		public void Check_LetsAnOrderThatStaysPaidThroughAndRecordsItsInvoice ()
		{
			int id;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				var order = line.SalesOrder;
				seed.Allocation (order, seed.Payment (PaymentType.Immediate, 20m), 20m);
				seed.Invoice (line, PaymentTerms.Immediate);
				id = order.Id;
			}

			using (new SessionScope ()) {
				var check = Check (id, -20m);

				Assert.That (check.IsBlocked, Is.False);
				Assert.That (check.Unpaid, Is.Empty);
				Assert.That (check.Invoiced, Is.EqualTo (id + ": TEST000001"));
				Assert.That (check.Delivered, Is.Null);
				Assert.That (check.Commission, Is.Null);
			}
		}

		[Test]
		public void Check_RecordsDeliveriesAndCommissionOfAnOrderItUnpays ()
		{
			int id, delivery;

			using (new SessionScope ()) {
				var line = PaidOrder ();
				delivery = seed.Delivery (line).Id;
				seed.Delivery (line, completed: false);
				id = line.SalesOrder.Id;
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);
				var check = Check (id, -100m);

				Assert.That (check.IsBlocked, Is.False);
				Assert.That (check.Delivered, Is.EqualTo (id + ": " + delivery));
				Assert.That (check.Commission, Is.EqualTo (string.Format ("{0}: {1:yyyy-MM-dd HH:mm} {2}", id,
											  order.ModificationTime, order.SalesPerson.Name)));
			}
		}

		[Test]
		public void Record_IsAFlatObjectWithoutTimestamps ()
		{
			int id, payment, session;

			using (new SessionScope ()) {
				var cash = seed.ClosedCashSession ();
				var order = seed.Order (paid: true);
				seed.Detail (order, 100m);
				var item = seed.Payment (PaymentType.Immediate, 150m, PaymentMethod.Cash, cash);
				seed.Allocation (order, item, 100m, change: 50m);
				order.BalanceZeroedTime = new DateTime (2026, 9, 1, 10, 0, 0);
				order.UpdateAndFlush ();
				id = order.Id;
				payment = item.Id;
				session = cash.Id;
			}

			using (new SessionScope ()) {
				var item = CustomerPayment.Find (payment);
				var record = new CorrectionRecord ("Reverse", item, Check (id, -100m))
					.After (PaymentMethod.Cash, 100m)
					.Set ("AllocatedAmount", 100m)
					.Set ("Change", 50m)
					.Set ("CreditNote", null);
				var json = JObject.Parse (record.ToJson ());

				Assert.That (json.Properties ().Select (x => x.Name), Is.EqualTo (new [] {
					"Operation", "CashSession", "MethodBefore", "NetBefore", "SalesOrders", "Unpaid",
					"PaidAt", "Commission", "MethodAfter", "NetAfter", "AllocatedAmount", "Change"
				}));
				Assert.That (json.Properties ().All (x => x.Value.Type != JTokenType.Object && x.Value.Type != JTokenType.Array));
				Assert.That ((int) json ["CashSession"], Is.EqualTo (session));
				Assert.That ((decimal) json ["NetBefore"], Is.EqualTo (100m));
				Assert.That ((string) json ["SalesOrders"], Is.EqualTo (id.ToString ()));
				Assert.That ((string) json ["Unpaid"], Is.EqualTo (id.ToString ()));
				Assert.That ((string) json ["PaidAt"], Is.EqualTo (id + ": 2026-09-01 10:00"));

				var incidence = record.ToIncidence (seed.Employee, "Wrong order");

				Assert.That (incidence.SourceType, Is.EqualTo (SourceType.CustomerPayment));
				Assert.That (incidence.Reference, Is.EqualTo (payment));
				Assert.That (incidence.Comment, Is.EqualTo ("Wrong order"));
				Assert.That (incidence.PreviousState, Is.EqualTo (record.ToJson ()));
			}
		}

		[Test]
		public void Record_ShowsTheMethodsByName ()
		{
			var content = IncidenceContent.Parse (new Incidence {
				SourceType = SourceType.CustomerPayment,
				PreviousState = "{\"MethodBefore\":1,\"MethodAfter\":4}"
			});

			Assert.That (content.Fields, Is.EqualTo (new [] {
				new KeyValuePair<string, string> (Resources.MethodBefore, PaymentMethod.Cash.GetDisplayName ()),
				new KeyValuePair<string, string> (Resources.MethodAfter, PaymentMethod.CreditCard.GetDisplayName ())
			}));
			Assert.That (content.IsPostChangeSnapshot, Is.False);
		}

		// The ticket prints the totals as signed above the corrected ones: same order,
		// so they line up; a method no longer received comes last.
		[Test]
		public void SignedTotals_FollowTheOrderOfTodaysTotals ()
		{
			var report = new CashCountReport {
				Payments = new List<CustomerPayment> {
					new CustomerPayment { Method = PaymentMethod.CreditCard, Amount = 100m },
					new CustomerPayment { Method = PaymentMethod.Cash, Amount = 50m }
				},
				Amendments = new List<CashCountAmendment> {
					new CashCountAmendment { MethodBefore = PaymentMethod.Cash, NetBefore = 20m, MethodAfter = PaymentMethod.Cash, NetAfter = 0m },
					new CashCountAmendment { MethodBefore = PaymentMethod.EFT, NetBefore = 10m, MethodAfter = PaymentMethod.EFT, NetAfter = 0m }
				}
			};

			Assert.That (report.PaymentsReceivedByMethod.Select (x => x.Method),
				     Is.EqualTo (new [] { PaymentMethod.CreditCard, PaymentMethod.Cash }));
			Assert.That (report.SignedByMethod.Select (x => x.Method + "=" + x.Amount),
				     Is.EqualTo (new [] { "CreditCard=100", "Cash=70", "EFT=10" }));
		}

		// The first transaction locks the payment, then changes it. The second asks
		// for the same lock in the meantime: it must wait for the first to commit and
		// then see its change -- without the lock it would read the old amount at once.
		[Test]
		public void Lock_HoldsTheRowUntilTheTransactionEnds ()
		{
			int id;

			using (new SessionScope ()) {
				id = seed.Payment (PaymentType.Immediate, 100m).Id;
			}

			var locked = new ManualResetEventSlim ();
			var committing = DateTime.MinValue;
			var acquired = DateTime.MinValue;
			var seen = 0m;
			Exception error = null;

			var first = new Thread (() => {
				try {
					using (new SessionScope ())
					using (new TransactionScope ()) {
						var item = PaymentCorrection.Lock<CustomerPayment> (id);
						locked.Set ();
						Thread.Sleep (1000);
						item.Amount = 90m;
						item.Update ();
						committing = DateTime.Now;
					}
				} catch (Exception e) {
					error = e;
					locked.Set ();
				}
			});
			var second = new Thread (() => {
				try {
					locked.Wait ();
					using (new SessionScope ())
					using (new TransactionScope ()) {
						var item = PaymentCorrection.Lock<CustomerPayment> (id);
						acquired = DateTime.Now;
						seen = item.Amount;
					}
				} catch (Exception e) {
					error = e;
				}
			});

			first.Start ();
			second.Start ();

			Assert.That (first.Join (TimeSpan.FromSeconds (30)) && second.Join (TimeSpan.FromSeconds (30)), Is.True);
			Assert.That (error, Is.Null);
			Assert.That (seen, Is.EqualTo (90m));
			Assert.That (acquired, Is.GreaterThanOrEqualTo (committing));
		}
	}
}
