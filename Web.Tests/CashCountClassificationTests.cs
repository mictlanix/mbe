//
// CashCountClassificationTests.cs
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
	// mictlanix/mbe#60: the cash count classified payments by type alone, and the
	// database holds four conventions that don't fit that:
	//
	//   type 0 (N/A), positive   2012-2025  sales     -- matched no filter
	//   type 0 (N/A), negative   2012-2017  refunds   -- matched no filter
	//   CreditNote, negative     2025-04/07 refunds   -- subtracting them added cash
	//   CreditNote, positive     2025-07 on refunds
	//
	// A payment is a refund if it is a CreditNote or negative, reported as its
	// absolute amount; any other positive payment is a sale. mbe-api's cash cut
	// (mictlanix/mbe-api#230) uses the same rule.
	[TestFixture]
	public class CashCountClassificationTests {
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

		void WithReport (Action<CashCountReport> assert)
		{
			using (new SessionScope ()) {
				var result = (ViewResult) controller.CloseSessionConfirmed (session.Id);
				assert ((CashCountReport) result.Model);
			}
		}

		static decimal Received (CashCountReport report, PaymentMethod method = PaymentMethod.Cash)
		{
			return report.PaymentsReceivedByMethod.Where (x => x.Method == method).Sum (x => x.Amount);
		}

		static decimal Refunded (CashCountReport report, PaymentMethod method = PaymentMethod.Cash)
		{
			return report.RefundsByMethod.Where (x => x.Method == method).Sum (x => x.Amount);
		}

		// (a) Sessions before 2024 recorded every sale as type 0 and printed zero sales.
		[Test]
		public void TypeZeroPayment_IsASale ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.NA, 500m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.NA, 200m, PaymentMethod.CreditCard, session);
			}

			WithReport (report => {
				Assert.That (Received (report), Is.EqualTo (500m));
				Assert.That (Received (report, PaymentMethod.CreditCard), Is.EqualTo (200m));
				Assert.That (report.CashInDrawer - report.StartingCash, Is.EqualTo (500m));
			});
		}

		[Test]
		public void NegativeTypeZeroPayment_IsARefund ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.NA, 500m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.NA, -80m, PaymentMethod.Cash, session);
			}

			WithReport (report => {
				Assert.That (Received (report), Is.EqualTo (500m));
				Assert.That (Refunded (report), Is.EqualTo (80m));
				Assert.That (report.CashInDrawer - report.StartingCash, Is.EqualTo (420m));
			});
		}

		// (b) A cash payout recorded as a negative credit note was added back to the
		// drawer: subtracting the negative sum overstated it by twice the payout.
		[Test]
		public void NegativeCreditNoteInCash_ComesOutOfTheDrawer ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.Immediate, 1000m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.CreditNote, -370m, PaymentMethod.Cash, session);
			}

			WithReport (report => {
				Assert.That (Refunded (report), Is.EqualTo (370m), "reported as the amount paid out");
				Assert.That (report.RefundsInCash, Is.EqualTo (370m));
				Assert.That (report.CashInDrawer - report.StartingCash, Is.EqualTo (630m));
			});
		}

		[Test]
		public void PositiveCreditNoteInCash_ComesOutOfTheDrawer ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.Immediate, 1000m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.CreditNote, 370m, PaymentMethod.Cash, session);
			}

			WithReport (report => {
				Assert.That (Received (report), Is.EqualTo (1000m));
				Assert.That (Refunded (report), Is.EqualTo (370m));
				Assert.That (report.CashInDrawer - report.StartingCash, Is.EqualTo (630m));
			});
		}

		// (c) The cash-sales row counted credit notes; the drawer total did not.
		[Test]
		public void CashSales_AgreesWithTheDrawerTotal ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.Immediate, 1000m, PaymentMethod.Cash, session);
				seed.Payment (PaymentType.CreditNote, 370m, PaymentMethod.Cash, session);
			}

			WithReport (report => {
				Assert.That (report.CashSales, Is.EqualTo (1000m));
				Assert.That (report.CashSales, Is.EqualTo (report.PaymentsReceivedInCash));
			});
		}

		[Test]
		public void ZeroAmountPayment_IsNeither ()
		{
			using (new SessionScope ()) {
				seed.Payment (PaymentType.Immediate, 0m, PaymentMethod.Cash, session);
			}

			WithReport (report => {
				Assert.That (report.Payments, Is.Empty);
				Assert.That (report.Refunds, Is.Empty);
			});
		}
	}
}
