//
// SalesOrdersControllerCancelTests.cs
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
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#78: cancelling from Sales Orders only flags the order, so any
	// payment still applied stayed on a cancelled order -- paying nothing, and not
	// available to apply elsewhere. It now refuses until they are reversed.
	[TestFixture]
	public class SalesOrdersControllerCancelTests {
		Seed seed;
		SalesOrdersController controller;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}

			controller = new SalesOrdersController ();

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

		// A credit order with one installment applied: unpaid, never corrected.
		[Test]
		public void Cancel_RefusesAnOrderWithPaymentsApplied ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				seed.Allocation (order, seed.Payment (PaymentType.CreditPayment, 40m), 40m);
				id = order.Id;
			}

			using (new SessionScope (FlushAction.Never)) {
				var result = (RedirectToRouteResult) controller.Cancel (id);

				Assert.That (result.RouteValues ["action"], Is.EqualTo ("View"));
				Assert.That (controller.TempData ["Error"], Is.EqualTo (Resources.SalesOrderCancelHasPayments));
			}

			using (new SessionScope ()) {
				var order = SalesOrder.Find (id);

				Assert.That (order.IsCancelled, Is.False);
				Assert.That (order.Payments, Has.Count.EqualTo (1));
			}
		}

		[Test]
		public void Cancel_CancelsAnOrderWithoutPayments ()
		{
			int id;

			using (new SessionScope ()) {
				var order = seed.Order ();
				seed.Detail (order, 100m);
				id = order.Id;
			}

			using (new SessionScope (FlushAction.Never)) {
				var result = (RedirectToRouteResult) controller.Cancel (id);
				Assert.That (result.RouteValues ["action"], Is.EqualTo ("Index"));
			}

			using (new SessionScope ()) {
				Assert.That (SalesOrder.Find (id).IsCancelled, Is.True);
			}
		}
	}
}
