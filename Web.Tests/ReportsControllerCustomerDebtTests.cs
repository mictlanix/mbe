//
// ReportsControllerCustomerDebtTests.cs
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
	// The debt summary lists every customer with orders, including soft-deleted
	// ones, and links each to its debt report. Looking the customer up through
	// MBEQueryable.IQCustomers hid deleted customers and threw "Sequence contains
	// no elements", so the report must find a customer by id whether deleted or not.
	[TestFixture]
	public class ReportsControllerCustomerDebtTests {
		const string Marker = "MBE-TEST";

		ReportsController controller;
		int customer;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				var price_list = PriceList.Queryable.First ();
				var item = new Customer {
					Code = Marker + "-D",
					Name = "Deleted Customer " + Marker,
					CreditLimit = 0m,
					CreditDays = 0,
					Comment = Marker,
					PriceList = price_list,
					PriceListId = price_list.Id,
					IsDeleted = true
				};

				item.CreateAndFlush ();
				customer = item.Id;
			}

			controller = new ReportsController ();
			FakeHttpContext.Attach (controller, null);
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				var item = Customer.TryFind (customer);

				if (item != null) {
					item.DeleteAndFlush ();
				}
			}

			FakeHttpContext.Detach ();
		}

		[Test]
		public void CustomerDebtReport_ForADeletedCustomer_ShowsTheReport ()
		{
			using (new SessionScope ()) {
				AssertShowsTheCustomer (controller.CustomerDebtReport (customer));
			}
		}

		[Test]
		public void PrintCustomerDebtReport_ForADeletedCustomer_ShowsTheReport ()
		{
			using (new SessionScope ()) {
				AssertShowsTheCustomer (controller.PrintCustomerDebtReport (customer));
			}
		}

		[Test]
		public void CustomerDebtReport_ForAMissingCustomer_IsNotFound ()
		{
			using (new SessionScope ()) {
				Assert.That (controller.CustomerDebtReport (int.MaxValue), Is.InstanceOf<HttpNotFoundResult> ());
				Assert.That (controller.PrintCustomerDebtReport (int.MaxValue), Is.InstanceOf<HttpNotFoundResult> ());
			}
		}

		void AssertShowsTheCustomer (ActionResult result)
		{
			Assert.That (result, Is.InstanceOf<ViewResult> ());

			var model = (CustomerDebtViewModel) ((ViewResult) result).Model;

			Assert.That (model.Customer.Id, Is.EqualTo (customer));
		}
	}
}
