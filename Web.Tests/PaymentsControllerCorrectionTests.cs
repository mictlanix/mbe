//
// PaymentsControllerCorrectionTests.cs
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
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Security;
using Mictlanix.BE.Web.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using NHibernate;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// Correcting customer payments (mictlanix/mbe#66 §9). Numbers in the comments
	// are the design's test numbers. Each controller call runs in a scope like the
	// per-request one Global.asax opens.
	[TestFixture]
	public class PaymentsControllerCorrectionTests {
		const string Reason = "Cashier mistake";

		Seed seed;
		CustomPrincipal principal;

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

		void LogIn (AccessRight rights, bool administrator = false)
		{
			using (new SessionScope ()) {
				var user = seed.User (SystemObjects.PaymentsEditor, rights, administrator);
				principal = new CustomPrincipal (user.UserName, user.Email, administrator,
								 seed.Employee, new List<AccessPrivilege> ());
			}
		}

		PaymentsController Controller ()
		{
			var controller = new PaymentsController ();
			FakeHttpContext.Attach (controller, principal);
			return controller;
		}

		static T Request<T> (Func<T> action)
		{
			using (new SessionScope (FlushAction.Never)) {
				return action ();
			}
		}

		static bool Refused (PaymentsController controller, ActionResult result)
		{
			return result is ContentResult && FakeHttpContext.StatusCode (controller) == 400;
		}

		static IList<JObject> Records (int payment)
		{
			using (new SessionScope ()) {
				return Incidence.Queryable
					.Where (x => x.SourceType == SourceType.CustomerPayment && x.Reference == payment)
					.ToList ()
					.Select (x => JObject.Parse (x.PreviousState))
					.ToList ();
			}
		}

		// An order of `total`, marked paid, with one allocation of a payment of
		// `amount` (cash beyond the total is change).
		SalesOrderPayment Paid (decimal total, decimal amount, CashSession session = null)
		{
			using (new SessionScope ()) {
				var order = seed.Order (paid: true);
				seed.Detail (order, total);
				order.BalanceZeroedTime = DateTime.Now;
				order.UpdateAndFlush ();

				var payment = seed.Payment (PaymentType.Immediate, amount, PaymentMethod.Cash, session);
				return seed.Allocation (order, payment, total, change: amount - total);
			}
		}

		// 1
		[Test]
		public void ReverseAllocation_TakesItOffTheOrderAndKeepsThePayment ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);
			var controller = Controller ();

			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<JsonResult> ());

			using (new SessionScope ()) {
				var order = SalesOrder.Find (allocation.SalesOrder.Id);
				var payment = CustomerPayment.Find (allocation.Payment.Id);

				Assert.That (SalesOrderPayment.TryFind (allocation.Id), Is.Null);
				Assert.That (payment.Amount, Is.EqualTo (100m));
				Assert.That (payment.Balance, Is.EqualTo (100m));
				Assert.That (order.IsPaid, Is.False);
				Assert.That (order.BalanceZeroedTime, Is.Null);
				Assert.That (order.Balance, Is.EqualTo (100m));
			}

			var record = Records (allocation.Payment.Id).Single ();

			Assert.That ((string) record ["Operation"], Is.EqualTo ("Reverse"));
			Assert.That ((int) record ["Allocation"], Is.EqualTo (allocation.Id));
			Assert.That ((decimal) record ["AllocatedAmount"], Is.EqualTo (100m));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo ((decimal) record ["NetBefore"]));
			Assert.That ((string) record ["Unpaid"], Is.EqualTo (allocation.SalesOrder.Id.ToString ()));
			Assert.That (record ["Change"], Is.Null);
		}

		// 2
		[Test]
		public void ReverseAllocation_TakesTheChangeOffThePaymentSoItsNetStays ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 150m);
			var controller = Controller ();

			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<JsonResult> ());

			using (new SessionScope ()) {
				var payment = CustomerPayment.Find (allocation.Payment.Id);

				Assert.That (payment.Amount, Is.EqualTo (100m));
				Assert.That (payment.Allocations, Is.Empty);
			}

			var record = Records (allocation.Payment.Id).Single ();

			Assert.That ((decimal) record ["Change"], Is.EqualTo (50m));
			Assert.That ((decimal) record ["NetBefore"], Is.EqualTo (100m));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo (100m));
		}

		// 3
		[Test]
		public void ReverseAllocation_LeavesTheOtherShareOfASplitPaymentAlone ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			SalesOrderPayment first, second;
			DateTime modified;

			using (new SessionScope ()) {
				var payment = seed.Payment (PaymentType.Immediate, 150m);
				var a = seed.Order (paid: true);
				var b = seed.Order (paid: true);
				seed.Detail (a, 100m);
				seed.Detail (b, 50m);
				first = seed.Allocation (a, payment, 100m);
				second = seed.Allocation (b, payment, 50m);
				modified = b.ModificationTime;
			}

			var controller = Controller ();

			Assert.That (Request (() => controller.ReverseAllocation (first.Id, Reason)), Is.InstanceOf<JsonResult> ());

			using (new SessionScope ()) {
				var b = SalesOrder.Find (second.SalesOrder.Id);

				Assert.That (SalesOrderPayment.TryFind (second.Id), Is.Not.Null);
				Assert.That (b.IsPaid, Is.True);
				Assert.That (b.ModificationTime, Is.EqualTo (modified).Within (TimeSpan.FromSeconds (1)));
				Assert.That (CustomerPayment.Find (first.Payment.Id).Balance, Is.EqualTo (100m));
			}
		}

		// 4
		[Test]
		public void ReversedPayment_IsOfferedAgainAndReappliesThroughAccountsReceivable ()
		{
			var allocation = Reversed ();
			var receivables = new AccountsReceivablesController ();
			FakeHttpContext.Attach (receivables, principal);

			using (new SessionScope ()) {
				var order = SalesOrder.Find (allocation.SalesOrder.Id);
				var view = (PartialViewResult) receivables.ApplyPayment (order.Id);
				var offered = (IEnumerable<CustomerPayment>) view.ViewData ["Payments"];

				Assert.That (offered.Select (x => x.Id), Does.Contain (allocation.Payment.Id));
				Assert.That (order.Customer.PrepaymentsBalance (order), Is.EqualTo (100m));
			}

			Request (() => receivables.ApplyPayment (new SalesOrderPayment {
				SalesOrder = new SalesOrder { Id = allocation.SalesOrder.Id },
				PaymentId = allocation.Payment.Id,
				Amount = 100m
			}));

			AssertReapplied (allocation);
		}

		// 4
		[Test]
		public void ReversedPayment_ReappliesThroughThePointOfSale ()
		{
			var allocation = Reversed ();
			var controller = Controller ();

			Assert.That (Request (() => controller.ApplyInAdvancePayment (allocation.SalesOrder.Id, 100m)), Is.InstanceOf<JsonResult> ());

			AssertReapplied (allocation);
		}

		SalesOrderPayment Reversed ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);
			var controller = Controller ();

			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<JsonResult> ());

			return allocation;
		}

		void AssertReapplied (SalesOrderPayment reversed)
		{
			using (new SessionScope ()) {
				var order = SalesOrder.Find (reversed.SalesOrder.Id);
				var allocation = order.Payments.Single ();

				seed.Track (allocation);

				Assert.That (allocation.Payment.Id, Is.EqualTo (reversed.Payment.Id));
				Assert.That (allocation.Amount, Is.EqualTo (100m));
				Assert.That (order.Balance, Is.EqualTo (0m));
			}
		}

		// 14 (reverse)
		[Test]
		public void ReverseAllocation_RefusesToUnpayAnOrderWithAnActiveInvoice ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);

			using (new SessionScope ()) {
				seed.Invoice (SalesOrder.Find (allocation.SalesOrder.Id).Details.Single (), PaymentTerms.Immediate);
			}

			var controller = Controller ();
			var result = Request (() => controller.ReverseAllocation (allocation.Id, Reason));

			Assert.That (Refused (controller, result), Is.True);
			Assert.That (((ContentResult) result).Content, Does.Contain ("TEST000001"));

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (allocation.Id), Is.Not.Null);
				Assert.That (SalesOrder.Find (allocation.SalesOrder.Id).IsPaid, Is.True);
			}

			Assert.That (Records (allocation.Payment.Id), Is.Empty);
		}

		// 14 (reverse)
		[Test]
		public void ReverseAllocation_UnpaysAPpdOrderOnlyOnceItsReceiptsAreCancelled ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);
			FiscalDocument receipt;

			using (new SessionScope ()) {
				var invoice = seed.Invoice (SalesOrder.Find (allocation.SalesOrder.Id).Details.Single (), PaymentTerms.NetD);
				receipt = seed.PaymentReceipt (invoice);
			}

			var controller = Controller ();
			Assert.That (Refused (controller, Request (() => controller.ReverseAllocation (allocation.Id, Reason))), Is.True);

			using (new SessionScope ()) {
				var item = FiscalDocument.Find (receipt.Id);
				item.IsCancelled = true;
				item.UpdateAndFlush ();
			}

			controller = Controller ();
			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<JsonResult> ());
			Assert.That ((string) Records (allocation.Payment.Id).Single () ["Invoiced"],
				     Is.EqualTo (allocation.SalesOrder.Id + ": TEST000001"));
		}

		// 18
		[Test]
		public void ReverseAllocation_RefusesAnEmptyReason ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);
			var controller = Controller ();

			Assert.That (Refused (controller, Request (() => controller.ReverseAllocation (allocation.Id, " "))), Is.True);

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (allocation.Id), Is.Not.Null);
			}
		}

		// 18
		[Test]
		public void ReverseAllocation_TwiceAtOnceSucceedsOnce ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 150m);
			var results = new bool [2];
			var errors = new Exception [2];
			var start = new Barrier (2);

			var threads = Enumerable.Range (0, 2).Select (i => new Thread (() => {
				try {
					var controller = Controller ();
					start.SignalAndWait ();
					results [i] = Request (() => controller.ReverseAllocation (allocation.Id, Reason)) is JsonResult;
				} catch (Exception e) {
					errors [i] = e;
				}
			})).ToList ();

			threads.ForEach (x => x.Start ());
			threads.ForEach (x => Assert.That (x.Join (TimeSpan.FromSeconds (60)), Is.True));

			Assert.That (errors, Is.All.Null);
			Assert.That (results.Count (x => x), Is.EqualTo (1));
			Assert.That (Records (allocation.Payment.Id).Count, Is.EqualTo (1));

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.Find (allocation.Payment.Id).Amount, Is.EqualTo (100m));
			}
		}

		ActionResult Edit (int payment, decimal amount, PaymentMethod method = PaymentMethod.Cash,
				   int? customer = null, string reason = Reason)
		{
			var controller = Controller ();

			return Request (() => controller.EditPayment (new CustomerPayment {
				Id = payment,
				Amount = amount,
				Method = method,
				Reference = Seed.Marker
			}, customer, reason));
		}

		static bool Edited (ActionResult result)
		{
			return result is PartialViewResult && ((PartialViewResult) result).ViewName == "_RefreshPayment";
		}

		static SalesOrderPayment Reload (SalesOrderPayment allocation)
		{
			using (new SessionScope ()) {
				var item = SalesOrderPayment.Find (allocation.Id);
				NHibernateUtil.Initialize (item.Payment);
				NHibernateUtil.Initialize (item.Payment.Allocations);
				NHibernateUtil.Initialize (item.SalesOrder);
				return item;
			}
		}

		// 5
		[Test]
		public void EditPayment_LowerAmountUnpaysTheOrder ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);

			Assert.That (Edited (Edit (allocation.Payment.Id, 80m)), Is.True);

			var item = Reload (allocation);
			Assert.That (item.Amount, Is.EqualTo (80m));
			Assert.That (item.Change, Is.EqualTo (0m));
			Assert.That (item.Payment.Amount, Is.EqualTo (80m));
			Assert.That (item.SalesOrder.IsPaid, Is.False);
			Assert.That (item.SalesOrder.BalanceZeroedTime, Is.Null);

			var record = Records (allocation.Payment.Id).Single ();
			Assert.That ((string) record ["Operation"], Is.EqualTo ("Edit"));
			Assert.That ((decimal) record ["AmountBefore"], Is.EqualTo (100m));
			Assert.That ((decimal) record ["AmountAfter"], Is.EqualTo (80m));
			Assert.That ((decimal) record ["NetBefore"], Is.EqualTo (100m));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo (80m));
			Assert.That ((string) record ["Unpaid"], Is.EqualTo (allocation.SalesOrder.Id.ToString ()));
			Assert.That (record ["ReferenceBefore"], Is.Null);
		}

		// 6
		[Test]
		public void EditPayment_LowerTenderedCashOnlyShrinksTheChange ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 150m);

			Assert.That (Edited (Edit (allocation.Payment.Id, 120m)), Is.True);

			var item = Reload (allocation);
			Assert.That (item.Amount, Is.EqualTo (100m));
			Assert.That (item.Change, Is.EqualTo (20m));
			Assert.That (item.SalesOrder.IsPaid, Is.True);

			var record = Records (allocation.Payment.Id).Single ();
			Assert.That ((decimal) record ["NetBefore"], Is.EqualTo (100m));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo (100m));
			Assert.That (record ["Unpaid"], Is.Null);
		}

		SalesOrderPayment Underpaid ()
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				return seed.Allocation (order, seed.Payment (PaymentType.Immediate, 60m), 60m);
			}
		}

		// 7
		[Test]
		public void EditPayment_RaiseInCashPaysTheOrderAndGivesChange ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Underpaid ();

			Assert.That (Edited (Edit (allocation.Payment.Id, 130m)), Is.True);

			var item = Reload (allocation);
			Assert.That (item.Amount, Is.EqualTo (100m));
			Assert.That (item.Change, Is.EqualTo (30m));
			Assert.That (item.SalesOrder.IsPaid, Is.True);
			Assert.That (item.SalesOrder.BalanceZeroedTime, Is.Not.Null);
			Assert.That (item.Payment.Balance, Is.EqualTo (0m));
		}

		// 7
		[Test]
		public void EditPayment_RaiseByCardPaysTheOrderAndKeepsTheRestAsCredit ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Underpaid ();

			Assert.That (Edited (Edit (allocation.Payment.Id, 130m, PaymentMethod.CreditCard)), Is.True);

			var item = Reload (allocation);
			Assert.That (item.Amount, Is.EqualTo (100m));
			Assert.That (item.Change, Is.EqualTo (0m));
			Assert.That (item.SalesOrder.IsPaid, Is.True);
			Assert.That (item.Payment.Balance, Is.EqualTo (30m));
		}

		// 8
		[Test]
		public void EditPayment_ChangesTheMethod ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);

			Assert.That (Edited (Edit (allocation.Payment.Id, 100m, PaymentMethod.CreditCard)), Is.True);

			Assert.That (Reload (allocation).Payment.Method, Is.EqualTo (PaymentMethod.CreditCard));

			var record = Records (allocation.Payment.Id).Single ();
			Assert.That ((int) record ["MethodBefore"], Is.EqualTo ((int) PaymentMethod.Cash));
			Assert.That ((int) record ["MethodAfter"], Is.EqualTo ((int) PaymentMethod.CreditCard));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo (100m));
			Assert.That (record ["AmountBefore"], Is.Null);
		}

		// 9
		[Test]
		public void EditPayment_ChangesTheCustomerOnlyWithoutAllocations ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);
			int other, unallocated;

			using (new SessionScope ()) {
				other = Customer.Queryable.First (x => x.Id != seed.Customer.Id).Id;
				unallocated = seed.Payment (PaymentType.Immediate, 50m).Id;
			}

			Assert.That (Edited (Edit (allocation.Payment.Id, 100m, customer: other)), Is.False);
			Assert.That (Reload (allocation).Payment.Customer.Id, Is.EqualTo (seed.Customer.Id));

			Assert.That (Edited (Edit (unallocated, 50m, customer: other)), Is.True);

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.Find (unallocated).Customer.Id, Is.EqualTo (other));
			}

			var record = Records (unallocated).Single ();
			Assert.That ((int) record ["CustomerBefore"], Is.EqualTo (seed.Customer.Id));
			Assert.That ((int) record ["CustomerAfter"], Is.EqualTo (other));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo ((decimal) record ["NetBefore"]));
		}

		[Test]
		public void EditPayment_KeepsSeveralAllocationsCovered ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			int payment;

			using (new SessionScope ()) {
				var item = seed.Payment (PaymentType.Immediate, 150m);
				var a = seed.Order (paid: true);
				var b = seed.Order (paid: true);
				seed.Detail (a, 100m);
				seed.Detail (b, 50m);
				seed.Allocation (a, item, 100m);
				seed.Allocation (b, item, 50m);
				payment = item.Id;
			}

			Assert.That (Edited (Edit (payment, 140m)), Is.False);
			Assert.That (Edited (Edit (payment, 200m)), Is.True);

			using (new SessionScope ()) {
				var item = CustomerPayment.Find (payment);
				Assert.That (item.Amount, Is.EqualTo (200m));
				Assert.That (item.Balance, Is.EqualTo (50m));
			}
		}

		// 11 (edit)
		[Test]
		public void EditPayment_RefusesARefund ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			int payment;

			using (new SessionScope ()) {
				payment = seed.Payment (PaymentType.CreditNote, 100m).Id;
			}

			Assert.That (Edited (Edit (payment, 50m)), Is.False);

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.Find (payment).Amount, Is.EqualTo (100m));
			}
		}

		// 14 (edit)
		[Test]
		public void EditPayment_RecordsTheInvoiceOfAnOrderThatStaysPaid ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);

			using (new SessionScope ()) {
				seed.Invoice (SalesOrder.Find (allocation.SalesOrder.Id).Details.Single (), PaymentTerms.Immediate);
			}

			Assert.That (Edited (Edit (allocation.Payment.Id, 80m)), Is.False, "un-pays an invoiced order");
			Assert.That (Edited (Edit (allocation.Payment.Id, 100m, PaymentMethod.CreditCard)), Is.True);
			Assert.That ((string) Records (allocation.Payment.Id).Single () ["Invoiced"],
				     Is.EqualTo (allocation.SalesOrder.Id + ": TEST000001"));
		}

		// 18 (edit)
		[Test]
		public void EditPayment_RefusesAnEmptyReason ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			var allocation = Paid (100m, 100m);

			Assert.That (Edited (Edit (allocation.Payment.Id, 80m, reason: "")), Is.False);
			Assert.That (Reload (allocation).Payment.Amount, Is.EqualTo (100m));
		}

		// 10
		[Test]
		public void DeletePayment_RemovesItWithItsAllocationsAndRecordsIt ()
		{
			LogIn (AccessRight.Read | AccessRight.Update | AccessRight.Delete);
			SalesOrderPayment first, second;
			CustomerPayment payment;

			using (new SessionScope ()) {
				payment = seed.Payment (PaymentType.Immediate, 200m);
				var a = seed.Order (paid: true);
				var b = seed.Order (paid: true);
				seed.Detail (a, 100m);
				seed.Detail (b, 50m);
				first = seed.Allocation (a, payment, 100m, change: 50m);
				second = seed.Allocation (b, payment, 50m);
			}

			var controller = Controller ();
			Assert.That (Request (() => controller.DeletePayment (payment.Id, Reason)), Is.InstanceOf<JsonResult> ());

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.TryFind (payment.Id), Is.Null);
				Assert.That (SalesOrderPayment.TryFind (first.Id), Is.Null);
				Assert.That (SalesOrderPayment.TryFind (second.Id), Is.Null);
				Assert.That (SalesOrder.Find (first.SalesOrder.Id).IsPaid, Is.False);
				Assert.That (SalesOrder.Find (second.SalesOrder.Id).IsPaid, Is.False);
			}

			var record = Records (payment.Id).Single ();
			Assert.That ((string) record ["Operation"], Is.EqualTo ("Delete"));
			Assert.That ((decimal) record ["NetBefore"], Is.EqualTo (150m));
			Assert.That ((decimal) record ["NetAfter"], Is.EqualTo (0m));
			Assert.That ((decimal) record ["Amount"], Is.EqualTo (200m));
			Assert.That ((int) record ["Customer"], Is.EqualTo (seed.Customer.Id));
			Assert.That ((string) record ["Reference"], Is.EqualTo (Seed.Marker));
			Assert.That (record ["Date"], Is.Not.Null);
			Assert.That ((string) record ["Unpaid"], Is.EqualTo (first.SalesOrder.Id + ", " + second.SalesOrder.Id));
		}

		// 11
		[Test]
		public void DeletePayment_RefusesARefundWhoseAllocationCanBeReversed ()
		{
			LogIn (AccessRight.Read | AccessRight.Update | AccessRight.Delete);
			SalesOrderPayment allocation;

			using (new SessionScope ()) {
				var order = seed.Order (paid: true);
				seed.Detail (order, 100m);
				allocation = seed.Allocation (order, seed.Payment (PaymentType.CreditNote, 100m), 100m);
			}

			var controller = Controller ();
			Assert.That (Refused (controller, Request (() => controller.DeletePayment (allocation.Payment.Id, Reason))), Is.True);

			controller = Controller ();
			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<JsonResult> ());
			Assert.That ((bool) Records (allocation.Payment.Id).Single () ["CreditNote"], Is.True);
		}

		// 14 (delete)
		[Test]
		public void DeletePayment_RefusesToUnpayAnOrderWithAnActiveInvoice ()
		{
			LogIn (AccessRight.Read | AccessRight.Update | AccessRight.Delete);
			var allocation = Paid (100m, 100m);

			using (new SessionScope ()) {
				seed.Invoice (SalesOrder.Find (allocation.SalesOrder.Id).Details.Single (), PaymentTerms.Immediate);
			}

			var controller = Controller ();
			Assert.That (Refused (controller, Request (() => controller.DeletePayment (allocation.Payment.Id, Reason))), Is.True);

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.TryFind (allocation.Payment.Id), Is.Not.Null);
			}
		}

		[Test]
		public void DeletePayment_RefusesAPaymentOnDelivery ()
		{
			LogIn (AccessRight.Read | AccessRight.Update | AccessRight.Delete);
			int payment;

			using (new SessionScope ()) {
				payment = seed.Payment (PaymentType.Immediate, 100m).Id;
			}

			Sql ("INSERT INTO payment_on_delivery (customer_payment) VALUES (:id)", payment);

			try {
				var controller = Controller ();
				Assert.That (Refused (controller, Request (() => controller.DeletePayment (payment, Reason))), Is.True);
			} finally {
				Sql ("DELETE FROM payment_on_delivery WHERE customer_payment = :id", payment);
			}
		}

		[Test]
		public void DeletePayment_NeedsTheDeleteRight ()
		{
			LogIn (AccessRight.Read | AccessRight.Update);
			int payment;

			using (new SessionScope ()) {
				payment = seed.Payment (PaymentType.Immediate, 100m).Id;
			}

			var controller = Controller ();
			Assert.That (Request (() => controller.DeletePayment (payment, Reason)), Is.InstanceOf<RedirectToRouteResult> ());

			using (new SessionScope ()) {
				Assert.That (CustomerPayment.TryFind (payment), Is.Not.Null);
			}
		}

		static void Sql (string sql, int id)
		{
			using (new SessionScope ()) {
				var holder = ActiveRecordMediator.GetSessionFactoryHolder ();
				var session = holder.CreateSession (typeof (CustomerPayment));

				try {
					session.CreateSQLQuery (sql).SetInt32 ("id", id).ExecuteUpdate ();
				} finally {
					holder.ReleaseSession (session);
				}
			}
		}

		[Test]
		public void ReverseAllocation_NeedsTheUpdateRight ()
		{
			LogIn (AccessRight.Read);
			var allocation = Paid (100m, 100m);
			var controller = Controller ();

			Assert.That (Request (() => controller.ReverseAllocation (allocation.Id, Reason)), Is.InstanceOf<RedirectToRouteResult> ());

			using (new SessionScope ()) {
				Assert.That (SalesOrderPayment.TryFind (allocation.Id), Is.Not.Null);
			}
		}
	}
}
