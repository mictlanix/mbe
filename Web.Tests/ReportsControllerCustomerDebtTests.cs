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
using System.Collections.Generic;
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
	// Both reports also flag a deleted customer so it isn't mistaken for an active one.
	[TestFixture]
	public class ReportsControllerCustomerDebtTests {
		const string Marker = "MBE-TEST";

		ReportsController controller;
		Seed seed;
		int customer;
		string name;

		[SetUp]
		public void SetUp ()
		{
			// Unique per run, so the summary's name filter matches only this customer.
			name = "Deleted Customer " + Marker + " Zz" + DateTime.Now.Ticks.ToString ("x");

			using (new SessionScope ()) {
				var price_list = PriceList.Queryable.First ();
				var item = new Customer {
					Code = Marker + "-D",
					Name = name,
					CreditLimit = 0m,
					CreditDays = 0,
					Comment = Marker,
					PriceList = price_list,
					PriceListId = price_list.Id,
					IsDeleted = true
				};

				item.CreateAndFlush ();
				customer = item.Id;

				// The summary only lists customers with orders.
				seed = new Seed ();
				var order = seed.Order ();
				order.Customer = item;
				order.CustomerName = item.Name;
				order.UpdateAndFlush ();
				seed.Detail (order, 100m);
			}

			controller = new ReportsController ();
			FakeHttpContext.Attach (controller, null);
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				seed.Dispose ();

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
		public void CustomersDebtSummary_ForADeletedCustomer_FlagsIt ()
		{
			using (new SessionScope ()) {
				var result = controller.CustomersDebtSummary (new CustomersStatusFilter { CustomerName = name });

				Assert.That (result, Is.InstanceOf<PartialViewResult> ());

				// Rows are ExpandoObjects; read them as dictionaries to avoid dynamic here.
				var rows = ((IEnumerable<object>) ((PartialViewResult) result).Model)
					.Cast<IDictionary<string, object>> ();
				var row = rows.Single (x => (int) x ["CustomerId"] == customer);

				Assert.That (row ["CustomerDeleted"], Is.True);
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
			Assert.That (model.Customer.IsDeleted, Is.True);
		}
	}
}
