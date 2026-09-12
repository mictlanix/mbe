//
// CloseSessionBreakdownTests.cs
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
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// The per-method money breakdown on the close-session screen (mictlanix/mbe#44).
	//
	// It used to be built by a projection that never assigned MoneyCount.Type and then
	// filtered and labelled by that unassigned field, which produced three faults at once:
	// every row rendered as "N/A", the credit-note exclusion was a no-op because Type was
	// always NA, and the amount came from the allocation sum rather than the payment, so a
	// payment taken during the session but not yet applied to an order showed as zero.
	//
	// The printed ticket had already moved to CashCountReport.PaymentsReceivedByMethod;
	// the screen now reads the same property, so the two agree by construction.
	[TestFixture]
	public class CloseSessionBreakdownTests {
		Seed seed;
		PaymentsController controller;
		CashSession session;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
				session = seed.OpenCashSession ();
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

		// PaymentsReceivedByMethod walks each payment's Allocations lazily, so the report
		// has to be read while a session is still open -- which is what the view does, the
		// web application running ActiveRecord with isWeb="true".
		void WithReport (Action<CashCountReport> assert)
		{
			using (new SessionScope ()) {
				var result = controller.CloseSessionConfirmed (session.Id) as ViewResult;

				Assert.That (result, Is.Not.Null);

				assert ((CashCountReport) result.Model);
			}
		}

		static decimal AmountFor (CashCountReport report, PaymentMethod method)
		{
			return report.PaymentsReceivedByMethod
				     .Where (x => x.Method == method)
				     .Sum (x => x.Amount);
		}

		// (a) Rows are grouped and labelled by payment method, so cash and card are
		// distinguishable. The old projection left Type unset and the view printed it.
		[Test]
		public void Breakdown_LabelsEachRowByItsPaymentMethod ()
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 300m);

				var cash = seed.Payment (PaymentType.Immediate, 100m, PaymentMethod.Cash, session);
				seed.Allocation (order, cash, 100m);

				var card = seed.Payment (PaymentType.Immediate, 200m, PaymentMethod.CreditCard, session);
				seed.Allocation (order, card, 200m);
			}

			WithReport (report => {
				var methods = report.PaymentsReceivedByMethod.Select (x => x.Method).ToList ();

				Assert.That (methods, Has.Member (PaymentMethod.Cash));
				Assert.That (methods, Has.Member (PaymentMethod.CreditCard));
				Assert.That (methods, Is.Unique, "one row per method");
				Assert.That (AmountFor (report, PaymentMethod.Cash), Is.EqualTo (100m));
				Assert.That (AmountFor (report, PaymentMethod.CreditCard), Is.EqualTo (200m));
			});
		}

		// (b) Credit notes are not money taken at the till and must stay out of the
		// breakdown. The old filter compared an always-NA Type and excluded nothing.
		[Test]
		public void Breakdown_LeavesCreditNotesOut ()
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);

				var cash = seed.Payment (PaymentType.Immediate, 100m, PaymentMethod.Cash, session);
				seed.Allocation (order, cash, 100m);

				seed.Payment (PaymentType.CreditNote, 30m, PaymentMethod.Cash, session);
			}

			WithReport (report => {

				Assert.That (AmountFor (report, PaymentMethod.Cash), Is.EqualTo (100m),
					     "the credit note must not be counted as cash received");
				Assert.That (report.PaymentsReceivedByMethod.Sum (x => x.Amount), Is.EqualTo (100m));
				Assert.That (report.Refunds.Count, Is.EqualTo (1), "it belongs under refunds instead");
			});
		}

		// (c) Money taken during the session but not yet applied to an order is still in
		// the drawer. The old projection summed allocations, so it reported zero.
		[Test]
		public void Breakdown_CountsPaymentsThatAreNotYetApplied ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.PaymentInAdvance, 50m, PaymentMethod.Cash, session);
			}

			WithReport (report => {

				Assert.That (AmountFor (report, PaymentMethod.Cash), Is.EqualTo (50m),
					     "an unapplied payment is still money received");
			});
		}

		// Change handed back is not money kept, so it comes off the amount received.
		[Test]
		public void Breakdown_SubtractsChangeGivenBack ()
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);

				var cash = seed.Payment (PaymentType.Immediate, 120m, PaymentMethod.Cash, session);
				seed.Allocation (order, cash, 100m, change: 20m);
			}

			WithReport (report => {

				Assert.That (AmountFor (report, PaymentMethod.Cash), Is.EqualTo (100m),
					     "120 taken less 20 returned");
			});
		}

		// All three faults together, which is how a real session looks.
		[Test]
		public void Breakdown_TotalsTheSessionCorrectly ()
		{
			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 300m);

				var cash = seed.Payment (PaymentType.Immediate, 120m, PaymentMethod.Cash, session);
				seed.Allocation (order, cash, 100m, change: 20m);

				seed.Payment (PaymentType.PaymentInAdvance, 50m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.CreditNote, 30m, PaymentMethod.Cash, session);

				var card = seed.Payment (PaymentType.Immediate, 200m, PaymentMethod.CreditCard, session);
				seed.Allocation (order, card, 200m);
			}

			WithReport (report => {

				Assert.That (AmountFor (report, PaymentMethod.Cash), Is.EqualTo (150m));
				Assert.That (AmountFor (report, PaymentMethod.CreditCard), Is.EqualTo (200m));
				Assert.That (report.PaymentsReceivedByMethod.Sum (x => x.Amount), Is.EqualTo (350m));
			});
		}
	}
}
